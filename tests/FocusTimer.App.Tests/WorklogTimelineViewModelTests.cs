namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Models;

public class WorklogTimelineViewModelTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Blocks_AreInStartOrderWithPositionsFromTheirTimes()
    {
        var h = await Harness.CreateAsync(
            Entry("late", 14, 0, 30),
            Entry("early", 8, 30, 90));

        var blocks = h.Timeline.Blocks;

        Assert.Equal(["early", "late"], blocks.Select(b => b.Row.Entry.EntryId));
        Assert.Equal(8.5 * 60, blocks[0].StartMinute);
        Assert.Equal(90, blocks[0].LengthMinutes);
        Assert.Equal("08:30–10:00", blocks[0].TimeText);
        Assert.True(h.Timeline.HasBlocks);
    }

    [Fact]
    public async Task Gaps_AreEmptySpaceBetweenBlocksInTheSameLane()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30), Entry("b", 11, 0, 30));

        var (a, b) = (h.Timeline.Blocks[0], h.Timeline.Blocks[1]);

        Assert.Equal((0, 1), (a.Lane, a.LaneCount));
        Assert.Equal((0, 1), (b.Lane, b.LaneCount));
        Assert.Equal(90, b.StartMinute - (a.StartMinute + a.LengthMinutes));
    }

    [Fact]
    public async Task OverlappingEntries_ShareTheWidthAndNoneIsHidden()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 60), Entry("b", 9, 30, 60), Entry("c", 10, 5, 30));

        var blocks = h.Timeline.Blocks;

        Assert.Equal(3, blocks.Count);
        Assert.Equal([0, 1, 0], blocks.Select(b => b.Lane));
        Assert.All(blocks, b => Assert.Equal(2, b.LaneCount));
    }

    [Fact]
    public async Task EntriesThatOnlyTouch_StayInTheSameLane()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 60), Entry("b", 10, 0, 60));

        Assert.All(h.Timeline.Blocks, b => Assert.Equal((0, 1), (b.Lane, b.LaneCount)));
    }

    [Fact]
    public async Task VeryShortNeighbours_AreSeparatedSoTheyStayVisible()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 2), Entry("b", 9, 5, 2));

        Assert.Equal([0, 1], h.Timeline.Blocks.Select(b => b.Lane));
        Assert.All(h.Timeline.Blocks, b => Assert.Equal(2, b.LaneCount));
    }

    [Fact]
    public async Task OverlapGroups_AreIndependent()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 60), Entry("b", 9, 30, 60), Entry("c", 13, 0, 60));

        Assert.Equal([2, 2, 1], h.Timeline.Blocks.Select(b => b.LaneCount));
    }

    [Fact]
    public async Task ManualEntry_IsMarkedByLabelAndFlagNotJustColor()
    {
        var manual = Entry("m", 10, 0, 45, "Manual entry", "Planning") with { CaptureSource = CaptureSource.Manual };
        var h = await Harness.CreateAsync(Entry("t", 9, 0, 30, "Code", "Program.cs"), manual);

        var tracked = h.Timeline.Blocks[0];
        var block = h.Timeline.Blocks[1];

        Assert.False(tracked.IsManual);
        Assert.Equal("Code", tracked.TitleText);
        Assert.True(block.IsManual);
        Assert.StartsWith("Manual", block.TitleText);
        Assert.Contains("Planning", block.TitleText);
        Assert.Contains("Manual", block.ToolTipText);
        Assert.Contains("Tracked", tracked.ToolTipText);
    }

    [Fact]
    public async Task EmptyDay_HasNoBlocksAndTheEntriesTabShowsTheEmptyState()
    {
        var h = await Harness.CreateAsync();

        Assert.False(h.Timeline.HasBlocks);
        Assert.Empty(h.Timeline.Blocks);
        Assert.Equal(SummaryViewStatus.NoData, h.Timeline.Entries.Status);
        Assert.Equal("No entries for this day.", h.Timeline.Entries.StatusMessage);
        Assert.Equal(8 * 60, h.Timeline.ScrollTargetMinute);
    }

    [Fact]
    public async Task ReadWarnings_AreTheSameAsOnTheEntriesTab()
    {
        var store = new MemoryWorklogStore();
        store.Add(Entry("a", 9, 0, 30));
        var warnings = new[] { new WorklogWarning("f", 2, "bad") };
        var entries = new WorklogEntriesViewModel(new WarningStore(store, warnings), new MutableClock(Noon));
        entries.SetDay(new DateOnly(2026, 5, 4));
        var timeline = new WorklogTimelineViewModel(entries);

        await entries.RefreshAsync();

        Assert.True(timeline.Entries.HasWarning);
        Assert.Equal(entries.WarningText, timeline.Entries.WarningText);
        Assert.Single(timeline.Blocks);
    }

    [Fact]
    public async Task ScrollTarget_IsHalfAnHourBeforeTheFirstEntry_AndNeverNegative()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30), Entry("b", 6, 10, 30));
        Assert.Equal((6 * 60) + 10 - 30, h.Timeline.ScrollTargetMinute);

        var early = await Harness.CreateAsync(Entry("a", 0, 10, 30));
        Assert.Equal(0, early.Timeline.ScrollTargetMinute);
    }

    [Fact]
    public async Task OneReload_UpdatesTheEntriesTableAndTheTimelineTogether()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30));
        Assert.Single(h.Timeline.Blocks);

        h.Store.Add(Entry("b", 11, 0, 30), Entry("c", 12, 0, 30));
        await h.Window.RefreshAsync();

        Assert.Equal(3, h.Window.Entries.Rows.Count);
        Assert.Equal(h.Window.Entries.Rows.Select(r => r.Entry.EntryId), h.Timeline.Blocks.Select(b => b.Row.Entry.EntryId));
        Assert.Same(h.Window.Entries, h.Timeline.Entries);
    }

    [Fact]
    public async Task SelectingTheTimelineTab_LoadsTheSelectedDay()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30));
        h.Store.Queries.Clear();
        h.Store.Add(Entry("b", 11, 0, 30));

        await h.Window.SelectTabAsync(WorklogTab.Timeline);

        Assert.Single(h.Store.Queries);
        Assert.Equal(2, h.Timeline.Blocks.Count);
        Assert.Equal(WorklogTab.Timeline, h.Window.SelectedTab);
    }

    [Fact]
    public async Task Search_DoesNotRemoveBlocksFromTheTimeline()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30, "Code"), Entry("b", 10, 0, 30, "Chrome"));

        h.Window.Entries.SearchText = "code";

        Assert.Single(h.Window.Entries.Rows);
        Assert.Equal(2, h.Timeline.Blocks.Count);
    }

    [Fact]
    public async Task Zoom_StepsBetweenTheLimitsAndShowsAPercentage()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30));
        Assert.Equal(60, h.Timeline.HourHeight);
        Assert.Equal("100%", h.Timeline.ZoomText);
        Assert.Equal(24 * 60, h.Timeline.AxisHeight);

        h.Timeline.ZoomIn();
        Assert.Equal(75, h.Timeline.HourHeight, 3);
        Assert.Equal("125%", h.Timeline.ZoomText);
        Assert.Equal(24 * 75, h.Timeline.AxisHeight, 3);

        h.Timeline.ZoomOut();
        h.Timeline.ZoomOut();
        Assert.Equal(48, h.Timeline.HourHeight, 3);

        h.Timeline.ResetZoom();
        Assert.Equal(60, h.Timeline.HourHeight);

        for (var i = 0; i < 30; i++)
        {
            h.Timeline.ZoomIn();
        }

        Assert.Equal(WorklogTimelineViewModel.MaxHourHeight, h.Timeline.HourHeight);
        Assert.False(h.Timeline.CanZoomIn);
        Assert.True(h.Timeline.CanZoomOut);

        for (var i = 0; i < 40; i++)
        {
            h.Timeline.ZoomOut();
        }

        Assert.Equal(WorklogTimelineViewModel.MinHourHeight, h.Timeline.HourHeight);
        Assert.False(h.Timeline.CanZoomOut);
    }

    [Fact]
    public async Task Zoom_CommandsDriveTheSameScale()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 30));

        h.Timeline.ZoomInCommand.Execute(null);
        h.Timeline.ZoomInCommand.Execute(null);
        await Task.Delay(1);

        Assert.True(h.Timeline.HourHeight > 60);
        h.Timeline.ResetZoomCommand.Execute(null);
        await Task.Delay(1);
        Assert.Equal(60, h.Timeline.HourHeight);
    }

    [Fact]
    public async Task Zoom_SeparatesVeryShortNeighboursThatOverlappedAtTheDefaultScale()
    {
        var h = await Harness.CreateAsync(Entry("a", 9, 0, 2), Entry("b", 9, 8, 2));
        Assert.Equal([0, 1], h.Timeline.Blocks.Select(b => b.Lane));

        h.Timeline.HourHeight = 240;

        Assert.Equal([0, 0], h.Timeline.Blocks.Select(b => b.Lane));
        Assert.All(h.Timeline.Blocks, b => Assert.Equal(1, b.LaneCount));
    }

    [Theory]
    [InlineData(0, 100, 60, 120, 100)]
    [InlineData(600, 50, 60, 120, 1250)]
    [InlineData(0, 0, 60, 30, 0)]
    public void AnchoredOffset_KeepsTheMomentUnderThePointerInPlace(double offset, double pointerY, double oldH, double newH, double expected)
    {
        var next = WorklogTimelineViewModel.AnchoredOffset(offset, pointerY, oldH, newH);

        Assert.Equal(expected, next, 3);
        Assert.Equal(((offset + pointerY) / oldH) * 60, ((next + pointerY) / newH) * 60, 3);
    }

    [Fact]
    public void AnchoredOffset_NeverGoesNegative()
    {
        Assert.Equal(0, WorklogTimelineViewModel.AnchoredOffset(10, 200, 120, 30));
    }

    [Fact]
    public async Task ChangingTheDay_RedrawsTheTimelineForThatDay()
    {
        var h = await Harness.CreateAsync(Entry("today", 9, 0, 30));
        h.Store.Add(WorklogTestData.Tracked("yesterday", new DateTimeOffset(2026, 5, 3, 15, 0, 0, TimeSpan.Zero), 60));

        await h.Window.SelectDayAsync(new DateOnly(2026, 5, 3));

        Assert.Equal(["yesterday"], h.Timeline.Blocks.Select(b => b.Row.Entry.EntryId));
        Assert.Equal(15 * 60, h.Timeline.Blocks[0].StartMinute);
    }

    private static TimeEntry Entry(string id, int hour, int minute, int length, string app = "Code", string window = "Window") =>
        WorklogTestData.Tracked(id, new DateTimeOffset(2026, 5, 4, hour, minute, 0, TimeSpan.Zero), length, app, window);

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

        public static async Task<Harness> CreateAsync(params TimeEntry[] entries)
        {
            var clock = new MutableClock(Noon);
            var store = new MemoryWorklogStore();
            store.Add(entries);
            var window = new WorklogWindowViewModel(
                new WorklogEntriesViewModel(store, clock),
                new WorklogSummaryViewModel(
                    new RecordingSummaryService(),
                    new FocusTimer.Core.Services.WorklogGroupingRegistry([new FocusTimer.Core.Services.ApplicationGrouping()]),
                    clock),
                new SettingsStub(),
                clock);
            await window.OpenAsync();
            return new Harness(window, store);
        }
    }

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
}
