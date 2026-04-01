namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class SessionTrackerTests
{
    [Fact]
    public async Task StartAsync_GivenWindowServiceResult_CreatesSingleEntryWithTag()
    {
        var windowService = new SequenceWindowService(new ActiveWindowInfo
        {
            ProcessName = "devenv",
            WindowTitle = "Solution",
        });

        var tracker = new SessionTracker(windowService, NullLogger.Instance);

        await tracker.StartAsync("FT");
        await Task.Delay(1100);

        var entries = tracker.CollectAndResetSegments();

        Assert.Single(entries);
        Assert.Equal("devenv", entries[0].AppName);
        Assert.Equal("FT", entries[0].ProjectTag);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenWindowChanged_ClosesOldSegment()
    {
        var windowService = new SequenceWindowService(
            new ActiveWindowInfo { ProcessName = "A", WindowTitle = "WinA" },
            new ActiveWindowInfo { ProcessName = "B", WindowTitle = "WinB" });

        var tracker = new SessionTracker(windowService, NullLogger.Instance);
        await tracker.StartAsync("Tag");
        await Task.Delay(1100);

        await tracker.OnTimerTickAsync();
        var completed = tracker.DrainCompletedSegments();

        Assert.Single(completed);
        Assert.Equal("A", completed[0].AppName);
        Assert.True(tracker.CompletedEntryCount >= 1);
    }

    [Fact]
    public async Task UpdateProjectTag_GivenOpenEntry_UpdatesCurrentSegmentTag()
    {
        var tracker = new SessionTracker(
            new SequenceWindowService(new ActiveWindowInfo { ProcessName = "proc", WindowTitle = "title" }),
            NullLogger.Instance);

        await tracker.StartAsync("Old");
        tracker.UpdateProjectTag("New");
        await Task.Delay(1100);

        var entries = tracker.CollectAndResetSegments();

        Assert.Single(entries);
        Assert.Equal("New", entries[0].ProjectTag);
    }

    [Fact]
    public async Task SetTrackingEnabled_GivenFalse_ClearsEntriesAndStopsTracking()
    {
        var tracker = new SessionTracker(
            new SequenceWindowService(new ActiveWindowInfo { ProcessName = "p", WindowTitle = "w" }),
            NullLogger.Instance);

        await tracker.StartAsync("Tag");
        tracker.SetTrackingEnabled(false);

        var entries = tracker.CollectAndResetSegments();

        Assert.Empty(entries);
        Assert.Equal(0, tracker.CompletedEntryCount);
    }

    [Fact]
    public async Task StartAsync_GivenTrackingDisabled_DoesNotCreateEntryUntilEnabled()
    {
        var tracker = new SessionTracker(
            new SequenceWindowService(new ActiveWindowInfo { ProcessName = "proc", WindowTitle = "title" }),
            NullLogger.Instance);

        tracker.SetTrackingEnabled(false);
        await tracker.StartAsync("P1");

        Assert.Empty(tracker.CollectAndResetSegments());

        tracker.SetTrackingEnabled(true);
        await tracker.StartAsync("P2");
        await Task.Delay(1100);

        var entries = tracker.CollectAndResetSegments();
        Assert.Single(entries);
        Assert.Equal("P2", entries[0].ProjectTag);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenSameWindow_DoesNotProduceCompletedSegments()
    {
        var window = new ActiveWindowInfo { ProcessName = "X", WindowTitle = "Y" };
        var tracker = new SessionTracker(new SequenceWindowService(window, window), NullLogger.Instance);

        await tracker.StartAsync("Tag");
        await Task.Delay(1100);
        await tracker.OnTimerTickAsync();

        Assert.Empty(tracker.DrainCompletedSegments());
        Assert.True(tracker.CompletedEntryCount >= 1);
    }

    [Fact]
    public async Task StartAsync_GivenWindowServiceThrows_CreatesFallbackUnknownEntry()
    {
        var tracker = new SessionTracker(new ThrowingWindowService(), NullLogger.Instance);

        await tracker.StartAsync("Tag");
        await Task.Delay(1100);

        var entries = tracker.CollectAndResetSegments();
        Assert.Single(entries);
        Assert.Equal("Unknown", entries[0].AppName);
    }

    private sealed class SequenceWindowService : IActiveWindowService
    {
        private readonly Queue<ActiveWindowInfo?> _queue;
        private ActiveWindowInfo? _last;

        public SequenceWindowService(params ActiveWindowInfo?[] values)
        {
            _queue = new Queue<ActiveWindowInfo?>(values);
        }

        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
        {
            if (_queue.Count > 0)
            {
                _last = _queue.Dequeue();
            }

            return Task.FromResult(_last);
        }
    }

    private sealed class ThrowingWindowService : IActiveWindowService
    {
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() => throw new InvalidOperationException("boom");
    }
}
