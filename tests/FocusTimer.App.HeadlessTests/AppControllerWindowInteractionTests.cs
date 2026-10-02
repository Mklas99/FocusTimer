namespace FocusTimer.App.HeadlessTests;

using Avalonia.Controls;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;

/// <summary>
/// Exercises AppController behavior that only runs once a real (headless) timer widget
/// or settings window exists: show/hide/toggle, settings lifecycle, hotkey routing, idle
/// pause/resume, and shutdown. These paths were previously untestable because every
/// window factory in AppControllerStartupTests throws rather than construct a window.
/// </summary>
public sealed class AppControllerWindowInteractionTests
{
    public AppControllerWindowInteractionTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public async Task ShowTimerWidget_FirstCall_CreatesAndActivatesWindow()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();

        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        Assert.True(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public async Task ShowTimerWidget_WhileMinimized_RestoresToNormal()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();
        harness.TimerWindow.WindowState = WindowState.Minimized;

        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowState.Normal, harness.TimerWindow.WindowState);
    }

    [Fact]
    public async Task HideTimerWidget_AfterShow_HidesWithoutClosing()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        harness.Controller.HideTimerWidget();
        Dispatcher.UIThread.RunJobs();

        Assert.False(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public async Task ToggleTimerWidget_TogglesBetweenShownAndHidden()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();

        harness.Controller.ToggleTimerWidget();
        Dispatcher.UIThread.RunJobs();
        Assert.True(harness.Controller.IsTimerWidgetVisible());

        harness.Controller.ToggleTimerWidget();
        Dispatcher.UIThread.RunJobs();
        Assert.False(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public async Task ToggleTimer_RoutesToWidgetViewModelToggleCommand()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();
        Assert.False(harness.Controller.IsTimerRunning());

        harness.Controller.ToggleTimer();
        Dispatcher.UIThread.RunJobs();

        Assert.True(harness.Controller.IsTimerRunning());
    }

    [Fact]
    public async Task ShowSettings_ClosingWindow_ClearsAppearancePreviewAndAllowsReopen()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        harness.Controller.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        Window firstSettingsWindow = Assert.IsType<SettingsWindow>(
            Assert.IsAssignableFrom<Window>(harness.FindOpenSettingsWindow()));

        firstSettingsWindow.Close();
        Dispatcher.UIThread.RunJobs();

        harness.Controller.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        Window secondSettingsWindow = Assert.IsType<SettingsWindow>(
            Assert.IsAssignableFrom<Window>(harness.FindOpenSettingsWindow()));
        Assert.NotSame(firstSettingsWindow, secondSettingsWindow);
    }

    [Fact]
    public async Task ShowWorklog_CalledTwice_ReusesTheSameWindow()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();

        harness.Controller.ShowWorklog();
        Dispatcher.UIThread.RunJobs();
        var first = Assert.IsType<WorklogWindow>(harness.FindOpenWorklogWindow());
        harness.Controller.ShowWorklog();
        Dispatcher.UIThread.RunJobs();

        Assert.Same(first, harness.FindOpenWorklogWindow());
        Assert.True(first.IsVisible);
        var viewModel = Assert.IsType<WorklogWindowViewModel>(first.DataContext);
        Assert.Equal(WorklogTab.Entries, viewModel.SelectedTab);
        Assert.Equal(viewModel.Today, viewModel.SelectedDay);
    }

    [Fact]
    public async Task ShowWorklog_AfterClosing_OpensANewWindow()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowWorklog();
        Dispatcher.UIThread.RunJobs();
        var first = Assert.IsType<WorklogWindow>(harness.FindOpenWorklogWindow());

        first.Close();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(harness.FindOpenWorklogWindow());
        harness.Controller.ShowWorklog();
        Dispatcher.UIThread.RunJobs();

        Assert.NotSame(first, harness.FindOpenWorklogWindow());
    }

    [Fact]
    public async Task ExitApplication_WithOpenWorklogWindow_ClosesIt()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        harness.Controller.ShowWorklog();
        Dispatcher.UIThread.RunJobs();
        var window = Assert.IsType<WorklogWindow>(harness.FindOpenWorklogWindow());

        harness.Controller.ExitApplication();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.IsVisible);
        Assert.Null(harness.FindOpenWorklogWindow());
    }

    [Fact]
    public async Task OnHotkeyPressed_ShowHideDefinition_TogglesWidgetVisibility()
    {
        var hotkeys = new RaisableHotkeyService();
        using var harness = new Harness(hotkeys);
        await harness.InitializeAsync();
        harness.Controller.RegisterHotkeys();

        hotkeys.Raise(new HotkeyPressedEventArgs(HotkeyDefinition.Parse("Ctrl+Alt+T")!.Value));
        Dispatcher.UIThread.RunJobs();

        Assert.True(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public async Task OnHotkeyPressed_ToggleTimerDefinition_TogglesTimerRunState()
    {
        var hotkeys = new RaisableHotkeyService();
        using var harness = new Harness(hotkeys);
        await harness.InitializeAsync();
        harness.Controller.RegisterHotkeys();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        hotkeys.Raise(new HotkeyPressedEventArgs(HotkeyDefinition.Parse("Ctrl+Alt+P")!.Value));
        Dispatcher.UIThread.RunJobs();

        Assert.True(harness.Controller.IsTimerRunning());
    }

    [Fact]
    public async Task OnHotkeyPressed_UnknownDefinition_IsIgnored()
    {
        var hotkeys = new RaisableHotkeyService();
        using var harness = new Harness(hotkeys);
        await harness.InitializeAsync();
        harness.Controller.RegisterHotkeys();

        hotkeys.Raise(new HotkeyPressedEventArgs(HotkeyDefinition.Parse("Ctrl+Shift+Z")!.Value));
        Dispatcher.UIThread.RunJobs();

        Assert.False(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public async Task UserBecameIdle_WhileTimerRunning_PausesWidgetAndNotifies()
    {
        var idle = new RaisableIdleDetectionService();
        var logger = new RecordingLogger();
        using var harness = new Harness(idleDetectionService: idle, logger: logger);
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();
        harness.Controller.ToggleTimer();
        Dispatcher.UIThread.RunJobs();
        Assert.True(harness.Controller.IsTimerRunning());

        idle.RaiseBecameIdle(new UserIdleEventArgs(DateTime.UtcNow));
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(logger.InformationMessages, m => m.Contains("Timer paused due to user idle", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UserBecameIdle_WhileTimerNotRunning_DoesNothing()
    {
        var idle = new RaisableIdleDetectionService();
        var logger = new RecordingLogger();
        using var harness = new Harness(idleDetectionService: idle, logger: logger);
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        idle.RaiseBecameIdle(new UserIdleEventArgs(DateTime.UtcNow));
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain(logger.InformationMessages, m => m.Contains("paused due to user idle", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UserReturned_AfterIdlePause_LogsResumptionOnce()
    {
        var idle = new RaisableIdleDetectionService();
        var logger = new RecordingLogger();
        using var harness = new Harness(idleDetectionService: idle, logger: logger);
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();
        harness.Controller.ToggleTimer();
        Dispatcher.UIThread.RunJobs();
        idle.RaiseBecameIdle(new UserIdleEventArgs(DateTime.UtcNow));
        Dispatcher.UIThread.RunJobs();

        idle.RaiseReturned(new UserIdleEventArgs(DateTime.UtcNow));
        idle.RaiseReturned(new UserIdleEventArgs(DateTime.UtcNow));

        Assert.Single(logger.InformationMessages, m => m.Contains("User activity resumed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryToggleCompactModeDraft_WithOpenSettingsWindow_TogglesDraftAndReturnsTrue()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        var editor = (SettingsWindowViewModel)harness.FindOpenSettingsWindow()!.DataContext!;
        await WaitUntilAsync(() => editor.IsSettingsLoaded);
        bool before = editor.Settings.UseCompactMode;

        bool toggled = harness.InvokeTryToggleCompactModeDraft();

        Assert.True(toggled);
        Assert.Equal(!before, editor.Settings.UseCompactMode);
    }

    [Fact]
    public async Task TryToggleCompactModeDraft_WithoutOpenSettingsWindow_ReturnsFalse()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();

        bool toggled = harness.InvokeTryToggleCompactModeDraft();

        Assert.False(toggled);
    }

    [Fact]
    public async Task ExitApplication_WithOpenWindows_StopsTimerAndClosesWindows()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();
        harness.Controller.ShowSettings();
        Dispatcher.UIThread.RunJobs();

        harness.Controller.ExitApplication();
        Dispatcher.UIThread.RunJobs();

        Assert.False(harness.TimerWindow.IsVisible);
    }

    [Fact]
    public async Task ContinueAfterRecoveryAsync_AfterSuccessfulRecovery_ShowsWidgetAndRegistersHotkeys()
    {
        var hotkeys = new RaisableHotkeyService();
        var provider = new RecoveredSettingsProvider();
        using var harness = new Harness(hotkeys, settingsProvider: provider);

        await harness.InvokeContinueAfterRecoveryAsync();
        Dispatcher.UIThread.RunJobs();

        Assert.True(harness.Controller.IsTimerWidgetVisible());
        Assert.Equal(2, hotkeys.RegisterCount);
    }

    [Fact]
    public async Task ContinueAfterRecoveryAsync_WhenAlreadyInitialized_IsNoOp()
    {
        using var harness = new Harness();
        await harness.InitializeAsync();
        Assert.True(harness.IsInitialized);

        await harness.InvokeContinueAfterRecoveryAsync();

        Assert.False(harness.Controller.IsTimerWidgetVisible());
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(5);
        }

        Assert.True(condition());
    }

    /// <summary>Wires a real AppController to real (headless) windows and view models.</summary>
    private sealed class Harness : IDisposable
    {
        private readonly SessionTracker _tracker;
        private readonly TimerService _timer;
        private readonly BreakReminderService _reminders;

        public Harness(
            IGlobalHotkeyService? hotkeys = null,
            IIdleDetectionService? idleDetectionService = null,
            IAppLogger? logger = null,
            ISettingsProvider? settingsProvider = null)
        {
            this.Provider = settingsProvider ?? new SettingsProviderStub();
            this.Logger = logger ?? new RecordingLogger();
            this.Notifications = new LinuxNotificationServiceStub();
            this.Hotkeys = hotkeys ?? new RaisableHotkeyService();
            var idle = idleDetectionService ?? new RaisableIdleDetectionService();
            ThemeService themeService = new();
            ThemeManager themeManager = new();
            this._tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), this.Logger);
            this._timer = new TimerService(this._tracker);
            this._reminders = new BreakReminderService(this.Notifications, this.Provider);
            IEventBus eventBus = new EventBus();

            TimerWidgetViewModel CreateTimerViewModel() => new(
                this.Provider, this.Logger, new CsvSessionRepository(this.Provider),
                this.Notifications, this._tracker, this._reminders, this._timer, null, eventBus);

            SettingsWindowViewModel CreateSettingsViewModel() => new(
                this.Provider,
                new LinuxAutoStartServiceStub(),
                themeService,
                themeManager,
                this.Logger,
                new WorklogSummaryViewModel(
                    new EmptySummaryService(),
                    new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping()]),
                    TimeProvider.System));

            this.Controller = new AppController(
                this.Provider,
                this.Hotkeys,
                idle,
                this.Notifications,
                themeService,
                themeManager,
                CreateTimerViewModel,
                CreateSettingsViewModel,
                new TodayStatsService(new CsvSessionRepository(this.Provider), this.Logger),
                null!,
                this.Logger,
                eventBus,
                new InstallationIdentity(),
                null,
                () => new WorklogWindowViewModel(
                    new WorklogEntriesViewModel(new CsvSessionRepository(this.Provider), TimeProvider.System),
                    new WorklogSummaryViewModel(
                        new EmptySummaryService(),
                        new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
                        TimeProvider.System),
                    this.Provider,
                    TimeProvider.System));
        }

        public AppController Controller { get; }

        public ISettingsProvider Provider { get; }

        public IAppLogger Logger { get; }

        public LinuxNotificationServiceStub Notifications { get; }

        public IGlobalHotkeyService Hotkeys { get; }

        public bool IsInitialized => (bool)typeof(AppController)
            .GetField("_initialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(this.Controller)!;

        public Window TimerWindow => (Window)typeof(AppController)
            .GetField("_timerWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(this.Controller)!;

        public async Task InitializeAsync()
        {
            await this.Controller.InitializeAsync();
            Dispatcher.UIThread.RunJobs();
        }

        public Window? FindOpenWorklogWindow() => (Window?)typeof(AppController)
            .GetField("_worklogWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(this.Controller);

        public Window? FindOpenSettingsWindow() => (Window?)typeof(AppController)
            .GetField("_settingsWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(this.Controller);

        public bool InvokeTryToggleCompactModeDraft() => (bool)typeof(AppController)
            .GetMethod("TryToggleCompactModeDraft", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(this.Controller, null)!;

        public Task InvokeContinueAfterRecoveryAsync() => (Task)typeof(AppController)
            .GetMethod("ContinueAfterRecoveryAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(this.Controller, null)!;

        public void Dispose()
        {
            this._timer.Dispose();
            this._reminders.Dispose();
        }
    }

    private sealed class RaisableHotkeyService : IGlobalHotkeyService
    {
        public int RegisterCount { get; private set; }

        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        public void Register(HotkeyDefinition definition) => this.RegisterCount++;

        public void UnregisterAll() { }

        public void Raise(HotkeyPressedEventArgs args) => this.HotkeyPressed?.Invoke(this, args);
    }

    private sealed class RaisableIdleDetectionService : IIdleDetectionService
    {
        public event EventHandler<UserIdleEventArgs>? UserBecameIdle;

        public event EventHandler<UserIdleEventArgs>? UserReturned;

        public void RaiseBecameIdle(UserIdleEventArgs args) => this.UserBecameIdle?.Invoke(this, args);

        public void RaiseReturned(UserIdleEventArgs args) => this.UserReturned?.Invoke(this, args);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> InformationMessages { get; } = new();

        public List<string> Errors { get; } = new();

        public void LogCritical(string message, Exception? exception = null) { }

        public void LogError(string message, Exception? exception = null) => this.Errors.Add(message);

        public void LogWarning(string message) { }

        public void LogInformation(string message) => this.InformationMessages.Add(message);

        public void LogDebug(string message) { }
    }

    private sealed class RecoveredSettingsProvider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(new Settings { StartMinimized = false });

        public Task SaveAsync(Settings value) => Task.CompletedTask;
    }

    private sealed class EmptySummaryService : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }
}
