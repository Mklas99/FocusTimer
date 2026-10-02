namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogTimelineGroupingTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Options_AreAllTogetherPlusTheSummaryGroupings()
    {
        var h = await Harness.CreateAsync();

        Assert.Equal(
            ["All entries together", "By application", "By project", "By window"],
            h.Timeline.GroupingOptions.Select(o => o.DisplayName));
        Assert.Equal(WorklogTimelineViewModel.NoGroupingId, h.Timeline.SelectedGrouping.Id);
    }

    [Fact]
    public void WithoutGroupings_OnlyTheUngroupedChoiceExists()
    {
        var entries = new WorklogEntriesViewModel(new MemoryWorklogStore(), new MutableClock(Noon));

        var timeline = new WorklogTimelineViewModel(entries);

        Assert.Single(timeline.GroupingOptions);
    }

    [Fact]
    public async Task Ungrouped_IsOneColumnWithoutHeaders()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "Chrome"));

        Assert.False(h.Timeline.IsGrouped);
        Assert.False(h.Timeline.HasGroups);
        Assert.Empty(h.Timeline.Groups);
        Assert.Equal(1, h.Timeline.ColumnCount);
        Assert.All(h.Timeline.Blocks, b => Assert.Equal((0, 1), (b.Column, b.ColumnCount)));
    }

    [Fact]
    public async Task ByApplication_MakesOneColumnPerApplication_LargestTotalFirst()
    {
        var h = await Harness.CreateAsync(
            Entry("a", 9, 0, 30, "Chrome"),
            Entry("b", 10, 0, 90, "Code"),
            Entry("c", 12, 0, 15, "Chrome"),
            Entry("d", 13, 0, 10, "Slack"));

        h.Select("app");

        Assert.True(h.Timeline.IsGrouped);
        Assert.Equal(["Code", "Chrome", "Slack"], h.Timeline.Groups.Select(g => g.Label));
        Assert.Equal(["1h 30m", "0h 45m", "0h 10m"], h.Timeline.Groups.Select(g => g.TotalText));
        Assert.Equal(["1 entry", "2 entries", "1 entry"], h.Timeline.Groups.Select(g => g.CountText));
        Assert.Equal([0, 1, 2], h.Timeline.Groups.Select(g => g.Index));
        Assert.Equal(3, h.Timeline.ColumnCount);
        var columnOf = h.Timeline.Blocks.ToDictionary(b => b.Row.Entry.EntryId, b => b.Column);
        Assert.Equal((1, 0, 1, 2), (columnOf["a"], columnOf["b"], columnOf["c"], columnOf["d"]));
        Assert.Equal(["a", "b", "c", "d"], h.Timeline.Blocks.Select(b => b.Row.Entry.EntryId));
        Assert.All(h.Timeline.Blocks, b => Assert.Equal(3, b.ColumnCount));
    }

    [Fact]
    public async Task ByApplication_MergesNamesThatDifferOnlyByCase()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "code"));

        h.Select("app");

        Assert.Equal("Code", Assert.Single(h.Timeline.Groups).Label);
        Assert.Equal("2 entries", h.Timeline.Groups[0].CountText);
    }

    [Fact]
    public async Task ByProject_PutsEntriesWithoutAProjectInTheLastColumn()
    {
        var h = await Harness.CreateAsync(
            Entry("a", 9, 0, 120, "Code"),
            Entry("b", 12, 0, 30, "Code", project: "Alpha"),
            Entry("c", 13, 0, 20, "Code", project: "Beta"));

        h.Select("project");

        Assert.Equal(["Alpha", "Beta", "Unassigned (no project)"], h.Timeline.Groups.Select(g => g.Label));
        Assert.True(h.Timeline.Groups[^1].IsUnassigned);
        Assert.Equal("2h 00m", h.Timeline.Groups[^1].TotalText);
    }

    [Fact]
    public async Task ByProject_UsesTheProjectResolver()
    {
        var h = await Harness.CreateAsync(
            new FixedResolver(entry => entry.AppName == "Code" ? "Resolved" : null),
            [Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "Chrome")]);

        h.Select("project");

        Assert.Equal(["Resolved", "Unassigned (no project)"], h.Timeline.Groups.Select(g => g.Label));
    }

    [Fact]
    public async Task ByWindow_GroupsEmptyTitlesApart()
    {
        var h = await Harness.CreateAsync(
            Entry("a", 9, 0, 30, "Code", "Program.cs"),
            Entry("b", 10, 0, 30, "Manual entry", string.Empty));

        h.Select("window");

        Assert.Equal(["Program.cs", "No window title"], h.Timeline.Groups.Select(g => g.Label));
    }

    [Fact]
    public async Task EqualTotals_AreOrderedByLabel()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Zed"), Entry("b", 10, 0, 30, "alpha"));

        h.Select("app");

        Assert.Equal(["alpha", "Zed"], h.Timeline.Groups.Select(g => g.Label));
    }

    [Fact]
    public async Task OverlapsAreOnlyLaneSplitInsideAColumn()
    {
        var h = await Harness.CreateAsync(
            Entry("code1", 9, 0, 60, "Code"),
            Entry("code2", 9, 30, 60, "Code"),
            Entry("chrome", 9, 15, 60, "Chrome"));

        h.Select("app");

        var byId = h.Timeline.Blocks.ToDictionary(b => b.Row.Entry.EntryId);
        Assert.Equal((0, 0), (byId["code1"].Column, byId["code1"].Lane));
        Assert.Equal((0, 1), (byId["code2"].Column, byId["code2"].Lane));
        Assert.Equal(2, byId["code1"].LaneCount);
        Assert.Equal((1, 0, 1), (byId["chrome"].Column, byId["chrome"].Lane, byId["chrome"].LaneCount));
    }

    [Fact]
    public async Task SwitchingBack_RestoresASingleColumn()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "Chrome"));
        h.Select("app");
        Assert.Equal(2, h.Timeline.ColumnCount);

        h.Select(WorklogTimelineViewModel.NoGroupingId);

        Assert.False(h.Timeline.IsGrouped);
        Assert.Empty(h.Timeline.Groups);
        Assert.Equal(1, h.Timeline.ColumnCount);
        Assert.All(h.Timeline.Blocks, b => Assert.Equal(0, b.Column));
    }

    [Fact]
    public async Task Grouping_SurvivesZoomSearchReloadAndDayChanges()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "Chrome"));
        h.Select("app");

        h.Timeline.ZoomIn();
        Assert.Equal(2, h.Timeline.Groups.Count);

        h.Window.Entries.SearchText = "code";
        Assert.Equal(2, h.Timeline.Groups.Count);
        Assert.Equal(2, h.Timeline.Blocks.Count);
        h.Window.Entries.SearchText = string.Empty;

        h.Store.Add(Entry("c", 11, 0, 30, "Slack"));
        await h.Window.RefreshAsync();
        Assert.Equal(3, h.Timeline.Groups.Count);

        h.Store.Add(WorklogTestData.Tracked("old", new DateTimeOffset(2026, 5, 3, 15, 0, 0, TimeSpan.Zero), 60, "Terminal"));
        await h.Window.SelectDayAsync(new DateOnly(2026, 5, 3));

        Assert.Equal(["Terminal"], h.Timeline.Groups.Select(g => g.Label));
        Assert.Equal("app", h.Timeline.SelectedGrouping.Id);
    }

    [Fact]
    public async Task ShortNeighbours_SeparateWhenZoomedInInsideTheirColumn()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 2, "Code"), Entry("b", 9, 8, 2, "Code"));
        h.Select("app");
        Assert.Equal([0, 1], h.Timeline.Blocks.Select(b => b.Lane));

        h.Timeline.HourHeight = 240;

        Assert.All(h.Timeline.Blocks, b => Assert.Equal(1, b.LaneCount));
    }

    [Fact]
    public async Task SelectingAnOptionFromAnotherSource_IsIgnored()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Code"));

        h.Timeline.SelectedGrouping = new GroupingOption("nonsense", "Nonsense");

        Assert.False(h.Timeline.IsGrouped);
    }

    [Fact]
    public async Task RememberedGrouping_IsAppliedOnOpenAndNotWrittenBack()
    {
        var viewState = new FakeViewState(new WorklogViewState(120, "app"));
        var h = await Harness.CreateAsync(viewState, Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "Chrome"));

        Assert.Equal("app", h.Timeline.SelectedGrouping.Id);
        Assert.Equal(120, h.Timeline.HourHeight);
        Assert.Equal(2, h.Timeline.Groups.Count);
        Assert.Empty(viewState.Saved);
    }

    [Theory]
    [InlineData("APP", "app")]
    [InlineData("window", "window")]
    [InlineData("no-such-grouping", "")]
    [InlineData("", "")]
    public async Task RememberedGrouping_IgnoresCaseAndUnknownIds(string remembered, string expectedId)
    {
        var viewState = new FakeViewState(new WorklogViewState(null, remembered));

        var h = await Harness.CreateAsync(viewState, Entry("a", 9, 0, 30, "Code"));

        Assert.Equal(expectedId, h.Timeline.SelectedGrouping.Id);
    }

    [Fact]
    public async Task ChangingTheGrouping_SavesItWithTheZoom_AndAllTogetherSavesNone()
    {
        var viewState = new FakeViewState(new WorklogViewState(90, null));
        var h = await Harness.CreateAsync(viewState, Entry("a", 9, 0, 30, "Code"));

        h.Select("project");
        await h.Timeline.PendingSave;

        var saved = Assert.Single(viewState.Saved);
        Assert.Equal("project", saved.TimelineGroupingId);
        Assert.Equal(90, saved.TimelineHourHeight);

        h.Select(WorklogTimelineViewModel.NoGroupingId);
        await h.Timeline.PendingSave;

        Assert.Null(viewState.Saved[^1].TimelineGroupingId);
        Assert.Equal(2, viewState.Saved.Count);
    }

    [Fact]
    public void LayoutGroups_WithoutAGrouping_GivesNoGroups()
    {
        var rows = new[] { new WorklogEntryRowViewModel(Entry("a", 9, 0, 30, "Code")) };

        var layout = WorklogTimelineViewModel.LayoutGroups(rows, 60, null);

        Assert.Empty(layout.Groups);
        Assert.Single(layout.Blocks);
    }

    private static TimeEntry Entry(string id, int hour, int minute, int length, string app = "Code", string window = "Window", string? project = null) =>
        WorklogTestData.Tracked(id, new DateTimeOffset(2026, 5, 4, hour, minute, 0, TimeSpan.Zero), length, app, window) with { ProjectTag = project };

    private sealed class FixedResolver(Func<TimeEntry, string?> resolve) : IProjectResolver
    {
        public string? Resolve(TimeEntry entry) => resolve(entry);
    }

    private sealed class FakeViewState(WorklogViewState initial) : IWorklogViewStateStore
    {
        public List<WorklogViewState> Saved { get; } = [];

        public Task<WorklogViewState> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(initial);

        public Task SaveAsync(WorklogViewState state, CancellationToken cancellationToken = default)
        {
            this.Saved.Add(state);
            return Task.CompletedTask;
        }
    }

    private sealed class Harness
    {
        private Harness(WorklogWindowViewModel window, MemoryWorklogStore store)
        {
            this.Window = window;
            this.Store = store;
        }

        public WorklogWindowViewModel Window { get; }

        public MemoryWorklogStore Store { get; }

        public WorklogTimelineViewModel Timeline => this.Window.Timeline;

        public static Task<Harness> CreateAsync(params TimeEntry[] entries) => CreateAsync(null, null, entries);

        public static Task<Harness> CreateAsync(IWorklogViewStateStore? viewState, params TimeEntry[] entries) => CreateAsync(viewState, null, entries);

        public static async Task<Harness> CreateAsync(IWorklogViewStateStore? viewState, IProjectResolver? resolver, TimeEntry[] entries)
        {
            var clock = new MutableClock(Noon);
            var store = new MemoryWorklogStore();
            store.Add(entries);
            var registry = new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]);
            var window = new WorklogWindowViewModel(
                new WorklogEntriesViewModel(store, clock),
                new WorklogSummaryViewModel(new RecordingSummaryService(), registry, clock),
                new SettingsStub(),
                clock,
                viewState,
                registry,
                resolver);
            await window.OpenAsync();
            return new Harness(window, store);
        }

        public static Task<Harness> CreateAsync(IProjectResolver resolver, TimeEntry[] entries) => CreateAsync(null, resolver, entries);

        public void Select(string id) => this.Timeline.SelectedGrouping = this.Timeline.GroupingOptions.First(o => o.Id == id);
    }
}
