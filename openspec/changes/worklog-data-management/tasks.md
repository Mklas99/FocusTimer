## 0. Prerequisite

- [x] 0.1 Archive `worklog-summary-breakdown` (F-02) before starting group 1; verified: archived as `2026-10-02-worklog-summary-breakdown`, `openspec/specs/worklog-summary/spec.md` exists, and `openspec validate worklog-data-management --strict` passes. Its task 3.2 (render in two themes incl. High Contrast) was archived unchecked and stays a manual check in 10.5

## 1. Manual capture source and storage rules (Core, Persistence)

- [x] 1.1 Add `CaptureSource.Manual` with stored value `manual` in `WorklogValues`, and map it in `CsvWorklogCodec.ParseCapture`; verify with a Persistence test that a manual entry round-trips and a query filtered on Manual returns only manual entries
- [x] 1.2 Add a test that a day file with only active-window rows still reads, appends, patches, and deletes unchanged (schema version not bumped); verify the test passes
- [x] 1.3 Change the same-day check in `CsvSessionRepository.MutateAsync` to compare against the start date (end on the start date, or exactly the next midnight when already stored); verify with tests that an old midnight-ending entry can be shortened and renamed, and that a patch moving the start's day is still rejected
- [x] 1.4 Update the `worklog-storage` wording in `ARCHITECTURE.md` for the new source and the day rule; verify no statement contradicts the code

## 2. Tracker midnight split (Core)

- [x] 2.1 Change `SessionTracker.SplitAtMidnight` to close at boundary minus one second (23:59:59) and start the replacement at the boundary; verify by updating `SessionTrackerTests` midnight test (end 23:59:59, next start 00:00:00, same session, different entry IDs, `DayBoundary` end reason) and multi-day gap case
- [x] 2.2 Run the summary, today-total, and persistence tests that use midnight entries (`WorklogSummaryServiceTests`, `ActivityPollingTests`, `FailurePathTests`, `CsvWorklogStoreTests`); verify they pass or are updated for the new boundary
- [x] 2.3 Update docs that describe midnight splitting (`ARCHITECTURE.md`, `docs/versions/current/Planning.md` "close exactly at the boundary"); verify no document still says entries end at midnight

## 3. Editing service (Core)

- [x] 3.1 Add `DurationParser` ("2h 30m", "45m", "1h", "2.5h"; whole minutes; error with example) and `WorklogDayBounds` (earliest day = today minus (retention days minus one), none when retention is 0 or less); verify with unit tests for each accepted format, rejects, and the bound at retention 1, 90, and 0
- [x] 3.2 Add `IWorklogEditingService`, result types (storage outcome, user message, overlapping entries), and `WorklogChangedEvent` with XML docs; verify the project builds
- [x] 3.3 Implement `AddManualAsync` (fixed "Manual entry" label, Manual source, new IDs, project Editor/Unassigned, trimmed 100-character project, optional window text, 23:59:59 limit, zero, future-time, retention-bound, DST gap rejection, ambiguous time earlier occurrence, allowed when work logging is off); verify with unit tests using a fake store for every scenario in `worklog-entry-management`
- [x] 3.4 Implement `UpdateAsync` (window title, project, duration; end changes only if duration changed; project source rules; 23:59:59 limit); verify with tests for duration up and down, title-only edit of an entry with seconds keeps the exact end, project changed/cleared/unchanged, and passing the day limit
- [x] 3.5 Implement `DeleteAsync` with the loaded revision; verify with tests for success, stale revision, and missing entry
- [x] 3.6 Implement overlap detection (strict overlap, touching is not overlap, self excluded on edit, running segment ignored); verify with tests for overlap, touching, and no entries
- [x] 3.7 Publish `WorklogChangedEvent` after a successful change touching today; verify with tests that today's changes publish and other days do not
- [x] 3.8 Add a Persistence test with the real CSV store: edit with a stale revision conflicts, and an append between load and edit leaves a valid file with both changes; verify it passes (covers editing today's file while tracking)

## 4. By-window grouping (Core)

- [x] 4.1 Add `WindowGrouping` (trimmed, case-insensitive key, first-original label, reserved empty-title key) and register it in the Host registry; verify with tests for case/whitespace variants, empty title, and a window literally named like the empty label
- [x] 4.2 Extend `ARCHITECTURE.md` summary groupings with by window; verify the text matches the registry

## 5. Worklog window shell and tray entry (App, Host)

- [x] 5.1 Add `WorklogWindow`, `WorklogWindowViewModel` (selected day limited by `WorklogDayBounds` and today, previous/next/Today/date pick, tab list, no automatic day change at midnight) and `AppController.ShowWorklog()` mirroring `ShowSettings()` including its initialization guard (single instance, activate on repeat), and close it in `ExitApplication`; verify with view model tests and a headless test that a second call reuses the window and that exit closes it
- [x] 5.2 Add the "Worklog..." tray item directly above "Settings..." in `App.axaml.cs`; verify with a test or code check of item order and by opening the window from the tray in the running app
- [x] 5.3 Register the window, view models, editing service, duration parser, and day bounds in `Program.cs`; verify the app starts and Host tests pass
- [x] 5.4 Handle `WorklogChangedEvent` in `TrayStateController` by calling its existing today refresh and tooltip update; verify with a test that the tooltip total changes after an add and after a delete
- [x] 5.5 Update the tray description in `README.md` and `ARCHITECTURE.md`; verify they agree with the tray order

## 6. Entries tab (App)

- [x] 6.1 Add `WorklogEntriesViewModel` loading the selected day via `QueryAsync` in start order with loading, empty, error, and warning states; verify with view model tests for each state and for a superseded reload
- [x] 6.2 Add `WorklogEntriesView` with the table, details of the selected row (ID, revision, last modified), Refresh, warning and error areas, 24x24 hit targets and keyboard focus, using design-system resources; verify it renders in at least two themes including High Contrast; verified by headless Skia renders in Light, Dark, Nord, and High Contrast (found and fixed unthemed list colors and an unthemed date picker background) plus `WorklogWindowThemeTests` loading the window under all seven built-in themes
- [x] 6.3 Verify entries with commas, quotes, and line breaks display intact; add a view model test with such a title
- [x] 6.4 Reload on window activation unless an add/edit dialog is open; verify with a view model test for both cases

## 7. Add, edit, delete UI and overlap warning (App)

- [x] 7.1 Add the add/edit dialog (date, start time, duration text, window text, editable project combo box with the selected day's and today's projects; edit shows application read-only and disables start) with inline messages from the service; verify with view model tests (duration formats, bad text message, project typed vs chosen, over 100 characters) and a headless dialog test
- [x] 7.2 Wire Add, Edit, Delete (with confirmation) and reload after success; verify with view model tests that the list reflects each change and that cancelling changes nothing
- [x] 7.3 Add the dismissible overlap warning banner (click and keyboard-reachable close control, replaced by the next save, never blocking); verify with tests for show, dismiss by each way, replace, and no-overlap
- [x] 7.4 Show specific messages and reload for conflict, not found, file in use, and unsupported schema; verify with fake-store view model tests for each outcome

## 8. Summary in the Worklog window (App)

- [x] 8.1 Move the Summary tab to the Worklog window and remove it from `SettingsWindow.axaml`, from `SettingsWindowViewModel` (constructor parameter, `WorklogSummary` property, refresh on tab selection near line 129), from the `Program.cs` wiring for Settings, and from the `ActivityPollingEditorTests` helper that builds the view model; verify the Settings tabs are General, Logging, Appearance, Hotkeys, About and the test projects build
- [x] 8.2 Drive the summary `RangeSelector` and `RangeLabel` from the selected day and keep the grouping across day changes; add by window to the grouping switch; verify with view model tests for day change, grouping kept, and empty previous day
- [x] 8.3 Confirm and test refresh: selecting the Summary or Entries tab reloads, Refresh picks up a newly persisted entry, and an added manual entry appears in the totals; verify the Summary total for today equals the tray total after an add, an edit, and a delete
- [x] 8.4 Update the Summary and Settings descriptions in `ARCHITECTURE.md` (line about `SettingsWindowViewModel` hosting the Summary view model); verify no document still says Summary lives in Settings

## 9. Timeline tab (App) - separable, can move to its own branch

- [x] 9.1 Add `WorklogTimelineViewModel` that orders the loaded day's entries, computes block positions and gaps, and marks manual entries; verify with tests for ordering, gaps, overlap placement, and empty day
- [x] 9.2 Add `WorklogTimelineView` with a time axis, blocks labeled with application and window, manual entries marked by label and shape (not color alone), and the shared warning area; verify it renders in at least two themes including High Contrast and that a day with overlap shows both entries
- [x] 9.3 Make Entries and Timeline use the same loaded day so they cannot disagree; verify with a test that one reload updates both

## 10. Documentation and final checks

- [ ] 10.1 Add the F-03 row to `docs/versions/current/Features.md` and update OI-15, OI-22, OI-23, OI-31, OI-33, and OI-04 in `OpenIssues.md` to match what was delivered; verify the rows agree with this change and with each other
- [ ] 10.2 Add the window, editing service, groupings, day selection, and day bounds to `ARCHITECTURE.md`; verify no statement contradicts the code or the specs
- [ ] 10.3 Confirm no change in `FocusTimer.Platform.Windows` or the Linux stubs; verify with a search of the diff
- [ ] 10.4 Run the full test suite, the StyleCop/`dotnet format` check, and `openspec validate worklog-data-management --strict`; verify all pass and record any pre-existing failures separately
- [ ] 10.5 Run the app and walk through, including the Summary tab in a light theme and in High Contrast (F-02 task 3.2 carried over): open from tray, add a manual entry that overlaps, dismiss the warning, edit duration, delete, change day, switch grouping to by window, press Refresh while tracking, watch the tray total; verify each behaves as specified
