namespace FocusTimer.App.HeadlessTests;

using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>
/// Covers tray icon state propagation, which was entirely untested because no prior test
/// could construct a real <see cref="TrayIcon"/>. Headless Avalonia makes that possible.
/// </summary>
public sealed class TrayStateControllerTests
{
    public TrayStateControllerTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task SetTrayIcon_FirstCall_AppliesPendingStateImmediately()
    {
        var timer = new FakeTimerService(TimerState.Paused);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger());
        var controller = new TrayStateController(timer, stats, new RecordingLogger());
        var trayIcon = new TrayIcon();

        controller.SetTrayIcon(trayIcon);
        await WaitUntilAsync(() => trayIcon.ToolTipText != null);

        Assert.Contains("Paused", trayIcon.ToolTipText);
    }

    [Fact]
    public void SetTrayIcon_CalledTwice_SubscribesToStateChangedOnlyOnce()
    {
        var timer = new FakeTimerService(TimerState.Idle);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger());
        var controller = new TrayStateController(timer, stats, new RecordingLogger());
        var trayIcon = new TrayIcon();

        controller.SetTrayIcon(trayIcon);
        controller.SetTrayIcon(trayIcon);
        timer.Raise(TimerState.Running);

        Assert.Equal(1, timer.SubscriberCount);
    }

    [Fact]
    public void OnTimerStateChanged_BeforeTrayIconSet_StoresPendingStateWithoutThrowing()
    {
        var timer = new FakeTimerService(TimerState.Idle);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger());
        var controller = new TrayStateController(timer, stats, new RecordingLogger());
        var trayIcon = new TrayIcon();
        controller.SetTrayIcon(trayIcon);

        timer.Raise(TimerState.Running);

        Assert.Equal("Running", TooltipState(trayIcon));
    }

    [Fact]
    public void UpdateState_CalledFromBackgroundThread_DispatchesToUiThreadInstead()
    {
        var timer = new FakeTimerService(TimerState.Idle);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger());
        var controller = new TrayStateController(timer, stats, new RecordingLogger());
        var trayIcon = new TrayIcon();
        controller.SetTrayIcon(trayIcon);

        // UpdateState calls Dispatcher.UIThread.Invoke on this background thread, which blocks
        // until the UI thread (this test thread) pumps the queue — so that pumping must happen
        // concurrently rather than after the background call completes.
        Task backgroundUpdate = Task.Run(() => controller.UpdateState(TimerState.Running));
        PumpUntilComplete(backgroundUpdate);

        Assert.Equal("Running", TooltipState(trayIcon));
    }

    private static void PumpUntilComplete(Task task)
    {
        for (int attempt = 0; attempt < 200 && !task.IsCompleted; attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        Assert.True(task.IsCompleted);
        task.GetAwaiter().GetResult();
    }

    [Fact]
    public void RaiseEntriesLogged_FiresOnEntriesLoggedEventSynchronously()
    {
        var timer = new FakeTimerService(TimerState.Idle);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger());
        var controller = new TrayStateController(timer, stats, new RecordingLogger());
        int raised = 0;
        controller.OnEntriesLogged += (_, _) => raised++;

        controller.RaiseEntriesLogged([]);

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task RaiseEntriesLogged_AddsEntryDurationToTodayTotal()
    {
        var timer = new FakeTimerService(TimerState.Idle);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger(), TimeProvider.System);
        var controller = new TrayStateController(timer, stats, new RecordingLogger());
        var trayIcon = new TrayIcon();
        controller.SetTrayIcon(trayIcon);
        await WaitUntilAsync(() => trayIcon.ToolTipText != null);
        DateTimeOffset now = DateTimeOffset.Now;
        var entry = new TimeEntry(
            "entry", "session", now, now.AddMinutes(45), "App", "Window", null,
            ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.ManualPause,
            CaptureSource.ActiveWindow, SourcePlatform.Windows, null, 1, DateTimeOffset.UtcNow);

        controller.RaiseEntriesLogged([entry]);
        await WaitUntilAsync(() => stats.GetTodayTotal() > TimeSpan.Zero);

        Assert.Equal(45, stats.GetTodayTotal().TotalMinutes, 0);
    }

    [Fact]
    public async Task RaiseEntriesLogged_WhenEntryEnumerationFails_LogsErrorInsteadOfThrowing()
    {
        var timer = new FakeTimerService(TimerState.Idle);
        var stats = new TodayStatsService(new EmptyWorklogStore(), new RecordingLogger());
        var logger = new RecordingLogger();
        var controller = new TrayStateController(timer, stats, logger);
        var trayIcon = new TrayIcon();
        controller.SetTrayIcon(trayIcon);
        await WaitUntilAsync(() => trayIcon.ToolTipText != null);

        controller.RaiseEntriesLogged(new ThrowingEntries());
        await WaitUntilAsync(() => logger.Errors.Count > 0);

        Assert.Contains(logger.Errors, m => m.Contains("Failed to refresh tray tooltip", StringComparison.Ordinal));
    }

    private static string TooltipState(TrayIcon trayIcon) =>
        trayIcon.ToolTipText!.Split(": ", 2)[1].Split(" | ", 2)[0];

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(5);
        }

        Assert.True(condition());
    }

    private sealed class FakeTimerService(TimerState initialState) : ITimerService
    {
        public int SubscriberCount { get; private set; }

        public event EventHandler<TimerState>? StateChangedCore;

        public event EventHandler<TimerState>? StateChanged
        {
            add { this.StateChangedCore += value; this.SubscriberCount++; }
            remove { this.StateChangedCore -= value; this.SubscriberCount--; }
        }

        public event EventHandler<TimeSpan>? Tick { add { } remove { } }

        public TimerState CurrentState { get; private set; } = initialState;

        public TimeSpan Elapsed => TimeSpan.Zero;

        public void Raise(TimerState state)
        {
            this.CurrentState = state;
            this.StateChangedCore?.Invoke(this, state);
        }

        public void Start(string? projectTag = null) { }

        public void Pause(EndReason reason = EndReason.ManualPause) { }

        public void Stop(EndReason reason = EndReason.ManualPause) { }

        public void Reset() { }
    }

    private sealed class EmptyWorklogStore : IWorklogStore
    {
        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), []));

        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());

        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorklogOutcome.Success());
    }

    private sealed class ThrowingEntries : IEnumerable<TimeEntry>
    {
        public IEnumerator<TimeEntry> GetEnumerator() => throw new InvalidOperationException("Synthetic enumeration failure.");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Errors { get; } = new();

        public void LogCritical(string message, Exception? exception = null) { }

        public void LogError(string message, Exception? exception = null) => this.Errors.Add(message);

        public void LogWarning(string message) { }

        public void LogInformation(string message) { }

        public void LogDebug(string message) { }
    }
}
