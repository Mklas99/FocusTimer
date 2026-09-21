namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class SessionTrackerTests
{
    [Fact]
    public async Task SessionTracker_GivenControlledClock_UsesDeterministicTimestampsAndMetadata()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "code", WindowTitle = "FocusTimer" });

        await tracker.StartAsync("FT");
        clock.Advance(TimeSpan.FromMinutes(5));
        var entry = Assert.Single(tracker.CollectAndResetSegments());

        Assert.Equal(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero), entry.StartedAt);
        Assert.Equal(TimeSpan.FromMinutes(5), entry.Duration);
        Assert.Equal("device-1", entry.SourceDeviceId);
        Assert.Equal(SourcePlatform.Windows, entry.SourcePlatform);
        Assert.Equal(ActivityKind.Active, entry.ActivityKind);
        Assert.Equal(CaptureSource.ActiveWindow, entry.CaptureSource);
        Assert.Equal(ProjectAssignmentSource.Session, entry.ProjectAssignmentSource);
        Assert.Equal(1, entry.Revision);
        Assert.Equal(EndReason.ManualPause, entry.EndReason);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenWindowChange_RetainsSessionAndCreatesNewEntry()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" }, new ActiveWindowInfo { ProcessName = "B", WindowTitle = "two" });
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromMinutes(1));
        await tracker.OnTimerTickAsync();
        clock.Advance(TimeSpan.FromMinutes(1));

        var entries = tracker.CollectAndResetSegments();

        Assert.Equal(2, entries.Count);
        Assert.NotEqual(entries[0].EntryId, entries[1].EntryId);
        Assert.Equal(entries[0].SessionId, entries[1].SessionId);
        Assert.Equal(EndReason.ApplicationChange, entries[0].EndReason);
    }

    [Fact]
    public async Task StartAsync_AfterPause_CreatesNewSession()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromMinutes(1));
        var first = Assert.Single(tracker.CollectAndResetSegments());
        clock.Advance(TimeSpan.FromMinutes(1));
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromMinutes(1));
        var second = Assert.Single(tracker.CollectAndResetSegments());

        Assert.NotEqual(first.SessionId, second.SessionId);
    }

    [Fact]
    public async Task StartAsync_GivenPauseBeforeWindowLookupCompletes_DoesNotCreateLateSegment()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var windowService = new DeferredWindowService();
        var tracker = new SessionTracker(windowService, NullLogger.Instance, clock, new WindowsPlatformProvider(), () => "device-1");
        var start = tracker.StartAsync(null);
        tracker.StopTracking(EndReason.ManualPause);
        windowService.Complete(new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await start;

        Assert.Empty(tracker.DrainCompletedSegments());
        Assert.Equal(0, tracker.CompletedEntryCount);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenStopDuringWindowLookup_DoesNotReopenStoppedSession()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var windowService = new QueuedDeferredWindowService();
        var startup = windowService.Enqueue();
        var tickLookup = windowService.Enqueue();
        var tracker = new SessionTracker(windowService, NullLogger.Instance, clock, new WindowsPlatformProvider(), () => "device-1");

        var start = tracker.StartAsync(null);
        startup.SetResult(new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await start;
        clock.Advance(TimeSpan.FromMinutes(1));

        var tick = tracker.OnTimerTickAsync();
        tracker.StopTracking(EndReason.ManualPause);
        tickLookup.SetResult(new ActiveWindowInfo { ProcessName = "B", WindowTitle = "two" });
        await tick;

        var entry = Assert.Single(tracker.DrainCompletedSegments());
        Assert.Equal(EndReason.ManualPause, entry.EndReason);
        Assert.False(string.IsNullOrWhiteSpace(entry.SessionId));
        Assert.Equal(0, tracker.CompletedEntryCount);
    }

    [Fact]
    public async Task UpdateProjectTag_GivenOpenSegment_UpdatesCompletedMetadata()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync("before");
        tracker.UpdateProjectTag("after");
        clock.Advance(TimeSpan.FromMinutes(1));

        var entry = Assert.Single(tracker.CollectAndResetSegments());

        Assert.Equal("after", entry.ProjectTag);
        Assert.Equal(ProjectAssignmentSource.Session, entry.ProjectAssignmentSource);
    }

    [Fact]
    public async Task SetTrackingEnabled_GivenFalse_ClearsActiveAndBufferedSegments()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync(null);
        tracker.SetTrackingEnabled(false);

        Assert.Empty(tracker.CollectAndResetSegments());
        Assert.Equal(0, tracker.CompletedEntryCount);
    }

    [Theory]
    [InlineData(EndReason.IdlePause)]
    [InlineData(EndReason.ApplicationExit)]
    [InlineData(EndReason.Unknown)]
    public async Task StopTracking_GivenExplicitReason_PreservesReason(EndReason reason)
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromMinutes(1));
        tracker.StopTracking(reason);

        Assert.Equal(reason, Assert.Single(tracker.DrainCompletedSegments()).EndReason);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenMidnightCrossing_SplitsEntriesWithoutCrossDayRecord()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 23, 59, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromMinutes(2));
        await tracker.OnTimerTickAsync();
        clock.Advance(TimeSpan.FromMinutes(1));

        var entries = tracker.CollectAndResetSegments();

        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.Empty(WorklogEntryValidator.Validate(entry)));
        Assert.Equal(EndReason.DayBoundary, entries[0].EndReason);
        Assert.Equal(entries[0].SessionId, entries[1].SessionId);
        Assert.NotEqual(entries[0].EntryId, entries[1].EntryId);
    }

    [Fact]
    public async Task DrainCompletedSegments_CalledTwice_DoesNotDuplicateEntries()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 3, 31, 9, 0, 0, TimeSpan.Zero));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromMinutes(1));
        tracker.StopTracking(EndReason.ManualPause);

        Assert.Single(tracker.DrainCompletedSegments());
        Assert.Empty(tracker.DrainCompletedSegments());
    }

    [Theory]
    [InlineData(2026, 3, 29, 0, 30)]
    [InlineData(2026, 10, 25, 0, 30)]
    public async Task OnTimerTickAsync_GivenDstTransition_DoesNotCreateCrossDayEntries(int year, int month, int day, int hour, int minute)
    {
        var clock = new MutableTimeProvider(
            new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero),
            TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"));
        var tracker = CreateTracker(clock, new ActiveWindowInfo { ProcessName = "A", WindowTitle = "one" });
        await tracker.StartAsync(null);
        clock.Advance(TimeSpan.FromHours(3));
        await tracker.OnTimerTickAsync();
        clock.Advance(TimeSpan.FromMinutes(1));

        var entries = tracker.CollectAndResetSegments();

        Assert.Single(entries);
        Assert.Empty(WorklogEntryValidator.Validate(entries[0]));
    }

    private static SessionTracker CreateTracker(MutableTimeProvider clock, params ActiveWindowInfo[] windows)
    {
        return new SessionTracker(new SequenceWindowService(windows), NullLogger.Instance, clock, new WindowsPlatformProvider(), () => "device-1");
    }

    private sealed class SequenceWindowService : IActiveWindowService
    {
        private readonly Queue<ActiveWindowInfo> _windows;
        private ActiveWindowInfo? _last;
        public SequenceWindowService(IEnumerable<ActiveWindowInfo> windows) => this._windows = new Queue<ActiveWindowInfo>(windows);
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            if (this._windows.Count > 0)
                this._last = this._windows.Dequeue();
            return Task.FromResult(this._last);
        }
    }

    private sealed class WindowsPlatformProvider : ISourcePlatformProvider
    {
        public SourcePlatform GetCurrentPlatform() => SourcePlatform.Windows;
    }

    private sealed class DeferredWindowService : IActiveWindowService
    {
        private readonly TaskCompletionSource<ActiveWindowInfo?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() => this._completion.Task;
        public void Complete(ActiveWindowInfo? window) => this._completion.SetResult(window);
    }

    private sealed class QueuedDeferredWindowService : IActiveWindowService
    {
        private readonly Queue<TaskCompletionSource<ActiveWindowInfo?>> _lookups = new();

        public TaskCompletionSource<ActiveWindowInfo?> Enqueue()
        {
            var completion = new TaskCompletionSource<ActiveWindowInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
            this._lookups.Enqueue(completion);
            return completion;
        }

        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() => this._lookups.Dequeue().Task;
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        private readonly TimeZoneInfo _localTimeZone;
        public MutableTimeProvider(DateTimeOffset utcNow, TimeZoneInfo? localTimeZone = null)
        {
            this._utcNow = utcNow.ToUniversalTime();
            this._localTimeZone = localTimeZone ?? TimeZoneInfo.Utc;
        }

        public override TimeZoneInfo LocalTimeZone => this._localTimeZone;
        public override DateTimeOffset GetUtcNow() => this._utcNow;
        public void Advance(TimeSpan amount) => this._utcNow = this._utcNow.Add(amount);
    }
}
