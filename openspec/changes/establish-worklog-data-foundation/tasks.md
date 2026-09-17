# Tasks

## 1. Core Worklog Model and Contracts

- [x] 1.1 Replace the mutable `TimeEntry` shape with an immutable or init-only current worklog record containing the specified identity, timestamp, project, provenance, revision, and modification fields; verify focused Core tests cover construction and round-trip equality.
- [x] 1.2 Add centralized validation for non-empty entry/session IDs, positive intervals, revision values, derived duration, and same-local-day timestamps; verify valid entries pass and each invalid invariant returns a validation failure without persistence access.
- [x] 1.3 Add string-stable controlled values for activity kind, end reason, capture source, platform, and project-assignment source; verify serialization tests prove stored values do not depend on enum ordinals.
- [x] 1.4 Define `IWorklogStore` plus append, lookup, query, patch, delete, warning, and typed outcome contracts with cancellation support; verify the Core project builds without persistence-specific types or `IQueryable` in its public API.
- [x] 1.5 Add the stable installation/device ID to settings with generate-once behavior and a Core-friendly source-platform provider; verify settings round-trip preserves the ID and Windows reports the expected platform while existing non-Windows stubs still compile.

## 2. Tracking Identity and Time Semantics

- [ ] 2.1 Inject `TimeProvider` and the metadata providers needed by `SessionTracker`, removing direct current-time reads; verify tracker tests control all generated timestamps deterministically.
- [ ] 2.2 Generate one session ID for each transition into an uninterrupted Running interval and a new entry ID for each segment; verify pause/resume changes the session ID while application/window changes retain it.
- [ ] 2.3 Populate completed active-window entries with capture, activity, platform, device, project-assignment, initial revision, and last-modified metadata; verify a normal completed segment contains every required field.
- [ ] 2.4 Propagate explicit application-change, manual-pause, idle-pause, day-boundary, application-exit, and unknown end reasons through timer and tracking events; verify focused tests assert the correct reason for each available stop path.
- [ ] 2.5 Split a running segment at the exact local midnight boundary, retaining its session ID and starting a new entry ID in the next day; verify tests cover an ordinary midnight plus ambiguous and invalid local-time transitions without cross-day records.
- [ ] 2.6 Preserve periodic and pause/stop flush behavior through idempotent batch appends; verify retrying the same completed segment cannot create duplicate tracked time.

## 3. Current-Schema CSV Codec

- [ ] 3.1 Select and centrally pin a maintained streaming RFC 4180 CSV dependency, recording the choice in dependency documentation; verify restore and license/package checks used by the repository succeed.
- [ ] 3.2 Implement a header-driven current-schema codec with a stable writer column order and explicit schema version; verify every worklog field round-trips independently of input header order.
- [ ] 3.3 Support commas, escaped quotes, Unicode, empty optional values, and record-spanning line breaks in text fields; verify codec round-trip tests reproduce the exact original values without extra records.
- [ ] 3.4 Return structured record warnings when malformed current-schema rows can be isolated, and a typed failure when record boundaries or schema cannot be trusted; verify tests distinguish partial safe reads from failed reads.
- [ ] 3.5 Detect unsupported or legacy headers before any append or rewrite and leave the target byte-for-byte unchanged; verify tests use a development-format fixture and confirm no migration, backup, or mixed-format output is produced.

## 4. CSV Store Reads and Idempotent Appends

- [ ] 4.1 Implement daily path selection for `worklogs/yyyy/MM/yyyy-MM-dd-worklog.csv` and create new files with the current header; verify entries on adjacent local dates are written to their respective paths.
- [ ] 4.2 Implement validated batch append with per-file serialization and all-or-nothing preflight checks; verify one invalid batch member leaves every targeted file unchanged.
- [ ] 4.3 Make append idempotent by entry ID, accepting identical retries and rejecting reused IDs with different content; verify repeated and conflicting append tests assert row counts and typed outcomes.
- [ ] 4.4 Implement entry lookup and half-open overlap queries across required daily partitions with chronological ordering; verify missing daily files contribute empty success rather than warnings.
- [ ] 4.5 Apply exact case-insensitive application/project filters and session, activity-kind, and capture-source filters; verify combined-filter tests return only entries satisfying every supplied condition.
- [ ] 4.6 Return valid entries together with structured file/record warnings without converting I/O, file-in-use, malformed, or unsupported-schema failures into empty success; verify each outcome is covered by persistence tests.

## 5. Revision-Checked Atomic Mutation

- [ ] 5.1 Add a same-directory temporary-file and atomic-replacement abstraction suitable for Windows, with platform-neutral failure injection; verify the Windows-targeted persistence tests prove readers never observe a partial target.
- [ ] 5.2 Implement same-day patch by entry ID and expected revision, deriving duration and incrementing revision/last-modified exactly once; verify success, not-found, stale-revision, validation, and cross-day rejection cases.
- [ ] 5.3 Implement delete by entry ID and expected revision through the same atomic rewrite path; verify success, not-found, and stale-revision cases leave all unrelated rows unchanged.
- [ ] 5.4 Serialize append, update, and delete operations per daily file while reading the latest content after the lock is acquired; verify concurrency tests produce no lost successful change, duplicate row, or invalid CSV.
- [ ] 5.5 Flush, re-read, and validate each complete replacement before activation, preserving the original on write, validation, or activation failure; verify injected failures leave the original bytes readable and unchanged.
- [ ] 5.6 Clean successful temporary artifacts and safely remove recognized stale artifacts without touching unrelated files; verify cleanup tests cover completed and interrupted rewrite attempts.

## 6. Application Integration

- [ ] 6.1 Replace `ISessionRepository` registrations and call sites with `IWorklogStore`, removing the old contract and unused `Session` model only after reference checks; verify the full solution builds with no remaining production references.
- [ ] 6.2 Adapt `TodayStatsService` to query the local-day half-open interval and aggregate derived durations without parsing or locating CSV files; verify existing Today totals remain correct at interval boundaries.
- [ ] 6.3 Adapt persistence event handling and shutdown/flush paths to typed store outcomes, logging or surfacing actionable failures and warnings; verify integration tests cover successful flush, unsupported schema, and file-in-use behavior.
- [ ] 6.4 Wire the clock, settings-backed device identity, source-platform provider, codec, and worklog store in the Windows host composition root; verify the Windows host starts from DI and Linux-compatible Core stubs continue to compile without adding Linux implementation work.
- [ ] 6.5 Preserve existing retention behavior for current-schema daily files without treating rewrite temporary files as worklogs; verify retention tests keep in-range data and remove only eligible finalized daily files.

## 7. Quality, Documentation, and Acceptance

- [ ] 7.1 Add representative large-day append/query benchmarks or performance tests for ID-cache invalidation and multi-day queries; verify recorded results establish a repeatable CSV baseline without introducing SQLite.
- [ ] 7.2 Update architecture, development, README, and `docs/versions/current/` documentation where affected, explicitly documenting the current schema, manual handling of unsupported development files, and OI-20 ownership of future migration strategy; verify the documents and OpenSpec artifacts do not contradict one another.
- [ ] 7.3 Run `dotnet format --verify-no-changes`, the full NUnit suite, and the repository's normal Windows quality checks; verify all commands pass or document any pre-existing unrelated failure with evidence.
- [ ] 7.4 Run strict OpenSpec validation for `establish-worklog-data-foundation` and manually trace each proposal outcome to a specification scenario and implementation task; verify the change is ready for `openspec-apply-change` with no migration implementation in scope.
