## 1. Manual capture source (Core, Persistence)

- [ ] 1.1 Add `CaptureSource.Manual` with stored value `manual` in `WorklogValues`, and map it in `CsvWorklogCodec.ParseCapture`; verify with a Persistence test that a manual entry round-trips and a query filtered on Manual returns only manual entries
- [ ] 1.2 Add a test that a day file with only active-window rows still reads, appends, patches, and deletes unchanged (schema version not bumped); verify the test passes
- [ ] 1.3 Update the `worklog-storage` wording in `ARCHITECTURE.md` for the new source; verify no statement contradicts the code

## 2. Editing service (Core)

- [ ] 2.1 Add `IWorklogEditingService` and result types (storage outcome, user message, overlapping entries) with XML docs; verify the project builds
- [ ] 2.2 Implement `AddManualAsync` (fixed "Manual entry" label, Manual source, new IDs, project source Editor/Unassigned, optional window text, midnight/zero/future-time rejection); verify with unit tests using a fake store for each scenario in `worklog-entry-management`
- [ ] 2.3 Implement `UpdateAsync` (window title, project, duration; end = start + duration; project source rules; same-day check); verify with tests for duration up and down, project changed/cleared/unchanged, and passing midnight
- [ ] 2.4 Implement `DeleteAsync` with the loaded revision; verify with tests for success, stale revision, and missing entry
- [ ] 2.5 Implement overlap detection (strict overlap, touching is not overlap, self excluded on edit); verify with tests for overlap, touching, and no entries
- [ ] 2.6 Add a Persistence test with the real CSV store: edit with a stale revision conflicts, and an append between load and edit leaves a valid file with both changes; verify it passes (covers editing today's file while tracking)

## 3. By-window grouping (Core)

- [ ] 3.1 Add `WindowGrouping` (trimmed, case-insensitive key, first-original label, reserved empty-title key) and register it in the Host registry; verify with tests for case/whitespace variants, empty title, and a window literally named like the empty label
- [ ] 3.2 Extend `ARCHITECTURE.md` summary groupings with by window; verify the text matches the registry

## 4. Worklog window shell and tray entry (App, Host)

- [ ] 4.1 Add `WorklogWindow`, `WorklogWindowViewModel` (selected day clamped to today, previous/next/Today/date pick, tab list) and `AppController.ShowWorklog()` mirroring `ShowSettings()` (single instance, activate on repeat, closed with the app); verify with view model tests and a headless test that a second call reuses the window
- [ ] 4.2 Add the "Worklog..." tray item directly above "Settings..." in `App.axaml.cs`; verify with a test or code check of item order and by opening the window from the tray in the running app
- [ ] 4.3 Register the window, view models, and editing service in `Program.cs`; verify the app starts and Host tests pass
- [ ] 4.4 Update the `system-tray` spec wording in `ARCHITECTURE.md` and README tray description; verify they agree with the tray order

## 5. Entries tab (App)

- [ ] 5.1 Add `WorklogEntriesViewModel` loading the selected day via `QueryAsync` in start order with loading, empty, error, and warning states; verify with view model tests for each state and for a superseded reload
- [ ] 5.2 Add `WorklogEntriesView` with the table, details of the selected row (ID, revision, last modified), Refresh, warning and error areas, 24x24 hit targets and keyboard focus, using design-system resources; verify it renders in at least two themes including High Contrast
- [ ] 5.3 Verify entries with commas, quotes, and line breaks display intact; add a view model test with such a title

## 6. Add, edit, delete UI and overlap warning (App)

- [ ] 6.1 Add the add/edit dialog (date, start time, duration, window text, project; edit shows application read-only and disables start) with inline validation messages from the service; verify with view model tests and a headless dialog test
- [ ] 6.2 Wire Add, Edit, Delete (with confirmation) and reload after success; verify with view model tests that the list reflects each change and that cancelling changes nothing
- [ ] 6.3 Add the dismissible overlap warning banner (close button and click to dismiss, replaced by the next save, never blocking); verify with tests for show, dismiss, replace, and no-overlap
- [ ] 6.4 Show specific messages and reload for conflict, not found, file in use, and unsupported schema; verify with fake-store view model tests for each outcome

## 7. Summary in the Worklog window (App)

- [ ] 7.1 Move the Summary tab to the Worklog window and remove it from `SettingsWindow` and `SettingsWindowViewModel`; verify the Settings tabs are General, Logging, Appearance, Hotkeys, About and no test still expects a Summary tab
- [ ] 7.2 Drive the summary `RangeSelector` and `RangeLabel` from the selected day and keep the grouping across day changes; add by window to the grouping switch; verify with view model tests for day change, grouping kept, and empty previous day
- [ ] 7.3 Confirm and test refresh: selecting the Summary or Entries tab reloads, Refresh picks up a newly persisted entry, and an added manual entry appears in the totals; compare the Summary total for today with the tray total after an edit and align or document any difference
- [ ] 7.4 Update `settings` and Summary descriptions in `ARCHITECTURE.md` and `README.md`; verify no document still says Summary lives in Settings

## 8. Timeline tab (App) - separable, can move to its own branch

- [ ] 8.1 Add `WorklogTimelineViewModel` that orders the loaded day's entries, computes block positions and gaps, and marks manual entries; verify with tests for ordering, gaps, overlap placement, and empty day
- [ ] 8.2 Add `WorklogTimelineView` with a time axis, blocks labeled with application and window, manual entries marked by label and shape (not color alone), and the shared warning area; verify it renders in at least two themes including High Contrast and that a day with overlap shows both entries
- [ ] 8.3 Make Entries and Timeline use the same loaded day so they cannot disagree; verify with a test that one reload updates both

## 9. Documentation and final checks

- [ ] 9.1 Add the F-03 row to `docs/versions/current/Features.md` and update OI-15, OI-22, OI-23, OI-31, OI-33, and OI-04 in `OpenIssues.md` to match what was delivered; verify the rows agree with this change and with each other
- [ ] 9.2 Add the new capabilities and changes to `ARCHITECTURE.md` (window, editing service, groupings, day selection); verify no statement contradicts the code or the specs
- [ ] 9.3 Confirm no change in `FocusTimer.Platform.Windows` or the Linux stubs; verify with a search of the diff
- [ ] 9.4 Run the full test suite, the StyleCop/`dotnet format` check, and `openspec validate worklog-data-management --strict`; verify all pass and record any pre-existing failures separately
- [ ] 9.5 Run the app and walk through: open from tray, add a manual entry that overlaps, dismiss the warning, edit duration, delete, change day, switch grouping to by window, press Refresh while tracking; verify each behaves as specified
