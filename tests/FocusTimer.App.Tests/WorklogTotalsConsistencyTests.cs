namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>
/// Wires the real editing service, summary service, and tray total to one store and checks that the Summary
/// tab and the tray "Today" total never disagree after the user changes today's worklog.
/// </summary>
public class WorklogTotalsConsistencyTests
{
    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SummaryTotalAndTrayTotal_StayEqualAcrossAddEditAndDelete()
    {
        var clock = new MutableClock(Noon);
        var store = new MemoryWorklogStore();
        store.Add(WorklogTestData.Tracked("tracked", new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero), 30));
        var bus = new EventBus();
        var stats = new TodayStatsService(store, new Log(), clock);
        Task trayRefresh = Task.CompletedTask;
        bus.Subscribe<WorklogChangedEvent>(e =>
        {
            if (e.IsToday)
            {
                trayRefresh = stats.RefreshTodayAsync();
            }
        });
        var identity = new InstallationIdentity();
        identity.Initialize("device-1");
        var editing = new WorklogEditingService(store, new SettingsStub(), identity, new SourcePlatformProvider(), clock, bus, new Log());
        var summaryService = new WorklogSummaryService(
            store,
            new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
            new StoredProjectResolver(),
            new Log());
        var window = new WorklogWindowViewModel(
            new WorklogEntriesViewModel(store, clock, editing),
            new WorklogSummaryViewModel(
                summaryService,
                new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
                clock),
            new SettingsStub(),
            clock);
        await stats.RefreshTodayAsync();
        await window.OpenAsync();
        await window.SelectTabAsync(WorklogTab.Summary);
        Assert.Equal("0h 30m", window.Summary.TotalText);
        Assert.Equal(TimeSpan.FromMinutes(30), stats.GetTodayTotal());

        var added = await editing.AddManualAsync(new ManualEntryRequest(new DateOnly(2026, 5, 4), new TimeOnly(9, 0), TimeSpan.FromMinutes(45), "Planning", "Alpha"));
        await trayRefresh;
        await window.RefreshAsync();
        Assert.True(added.IsSuccess, added.Message);
        await AssertTotalsEqualAsync(window, summaryService, stats, TimeSpan.FromMinutes(75));
        Assert.Equal("1h 15m", window.Summary.TotalText);

        var manual = store.Entries.Single(e => e.CaptureSource == CaptureSource.Manual);
        var edited = await editing.UpdateAsync(manual, new WorklogEntryEdit("Planning", "Alpha", TimeSpan.FromMinutes(20)));
        await trayRefresh;
        await window.RefreshAsync();
        Assert.True(edited.IsSuccess, edited.Message);
        await AssertTotalsEqualAsync(window, summaryService, stats, TimeSpan.FromMinutes(50));

        var deleted = await editing.DeleteAsync(store.Entries.Single(e => e.CaptureSource == CaptureSource.Manual));
        await trayRefresh;
        await window.RefreshAsync();
        Assert.True(deleted.IsSuccess, deleted.Message);
        await AssertTotalsEqualAsync(window, summaryService, stats, TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task ChangesToAnotherDay_DoNotTouchTheTrayTotal()
    {
        var clock = new MutableClock(Noon);
        var store = new MemoryWorklogStore();
        var bus = new EventBus();
        var events = new List<WorklogChangedEvent>();
        bus.Subscribe<WorklogChangedEvent>(events.Add);
        var identity = new InstallationIdentity();
        identity.Initialize("device-1");
        var editing = new WorklogEditingService(store, new SettingsStub(), identity, new SourcePlatformProvider(), clock, bus, new Log());

        await editing.AddManualAsync(new ManualEntryRequest(new DateOnly(2026, 5, 2), new TimeOnly(9, 0), TimeSpan.FromHours(1), null, null));

        var changed = Assert.Single(events);
        Assert.False(changed.IsToday);
        Assert.Equal(new DateOnly(2026, 5, 2), changed.Day);
    }

    private static async Task AssertTotalsEqualAsync(
        WorklogWindowViewModel window,
        WorklogSummaryService summaryService,
        TodayStatsService stats,
        TimeSpan expected)
    {
        var summary = await summaryService.SummarizeAsync(new WorklogSummaryRequest(window.Summary.RangeSelector(), "app"));
        Assert.Equal(expected, summary.Total);
        Assert.Equal(summary.Total, stats.GetTodayTotal());
        await window.SelectTabAsync(WorklogTab.Summary);
        Assert.Equal(SummaryFormatting.Duration(expected), window.Summary.TotalText);
    }

    private sealed class Log : FocusTimer.Core.Interfaces.IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) { }
    }
}
