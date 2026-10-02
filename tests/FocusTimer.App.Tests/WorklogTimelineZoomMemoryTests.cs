namespace FocusTimer.App.Tests;

using FocusTimer.App.Controls;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogTimelineZoomMemoryTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadViewStateAsync_AppliesTheRememberedZoomWithoutWritingItBack()
    {
        var store = new FakeViewStateStore(new WorklogViewState(150));
        var timeline = CreateTimeline(store);

        await timeline.LoadViewStateAsync();
        await timeline.PendingSave;

        Assert.Equal(150, timeline.HourHeight);
        Assert.Equal("250%", timeline.ZoomText);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task LoadViewStateAsync_WithNothingRemembered_KeepsTheDefault()
    {
        var timeline = CreateTimeline(new FakeViewStateStore(new WorklogViewState()));

        await timeline.LoadViewStateAsync();

        Assert.Equal(WorklogTimelineViewModel.DefaultHourHeight, timeline.HourHeight);
    }

    [Theory]
    [InlineData(5, WorklogTimelineViewModel.MinHourHeight)]
    [InlineData(5000, WorklogTimelineViewModel.MaxHourHeight)]
    public async Task LoadViewStateAsync_ClampsAnOutOfRangeValue(double remembered, double expected)
    {
        var timeline = CreateTimeline(new FakeViewStateStore(new WorklogViewState(remembered)));

        await timeline.LoadViewStateAsync();

        Assert.Equal(expected, timeline.HourHeight);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task LoadViewStateAsync_IgnoresAValueThatIsNotANumber(double remembered)
    {
        var timeline = CreateTimeline(new FakeViewStateStore(new WorklogViewState(remembered)));

        await timeline.LoadViewStateAsync();

        Assert.Equal(WorklogTimelineViewModel.DefaultHourHeight, timeline.HourHeight);
    }

    [Fact]
    public async Task ChangingTheZoom_SavesTheLastValueOnceAfterTheDelay()
    {
        var store = new FakeViewStateStore(new WorklogViewState());
        var timeline = CreateTimeline(store, TimeSpan.FromMilliseconds(40));
        await timeline.LoadViewStateAsync();

        timeline.ZoomIn();
        timeline.ZoomIn();
        timeline.ZoomIn();
        Assert.Empty(store.Saved);
        await timeline.PendingSave;

        var saved = Assert.Single(store.Saved);
        Assert.Equal(timeline.HourHeight, saved.TimelineHourHeight!.Value, 3);
        Assert.Equal(60 * 1.25 * 1.25 * 1.25, saved.TimelineHourHeight!.Value, 3);
    }

    [Fact]
    public async Task ResetZoom_SavesTheDefaultSoTheNextRunStartsThere()
    {
        var store = new FakeViewStateStore(new WorklogViewState(150));
        var timeline = CreateTimeline(store, TimeSpan.Zero);
        await timeline.LoadViewStateAsync();

        timeline.ResetZoom();
        await timeline.PendingSave;

        Assert.Equal(60, Assert.Single(store.Saved).TimelineHourHeight);
    }

    [Fact]
    public async Task WithoutAStore_TheZoomStillWorksAndNothingIsSaved()
    {
        var timeline = new WorklogTimelineViewModel(new WorklogEntriesViewModel(new MemoryWorklogStore(), new MutableClock(Noon)));

        await timeline.LoadViewStateAsync();
        timeline.ZoomIn();
        await timeline.PendingSave;

        Assert.Equal(75, timeline.HourHeight, 3);
    }

    [Fact]
    public async Task AFailingStore_DoesNotBreakZoomingOrLoading()
    {
        var store = new FakeViewStateStore(new WorklogViewState()) { FailSave = true };
        var timeline = CreateTimeline(store, TimeSpan.Zero);
        await timeline.LoadViewStateAsync();

        timeline.ZoomIn();
        await timeline.PendingSave;

        Assert.Equal(75, timeline.HourHeight, 3);
    }

    [Fact]
    public async Task OpeningTheWindow_AppliesTheRememberedZoomToTheTimeline()
    {
        var clock = new MutableClock(Noon);
        var viewState = new FakeViewStateStore(new WorklogViewState(120));
        var window = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(new MemoryWorklogStore(), clock),
            new WorklogSummaryViewModel(new RecordingSummaryService(), new WorklogGroupingRegistry([new ApplicationGrouping()]), clock),
            new SettingsStub(),
            clock,
            viewState);

        await window.OpenAsync();

        Assert.Equal(120, window.Timeline.HourHeight);
        Assert.Equal(24 * 120, window.Timeline.AxisHeight);
        Assert.Empty(viewState.Saved);
    }

    [Fact]
    public async Task ARememberedZoom_SeparatesShortNeighboursLikeAnyOtherZoom()
    {
        var clock = new MutableClock(Noon);
        var store = new MemoryWorklogStore();
        store.Add(
            WorklogTestData.Tracked("a", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 2),
            WorklogTestData.Tracked("b", new DateTimeOffset(2026, 5, 4, 9, 8, 0, TimeSpan.Zero), 2));
        var window = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(store, clock),
            new WorklogSummaryViewModel(new RecordingSummaryService(), new WorklogGroupingRegistry([new ApplicationGrouping()]), clock),
            new SettingsStub(),
            clock,
            new FakeViewStateStore(new WorklogViewState(240)));

        await window.OpenAsync();

        Assert.All(window.Timeline.Blocks, b => Assert.Equal(1, b.LaneCount));
    }

    private static WorklogTimelineViewModel CreateTimeline(FakeViewStateStore store, TimeSpan? saveDelay = null) =>
        new(new WorklogEntriesViewModel(new MemoryWorklogStore(), new MutableClock(Noon)), store, saveDelay ?? TimeSpan.Zero);

    private sealed class FakeViewStateStore(WorklogViewState initial) : IWorklogViewStateStore
    {
        public List<WorklogViewState> Saved { get; } = [];

        public bool FailSave { get; set; }

        public Task<WorklogViewState> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(initial);

        public Task SaveAsync(WorklogViewState state, CancellationToken cancellationToken = default)
        {
            if (this.FailSave)
            {
                throw new IOException("disk full");
            }

            this.Saved.Add(state);
            return Task.CompletedTask;
        }
    }
}

public class HighlightMatchTests
{
    [Theory]
    [InlineData("Program.cs - FocusTimer", "prog", "0:4")]
    [InlineData("Program.cs - FocusTimer", "FOCUS timer", "13:10")]
    [InlineData("abcabc", "abc", "0:6")]
    [InlineData("alpha beta alpha", "alpha", "0:5;11:5")]
    [InlineData("nothing", "zzz", "")]
    public void FindMatches_FindsEveryOccurrenceIgnoringCase_AndMergesTouchingOnes(string text, string terms, string expected)
    {
        var matches = HighlightTextBlock.FindMatches(text, terms.Split(' '));

        Assert.Equal(expected, string.Join(';', matches.Select(m => $"{m.Start}:{m.Length}")));
    }

    [Fact]
    public void FindMatches_MergesOverlappingMatchesFromDifferentWords()
    {
        var matches = HighlightTextBlock.FindMatches("information", ["inform", "format", "mation"]);

        Assert.Equal([(0, 11)], matches);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FindMatches_GivenNoText_ReturnsNothing(string? text)
    {
        Assert.Empty(HighlightTextBlock.FindMatches(text, ["a"]));
    }

    [Fact]
    public void FindMatches_IgnoresBlankAndMissingTerms()
    {
        Assert.Empty(HighlightTextBlock.FindMatches("text", ["", "  "]));
        Assert.Empty(HighlightTextBlock.FindMatches("text", null));
        Assert.Empty(HighlightTextBlock.FindMatches("text", []));
    }
}
