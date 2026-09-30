# Proposal

## Why

Before this change, foreground capture performed a process-name lookup every second even when the user stayed in one application. Developers could not tune capture frequency independently of the timer display. This change addresses the polling portion of OI-14 and provides measured evidence for that portion of OI-17 in `docs/versions/current/OpenIssues.md`.

## What Changes

- Add a persisted `ActivityPollingIntervalSeconds` setting, defaulting to 10, with whole-second values from 1 through 60. Expose it in the existing unlocked Developer Options section.
- Apply the interval after successful Apply/OK, without restarting. Explain that longer intervals can miss short app visits and attribute intervening time to the last observed window.
- Schedule foreground capture independently of the one-second timer display and day-boundary maintenance. Capture immediately at tracking start, avoid overlapping lookups and queued catch-up polls, and retain pause/stop/disable behavior.
- Reuse the current Windows process name only while its process lifetime remains valid. Continue sampling window titles and process IDs on every capture, preserve failure fallbacks, and release cached handles deterministically.
- Compare Release-build capture behavior and resource costs before and after. Retain a cache optimization only if it reduces measured lookup work without correctness or resource regressions.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `activity-tracking`: replace mandatory fixed one-second polling with configurable polling that defaults to ten seconds; define bounded capture scheduling and unchanged day-boundary and session behavior.
- `settings`: extend hidden Developer Options with a validated, persisted polling interval and its sampling tradeoff.

## Impact

- Core: `Settings`, `SessionTracker`, its injected `TimeProvider`, and the existing `TimerService` tick integration.
- App: `SettingsWindow.axaml`, `SettingsWindowViewModel`, `AppController`, and `TimerWidgetViewModel` settings initialization/reload paths.
- Windows: `WindowsActiveWindowService` process lifetime/cache handling and deterministic disposal. This Windows-specific optimization has no real Linux equivalent yet (OI-06); keep `IActiveWindowService` and Linux stubs compatible.
- Persistence: exercise existing JSON settings serialization; no worklog schema, backend, or CSV safety changes.
- Tests: existing xUnit Core, App, Persistence, Host, and Windows suites as applicable. Planning context was corrected to match the checked-in xUnit tests and mutable Settings model.
- Documentation: OI-14 remains partially scoped because segmentation rules are not added; OI-17 remains open beyond foreground capture. The main activity-tracking and settings specs now describe the implemented behavior.
- No new runtime package is required. Settings-read caching, worklog write optimization, event-based capture, subsecond polling, and Linux capture implementation are outside this change.
