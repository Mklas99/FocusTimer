# Proposal

## Why

Some applications (password managers, private chats, banking) must never appear in the worklog, and hiding rows in
reports does not satisfy that: the data would still be on disk. Exclusion therefore has to happen at capture time.
This is `OI-32`, the first change of the "smarter tracking" branch (`OI-32`, `OI-14`, `OI-08`). It also introduces the
application/title matcher that segmentation rules (`OI-14`) and project rules (`OI-08`) will reuse.

## What Changes

- Add a shared, UI-free **window matcher**: a rule has an optional application (process name) pattern and an optional
  window-title pattern. A rule needs at least one; when both are set, both must match. Matching is case-insensitive
  and supports `*` and `?` wildcards.
- Add an ordered **exclusion rule list** to settings, edited in Developer Options and committed through the existing
  Apply/OK/Cancel flow.
- `SessionTracker` consults the rules during capture. While the foreground window matches an exclusion, no segment is
  open: the current segment closes at the observation time, nothing is recorded, and a new segment starts when a
  non-excluded window is next observed. Excluded time is never attributed to the preceding or following application.
- Existing worklogs are not modified. Rule changes affect only tracking from the moment they are applied.
- Excluded time still counts on the running timer; only the worklog omits it.

## Capabilities

### New Capabilities
- `window-matching`: pattern semantics for matching a foreground window by application and/or title, shared by
  exclusion, segmentation, and project rules.

### Modified Capabilities
- `activity-tracking`: adds application exclusion during capture and the gap behavior for excluded intervals.
- `settings`: adds the persisted exclusion rule list and its Developer Options editing and commit behavior.

## Impact

- `FocusTimer.Core`: new matcher and rule model, `Settings` property, `SessionTracker` capture path.
- `FocusTimer.Persistence`: tolerant JSON handling of the new list (one bad rule must not reset other settings).
- `FocusTimer.App`: Developer Options editor and view model; applying rules to the tracker on settings load/apply.
- `FocusTimer.Platform.Windows`: none. Matching works on `ActiveWindowInfo`, which Linux stubs already provide.
- Docs: `OpenIssues.md` (OI-32), `Features.md` ("Smarter automatic tracking"), `ARCHITECTURE.md` if the tracker
  description changes.
- Follow-ups, not in this change: `OI-14` segmentation rules and `OI-08` rule-based project detection.
