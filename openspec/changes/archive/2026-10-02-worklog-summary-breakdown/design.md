## Context

- F-01 delivered `IWorklogStore.QueryAsync(WorklogQuery)`, which returns entries that overlap a half-open interval plus structured warnings and a typed outcome. `TodayStatsService` already uses it for the single Today total.
- `TimeEntry` carries `AppName`, `ProjectTag` (nullable), `ProjectAssignmentSource`, and `Duration`. Nothing new needs to be recorded.
- The Today total is currently shown only in the tray tooltip. The Settings window is a tab control opened by `AppController.ShowSettings()`; its view model is created through a factory registered in `Program.cs`.
- Core has no UI or platform dependencies; App holds views and view models; Host wires DI. See proposal.md for motivation and scope.
- Planned later, and shaping this design: a report/details window opened from the tray, more time ranges, more filters, more groupings (for example by day), and rule-based project detection (OI-08).

## Goals / Non-Goals

**Goals:**
- One request/result model that every future report, export (OI-12), and edit list (OI-15) can reuse.
- Each planned extension (range, filter, grouping, project detection, host window) is an addition at one seam, not an edit to the service or view.
- Results that can never present a failed read as zero time.

**Non-Goals:**
- No plugin system, configuration file, or user-defined groupings. Seams are plain interfaces registered in code.
- No caching or incremental aggregation. A day of CSV entries is small; measure before adding it (see OI-09, OI-17).
- No change to `IWorklogStore`, the CSV schema, or `TodayStatsService` behavior.

## Decisions

### 1. Request and result records in Core

`WorklogSummaryRequest(Range, GroupingId, Filter)` and `WorklogSummary(Request, Outcome, Rows, Total, Warnings)`, with `SummaryRow(Key, Label, Duration, Share, EntryCount, IsUnassigned)`.

- `Range` is a `SummaryRange(StartInclusive, EndExclusive)` value, not an enum, so any future range (yesterday, week, custom) is just a different value. A small `SummaryRanges` helper builds `Today(TimeProvider)` now and can gain `Yesterday`, `ThisWeek` later. Only Today is used in the UI in this change.
- `GroupingId` is a string id, not an enum, so adding a grouping needs no change to shared types.
- `Filter` is a `SummaryFilter(Application?, Project?)` record. Adding a new filter field later is an additive change with a default of "no filter".

Alternatives: separate methods per report (`GetByApp`, `GetByProject`) would duplicate range, clipping, and diagnostics logic and grow with every new grouping. An enum for grouping would force edits in shared code for each new grouping.

### 2. Groupings as a small strategy behind a registry

```
IWorklogGrouping
  Id           "app" | "project" | (later "day")
  DisplayName  "By application" ...
  Select(entry, context) -> GroupKey(Key, Label, IsUnassigned)

WorklogGroupingRegistry  (all registered groupings, ordered, looked up by Id)
```

- The service groups by `Select`, sums clipped durations, computes shares, and sorts. It knows nothing about apps or projects.
- Built-ins: application (case-insensitive key) and project (trimmed, case-insensitive key; empty or missing goes to an Unassigned key that cannot collide with a project literally named "Unassigned").
- The view model lists `registry.All` in its grouping switch, so a new grouping appears in the UI by registering it.

Alternative: a `Func<TimeEntry,string>` per grouping is lighter but cannot carry a display name, an Unassigned flag, or a sort hint, and would be replaced as soon as day grouping needs ordering by key instead of duration. The interface can add an optional ordering member later without breaking existing groupings.

### 3. Project resolution behind its own seam

`IProjectResolver.Resolve(TimeEntry) -> string?`. The default, `StoredProjectResolver`, returns the entry's stored `ProjectTag`. The project grouping and the project filter both go through the resolver.

- OI-08 can then supply a rule-based resolver (or one composed of stored tag first, rules second) and every summary, filter, and later export picks it up with no changes to the grouping or the service.
- This keeps "what project is this entry" separate from "how do we group by it", which is the part most likely to change.

Alternative: read `ProjectTag` directly in the project grouping. Simpler today, but rule-based detection would then have to rewrite the grouping and the filter.

### 4. Filters: store where the store can, resolver for the rest

The application filter is passed to `WorklogQuery.Application`, which the store already supports case-insensitively. The project filter is applied in the service after resolving the project, because a future resolver may produce project names the stored tag does not contain. If the store is later replaced by a backend that can filter more, only this mapping changes.

### 5. Clip to the range, then aggregate

For each entry the counted duration is `min(EndedAt, range end) - max(StartedAt, range start)`, ignoring non-positive results. F-01 already splits entries at local midnight, so for Today this equals the existing total, and the same code stays correct for weekly or custom ranges. A test asserts equality with `TodayStatsService` for the same data.

### 6. Diagnostics pass through

If `QueryAsync` returns a non-success outcome, `WorklogSummary` carries that outcome and no rows; it never returns an empty successful result. Warnings from a partial read are copied to the summary. The view model turns these into distinct states: loading, ready, no data, error, ready with warnings.

### 7. UI: one host-independent view and view model

- `WorklogSummaryViewModel` (App) owns range, selected grouping, filter, rows, total, status, and a refresh command. It depends only on `IWorklogSummaryService`, the grouping registry, and `TimeProvider`. It does not reference the Settings window or its view model.
- `WorklogSummaryView` is a `UserControl` that binds to that view model only and uses design-system resources.
- Settings hosts it in a new Summary tab; `SettingsWindowViewModel` receives the summary view model and exposes it as a property. A future report window creates the same view model and puts the same view in its own layout.
- Refresh: when the tab is selected, when the user presses refresh, and when the view model is asked to reload by its host. The view model exposes a plain `RefreshAsync` so a host can subscribe it to `EntriesLoggedEvent` later. This change wires the tab-open and manual paths only.
- The view model holds the current request as one value. A later range picker or filter control only edits that value and calls refresh.

Alternative: put the properties directly on `SettingsWindowViewModel`. It is quicker but would have to be pulled apart when the report window arrives, which is the rewrite this design avoids.

### 8. Wiring and platform

Services are registered as singletons in `Program.cs` (service, registry with both groupings, stored-project resolver); the view model is transient. Everything is in Core and App, so no Windows-specific code is needed and no Linux stub is required.

## Risks / Trade-offs

- [Abstractions built for features that do not exist yet] → Keep each seam to one small interface or record with one or two real implementations now (two groupings, one resolver); write no configuration or registration framework.
- [Time shown does not include the segment that is currently running, so the total can lag the timer] → Documented as out of scope; refresh on open and manual refresh; add live segment inclusion in a later change.
- [Summary total and tray total drifting apart] → A test compares them on the same data; both use the same query and clipping rules.
- [Case-insensitive grouping merges names a user considers different] → Matches the store's existing case-insensitive filters; revisit if it causes confusion.
- [Filtering by resolved project in the service reads more entries than a store-level filter would] → Fine for single-day and 90-day CSV volumes; revisit with measurements before a storage change.
- [Reading a large range on the UI thread] → The view model calls the service asynchronously and shows the loading state.

## Open Questions

- Should the "Other" bucket for very small rows be added when the list gets long? It can be added in the view model later without changing the result model.
