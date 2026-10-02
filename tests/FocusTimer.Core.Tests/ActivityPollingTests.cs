#pragma warning disable
namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class ActivityPollingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(60)]
    public async Task ImmediateStartAndDueBoundary(int interval)
    {
        var (tracker, clock, windows) = Create();
        tracker.SetPollingInterval(interval);
        await tracker.StartAsync(null);
        Assert.Equal(1, windows.Calls);
        clock.Advance(interval - 1);
        await tracker.OnTimerTickAsync();
        Assert.Equal(1, windows.Calls);
        clock.Advance(1);
        await tracker.OnTimerTickAsync();
        Assert.Equal(2, windows.Calls);
        clock.Advance(1000);
        await tracker.OnTimerTickAsync();
        await tracker.OnTimerTickAsync();
        Assert.Equal(3, windows.Calls);
    }

    [Fact]
    public async Task IntervalChangeDoesNotSplitAndSameValueDoesNotReschedule()
    {
        var (tracker, clock, windows) = Create();
        await tracker.StartAsync("project");
        clock.Advance(5);
        tracker.SetPollingInterval(10);
        clock.Advance(5);
        await tracker.OnTimerTickAsync();
        Assert.Equal(2, windows.Calls);
        tracker.SetPollingInterval(1);
        clock.Advance(1);
        await tracker.OnTimerTickAsync();
        Assert.Equal(3, windows.Calls);
        Assert.Empty(tracker.DrainCompletedSegments());
        Assert.Equal("project", Assert.Single(tracker.CollectAndResetSegments()).ProjectTag);
    }

    [Fact]
    public async Task CivilTimeJumpDoesNotTriggerCapture()
    {
        var (tracker, clock, windows) = Create();
        await tracker.StartAsync(null);
        clock.Now = clock.Now.AddHours(1);
        await tracker.OnTimerTickAsync();
        Assert.Equal(1, windows.Calls);
        clock.Advance(10);
        await tracker.OnTimerTickAsync();
        Assert.Equal(2, windows.Calls);
    }

    [Fact]
    public async Task MidnightMaintenanceDoesNotWaitForCapture()
    {
        var (tracker, clock, windows) = Create();
        clock.Now = new DateTimeOffset(2026, 9, 27, 23, 59, 58, TimeSpan.Zero);
        tracker.SetPollingInterval(60);
        await tracker.StartAsync(null);
        clock.Advance(2);
        await tracker.OnTimerTickAsync();
        Assert.Equal(1, windows.Calls);
        var boundary = Assert.Single(tracker.DrainCompletedSegments());
        Assert.Equal(EndReason.DayBoundary, boundary.EndReason);
        clock.Advance(1);
        Assert.Empty(WorklogEntryValidator.Validate(Assert.Single(tracker.CollectAndResetSegments())));
    }

    [Fact]
    public async Task SlowLookupHasNoBacklogAndRejectsStoppedResult()
    {
        var (tracker, clock, windows) = Create();
        await tracker.StartAsync(null);
        windows.Pending = new TaskCompletionSource<ActiveWindowInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        clock.Advance(10);
        var capture = tracker.OnTimerTickAsync();
        for (var i = 0; i < 20; i++) { clock.Advance(1); await tracker.OnTimerTickAsync(); }
        Assert.Equal(2, windows.Calls);
        tracker.StopTracking(EndReason.ManualPause);
        windows.Pending.SetResult(new ActiveWindowInfo { ProcessName = "late", WindowTitle = "late" });
        await capture;
        Assert.Equal("app", Assert.Single(tracker.DrainCompletedSegments()).AppName);
        Assert.Equal(0, tracker.CompletedEntryCount);
    }

    [Fact]
    public async Task RestartKeepsOnlyLatestPendingInitialCapture()
    {
        var (tracker, clock, windows) = Create();
        windows.Pending = new TaskCompletionSource<ActiveWindowInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var old = tracker.StartAsync("old");
        tracker.StopTracking(EndReason.ManualPause);
        var middle = tracker.StartAsync("middle");
        tracker.SetTrackingEnabled(false);
        tracker.SetTrackingEnabled(true);
        var latest = tracker.StartAsync("latest");
        await middle;
        Assert.Equal(1, windows.Calls);
        var pending = windows.Pending;
        windows.Pending = null;
        pending.SetResult(new ActiveWindowInfo { ProcessName = "old", WindowTitle = "old" });
        await Task.WhenAll(old, latest).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(2, windows.Calls);
        clock.Advance(1);
        var entry = Assert.Single(tracker.CollectAndResetSegments());
        Assert.Equal("latest", entry.ProjectTag);
        Assert.Equal("app", entry.AppName);
    }

    [Fact]
    public async Task ChangeDuringLookupKeepsNewDeadlineAndMidnightMaintenance()
    {
        var (tracker, clock, windows) = Create();
        clock.Now = new DateTimeOffset(2026, 9, 27, 23, 59, 40, TimeSpan.Zero);
        await tracker.StartAsync(null);
        windows.Pending = new TaskCompletionSource<ActiveWindowInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        clock.Advance(10);
        var capture = tracker.OnTimerTickAsync();
        tracker.SetPollingInterval(60);
        clock.Advance(15);
        await tracker.OnTimerTickAsync();
        Assert.Equal(EndReason.DayBoundary, Assert.Single(tracker.DrainCompletedSegments()).EndReason);
        var pending = windows.Pending; windows.Pending = null; pending.SetResult(windows.Window);
        await capture;
        clock.Advance(45);
        await tracker.OnTimerTickAsync();
        Assert.Equal(3, windows.Calls);
    }

    [Fact]
    public async Task FailureWaitsForNextIntervalAndTitleChangesStillSplit()
    {
        var (tracker, clock, windows) = Create();
        tracker.SetPollingInterval(1);
        await tracker.StartAsync(null);
        windows.Fail = true;
        clock.Advance(1); await tracker.OnTimerTickAsync(); await tracker.OnTimerTickAsync();
        Assert.Equal(2, windows.Calls);
        windows.Fail = false;
        windows.Window = new ActiveWindowInfo { ProcessName = "app", WindowTitle = "changed" };
        clock.Advance(1); await tracker.OnTimerTickAsync();
        Assert.Equal("title", Assert.Single(tracker.DrainCompletedSegments()).WindowTitle);
        tracker.SetTrackingEnabled(false);
        clock.Advance(20); await tracker.OnTimerTickAsync();
        Assert.Equal(3, windows.Calls);
        Assert.Empty(tracker.DrainCompletedSegments());
    }

    [Fact]
    public async Task OneSecondCapturesPreserveNormalizedTransitionSequence()
    {
        var (tracker, clock, windows) = Create();
        tracker.SetPollingInterval(1);
        await tracker.StartAsync("project");
        clock.Advance(1); await tracker.OnTimerTickAsync();
        windows.Window = new ActiveWindowInfo { ProcessName = "app", WindowTitle = "second" };
        clock.Advance(1); await tracker.OnTimerTickAsync();
        windows.Window = new ActiveWindowInfo { ProcessName = "other", WindowTitle = "third" };
        clock.Advance(1); await tracker.OnTimerTickAsync();
        clock.Advance(1);
        var entries = tracker.CollectAndResetSegments(EndReason.ApplicationExit);
        Assert.Equal(new[] { "app/title/ApplicationChange/2", "app/second/ApplicationChange/1", "other/third/ApplicationExit/1" },
            entries.Select(e => $"{e.AppName}/{e.WindowTitle}/{e.EndReason}/{(e.EndedAt - e.StartedAt).TotalSeconds}"));
        Assert.All(entries, e => Assert.Empty(WorklogEntryValidator.Validate(e)));
    }

    [Fact]
    public async Task PauseAfterMultiDayDelaySplitsEveryBoundaryWithoutAnotherCapture()
    {
        var (tracker, clock, windows) = Create();
        clock.Now = new DateTimeOffset(2026, 9, 27, 23, 59, 58, TimeSpan.Zero);
        tracker.SetPollingInterval(60);
        await tracker.StartAsync(null);
        clock.Advance(172803);
        var entries = tracker.CollectAndResetSegments();
        Assert.Equal(4, entries.Count);
        Assert.Equal(1, windows.Calls);
        Assert.Equal(3, entries.Count(e => e.EndReason == EndReason.DayBoundary));
        Assert.Equal(EndReason.ManualPause, entries[^1].EndReason);
        Assert.All(entries, e => Assert.Empty(WorklogEntryValidator.Validate(e)));
    }

    private static (SessionTracker, Clock, Windows) Create()
    {
        var clock = new Clock(); var windows = new Windows();
        return (new SessionTracker(windows, NullLogger.Instance, clock, new SourcePlatformProvider(), () => "device"), clock, windows);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);
        private long timestamp;
        public override DateTimeOffset GetUtcNow() => Now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override long TimestampFrequency => 1;
        public override long GetTimestamp() => timestamp;
        public void Advance(int seconds) { timestamp += seconds; Now = Now.AddSeconds(seconds); }
    }

    private sealed class Windows : IActiveWindowService
    {
        public int Calls;
        public bool Fail;
        public TaskCompletionSource<ActiveWindowInfo?>? Pending;
        public ActiveWindowInfo Window = new() { ProcessName = "app", WindowTitle = "title" };
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("synthetic failure");
            return Pending?.Task ?? Task.FromResult<ActiveWindowInfo?>(Window);
        }
    }
}
