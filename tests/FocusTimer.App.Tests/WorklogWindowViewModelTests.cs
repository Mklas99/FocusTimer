namespace FocusTimer.App.Tests;

using System.Reactive.Linq;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class WorklogWindowViewModelTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 5, 4);

    [Fact]
    public void Constructor_SelectsTodayOnAllTabs()
    {
        var h = new Harness();

        Assert.Equal(Today, h.Vm.SelectedDay);
        Assert.Equal("Today", h.Vm.SelectedDayText);
        Assert.Equal(Today, h.Vm.Entries.Day);
        Assert.Equal("Today", h.Vm.Summary.RangeLabel);
        Assert.Equal(WorklogTab.Entries, h.Vm.SelectedTab);
        Assert.False(h.Vm.CanSelectNext);
        Assert.True(h.Vm.CanSelectPrevious);
    }

    [Fact]
    public async Task SelectDayAsync_GivenPreviousDay_AllTabsShowThatDay()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();
        h.Store.Queries.Clear();

        await h.Vm.SelectDayAsync(Today.AddDays(-2));

        Assert.Equal(Today.AddDays(-2), h.Vm.SelectedDay);
        Assert.Equal("Saturday, 2 May 2026", h.Vm.SelectedDayText);
        Assert.Equal(Today.AddDays(-2), h.Vm.Entries.Day);
        var query = Assert.Single(h.Store.Queries);
        Assert.Equal(new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero), query.StartInclusive);
        var range = h.Vm.Summary.RangeSelector();
        Assert.Equal(new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero), range.StartInclusive);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero), range.EndExclusive);
        Assert.Equal("Sat, 2 May 2026", h.Vm.Summary.RangeLabel);
        Assert.Equal("on 2026-05-02", h.Vm.Summary.RangePhrase);
    }

    [Fact]
    public async Task SelectDayAsync_KeepsTheSummaryGrouping()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();
        h.Vm.Summary.SelectedGrouping = h.Vm.Summary.Groupings.First(g => g.Id == "window");
        await h.Vm.Summary.PendingRefresh;

        await h.Vm.SelectDayAsync(Today.AddDays(-1));
        await h.Vm.SelectTabAsync(WorklogTab.Summary);

        Assert.Equal("window", h.Vm.Summary.SelectedGrouping.Id);
        Assert.Equal("window", h.Summary.Requests[^1].GroupingId);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 0, 0, 0, TimeSpan.Zero), h.Summary.Requests[^1].Range.StartInclusive);
    }

    [Fact]
    public async Task SelectDayAsync_GivenFutureDay_DoesNotChangeTheSelection()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();
        h.Store.Queries.Clear();

        await h.Vm.SelectDayAsync(Today.AddDays(1));

        Assert.Equal(Today, h.Vm.SelectedDay);
        Assert.Empty(h.Store.Queries);
    }

    [Fact]
    public async Task SelectDayAsync_GivenDayBeforeRetentionWindow_RefusesAndNamesEarliestDay()
    {
        var h = new Harness(new Settings { DataRetentionDays = 3 });
        await h.Vm.OpenAsync();

        await h.Vm.SelectDayAsync(Today.AddDays(-3));

        Assert.Equal(Today, h.Vm.SelectedDay);
        Assert.True(h.Vm.HasDayMessage);
        Assert.Contains("2026-05-02", h.Vm.DayMessage);
        Assert.Equal(new DateOnly(2026, 5, 2), h.Vm.EarliestDay);

        await h.Vm.SelectDayAsync(Today.AddDays(-2));

        Assert.Equal(Today.AddDays(-2), h.Vm.SelectedDay);
        Assert.False(h.Vm.CanSelectPrevious);
    }

    [Fact]
    public async Task SelectDayAsync_GivenRetentionOff_AllowsAnyPastDay()
    {
        var h = new Harness(new Settings { DataRetentionDays = 0 });
        await h.Vm.OpenAsync();

        await h.Vm.SelectDayAsync(new DateOnly(2020, 1, 1));

        Assert.Equal(new DateOnly(2020, 1, 1), h.Vm.SelectedDay);
        Assert.Null(h.Vm.EarliestDay);
        Assert.True(h.Vm.CanSelectPrevious);
    }

    [Fact]
    public async Task Commands_StepBetweenDaysAndJumpBackToToday()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();

        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)h.Vm.PreviousDayCommand).Execute();
        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)h.Vm.PreviousDayCommand).Execute();
        Assert.Equal(Today.AddDays(-2), h.Vm.SelectedDay);
        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)h.Vm.NextDayCommand).Execute();
        Assert.Equal(Today.AddDays(-1), h.Vm.SelectedDay);
        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)h.Vm.TodayCommand).Execute();

        Assert.Equal(Today, h.Vm.SelectedDay);
        Assert.False(h.Vm.CanSelectNext);
    }

    [Fact]
    public async Task SelectedDate_WhenSetByThePicker_SelectsThatDay()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();

        h.Vm.SelectedDate = new DateTime(2026, 5, 1);
        await h.Vm.PendingDaySelection;

        Assert.Equal(new DateOnly(2026, 5, 1), h.Vm.SelectedDay);
        Assert.Equal(new DateTime(2026, 5, 1), h.Vm.SelectedDate);
    }

    [Fact]
    public async Task SelectTabAsync_ReloadsTheChosenTabForTheSelectedDay()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();
        h.Store.Queries.Clear();

        await h.Vm.SelectTabAsync(WorklogTab.Summary);
        Assert.Single(h.Summary.Requests);
        Assert.Empty(h.Store.Queries);

        await h.Vm.SelectTabAsync(WorklogTab.Entries);
        Assert.Single(h.Store.Queries);
        Assert.Equal(WorklogTab.Entries, h.Vm.SelectedTab);
    }

    [Fact]
    public async Task OpenAsync_ResetsToEntriesTabTodayAndLoads()
    {
        var h = new Harness();
        await h.Vm.SelectTabAsync(WorklogTab.Summary);
        await h.Vm.SelectDayAsync(Today.AddDays(-1));
        h.Store.Queries.Clear();

        await h.Vm.OpenAsync();

        Assert.Equal(WorklogTab.Entries, h.Vm.SelectedTab);
        Assert.Equal(Today, h.Vm.SelectedDay);
        Assert.Single(h.Store.Queries);
    }

    [Fact]
    public async Task ActivateAsync_ReloadsUnlessADialogIsOpen()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();
        h.Store.Queries.Clear();

        h.Vm.Entries.IsDialogOpen = true;
        await h.Vm.ActivateAsync();
        Assert.Empty(h.Store.Queries);

        h.Vm.Entries.IsDialogOpen = false;
        await h.Vm.ActivateAsync();
        Assert.Single(h.Store.Queries);
    }

    [Fact]
    public async Task RefreshAsync_PicksUpAnEntryPersistedAfterTheLastLoad()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();
        Assert.Empty(h.Vm.Entries.Rows);

        h.Store.Add(WorklogTestData.Tracked("new", new DateTimeOffset(2026, 5, 4, 9, 0, 0, TimeSpan.Zero), 30));
        await h.Vm.RefreshAsync();

        Assert.Equal(["new"], h.Vm.Entries.Rows.Select(r => r.Entry.EntryId));
    }

    [Fact]
    public async Task SelectedDay_WhenWindowStaysOpenPastMidnight_DoesNotChangeByItself()
    {
        var h = new Harness();
        await h.Vm.OpenAsync();

        h.Clock.Advance(TimeSpan.FromHours(13));
        await h.Vm.ActivateAsync();

        Assert.Equal(Today, h.Vm.SelectedDay);
        Assert.Equal(Today.AddDays(1), h.Vm.Today);
        Assert.True(h.Vm.CanSelectNext);

        await ((ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit>)h.Vm.TodayCommand).Execute();

        Assert.Equal(Today.AddDays(1), h.Vm.SelectedDay);
        Assert.Equal("Today", h.Vm.SelectedDayText);
    }

    [Fact]
    public async Task EmptyDay_ShowsEmptyStateOnEntriesAndSummary()
    {
        var h = new Harness();
        await h.Vm.SelectDayAsync(Today.AddDays(-1));
        await h.Vm.SelectTabAsync(WorklogTab.Entries);
        Assert.Equal(SummaryViewStatus.NoData, h.Vm.Entries.Status);

        await h.Vm.SelectTabAsync(WorklogTab.Summary);
        await h.Vm.Summary.RefreshAsync();

        Assert.Equal(SummaryViewStatus.NoData, h.Vm.Summary.Status);
        Assert.Equal("No time tracked on 2026-05-03.", h.Vm.Summary.StatusMessage);
    }

    private sealed class Harness
    {
        public Harness(Settings? settings = null)
        {
            this.Clock = new MutableClock(Noon);
            this.Store = new MemoryWorklogStore();
            this.Summary = new RecordingSummaryService();
            var summary = new WorklogSummaryViewModel(
                this.Summary,
                new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
                this.Clock);
            this.Vm = new WorklogWindowViewModel(
                new WorklogEntriesViewModel(this.Store, this.Clock),
                summary,
                new SettingsStub(settings),
                this.Clock);
        }

        public MutableClock Clock { get; }

        public MemoryWorklogStore Store { get; }

        public RecordingSummaryService Summary { get; }

        public WorklogWindowViewModel Vm { get; }
    }
}
