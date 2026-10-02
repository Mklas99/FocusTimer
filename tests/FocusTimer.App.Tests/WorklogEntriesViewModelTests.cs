namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Models;

public class WorklogEntriesViewModelTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RefreshAsync_GivenEntries_ShowsThemInStartOrderWithStoredValues()
    {
        var store = new MemoryWorklogStore();
        store.Add(
            WorklogTestData.Tracked("late", new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), 30, "Chrome", "Mail"),
            WorklogTestData.Tracked("early", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 90, "Code", "Program.cs"));
        var vm = Create(store);

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Ready, vm.Status);
        Assert.Equal(["early", "late"], vm.Rows.Select(r => r.Entry.EntryId));
        var first = vm.Rows[0];
        Assert.Equal("09:00:00", first.StartText);
        Assert.Equal("10:30:00", first.EndText);
        Assert.Equal("1:30:00", first.DurationText);
        Assert.Equal("Code", first.ApplicationText);
        Assert.Equal("Program.cs", first.WindowText);
        Assert.Equal("Tracked", first.SourceText);
        Assert.Equal("—", first.ProjectText);
        Assert.Equal(vm.Rows.Select(r => r.Entry), vm.LoadedEntries);
    }

    [Fact]
    public async Task RefreshAsync_UsesTheLocalDayRangeOfTheShownDay()
    {
        var store = new MemoryWorklogStore();
        var vm = Create(store);
        vm.SetDay(new DateOnly(2026, 5, 2));

        await vm.RefreshAsync();

        var query = Assert.Single(store.Queries);
        Assert.Equal(new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero), query.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero), query.EndExclusive);
    }

    [Fact]
    public async Task RefreshAsync_GivenNoEntries_ShowsEmptyStateNotError()
    {
        var vm = Create(new MemoryWorklogStore());

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.NoData, vm.Status);
        Assert.True(vm.IsMessageVisible);
        Assert.Equal("No entries for this day.", vm.StatusMessage);
        Assert.Empty(vm.Rows);
    }

    [Fact]
    public async Task RefreshAsync_GivenUnreadableDay_ShowsErrorAndNotAnEmptyTable()
    {
        var store = new MemoryWorklogStore { NextQueryOutcome = new WorklogOutcome(WorklogOutcomeKind.UnsupportedSchema, "Unsupported schema.") };
        var vm = Create(store);

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Error, vm.Status);
        Assert.Equal("Unsupported schema.", vm.StatusMessage);
        Assert.False(vm.IsReady);
        Assert.Empty(vm.LoadedEntries);
    }

    [Fact]
    public async Task RefreshAsync_GivenReadWarnings_ShowsValidEntriesAndWarning()
    {
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 30));
        var warnings = new[] { new WorklogWarning("file", 3, "bad"), new WorklogWarning("file", 5, "bad") };
        var vm = new WorklogEntriesViewModel(new WarningStore(store, warnings), new MutableClock(Noon));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Ready, vm.Status);
        Assert.Single(vm.Rows);
        Assert.True(vm.HasWarning);
        Assert.Contains("2 worklog record(s)", vm.WarningText);
    }

    [Fact]
    public async Task RefreshAsync_GivenOnlyUnreadableRecords_ShowsNoReadableEntriesWithWarning()
    {
        var store = new WarningStore(new MemoryWorklogStore(), [new WorklogWarning("file", 2, "bad")]);
        var vm = new WorklogEntriesViewModel(store, new MutableClock(Noon));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.NoData, vm.Status);
        Assert.Equal("No readable entries for this day.", vm.StatusMessage);
        Assert.True(vm.HasWarning);
    }

    [Fact]
    public async Task RefreshAsync_GivenTitleWithCommasQuotesAndLineBreaks_ShowsItIntact()
    {
        const string title = "Report, \"final\"\r\nversion 2";
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 30, window: title));
        var vm = Create(store);

        await vm.RefreshAsync();

        Assert.Equal(title, Assert.Single(vm.Rows).WindowText);
    }

    [Fact]
    public async Task RefreshAsync_GivenManualEntry_MarksItManualWithDetails()
    {
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("m", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 45, "Manual entry", string.Empty)
            with { CaptureSource = CaptureSource.Manual, ProjectTag = "Alpha", ProjectAssignmentSource = ProjectAssignmentSource.Editor });
        var vm = Create(store);

        await vm.RefreshAsync();

        var row = Assert.Single(vm.Rows);
        Assert.True(row.IsManual);
        Assert.Equal("Manual", row.SourceText);
        Assert.Equal("—", row.WindowText);
        Assert.Equal("Alpha", row.ProjectText);
        Assert.Equal("m", row.EntryIdText);
        Assert.Equal("1", row.RevisionText);
        Assert.Equal("editor", row.ProjectSourceText);
        Assert.Contains("2026-05-04", row.LastModifiedText);
    }

    [Fact]
    public async Task RefreshAsync_WhenNewerReloadStarts_OnlyTheLatestResultIsShown()
    {
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("fresh", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 30));
        var slow = new TaskCompletionSource<WorklogReadResult>();
        store.NextQueryOverride = slow.Task;
        var vm = Create(store);

        var first = vm.RefreshAsync();
        var second = vm.RefreshAsync();
        await second;
        slow.SetResult(new WorklogReadResult(
            WorklogOutcome.Success(),
            [WorklogTestData.Tracked("stale", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 10)]));
        await first;

        Assert.Equal(["fresh"], vm.Rows.Select(r => r.Entry.EntryId));
        Assert.Equal(SummaryViewStatus.Ready, vm.Status);
    }

    [Fact]
    public async Task RefreshAsync_KeepsTheSelectedRowWhenItStillExists()
    {
        var store = new MemoryWorklogStore();
        store.Add(
            WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 30),
            WorklogTestData.Tracked("b", new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero), 30));
        var vm = Create(store);
        await vm.RefreshAsync();
        vm.SelectedRow = vm.Rows[1];

        await vm.RefreshAsync();

        Assert.Equal("b", vm.SelectedRow?.Entry.EntryId);
        Assert.True(vm.HasSelection);
    }

    [Fact]
    public async Task RefreshAsync_WhenStoreThrows_ShowsError()
    {
        var vm = new WorklogEntriesViewModel(new ThrowingStore(), new MutableClock(Noon));

        await vm.RefreshAsync();

        Assert.Equal(SummaryViewStatus.Error, vm.Status);
        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
    }

    private static WorklogEntriesViewModel Create(MemoryWorklogStore store) => new(store, new MutableClock(Noon));

    private sealed class WarningStore(FocusTimer.Core.Interfaces.IWorklogStore inner, IReadOnlyList<WorklogWarning> warnings)
        : FocusTimer.Core.Interfaces.IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) =>
            inner.AppendAsync(entries, cancellationToken);

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            inner.GetAsync(entryId, cancellationToken);

        public async Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            var read = await inner.QueryAsync(query, cancellationToken);
            return read with { Outcome = WorklogOutcome.Success(warnings) };
        }

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            inner.PatchAsync(entryId, expectedRevision, patch, cancellationToken);

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) =>
            inner.DeleteAsync(entryId, expectedRevision, cancellationToken);
    }

    private sealed class ThrowingStore : FocusTimer.Core.Interfaces.IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) => throw new IOException();

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) => throw new IOException();

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) => throw new IOException("down");

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) => throw new IOException();

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) => throw new IOException();
    }
}
