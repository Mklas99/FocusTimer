# Design

## Context

See `proposal.md` for motivation. Today, `SessionTracker` creates mutable `TimeEntry` objects with local `DateTime` values, `ISessionRepository` appends them to positional CSV rows and reads one date, and `TodayStatsService` aggregates every entry by its start date. The existing `Session` model is not used by persistence or tracking. Reads may silently skip malformed rows or return an empty collection after I/O failures, and writes have no durable identity with which to distinguish a retry from a duplicate.

The worklog remains local, single-user, and partitioned into daily files under the configured directory. This is development data: compatibility and migration of the existing row format are deliberately deferred to OI-20.

## Goals / Non-Goals

**Goals:**

- Establish one precise domain record and set of invariants for future reporting, editing, export, attribution, and alternative storage backends.
- Preserve the simplicity and inspectability of daily CSV files while making reads, retries, and same-day mutations trustworthy.
- Make time, identity, error, and concurrency semantics explicit and testable.
- Keep storage behavior behind a Core contract so future consumers do not parse CSV directly.

**Non-Goals:**

- Reading, converting, backing up, or restoring worklogs written by the previous development schema.
- New reporting, editing, export, project-rule, or migration user interfaces.
- Cross-day entry moves, bulk mutation, cross-device synchronization, or SQLite implementation.
- Recording idle time as work; the foundation records that active work ended because of idle detection.

## Decisions

### 1. Use an entry-centric domain model

`TimeEntry` remains the persisted unit. It becomes an immutable or init-only record with validation at construction/persistence boundaries. The unused `Session` class is not promoted to a persistence aggregate; session relationships are represented by `SessionId` and the class can be removed if the implementation confirms no remaining references.

Alternative considered: persist a separate session object containing child entries. Rejected because it complicates the daily file partition, updates, and future SQL mapping without a current session-level data requirement.

### 2. Create identity at capture time

`EntryId` is a random GUID created when a segment starts, so retries carry the same identity. `SessionId` is a random GUID created whenever tracking transitions from a non-running state into a new uninterrupted Running interval. Application/window switches create new entry IDs while retaining the session ID; pause/resume creates a new session ID.

Alternative considered: derive identity from timestamps and row content. Rejected because edits, duplicate-looking entries, clock changes, and copied files make derived identity unstable or ambiguous.

### 3. Store offset-aware timestamps and split at local midnight

Start and end values use `DateTimeOffset`; duration is derived as `EndedAt - StartedAt` and is never independently editable. `TimeProvider` supplies current time so session and boundary behavior are deterministic in tests. On a tick that crosses local midnight, tracking closes the current entry at the exact boundary with `DayBoundary`, then opens an otherwise identical entry with a new ID and the same session ID.

Alternative considered: allow entries to cross midnight and clip them in every query. Rejected because it complicates daily-file lookup, retention, editing, and every downstream aggregation.

### 4. Store factual provenance and controlled vocabularies

The current schema stores the fields listed in the worklog-storage spec. String-backed controlled values are used at the CSV boundary so enum reordering cannot reinterpret history. Initial capture uses `Active`, `ActiveWindow`, the detected platform, and a stable installation/device GUID kept in Settings. `EndReason` distinguishes application change, manual pause, idle pause, day boundary, application exit, and unknown. Project assignment records whether the tag is unassigned, entered for the running session, assigned by a future rule, changed by a future editor, or imported.

`ProjectRuleId` is nullable until OI-08 exists. No project entity ID, billing field, client field, or arbitrary extension map is added; format evolution, not reserved columns, is the extensibility mechanism.

Alternative considered: only add `IdleFlag`, `SessionId`, and `SourcePlatform`. Rejected because a Boolean cannot distinguish an idle interval from active work that ended due to idle detection, and it leaves retry, edit, and attribution provenance undefined.

### 5. Replace `ISessionRepository` with an explicit worklog store

Core defines `IWorklogStore` around operations equivalent to:

- append a closed-entry batch idempotently;
- get an entry by ID;
- query an explicit half-open interval with optional filters and ordering;
- patch a same-day entry with an expected revision;
- delete an entry with an expected revision.

Methods accept cancellation and return typed results that distinguish success, validation error, not found, conflict, unsupported schema, malformed data, and I/O/file-in-use failure. Query results contain valid entries plus structured warnings. They do not expose `IQueryable`, file paths as domain keys, or backend-specific exceptions.

Alternative considered: extend `ISessionRepository` with more date methods. Rejected because its name and return types already disagree and would keep reporting coupled to storage partitions rather than user queries.

### 6. Keep one current CSV schema with a header-driven codec

`FocusTimer.Persistence` owns a current-schema codec. CSV files keep the existing `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv` layout. Column lookup is by header name, but the writer emits a stable documented order. A maintained RFC 4180-capable streaming CSV library is preferred over expanding the current regular expression, especially because quoted fields may contain record-spanning line breaks.

Every new file is current schema. Before appending to an existing file, the store verifies its header/schema. A non-current file returns `UnsupportedSchema` and is never appended to or rewritten. This guard is not a legacy reader or migration mechanism.

Alternative considered: accept both old and new row widths in the normal codec. Rejected because that permanently mixes compatibility policy into functional persistence and can silently corrupt a file whose header no longer describes its rows.

### 7. Make appends idempotent by entry ID

The store maintains or constructs the set of IDs for each daily file before committing an append batch. Repeating the same ID with identical stored content succeeds without another row. Reusing an ID with different content is a conflict. Batch validation occurs before any row is written.

Alternative considered: continue blind append and deduplicate only when querying. Rejected because raw files and exports would remain incorrect and mutations would become ambiguous.

### 8. Use atomic per-day rewrites for update and delete

Mutations take a repository-level keyed lock for the daily file and obtain an exclusive file lease. The store reads the latest file, validates all records and the expected entry revision, applies exactly one mutation, writes a temporary file in the same directory, flushes it to disk, re-reads and validates it, then atomically replaces the original. Failure before activation leaves the original untouched. Temporary recovery artifacts are cleaned after a successful activation and by safe startup cleanup after interrupted attempts.

An update that changes the local partition date is rejected. Cross-day moves require a later two-file transaction or transaction journal and belong to OI-15 rather than this foundation.

Alternative considered: edit a matching CSV line in place. Rejected because variable-length quoting and interruption can corrupt the remainder of the file.

### 9. Allocate responsibility by layer

- Core owns the entry model, controlled value types, query/patch/result contracts, validation rules, `TimeProvider` usage, and session tracking.
- Persistence owns current CSV serialization, daily-file selection, locking, idempotency, query execution, atomic replacement, and retention.
- App adapts `TodayStatsService` and persistence event consumers to the query contract without adding new UI.
- Host updates DI registration only.
- No Platform.Windows change is required for source-platform detection; use runtime platform information behind a Core-friendly provider, with Linux-compatible behavior retained.

## Risks / Trade-offs

- **[Existing development worklogs block writes]** -> Return a clear unsupported-schema result, document manual move/removal, and never mix formats. OI-20 owns any future compatibility strategy.
- **[The schema anticipates features not yet visible]** -> Include only fields tied to known OI-08/OI-15/reporting needs and keep speculative billing/client/custom fields out.
- **[Per-file ID checks add append I/O]** -> Cache IDs for the active day behind file metadata and verify correctness with representative large-day benchmarks; retain correctness if the cache is invalidated.
- **[External programs can lock or modify CSV]** -> Use exclusive leases for mutation, read the latest content after acquiring the lock, and return typed file-in-use/conflict results rather than overwriting.
- **[Atomic replace behavior differs by platform/filesystem]** -> Keep the algorithm behind a file-operation abstraction and test Windows directly plus platform-neutral failure injection; retain a same-directory replace/fallback strategy that never exposes a partial target.
- **[Midnight and DST boundaries are error-prone]** -> Centralize boundary calculation using `TimeProvider` and `TimeZoneInfo`, and test normal, ambiguous, and invalid local times.
- **[A CSV dependency increases package surface]** -> Select a small maintained library, pin it centrally, and cover the codec with round-trip tests so it can be replaced without changing Core contracts.

## Development Rollout

This change provides no automated data migration or rollback. Before testing the new build, developers move or remove files with an unsupported header. The current store refuses to alter such files. Rolling code back may likewise require moving newly generated current-schema worklogs out of the configured directory. Research and design for release-grade backup, installer/portable coordination, idempotent migration, and rollback remains in OI-20.
