# Design

## Context

- `IWorklogStore` already provides `AppendAsync`, `QueryAsync`, `PatchAsync` (expected revision, same-day, atomic replace) and `DeleteAsync` (expected revision). Nothing new is needed in the file handling.
- `CaptureSource` has only `ActiveWindow`, and `CsvWorklogCodec.ParseCapture` throws on any other value, so a new source needs a codec change.
- `WorklogSummaryViewModel` and `WorklogSummaryView` (F-02) do not reference Settings. The range comes from a replaceable `RangeSelector` function, so a day selector is a small addition.
- Settings opens through `AppController.ShowSettings()` (single instance, `Closed` clears the field, closed on exit); the tray item is created in `App.axaml.cs` next to "Settings...".
- The running segment is held in memory and only reaches the store when it closes, so the Entries view only ever shows persisted entries. The tracker appends to today's file through `WorklogPersistenceCoordinator`; the store serializes writes per day file.
- Dependency: `worklog-summary` and the Summary tab exist in code but the `worklog-summary-breakdown` change (F-02) is not archived. F-03 modifies that capability, so F-02 is verified and archived before implementation starts.
- `SessionTracker.SplitAtMidnight` closes a day-boundary segment exactly at the next midnight, so tracked entries end at 00:00 of the next day. `MutateAsync` rejects a patch whose `EndedAt.Date` differs from the stored one, which would block shortening such an entry. `DataRetentionDays` (default 90) deletes day files before today minus that many days.
- `TodayStatsService` only adds entries (`AddEntriesAsync`) or recomputes at startup and on a day change; it is not told about edits or deletes.

## Goals / Non-Goals

**Goals:**
- A Worklog window with Entries, Timeline, and Summary tabs sharing one selected day.
- Add, edit (window title, project, duration), and delete entries through one UI-free service with typed results.
- Keep all storage safety guarantees (revision checks, atomic replace, same-day rule).
- Entries, Timeline, and Summary use the same read path and show the same warnings.

**Non-Goals:**
- Editing start, end, or application; moving entries across days; ranges beyond one day; export; rule-based project detection; platform code.

## Decisions

1. **Own window, not a Settings tab.** `WorklogWindow` + `WorklogWindowViewModel`, opened by `AppController.ShowWorklog()` that mirrors `ShowSettings()`. It does not take part in the Settings draft, Apply/Cancel, or startup recovery, so no coupling to the Settings commit flow. Alternative (keep tabs in Settings) rejected: Settings Cancel/Apply semantics do not fit data that saves immediately.
2. **Shared day state in the window view model.** `WorklogWindowViewModel` owns `SelectedDay` (a `DateOnly`, never later than today) and pushes it to the child view models. The existing summary view model keeps its `RangeSelector`; the window sets it to the selected day and updates `RangeLabel`. This delivers the previous-day part of OI-33 without changing the summary service.
3. **Editing service in Core.** `IWorklogEditingService` with `AddManualAsync`, `UpdateAsync`, `DeleteAsync` returning a result that carries the storage outcome, a user-facing message, and overlap warnings. It validates, builds the `TimeEntry`/`WorklogPatch`, calls the store, and finds overlaps with a day query. The view model contains no storage logic and can be unit tested with a fake store.
4. **Manual entry shape.** Allowed even when `WorkLoggingEnabled` is off, because it is an explicit user action. Application is the constant "Manual entry", `CaptureSource.Manual`, `ActivityKind.Active`, new entry and session IDs, source platform and device ID as for tracked entries, project source `Editor` when a project is set and `Unassigned` when not. The end reason is `Unknown` (assumption: adding a value for a state that does not exist for manual entries would only add a codec case; revisit if reports need to tell them apart, since capture source already does). The stored value of the new source is `manual`.
5. **Edit semantics.** The patch keeps start, application, capture source, activity kind, and end reason; the end is start + new duration only if the duration changed, otherwise the stored end is kept (no rounding of tracked seconds). A changed project sets source `Editor` and clears the rule ID; an unchanged project keeps all three project fields. A duration that would put the end after 23:59:59 of the start's day is rejected in the service, before the store would reject it.
6. **Overlap is a result, not an error.** The service returns the overlapping entries (strict overlap; touching is not overlap, matching the store's half-open query). The window shows one dismissible banner (a keyboard-reachable close button, and a click on the banner also dismisses it); a new save replaces it. Nothing blocks.
7. **Conflict handling.** The row view model carries the revision it loaded. On Conflict, NotFound, FileInUse, or UnsupportedSchema the user gets a specific message, no change is written, and the day reloads. Editing today's file while tracking runs relies on the store's per-file serialization; tests cover a stale revision and an append between load and save.
8. **By-window grouping.** A new `WindowGrouping` registered in `WorklogGroupingRegistry`, key = trimmed case-insensitive title, label = first original text, and a reserved key for empty titles that cannot collide with a real window title (same technique as the Unassigned project key).
9. **Entries table.** A read-only list view model over `QueryAsync` for the selected day, ordered by start; row details (entry ID, revision, last modified) are shown for the selected row. Actions: Add, Edit, Delete on the selected row, and Refresh. The same loaded day feeds the Timeline so the two never disagree.
10. **Refresh.** Reload on tab selection, Refresh, day change, window activation (skipped while an add/edit dialog is open, so typing is never interrupted), and after each successful mutation; each reload uses the superseding-token pattern already used by the summary view model. No push updates from the tracker in this change.
11. **Timeline last and separable.** The Timeline tab is a read-only drawing of the already loaded entries (vertical time axis, one block per entry, manual entries marked with a label and different shape, gaps as empty space, overlaps drawn side by side). It has no write path, so it is the last task group and can be split into its own branch if the change grows.
12. **Tracker closes at 23:59:59.** `SplitAtMidnight` closes the segment at boundary minus one second and starts the next at the boundary, so no stored entry ends at the next day's midnight. One second per day is not recorded; this is accepted. Existing entries ending at midnight stay valid, and the store's same-day check changes to compare against the start date (end on the start date, or exactly the next midnight when already stored), so those entries remain editable.
13. **Duration input.** A Core `DurationParser` accepts "2h 30m", "45m", "1h", and "2.5h" (whole minutes, no seconds) and returns an error with an example otherwise. Elapsed time is used everywhere, so DST days just work; the start's local time is converted with the time zone, a nonexistent time is rejected, and an ambiguous time takes the earlier occurrence.
14. **Retention bound.** A small `WorklogDayBounds` helper gives the earliest allowed day as today minus (`DataRetentionDays` - 1), or none when retention is 0 or less. The day selector and the editing service use it, so the selector and the add dialog cannot disagree.
15. **Project input.** An editable combo box with the distinct, non-empty projects of the selected day and today; typed text is trimmed and limited to 100 characters.
16. **Tray total.** After a successful add, edit, or delete that touches today, the editing service publishes a `WorklogChangedEvent` on the event bus; `TrayStateController` handles it with its existing `RefreshTodayAndUpdateTooltipAsync`. `EntriesLoggedEvent` stays as is for tracker output.
17. **No platform work.** Everything is Core, Persistence, and App; the Windows project and the Linux stubs are untouched.

## Risks / Trade-offs

- **Older builds cannot read manual rows.** `ParseCapture` throws on `manual`, so an older version reads such a record as malformed (a warning, not data loss). Acceptable for a development-stage format (see OI-20); noted in release notes.
- **Stale view while tracking.** Without push updates, a new tracked entry only appears after Refresh. Mitigation: refresh on tab select and on window activation; push updates can follow if wanted.
- **One second per day is not recorded** by the new midnight split. Accepted; Summary and tray totals simply exclude it.
- **Edit and delete scan every daily file** to find the entry by ID (cached by file fingerprint). Accepted for now; an optional hint of the day could be passed to the store later.
- **Overlap with the running segment is not detected** because it is not persisted. Accepted.
- **Overlap not prevented.** Manual or edited time may overlap tracked time and be counted twice in totals. The warning is the only guard, by decision; reports do not de-duplicate.
- **Scope.** Window, table, dialog, and timeline are large for one change; the timeline group is isolated so it can move out.

## Open Questions

None blocking. Assumptions recorded above: the "Manual entry" label is a constant (not localized yet), the end reason for manual entries is `Unknown`, a future-time check uses the system clock, and the project dropdown uses the selected day plus today.
