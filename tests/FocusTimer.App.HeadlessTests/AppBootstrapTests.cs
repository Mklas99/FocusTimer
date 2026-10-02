namespace FocusTimer.App.HeadlessTests;

using System.Reflection;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;

/// <summary>
/// Covers App.axaml.cs's bootstrap (IAppInitializer.InitializeAsync, InitializeAppAsync's
/// three startup branches, tray menu delegation, and shutdown cleanup), none of which any
/// prior test could reach without constructing a real tray icon and a real AppController.
/// Each test gets its own App instance (never assigned to Application.Current) so state
/// never leaks between tests despite this assembly's single-threaded headless dispatcher.
/// </summary>
public sealed class AppBootstrapTests
{
    public AppBootstrapTests() => HeadlessAvaloniaFixture.EnsureInitialized();

    [Fact]
    public void InitializeAsync_NormalStartup_ShowsWidgetAndRegistersHotkeys()
    {
        var hotkeys = new RecordingHotkeys();
        using var harness = new ControllerHarness(hotkeys);
        var app = new FocusTimer.App.App();

        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);

        Assert.True(harness.Controller.IsTimerWidgetVisible());
        Assert.Equal(2, hotkeys.RegisterCount);
    }

    [Fact]
    public void InitializeAsync_StartupRecoveryRequired_ShowsSettingsInsteadOfWidget()
    {
        using var harness = new ControllerHarness(settingsProvider: new RecoveryRequiredProvider());
        var app = new FocusTimer.App.App();

        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);

        Assert.False(harness.Controller.IsTimerWidgetVisible());
        Assert.NotNull(GetSettingsWindow(harness.Controller));
    }

    [Fact]
    public void InitializeAsync_GivenWrongControllerType_LogsErrorInsteadOfCrashing()
    {
        var app = new FocusTimer.App.App();
        var logger = new RecordingLogger();

        RunInitializeAsync(app, "not a controller", logger, null, null);

        Assert.Contains(logger.Errors, m => m.Contains("Error initializing application.", StringComparison.Ordinal));
    }

    [Fact]
    public void InitializeAsync_WithTrayIconController_RegistersTrayIconOnBoth()
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        var trayController = new RecordingTrayIconController();

        RunInitializeAsync(app, harness.Controller, harness.Logger, trayController, null);

        Assert.NotNull(trayController.ReceivedTrayIcon);
    }

    [Theory]
    [InlineData("TrayIcon_Clicked")]
    [InlineData("TrayMenu_ShowHide")]
    public void TrayClickHandlers_ToggleWidgetViaController(string methodName)
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);
        // Normal startup already shows the widget (StartMinimized defaults to false).
        Assert.True(harness.Controller.IsTimerWidgetVisible());

        InvokePrivate(app, methodName, null, EventArgs.Empty);
        Dispatcher.UIThread.RunJobs();

        Assert.False(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public void TrayMenu_ToggleTimer_TogglesTimerViaController()
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();
        Assert.False(harness.Controller.IsTimerRunning());

        InvokePrivate(app, "TrayMenu_ToggleTimer", null, EventArgs.Empty);
        Dispatcher.UIThread.RunJobs();

        Assert.True(harness.Controller.IsTimerRunning());
    }

    [Fact]
    public void TrayMenu_Settings_ShowsSettingsViaController()
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);
        Assert.Null(GetSettingsWindow(harness.Controller));

        InvokePrivate(app, "TrayMenu_Settings", null, EventArgs.Empty);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(GetSettingsWindow(harness.Controller));
    }

    [Fact]
    public void TrayMenu_Worklog_ShowsWorklogViaController()
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);
        Assert.Null(GetWorklogWindow(harness.Controller));

        InvokePrivate(app, "TrayMenu_Worklog", null, EventArgs.Empty);
        Dispatcher.UIThread.RunJobs();

        Assert.NotNull(GetWorklogWindow(harness.Controller));
    }

    [Fact]
    public void TrayMenu_HasWorklogDirectlyAboveSettings()
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        var trayController = new RecordingTrayIconController();

        RunInitializeAsync(app, harness.Controller, harness.Logger, trayController, null);

        var items = trayController.ReceivedTrayIcon!.Menu!.Items
            .Select(item => (item as NativeMenuItem)?.Header ?? "-")
            .ToList();
        Assert.Equal(["Show/Hide Timer", "Start/Pause Timer", "-", "Worklog...", "Settings...", "-", "Exit"], items);
    }

    [Fact]
    public void TrayMenu_Exit_ExitsViaController()
    {
        using var harness = new ControllerHarness();
        var app = new FocusTimer.App.App();
        RunInitializeAsync(app, harness.Controller, harness.Logger, null, null);
        harness.Controller.ShowTimerWidget();
        Dispatcher.UIThread.RunJobs();

        InvokePrivate(app, "TrayMenu_Exit", null, EventArgs.Empty);
        Dispatcher.UIThread.RunJobs();

        Assert.False(harness.Controller.IsTimerWidgetVisible());
    }

    [Fact]
    public void OnShutdownRequested_DisposesTheServiceProvider()
    {
        var app = new FocusTimer.App.App();
        var provider = new DisposableProvider();
        SetField(app, "_serviceProvider", provider);

        InvokePrivate(app, "OnShutdownRequested", null, null!);

        Assert.True(provider.Disposed);
    }

    [Fact]
    public void OnShutdownRequested_WhenDisposeThrows_LogsErrorInsteadOfCrashing()
    {
        var app = new FocusTimer.App.App();
        var logger = new RecordingLogger();
        SetField(app, "_logger", logger);
        SetField(app, "_serviceProvider", new ThrowingDisposableProvider());

        InvokePrivate(app, "OnShutdownRequested", null, null!);

        Assert.Contains(logger.Errors, m => m.Contains("Error during application shutdown.", StringComparison.Ordinal));
    }

    // InitializeAppAsync (called from IAppInitializer.InitializeAsync) awaits
    // Dispatcher.UIThread.InvokeAsync, which needs this very thread to pump the queue.
    // Awaiting it directly would deadlock, so start it and pump concurrently instead —
    // the same fix TrayStateControllerTests uses for the analogous cross-thread case.
    private static void RunInitializeAsync(
        FocusTimer.App.App app, object? controller, IAppLogger? logger, object? trayController, object? serviceProvider)
    {
        Task init = ((IAppInitializer)app).InitializeAsync(controller, logger, trayController, serviceProvider);
        for (int attempt = 0; attempt < 200 && !init.IsCompleted; attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }

        Assert.True(init.IsCompleted);
        init.GetAwaiter().GetResult();
    }

    private static Window? GetWorklogWindow(AppController controller) => (Window?)typeof(AppController)
        .GetField("_worklogWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(controller);

    private static Window? GetSettingsWindow(AppController controller) => (Window?)typeof(AppController)
        .GetField("_settingsWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(controller);

    private static void InvokePrivate(object target, string methodName, params object?[] args) =>
        target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    private static void SetField(object target, string fieldName, object? value) =>
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    /// <summary>Wires a real AppController with real (headless) windows, matching AppControllerWindowInteractionTests.</summary>
    private sealed class ControllerHarness : IDisposable
    {
        private readonly SessionTracker _tracker;
        private readonly TimerService _timer;
        private readonly BreakReminderService _reminders;

        public ControllerHarness(IGlobalHotkeyService? hotkeys = null, ISettingsProvider? settingsProvider = null)
        {
            ISettingsProvider provider = settingsProvider ?? new SettingsProviderStub();
            this.Logger = new RecordingLogger();
            var notifications = new LinuxNotificationServiceStub();
            IGlobalHotkeyService hotkeyService = hotkeys ?? new RecordingHotkeys();
            ThemeService themeService = new();
            ThemeManager themeManager = new();
            this._tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), this.Logger);
            this._timer = new TimerService(this._tracker);
            this._reminders = new BreakReminderService(notifications, provider);
            IEventBus eventBus = new EventBus();

            TimerWidgetViewModel CreateTimerViewModel() => new(
                provider, this.Logger, new CsvSessionRepository(provider),
                notifications, this._tracker, this._reminders, this._timer, null, eventBus);

            SettingsWindowViewModel CreateSettingsViewModel() => new(
                provider,
                new LinuxAutoStartServiceStub(),
                themeService,
                themeManager,
                this.Logger);

            this.Controller = new AppController(
                provider,
                hotkeyService,
                new RaisableIdleDetectionService(),
                notifications,
                themeService,
                themeManager,
                CreateTimerViewModel,
                CreateSettingsViewModel,
                new TodayStatsService(new CsvSessionRepository(provider), this.Logger),
                null!,
                this.Logger,
                eventBus,
                new InstallationIdentity(),
                null,
                () => new WorklogWindowViewModel(
                    new WorklogEntriesViewModel(new CsvSessionRepository(provider), TimeProvider.System),
                    new WorklogSummaryViewModel(
                        new EmptySummaryService(),
                        new WorklogGroupingRegistry([new ApplicationGrouping(), new ProjectGrouping(), new WindowGrouping()]),
                        TimeProvider.System),
                    provider,
                    TimeProvider.System));
        }

        public AppController Controller { get; }

        public RecordingLogger Logger { get; }

        public void Dispose()
        {
            this._timer.Dispose();
            this._reminders.Dispose();
        }
    }

    private sealed class RecordingHotkeys : IGlobalHotkeyService
    {
        public int RegisterCount { get; private set; }

        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed { add { } remove { } }

        public void Register(HotkeyDefinition definition) => this.RegisterCount++;

        public void UnregisterAll() { }
    }

    private sealed class RaisableIdleDetectionService : IIdleDetectionService
    {
        public event EventHandler<UserIdleEventArgs>? UserBecameIdle { add { } remove { } }

        public event EventHandler<UserIdleEventArgs>? UserReturned { add { } remove { } }
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

    private sealed class RecoveryRequiredProvider : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromException<Settings>(new SettingsRecoveryRequiredException());

        public Task SaveAsync(Settings value) => Task.CompletedTask;
    }

    private sealed class EmptySummaryService : IWorklogSummaryService
    {
        public Task<WorklogSummary> SummarizeAsync(WorklogSummaryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorklogSummary(request, WorklogOutcome.Success(), [], TimeSpan.Zero, []));
    }

    private sealed class RecordingTrayIconController : ITrayIconController
    {
        public Avalonia.Controls.TrayIcon? ReceivedTrayIcon { get; private set; }

        public event EventHandler<MenuActionEventArgs>? MenuAction { add { } remove { } }

        public event EventHandler? OnEntriesLogged { add { } remove { } }

        public void SetTrayIcon(Avalonia.Controls.TrayIcon trayIcon) => this.ReceivedTrayIcon = trayIcon;

        public void UpdateState(TimerState state) { }

        public void ShowMenu() { }

        public void HideMenu() { }

        public void RaiseEntriesLogged(IEnumerable<FocusTimer.Core.Models.TimeEntry> entries) { }
    }

    private sealed class DisposableProvider : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => this.Disposed = true;
    }

    private sealed class ThrowingDisposableProvider : IDisposable
    {
        public void Dispose() => throw new InvalidOperationException("Synthetic dispose failure.");
    }
}
