## 1. Core summary model

- [x] 1.1 Add `SummaryRange`, `SummaryFilter`, `WorklogSummaryRequest`, `SummaryRow`, and `WorklogSummary` records in `FocusTimer.Core/Models` with XML docs, plus a `SummaryRanges.Today(TimeProvider)` helper; verify with unit tests for the Today range at a normal day and a DST change day
- [x] 1.2 Add the `IWorklogGrouping` interface, the `GroupKey` value, and `WorklogGroupingRegistry` (ordered list, lookup by id, duplicate id rejected); verify with registry tests for lookup, order, unknown id, and duplicate id
- [x] 1.3 Add the `IProjectResolver` interface and `StoredProjectResolver` that returns the stored project tag; verify with tests for a tagged, empty, and null tag

## 2. Core summary service

- [x] 2.1 Implement the application grouping (case-insensitive key, original-cased label from the first entry) and the project grouping (trimmed, case-insensitive key, Unassigned key that cannot collide with a project named "Unassigned"); verify with unit tests for both, including case and whitespace variants
- [x] 2.2 Add `IWorklogSummaryService` and its implementation: query the store, clip entries to the range, apply the resolver-based project filter, group, sum, compute shares, and sort by duration descending; verify with tests for grouping, shares summing to one, sorting, empty result, and a zero total
- [x] 2.3 Pass the application filter to the store query and apply the project filter after resolving; verify with tests where filters match, match nothing, and are omitted
- [x] 2.4 Carry a non-success store outcome and partial-read warnings onto the result and never return an empty success for a failed read; verify with fake-store tests for unreadable, unsupported-schema, and warning-with-entries results
- [x] 2.5 Add a test that the Today summary total equals `TodayStatsService.GetTodayTotal()` on the same fake worklog (entries do not cross midnight, as F-01 splits them, so the non-clipping tray total is comparable), and add a separate clipping test for an entry that crosses a range boundary; verify both tests pass
- [x] 2.6 Add a test that registers a third fake grouping and a fake resolver and produces a correct summary without changing service code; verify the test passes (proves the extension seams)

## 3. App view model and view

- [x] 3.1 Add `WorklogSummaryViewModel` in `FocusTimer.App/ViewModels` with the current request, grouping list from the registry, selected grouping, rows, total text, status (loading, ready, no data, error, ready with warnings), warning text, and an async refresh command; it must not reference the Settings window or its view model; verify with view model tests using a fake summary service for each status and for switching grouping
- [ ] 3.2 Add `WorklogSummaryView` (a `UserControl`) with the total, a grouping switch, a refresh button, a row list with label, duration, share bar and percentage, and distinct loading, no-data, and error messages, using design-system semantic resources, 24x24 minimum hit targets, and keyboard focus; verify the app builds and the view renders in the Avalonia previewer or a running app in at least two themes including High Contrast
- [x] 3.3 Format durations as hours and minutes consistently with the tray text and label the Unassigned row distinctly; verify with view model tests for formatting of zero, under one minute, and over ten hours

## 4. Settings integration and wiring

- [ ] 4.1 Add a Summary tab to `SettingsWindow.axaml` between Logging and Appearance that hosts `WorklogSummaryView`, expose the summary view model on `SettingsWindowViewModel`, and refresh it when the tab is selected; verify by opening Settings, selecting the tab, and seeing today's rows, and by a test that selecting the tab triggers one refresh
- [x] 4.2 Register the summary service, grouping registry with both groupings, the stored-project resolver, and the summary view model in `FocusTimer.Host/Program.cs`; verify the app starts and existing Host tests still pass, and confirm the design-time data context of the settings window still builds
- [x] 4.3 Confirm no platform-specific code was added, so `FocusTimer.Platform.Windows` and the Linux stubs are untouched; verify with a search of the diff for changes in those projects

## 5. Extensibility check and docs

- [x] 5.1 Confirm the view model and view have no reference to `SettingsWindow` and can be hosted in another window (for example by a small test host or a code search for references); verify and note the result in the design notes if anything had to change
- [x] 5.2 Update `ARCHITECTURE.md` with the summary service, the grouping and project-resolver seams, and the host-independent view; verify no statement contradicts the current code
- [x] 5.3 Update `docs/versions/current/OpenIssues.md` (OI-04: Today breakdown by app/project delivered, ranges, filter UI, and report window remain) and add an F-02 row to `docs/versions/current/Features.md`; verify the rows agree with each other and with the OpenSpec change
- [x] 5.4 Run the full test suite, the StyleCop/`dotnet format` check, and `openspec validate worklog-summary-breakdown --strict`; verify all pass and record any pre-existing failures separately
