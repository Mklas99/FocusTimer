# Thoughts from dev
this is a collection of the devs thouhgs and might nod be accurat or not explored further dont treat the notes as definate future changes/plans.

## Current decision: defer migration strategy

The `establish-worklog-data-foundation` OpenSpec change targets development data and intentionally excludes legacy worklog migration, backup orchestration, installer integration, and rollback. Existing development worklogs may be moved or removed manually, but the new persistence implementation must detect an unsupported existing schema and refuse to append mixed-format rows. A general release-to-release migration strategy is tracked separately as `OI-20`; earlier migration exploration is not part of the current proposal.

## Open issues from previous version



| ID | Area | Gap | Origin | Notes |
|---|---|---|---|---|
| OI-01 | Global Hotkeys | editable hotkeys | | Registration/dispatch works; only the capture UI is missing |
| OI-02 | System Tray | Quick actions (e.g. switch project, etc.) |  |  |
| OI-03 | Widget UI | Window position/size is not persisted |  | No `WindowX/Y/Width/Height` in `Settings` |
| OI-04 | Settings / Reporting | breakdown view / user report |  |  only running total, not per-app/per-project sums or reports |
| OI-05 | Diagnostics | "Open logs folder" button in Settings |  |  |
| OI-06 | Cross-Platform | Linux gap close |  | App builds and runs on Linux probably; feature parity is the gap |
| OI-07 | Data Features | `TimeEntry` schema unchanged since Milestone 3 — no `IdleFlag`, `SessionId`, or `SourcePlatform` columns | | Blocks idle-segment tagging and cross-session/platform analysis later |
| OI-08 | Data Features | rules-based automatic project tagging (pattern → project mapping) | | currently tagging is manual |
| OI-09 | Data Features | improved storage option | | currently only CSV. Would help once per-app/per-project querying (OI-04) is needed |
| OI-10 | Focus Modes | No Pomodoro mode (work/break cycle automation) | | |
| OI-11 | Focus Modes | (enable/disable) sound cues on break/resume events | | |
| OI-12 | Reporting | data export + presets | | currently theme export only |
| OI-13 | Widget UI | function of minimise button "weird" |  | it collapses and moves to the bottom (often missed) -> change fnc to system tray click? |

## Changes
### Requirement: Per-Application Time Segmentation
Current: The system SHALL poll the foreground application/window once per second while the timer is Running, and SHALL start a new log segment whenever the active application/window changes.
Update: -> introduce "advanced" settings to make this configuratble


## New features

### Requirement: Work-Logging On/Off Switch

add delete loggs functinality
add edit log functionality


## improvements

improve installer size
improve performants



The ideal foundation is not merely “add four CSV columns.” It is a versioned worklog contract, a storage-neutral API, explicit schema isolation, and safe mutation behavior.

I recommend one focused OpenSpec change: `establish-worklog-data-foundation`. It should close `OI-07`, partially enable `OI-15`, and explicitly prepare—but not implement—`OI-04`, `OI-08`, and `OI-12`. `OI-09` should remain an investigation.

The resulting `establish-worklog-data-foundation` OpenSpec proposal is now captured and validated. Migration strategy remains a separate investigation under `OI-20`.

## Current weaknesses that matter

The current implementation has several hidden blockers:

- `TimeEntry` has no identity, provenance, timezone offset, or concurrency metadata: [TimeEntry.cs](C:/Users/mklas/Documents/_Development/Repos/FocusTimer/src/FocusTimer.Core/Models/TimeEntry.cs:6).
- `ISessionRepository` really stores entries, not sessions, and only supports append plus one-day reads: [ISessionRepository.cs](C:/Users/mklas/Documents/_Development/Repos/FocusTimer/src/FocusTimer.Core/Interfaces/ISessionRepository.cs:11).
- CSV parsing is positional and assumes exactly the current schema. Appending extended rows to an existing legacy file would leave an old header describing new-width rows: [CsvSessionRepository.cs](C:/Users/mklas/Documents/_Development/Repos/FocusTimer/src/FocusTimer.Persistence/CsvSessionRepository.cs:19).
- The regex parser cannot safely handle quoted fields containing line breaks, even though the writer allows them.
- Read failures and malformed rows can become an empty result, which a report could incorrectly present as “zero time.”
- Entries are assigned to a day solely by `StartTime`. An entry crossing midnight is counted entirely on the starting day: [TodayStatsService.cs](C:/Users/mklas/Documents/_Development/Repos/FocusTimer/src/FocusTimer.Core/Services/TodayStatsService.cs:35).
- `DateTime` plus separate time-only CSV fields loses timezone and daylight-saving context.
- `Session` exists but is unused; adding another session abstraction around it would create competing models.
- The architecture document claims thread-safe CSV writes and shows a filename that differs from the implemented/specification path. These should be corrected when the change is implemented: [ARCHITECTURE.md](C:/Users/mklas/Documents/_Development/Repos/FocusTimer/ARCHITECTURE.md:164), [activity-tracking spec](C:/Users/mklas/Documents/_Development/Repos/FocusTimer/openspec/specs/activity-tracking/spec.md:23).

## Recommended architecture

```text
+----------------+
| SessionTracker |
+-------+--------+
        |
        | creates stable, validated entries
        v
+-----------------------+
| Worklog normalization |
| - IDs                 |
| - session semantics   |
| - midnight splitting  |
| - provenance          |
+-----------+-----------+
            |
            v
+-----------------------+
| IWorklogStore         |
| append / query / get  |
| update / delete       |
+-----------+-----------+
            |
      +-----+------+
      |            |
      v            v
+-----------+  +-----------+
| CSV store |  | SQLite    |
| current   |  | later     |
+-----------+  +-----------+
      ^
      |
+-----+--------------------------------+
| Today / reports / edits / exports   |
| depend only on IWorklogStore        |
+--------------------------------------+
```

Rename or replace `ISessionRepository` with `IWorklogStore`. The existing interface name is misleading because it returns `TimeEntry` objects rather than `Session` objects.

Do not expose `IQueryable`. Use an explicit query object so CSV and a future SQLite backend can provide the same semantics.

## Current worklog schema

### Store now

| Field | Recommended type | Semantics |
|---|---|---|
| `SchemaVersion` | integer | Identifies the current file/row format independently of the application version. |
| `EntryId` | `Guid` | Globally unique, created when the segment begins, never changed. |
| `SessionId` | `Guid` | Groups all segments from one uninterrupted Running interval. |
| `StartedAt` | `DateTimeOffset` | Inclusive start timestamp with offset. |
| `EndedAt` | `DateTimeOffset` | Exclusive end timestamp with offset. |
| `AppName` | string | Captured process/application name. |
| `WindowTitle` | string | Captured title; sensitive data requiring privacy controls later. |
| `ProjectTag` | nullable string | Project label snapshot at the time of tracking. |
| `ProjectAssignmentSource` | string enum | `Unassigned`, `ManualSession`, `Rule`, `UserEdit`, or `Import`. |
| `ProjectRuleId` | nullable `Guid` | Identifies the automatic rule when OI-08 is implemented. |
| `ActivityKind` | string enum | `Active`, `Idle`, or `Unknown`; more extensible than `IdleFlag`. |
| `EndReason` | string enum | `WindowChanged`, `ManualPause`, `IdlePause`, `DayBoundary`, `AppExit`, `Unknown`. |
| `CaptureSource` | string enum | `ActiveWindow`, `Manual`, or `Import`. |
| `SourcePlatform` | string | `windows`, `linux`, `macos`, or `unknown`. |
| `SourceDeviceId` | nullable `Guid` | Stable local installation/device identity for future import or sync. |
| `Revision` | integer | Starts at 1 and increments on edit. |
| `LastModifiedAtUtc` | `DateTimeOffset` | Supports conflicts, auditing, and future synchronization. |
| `DurationSeconds` | derived CSV convenience | Must be calculated from timestamps, never treated as authoritative. |

Serialize enums as stable strings rather than numeric enum values.

### Do not add yet

Do not pre-create speculative columns for clients, billing rates, invoicing, arbitrary tags, calendar events, AI classifications, or custom fields. Future-proofing comes from explicit versioning and evolvable contracts, not from guessing every future feature.

Also avoid inventing `ProjectId` until FocusTimer has a real project catalog. `ProjectTag` remains the historical label snapshot.

## Important semantics

### Identity

- Generate `EntryId` when the segment starts, not when it is saved.
- Retries must reuse that ID.
- Appending the same ID twice must be idempotent or return a duplicate-ID error.
- An edit never changes `EntryId`, `SessionId`, or original capture provenance.

### Sessions

Define `SessionId` as one uninterrupted transition into `Running`.

- Window switches retain the same `SessionId`.
- Manual pause, idle pause, stop, or reset ends that session.
- Resuming creates a new `SessionId`.
- A future Pomodoro cycle can introduce a separate `FocusCycleId`; do not overload `SessionId`.

### Idle handling

`IdleFlag` alone is misleading. A segment recorded before auto-pause is not entirely idle.

Recommended behavior:

- End active work at the calculated idle-start timestamp.
- Set `EndReason = IdlePause`.
- Do not record idle time as work by default.
- Reserve `ActivityKind = Idle` for a future explicit idle timeline feature.
- Extend the idle event later to expose `IdleSince`, not only the detection timestamp.

### Day boundaries

Persisted entries should not cross a local calendar boundary.

At midnight, close the entry exactly at the boundary and start a new entry with:

- a new `EntryId`;
- the same `SessionId`;
- the same app, window, and project;
- `EndReason = DayBoundary` on the first part.

This keeps per-day files, retention, Today reports, and editing predictable.

## Store contract

A suitable storage-neutral contract is conceptually:

```csharp
AppendAsync(entries, cancellationToken)
GetByIdAsync(entryId, cancellationToken)
QueryAsync(query, cancellationToken)
UpdateAsync(entryId, expectedRevision, patch, cancellationToken)
DeleteAsync(entryId, expectedRevision, cancellationToken)
```

`WorklogQuery` should support:

- inclusive start and exclusive end;
- interval-overlap semantics;
- application;
- project tag;
- session ID;
- activity kind;
- capture source;
- ascending or descending time order.

A result should contain entries plus warnings. File corruption, unreadable files, and unsupported schema versions must not silently look like an empty worklog.

Updates should use a patch model so callers cannot accidentally replace identity or provenance fields.

## CSV safety and schema isolation

### Schema rules

- Parse by header name, not column position.
- Never mix current-schema and unsupported-schema rows beneath one header.
- Use a standards-compliant streaming CSV parser instead of the current line-based regex.
- The current reader may tolerate unknown columns only when the declared schema is supported.
- A writer must refuse to append to or rewrite an unsupported schema.

### Migration is deferred

This development-only change does not read, migrate, back up, restore, or roll back an older worklog format. Developers may move or remove unsupported files manually. The current store must identify an unsupported header before a write and leave the file byte-for-byte unchanged. Installer versus first-run ownership, portable upgrades, idempotent recovery, rollback, and retirement of compatibility code require separate research under `OI-20` before FocusTimer makes a release-to-release data compatibility promise.

### Atomic editing and deletion

For a same-day mutation:

1. Acquire an in-process repository lock and an exclusive file lease.
2. Read and validate the latest file.
3. Locate exactly one matching `EntryId`.
4. Validate `expectedRevision`.
5. Apply the mutation.
6. Write a same-directory temporary file.
7. Flush and validate the temporary file.
8. Atomically replace the original.
9. Recover the original if replacement or validation fails.

Edits that move an entry to another date are a separate cross-file transaction. Keep those out of the first foundation change unless you also implement a recovery journal. The first editing release can require the entry to remain on its original local date.

## What users are most likely to value

This is a product inference based on FocusTimer’s local personal-productivity model, not FocusTimer telemetry.

| Priority | Likely user question or workflow | Foundation needed |
|---|---|---|
| Very high | “Where did my time go today?” | Date-range query, app/project aggregation |
| Very high | “Did I track the correct project and time?” | Detailed entries, stable ID, edit/delete |
| Very high | “Export last week for my timesheet.” | Relative date presets, filters, stable export schema |
| High | “Which time is still unassigned?” | Nullable project plus assignment source |
| High | “Can project selection happen automatically?” | Rule provenance and stable rule IDs |
| High | “Can I exclude private apps or titles?” | Capture-source separation and privacy settings |
| Medium | “How does this week compare with last week?” | Date-range aggregation |
| Medium | “How long are my uninterrupted focus blocks?” | Defined session identity and end reasons |
| Medium | “How often do I switch context?” | Ordered segments and session grouping |
| Medium | “Can I fix a whole batch?” | Stable IDs and bulk mutation |
| Lower | Billing, clients, rounding, invoices | Can be added through schema migrations later |
| Much later | Multi-device sync or team workflows | Device ID, revision, modification timestamp |

Current time-tracking products commonly emphasize summary, detailed, and weekly reports; filtering; editing; and exporting filtered results. That supports prioritizing those workflows ahead of sophisticated analytics. [Clockify report overview](https://clockify.me/help/getting-started/understand-use-your-reports), [Clockify detailed report](https://clockify.me/help/reports/detailed-report).

ActivityWatch’s model also illustrates why source and device/host metadata are valuable when activity can originate from multiple collectors, although FocusTimer should retain timestamp offsets instead of discarding them. [ActivityWatch data model](https://docs.activitywatch.net/en/latest/buckets-and-events.html).

## Recommended delivery sequence

```text
Data contract + schema isolation + store abstraction
                    |
        +-----------+-----------+
        |           |           |
        v           v           v
   Read/query    Safe CRUD   Project attribution
        |           |           |
        v           v           v
 Today report   Edit/delete  Rules engine
        |
        v
 Export presets and broader reports
        |
        v
 Measure whether CSV is still sufficient
```

Recommended order:

1. `OI-07`: worklog data foundation.
2. `OI-04`: query service and Today breakdown.
3. `OI-15`: detailed worklog view and edit/delete UI.
4. `OI-12`: filtered exports and relative date presets.
5. `OI-08`: automatic project rules; it can also proceed after step 1 if desired.
6. `OI-09`: SQLite only when measurements or required mutations justify it.

`OI-20` is an independent investigation that must be resolved before a compatibility-sensitive release changes a persistent format; it is not an implementation dependency for the current development-only foundation.

Do not make SQLite a prerequisite. Per-day CSV remains reasonable for a single-user desktop application with 90-day retention.

Revisit SQLite when one or more of these become real:

- multi-month queries are measurably slow;
- cross-day or bulk mutations become common;
- multiple writers or sync are required;
- full-text search or indexed multi-dimensional filtering is required;
- CSV recovery and transaction logic becomes more complex than a database migration.

## OpenSpec proposal package

### Change ID

`establish-worklog-data-foundation`

### Proposal intent

> FocusTimer’s append-only positional CSV model lacks durable entry identity, explicit session and provenance semantics, timezone-safe timestamps, schema isolation, and safe mutation behavior. This blocks trustworthy reporting, correction, automatic attribution, and export workflows. Establish a versioned storage-neutral worklog foundation while retaining CSV as the current backend.

### Scope

- Close `OI-07`.
- Introduce one explicit current worklog schema for newly created development files.
- Add stable entry/session identity and provenance.
- Add timestamp and midnight-boundary correctness.
- Replace the session-shaped repository contract with a worklog store contract.
- Add header-driven current-schema reading and reject unsupported schemas without modifying them.
- Add storage-level querying.
- Add same-day atomic update/delete primitives and optimistic revision checks.
- Preserve existing tracking and Today-total behavior.

### Explicitly out of scope

- New Today/report UI (`OI-04`).
- Edit/delete UI (`OI-15`).
- Export UI and presets (`OI-12`).
- Rules editor and rule evaluation (`OI-08`).
- SQLite (`OI-09`).
- Legacy migration, backups, installer integration, and rollback strategy (`OI-20`).
- Cloud sync, billing, teams, or invoicing.

### Spec changes

Modify `activity-tracking` for:

- entry and session identity creation;
- provenance population;
- midnight splitting;
- timestamp behavior;
- idempotent persistence.

Add a `worklog-storage` capability for:

- the current worklog schema and invariants;
- unsupported-schema detection and non-destructive rejection;
- query semantics;
- atomic mutation;
- conflict detection;
- diagnostics and corruption handling;
- cleanup of recognized atomic-rewrite temporary artifacts.

### Core requirement scenarios

1. **Stable identity survives reload**
   - Given an entry is persisted, reading it again returns the same `EntryId`.

2. **Segments share a running session**
   - When the active window changes, the new entry has a new `EntryId` and the same `SessionId`.

3. **Resume begins a new session**
   - When the timer resumes after pause, subsequent entries receive a new `SessionId`.

4. **Duplicate append is safe**
   - Re-appending an existing `EntryId` does not create a duplicate row.

5. **Midnight is allocated correctly**
   - A running segment crossing midnight is divided into two entries assigned to their respective dates.

6. **Unsupported files remain untouched**
   - A file using an unsupported header is rejected and remains byte-for-byte unchanged.

7. **New files use only the current schema**
   - Creating a daily worklog emits the current header and all required identity and provenance fields.

8. **Malformed data is visible**
   - Unreadable records produce warnings or quarantine output and are not silently reported as zero time.

9. **Query boundaries are deterministic**
   - Queries use `[fromInclusive, toExclusive)` and include entries overlapping that interval.

10. **Mutation is atomic**
    - An interrupted same-day edit leaves either the complete original or complete updated file, never a partial file.

11. **Concurrent edits detect conflicts**
    - An update with an obsolete revision returns a conflict and does not overwrite newer data.

12. **Rewrite artifacts are contained**
    - Recognized temporary rewrite artifacts are cleaned safely without treating unrelated files as worklogs.

### Implementation tasks

1. Add unsupported-schema fixtures and fault-injection tests before changing serialization.
2. Define current-schema value types, enums, validation, and invariants.
3. Inject `TimeProvider` and platform/device identity providers.
4. Generate entry/session IDs inside `SessionTracker`.
5. Add exact midnight splitting and explicit end reasons.
6. Replace positional regex CSV handling with header-driven streaming serialization.
7. Implement unsupported-schema rejection and structured malformed-record diagnostics without legacy conversion.
8. Introduce `IWorklogStore`, `WorklogQuery`, result warnings, and typed mutation outcomes.
9. Implement idempotent append and duplicate-ID validation.
10. Implement locked same-day atomic update/delete operations.
11. Adapt the existing Today total to the new query boundary without adding the OI-04 UI.
12. Add tests for DST, midnight, commas, quotes, multiline titles, duplicates, stale revisions, locked files, and failed replacements.
13. Update `ARCHITECTURE.md`, `README.md`, current planning docs, and OpenSpec specs together.

## Final review

The original recommendation is directionally correct, but five additions are essential:

- Stable IDs require an explicit current schema and non-destructive rejection of unsupported files; adding columns alone is unsafe.
- `SessionId` needs an exact lifecycle definition.
- `IdleFlag` should become explicit activity and end-reason semantics.
- `DateTimeOffset` and midnight splitting are required for trustworthy daily reports.
- Reports, exports, and editing must use one storage-neutral query/mutation contract rather than parsing CSV independently.

With those corrections, CSV remains a sound first backend and the later features can be added without another foundational redesign. The `establish-worklog-data-foundation` proposal, design, specifications, and task breakdown have been scaffolded and pass strict OpenSpec validation.
