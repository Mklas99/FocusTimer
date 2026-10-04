# Proposal

## Why

Every foreground window-title change closes a segment. In browsers and editors the title changes with each tab or
file, so one stretch of work becomes dozens of short entries that clutter the Entries table and Timeline.
`OI-14` left "segmentation rules" open after polling became configurable. Users need to say which applications should
not be split by title changes. This is the second change of the smarter-tracking branch and reuses the window matcher
delivered by `app-exclusions` (`OI-32`).

## What Changes

- Add an ordered list of **segmentation rules** in Developer Options. A rule is a window rule (application and/or title
  pattern, same semantics as exclusions) meaning "title changes inside matching windows do not start a new segment".
- `SessionTracker` compares consecutive samples by application only when the previous and the new window both match a
  segmentation rule and share the same process name. The segment keeps the title it started with.
- No rules means today's behavior (any application or title change splits). Existing worklogs are not changed.
- Exclusions take precedence over segmentation: an excluded window never forms a segment.
- A minimum-duration merge (absorbing very short segments) is not part of this change; see Impact.

## Capabilities

### New Capabilities

None. Matching reuses `window-matching`.

### Modified Capabilities
- `activity-tracking`: the segmentation requirement gains the rule-controlled exception for title changes.
- `settings`: adds the Developer Options segmentation rule list with the same draft/commit behavior as exclusions.

## Impact

- `FocusTimer.Core`: `Settings` list property, `SessionTracker.HasWindowChanged`.
- `FocusTimer.Persistence`: tolerant list loading (reuse the exclusion converter).
- `FocusTimer.App`: Developer Options editor; push to the tracker alongside exclusions.
- `FocusTimer.Platform.Windows`: none.
- Docs: `OpenIssues.md` (OI-14 segmentation half), `Features.md`, `ARCHITECTURE.md`.
- Deferred: minimum-duration merging needs a retroactive merge of the previous entry and changes what Entries and
  Timeline show; it deserves its own change if the title rule is not enough.
