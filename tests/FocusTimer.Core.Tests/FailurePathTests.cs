namespace FocusTimer.Core.Tests;

using System.Reflection;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

/// <summary>Covers error, disabled and boundary branches of the Core services.</summary>
public class FailurePathTests
{
    // ---- BreakReminderService --------------------------------------------------

    private static Task InvokeElapsed(BreakReminderService service, int minutes)
    {
        var method = typeof(BreakReminderService).GetMethod("OnReminderElapsedAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(service, new object[] { minutes })!;
    }

    private static System.Timers.Timer? ReflectTimer(BreakReminderService service) =>
        (System.Timers.Timer?)typeof(BreakReminderService).GetField("_reminderTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service);

    [Fact]
    public async Task ReminderElapsed_GivenRemindersDisabledMeanwhile_DoesNotNotify()
    {
        var notifications = new RecordingNotificationService();
        using var service = new BreakReminderService(
            notifications, new FixedSettingsProvider(new Settings { BreakRemindersEnabled = false }), NullLogger.Instance);

        await InvokeElapsed(service, 30);

        Assert.Equal(0, notifications.BreakReminderCount);
        Assert.Null(ReflectTimer(service));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReminderElapsed_GivenEnabled_NotifiesAndSchedulesFollowUp(bool requireAck)
    {
        var notifications = new RecordingNotificationService();
        var settings = new Settings { BreakRemindersEnabled = true, RequireBreakReminderAcknowledgement = requireAck, BreakIntervalMinutes = 25 };
        using var service = new BreakReminderService(notifications, new FixedSettingsProvider(settings), NullLogger.Instance);

        await InvokeElapsed(service, 25);

        Assert.Equal(1, notifications.BreakReminderCount);
        var timer = ReflectTimer(service);
        Assert.NotNull(timer);
        Assert.Equal(requireAck ? 25 * 60_000 : 10 * 60_000, timer!.Interval);
    }

    [Fact]
    public async Task ReminderElapsed_GivenZeroIntervalAndAcknowledgement_DoesNotScheduleFollowUp()
    {
        var settings = new Settings { RequireBreakReminderAcknowledgement = true, BreakIntervalMinutes = 0 };
        using var service = new BreakReminderService(new RecordingNotificationService(), new FixedSettingsProvider(settings), NullLogger.Instance);

        await InvokeElapsed(service, 5);

        Assert.Null(ReflectTimer(service));
    }

    [Fact]
    public async Task ReminderElapsed_GivenNotificationFailure_LogsErrorInsteadOfThrowing()
    {
        var logger = new CapturingLogger();
        using var service = new BreakReminderService(
            new ThrowingNotificationService(), new FixedSettingsProvider(new Settings()), logger);

        var ex = await Record.ExceptionAsync(() => InvokeElapsed(service, 10));

        Assert.Null(ex);
        Assert.Contains(logger.Errors, e => e.Contains("Break reminder failed"));
    }

    [Fact]
    public async Task ReminderElapsed_GivenSettingsFailureAndNoLogger_DoesNotThrow()
    {
        using var service = new BreakReminderService(new RecordingNotificationService(), new ThrowingSettingsProvider());

        var ex = await Record.ExceptionAsync(() => InvokeElapsed(service, 10));

        Assert.Null(ex);
    }

    [Fact]
    public async Task OnTimerStarted_GivenSettingsFailure_LogsErrorAndCreatesNoTimer()
    {
        var logger = new CapturingLogger();
        using var service = new BreakReminderService(new RecordingNotificationService(), new ThrowingSettingsProvider(), logger);

        service.OnTimerStarted();
        await WaitUntil(() => logger.Errors.Count > 0);

        Assert.Contains(logger.Errors, e => e.Contains("Failed to schedule"));
        Assert.Null(ReflectTimer(service));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task OnTimerStarted_GivenNonPositiveInterval_DoesNotScheduleTimer(int minutes)
    {
        var provider = new CountingSettingsProvider(new Settings { BreakIntervalMinutes = minutes });
        using var service = new BreakReminderService(new RecordingNotificationService(), provider, NullLogger.Instance);

        service.OnTimerStarted();
        await WaitUntil(() => provider.Loads > 0);
        await Task.Delay(50);

        Assert.Null(ReflectTimer(service));
    }

    [Fact]
    public async Task OnTimerStarted_GivenValidSettings_SchedulesTimerAndPauseCancelsIt()
    {
        var provider = new CountingSettingsProvider(new Settings { BreakIntervalMinutes = 30 });
        using var service = new BreakReminderService(new RecordingNotificationService(), provider, NullLogger.Instance);

        service.OnTimerStarted();
        await WaitUntil(() => ReflectTimer(service) is not null);
        service.OnTimerPaused();

        Assert.Null(ReflectTimer(service));
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = new BreakReminderService(new RecordingNotificationService(), new FixedSettingsProvider(new Settings()));

        var ex = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(ex);
    }

    // ---- TodayStatsService -----------------------------------------------------

    [Fact]
    public async Task RefreshTodayAsync_GivenFailedRead_KeepsPreviousTotalAndLogsWarning()
    {
        var logger = new CapturingLogger();
        var store = new ScriptedStore(new WorklogReadResult(WorklogOutcome.Success(), new[] { Entry(60) }));
        var service = new TodayStatsService(store, logger, new FixedClock(Noon));
        await service.RefreshTodayAsync();

        store.Next = new WorklogReadResult(new WorklogOutcome(WorklogOutcomeKind.IoFailure, "disk gone"), Array.Empty<TimeEntry>());
        await service.RefreshTodayAsync();

        Assert.Equal(TimeSpan.FromMinutes(60), service.GetTodayTotal());
        Assert.Contains("disk gone", logger.Warnings);
    }

    [Fact]
    public async Task RefreshTodayAsync_GivenFailedReadWithoutMessage_LogsDefaultWarning()
    {
        var logger = new CapturingLogger();
        var store = new ScriptedStore(new WorklogReadResult(new WorklogOutcome(WorklogOutcomeKind.MalformedData), Array.Empty<TimeEntry>()));

        await new TodayStatsService(store, logger, new FixedClock(Noon)).RefreshTodayAsync();

        Assert.Contains("Unable to read worklog.", logger.Warnings);
    }

    [Fact]
    public async Task RefreshTodayAsync_GivenStoreThrows_LogsErrorAndKeepsZero()
    {
        var logger = new CapturingLogger();
        var service = new TodayStatsService(new ScriptedStore(null), logger, new FixedClock(Noon));

        var ex = await Record.ExceptionAsync(service.RefreshTodayAsync);

        Assert.Null(ex);
        Assert.Single(logger.Errors);
        Assert.Equal(TimeSpan.Zero, service.GetTodayTotal());
    }

    [Fact]
    public async Task AddEntriesAsync_GivenEntriesForToday_AddsToTotal()
    {
        var service = new TodayStatsService(new ScriptedStore(new WorklogReadResult(WorklogOutcome.Success(), Array.Empty<TimeEntry>())), NullLogger.Instance, new FixedClock(Noon));
        await service.RefreshTodayAsync();

        await service.AddEntriesAsync(new[] { Entry(30), Entry(15) });

        Assert.Equal(TimeSpan.FromMinutes(45), service.GetTodayTotal());
        Assert.Equal("Today: 0h 45m", service.GetTodaySummaryText());
    }

    [Fact]
    public async Task AddEntriesAsync_GivenEntriesFromOtherDay_IgnoresThem()
    {
        var service = new TodayStatsService(new ScriptedStore(new WorklogReadResult(WorklogOutcome.Success(), Array.Empty<TimeEntry>())), NullLogger.Instance, new FixedClock(Noon));
        await service.RefreshTodayAsync();

        await service.AddEntriesAsync(new[] { Entry(30, Noon.AddDays(-1)) });

        Assert.Equal(TimeSpan.Zero, service.GetTodayTotal());
    }

    [Fact]
    public async Task AddEntriesAsync_GivenDayRolledOver_RefreshesFromStoreInstead()
    {
        var clock = new FixedClock(Noon);
        var store = new ScriptedStore(new WorklogReadResult(WorklogOutcome.Success(), new[] { Entry(20) }));
        var service = new TodayStatsService(store, NullLogger.Instance, clock);
        await service.RefreshTodayAsync();

        clock.Now = Noon.AddDays(1);
        store.Next = new WorklogReadResult(WorklogOutcome.Success(), new[] { Entry(5, Noon.AddDays(1)) });
        await service.AddEntriesAsync(new[] { Entry(99, Noon.AddDays(1)) });

        Assert.Equal(TimeSpan.FromMinutes(5), service.GetTodayTotal());
        Assert.Equal(2, store.QueryCount);
    }

    [Fact]
    public void GetTodaySummaryText_GivenOverOneHour_FormatsHoursAndMinutes()
    {
        var service = new TodayStatsService(new ScriptedStore(null), NullLogger.Instance, new FixedClock(Noon));

        Assert.Equal("Today: 0h 00m", service.GetTodaySummaryText());
    }

    // ---- SessionTracker --------------------------------------------------------

    [Fact]
    public async Task StartAsync_GivenWindowLookupFails_LogsErrorAndTracksUnknownWindow()
    {
        var logger = new CapturingLogger();
        var clock = new FixedClock(Noon);
        var tracker = new SessionTracker(new FailingWindowService(), logger, clock, new PlatformStub(), () => null);

        await tracker.StartAsync("P");
        clock.Now = Noon.AddMinutes(3);
        var entry = Assert.Single(tracker.CollectAndResetSegments());

        Assert.Single(logger.Errors);
        Assert.Equal("Unknown", entry.AppName);
        Assert.Equal("No active window", entry.WindowTitle);
    }

    [Fact]
    public async Task StartAsync_GivenTrackingDisabled_RecordsNothing()
    {
        var clock = new FixedClock(Noon);
        var tracker = new SessionTracker(new FailingWindowService(), NullLogger.Instance, clock, new PlatformStub(), () => null);
        tracker.SetTrackingEnabled(false);

        await tracker.StartAsync("P");
        clock.Now = Noon.AddMinutes(3);

        Assert.Empty(tracker.CollectAndResetSegments());
        Assert.Equal(0, tracker.CompletedEntryCount);
    }

    [Fact]
    public async Task StartAsync_GivenAlreadyTracking_OnlyUpdatesProjectTag()
    {
        var clock = new FixedClock(Noon);
        var tracker = new SessionTracker(new FixedWindowService(new ActiveWindowInfo { ProcessName = "a", WindowTitle = "b" }), NullLogger.Instance, clock, new PlatformStub(), () => null);
        await tracker.StartAsync("A");

        await tracker.StartAsync("B");
        clock.Now = Noon.AddMinutes(2);
        var entries = tracker.CollectAndResetSegments();

        Assert.Single(entries);
        Assert.Equal("A", entries[0].ProjectTag);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenNotTracking_DoesNothing()
    {
        var window = new CountingWindowService();
        var tracker = new SessionTracker(window, NullLogger.Instance, new FixedClock(Noon), new PlatformStub(), () => null);

        await tracker.OnTimerTickAsync();

        Assert.Equal(0, window.Calls);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenLookupThrows_LogsWarningAndKeepsSegment()
    {
        var logger = new CapturingLogger();
        var clock = new FixedClock(Noon);
        var service = new ToggleWindowService();
        var tracker = new SessionTracker(service, logger, clock, new PlatformStub(), () => null);
        await tracker.StartAsync(null);

        service.Throw = true;
        await tracker.OnTimerTickAsync();
        clock.Now = Noon.AddMinutes(1);

        Assert.Contains(logger.Warnings, w => w.Contains("tick failed"));
        var entry = Assert.Single(tracker.CollectAndResetSegments());
        Assert.Equal(ProjectAssignmentSource.Unassigned, entry.ProjectAssignmentSource);
    }

    [Fact]
    public async Task OnTimerTickAsync_GivenWindowDisappears_ClosesEntryWithApplicationChange()
    {
        var clock = new FixedClock(Noon);
        var service = new ToggleWindowService();
        var tracker = new SessionTracker(service, NullLogger.Instance, clock, new PlatformStub(), () => null);
        await tracker.StartAsync(null);

        clock.Now = Noon.AddMinutes(1);
        service.Window = null;
        await tracker.OnTimerTickAsync();

        var completed = tracker.DrainCompletedSegments();
        Assert.Single(completed);
        Assert.Equal(EndReason.ApplicationChange, completed[0].EndReason);
    }

    [Fact]
    public async Task StopTracking_GivenZeroLengthSegment_DiscardsIt()
    {
        var tracker = new SessionTracker(new ToggleWindowService(), NullLogger.Instance, new FixedClock(Noon), new PlatformStub(), () => null);
        await tracker.StartAsync(null);

        tracker.StopTracking(EndReason.ManualPause);

        Assert.False(tracker.HasCompletedEntries);
    }

    [Fact]
    public void Constructor_GivenNullDependencies_Throws()
    {
        var w = new ToggleWindowService();
        var l = NullLogger.Instance;
        var c = new FixedClock(Noon);
        var p = new PlatformStub();

        Assert.Throws<ArgumentNullException>(() => new SessionTracker(null!, l, c, p, () => null));
        Assert.Throws<ArgumentNullException>(() => new SessionTracker(w, null!, c, p, () => null));
        Assert.Throws<ArgumentNullException>(() => new SessionTracker(w, l, null!, p, () => null));
        Assert.Throws<ArgumentNullException>(() => new SessionTracker(w, l, c, null!, () => null));
        Assert.Throws<ArgumentNullException>(() => new SessionTracker(w, l, c, p, null!));
    }

    // ---- WorklogEntryValidator ---------------------------------------------------

    [Fact]
    public void Validate_GivenEveryKindOfBadEntry_ReportsAllErrors()
    {
        var start = new DateTimeOffset(2026, 5, 4, 23, 0, 0, TimeSpan.Zero);
        var bad = new TimeEntry(" ", "", start, start.AddDays(1).AddHours(-2).AddMinutes(-30), "a", "t", null,
            ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow,
            SourcePlatform.Windows, null, 0, new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(2)));

        var errors = WorklogEntryValidator.Validate(bad);

        Assert.Contains("Entry ID is required.", errors);
        Assert.Contains("Session ID is required.", errors);
        Assert.Contains("Revision must be at least one.", errors);
        Assert.Contains("Last modified time must be UTC.", errors);
    }

    [Fact]
    public void Validate_GivenEndBeforeStart_ReportsOrderError()
    {
        var start = new DateTimeOffset(2026, 5, 4, 10, 0, 0, TimeSpan.Zero);
        var entry = new TimeEntry("e", "s", start, start, "a", "t", null, ProjectAssignmentSource.Unassigned, null,
            ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow, SourcePlatform.Windows, null, 1, start);

        Assert.Contains("End time must be later than start time.", WorklogEntryValidator.Validate(entry));
    }

    [Fact]
    public void Validate_GivenEntrySpanningDays_ReportsButAllowsEndAtNextMidnight()
    {
        var start = new DateTimeOffset(2026, 5, 4, 23, 0, 0, TimeSpan.Zero);
        TimeEntry Make(DateTimeOffset end) => new("e", "s", start, end, "a", "t", null, ProjectAssignmentSource.Unassigned, null,
            ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow, SourcePlatform.Windows, null, 1, start);

        Assert.Contains("Entries must not span local calendar days.", WorklogEntryValidator.Validate(Make(start.AddHours(2))));
        Assert.Empty(WorklogEntryValidator.Validate(Make(start.AddHours(1))));
    }

    // ---- helpers -----------------------------------------------------------------

    private static readonly DateTimeOffset Noon = new(2026, 5, 4, 12, 0, 0, TimeSpan.Zero);

    private static TimeEntry Entry(int minutes, DateTimeOffset? start = null)
    {
        var s = start ?? Noon.AddHours(-1);
        return new TimeEntry(Guid.NewGuid().ToString(), "s", s, s.AddMinutes(minutes), "a", "t", null,
            ProjectAssignmentSource.Unassigned, null, ActivityKind.Active, EndReason.Unknown, CaptureSource.ActiveWindow,
            SourcePlatform.Windows, null, 1, s);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(20);
        }
    }

    private sealed class CapturingLogger : IAppLogger
    {
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();
        public void LogCritical(string message, Exception? ex = null) { }
        public void LogDebug(string message) { }
        public void LogError(string message, Exception? ex = null) => this.Errors.Add(message);
        public void LogInformation(string message) { }
        public void LogWarning(string message) => this.Warnings.Add(message);
    }

    private sealed class ThrowingNotificationService : INotificationService
    {
        public Task ShowNotificationAsync(string title, string message) => throw new InvalidOperationException("no toast");
        public Task ShowBreakReminderAsync(string message, bool requireAcknowledgement) => throw new InvalidOperationException("no toast");
    }

    private sealed class ThrowingSettingsProvider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => throw new IOException("settings unreadable");
        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class CountingSettingsProvider : ISettingsProvider
    {
        private readonly Settings _settings;
        public CountingSettingsProvider(Settings settings) => this._settings = settings;
        public int Loads { get; private set; }
        public Task<Settings> LoadAsync() { this.Loads++; return Task.FromResult(this._settings); }
        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class FixedClock : TimeProvider
    {
        public FixedClock(DateTimeOffset now) => this.Now = now;
        public DateTimeOffset Now { get; set; }
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => this.Now;
    }

    private sealed class ScriptedStore : IWorklogStore
    {
        public ScriptedStore(WorklogReadResult? next) => this.Next = next;
        public WorklogReadResult? Next { get; set; }
        public int QueryCount { get; private set; }

        public Task<WorklogReadResult> QueryAsync(WorklogQuery query, CancellationToken cancellationToken = default)
        {
            this.QueryCount++;
            return this.Next is null ? throw new IOException("store down") : Task.FromResult(this.Next);
        }

        public Task<WorklogOutcome> AppendAsync(IReadOnlyCollection<TimeEntry> entries, CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());
        public Task<WorklogReadResult> GetAsync(string entryId, CancellationToken cancellationToken = default) => Task.FromResult(new WorklogReadResult(WorklogOutcome.Success(), Array.Empty<TimeEntry>()));
        public Task<WorklogOutcome> PatchAsync(string entryId, int expectedRevision, WorklogPatch patch, CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());
        public Task<WorklogOutcome> DeleteAsync(string entryId, int expectedRevision, CancellationToken cancellationToken = default) => Task.FromResult(WorklogOutcome.Success());
    }

    private sealed class PlatformStub : ISourcePlatformProvider
    {
        public SourcePlatform GetCurrentPlatform() => SourcePlatform.Windows;
    }

    private sealed class FailingWindowService : IActiveWindowService
    {
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() => throw new InvalidOperationException("no window api");
    }

    private sealed class FixedWindowService : IActiveWindowService
    {
        private readonly ActiveWindowInfo? _window;
        public FixedWindowService(ActiveWindowInfo? window) => this._window = window;
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() => Task.FromResult(this._window);
    }

    private sealed class CountingWindowService : IActiveWindowService
    {
        public int Calls { get; private set; }
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() { this.Calls++; return Task.FromResult<ActiveWindowInfo?>(null); }
    }

    private sealed class ToggleWindowService : IActiveWindowService
    {
        public bool Throw { get; set; }
        public ActiveWindowInfo? Window { get; set; } = new() { ProcessName = "app", WindowTitle = "win" };
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync() =>
            this.Throw ? throw new InvalidOperationException("tick lookup failed") : Task.FromResult(this.Window);
    }
}
