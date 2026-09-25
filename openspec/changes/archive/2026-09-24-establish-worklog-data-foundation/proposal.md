# Proposal

## Why

FocusTimer's append-only, position-based CSV model has no durable entry identity, explicit session or provenance semantics, timezone-safe timestamps, or storage-neutral query and mutation boundary. This blocks trustworthy reporting, correction, automatic project attribution, and reusable exports tracked by OI-04, OI-07, OI-08, OI-12, and OI-15.

## What Changes

- **BREAKING** Replace the development worklog row format with a versioned schema that gives every entry a durable ID, groups segments into a defined running session, records capture/activity/platform provenance, and stores offset-aware timestamps.
- Define a running session as one uninterrupted transition into `Running`: application/window changes retain the session ID, while pause followed by resume begins a new session.
- Split entries at local calendar-day boundaries so per-day storage and date-based reporting allocate time consistently.
- Replace the session-shaped persistence contract with a storage-neutral worklog store supporting idempotent append, entry lookup, explicit date/app/project/session queries, revision-checked same-day update, and delete.
- Replace positional, line-based CSV parsing with a header-driven current-schema codec that handles quoted CSV fields correctly and reports malformed or unsupported data instead of returning a misleading empty result.
- Keep CSV as the active backend and preserve the existing per-day directory layout and retention behavior.
- Refuse to append to an existing unsupported-schema file so development data is never silently mixed. Existing development worklogs may be moved or removed manually.
- Exclude legacy migration, backup orchestration, installer integration, and rollback from this change; the general strategy is tracked independently by OI-20.
- Exclude reporting UI, worklog editor UI, export presets, project-rule evaluation, and SQLite adoption from this foundation change.

## Capabilities

### New Capabilities

- `worklog-storage`: Defines the current worklog schema, identity and timestamp invariants, storage-neutral querying and mutation, CSV safety, error reporting, and atomic same-day rewrites.

### Modified Capabilities

- `activity-tracking`: Logged segments gain durable identity, running-session grouping, capture/activity/platform metadata, day-boundary segmentation, and idempotent persistence behavior.

## Impact

- `FocusTimer.Core`: `TimeEntry` semantics and persistence interfaces/query models change; session tracking must create stable entry and session identities.
- `FocusTimer.Persistence`: the CSV repository is replaced or substantially revised around the current schema, robust CSV serialization, explicit query results, schema rejection, and atomic same-day mutation.
- `FocusTimer.App` and `FocusTimer.Host`: dependency injection and current Today-total consumers adapt to the new store contract without adding new reporting or editing UI.
- Tests gain coverage for identity stability, session lifecycle, time-zone offsets, midnight splitting, duplicate append, filtering, malformed data, unsupported schemas, stale revisions, and atomic file replacement.
- Root architecture/development documentation, `docs/versions/current/`, and the durable OpenSpec activity-tracking specification must be synchronized during implementation.
- No compatibility promise is made for existing development worklogs in this change. Release-to-release migration remains an explicit follow-up investigation under OI-20.
