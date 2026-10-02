## Why

FocusTimer records detailed worklog entries but shows only one number, the Today total in the tray tooltip. Users cannot see where their time went by application or project (OI-04). F-01 delivered the storage-neutral query contract this needs, so a breakdown can now be built without touching CSV code.

The breakdown is only the first step. A report/details window opened from the tray, more time ranges, more filters, more groupings, and rule-based project detection (OI-08) are all planned. The first version must be built so those additions plug in without a rewrite.

## What Changes

- Add a UI-free summary service in Core that turns a request (time range, grouping, optional filters) into a result (rows with duration, share, and entry count; total; read warnings).
- Make grouping and project resolution replaceable seams: the first release ships "by application" and "by project" groupings and a resolver that reads the stored project tag. Adding a day grouping or a rule-based resolver later is an addition, not a change to the service.
- Report time with no project as an explicit "Unassigned" row instead of dropping it.
- Clip entries to the requested range so any future range (yesterday, week, custom) is counted correctly, and keep the Today total identical to the existing tray total.
- Surface an unreadable or partly unreadable worklog as a visible warning or error, never as "0 minutes".
- Add a Summary tab to Settings that shows Today's breakdown with a By application / By project switch and a refresh.
- Build the tab as a self-contained view and view model that do not depend on the Settings window, so they can move into a future report window unchanged.

Out of scope: date-range picker, extra filters in the UI, day grouping, rule-based project detection (OI-08), export (OI-12), edit/delete (OI-15), the tray entry and report window, and including the not-yet-persisted running segment.

## Capabilities

### New Capabilities
- `worklog-summary`: Aggregating worklog entries for a time range into grouped rows with totals, shares, an Unassigned bucket, range clipping, and visible read diagnostics; plus the Today breakdown view shown to the user.

### Modified Capabilities
- `settings`: The Settings window gains a Summary tab, so the list of tabs in the "Settings Tabs" requirement changes.

## Impact

- `FocusTimer.Core`: new summary models and service, grouping and project-resolver abstractions (no platform code, so nothing is Windows-only and no Linux stub is needed).
- `FocusTimer.App`: new summary view model and view; `SettingsWindow` gets one tab; `SettingsWindowViewModel` hosts the summary view model.
- `FocusTimer.Host`: DI registration in `Program.cs`.
- Tests: new Core tests for the service and App tests for the view model.
- Docs: `docs/versions/current/OpenIssues.md` (OI-04 partly delivered), `Features.md` (new F-02 row), and `ARCHITECTURE.md`.
- Related issues: OI-04 (partly delivered; ranges, filters UI, and report window remain), enables OI-08, OI-12, OI-15.
