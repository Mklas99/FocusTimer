namespace FocusTimer.App.Services
{
    using System;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Threading;
    using FocusTimer.App.ViewModels;
    using FocusTimer.App.Views;
    using FocusTimer.Core;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;

    /// <summary>
    /// Central coordinator for managing application windows and lifecycle.
    /// Handles show/hide logic, settings changes, and clean shutdown.
    /// </summary>
    public class AppController
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly ProjectRuleStore? _projectRuleStore;
        private readonly IAutoStartService? _autoStartService;
        private readonly IGlobalHotkeyService _hotkeyService;
        private readonly INotificationService _notificationService;
        private readonly IThemeService _themeService;
        private readonly ThemeManager _themeManager;
        private readonly Func<TimerWidgetViewModel> _timerViewModelFactory;
        private readonly Func<SettingsWindowViewModel> _settingsViewModelFactory;
        private readonly Func<WorklogWindowViewModel>? _worklogViewModelFactory;
        private readonly ITrayIconController? _trayIconController;
        private readonly IAppLogger _logWriter;
        private readonly InstallationIdentity _installationIdentity;
        private TrayIcon? _trayIcon;
        private TimerWidgetWindow? _timerWindow;
        private SettingsWindow? _settingsWindow;
        private WorklogWindow? _worklogWindow;
        private HotkeyDefinition _showHideHotkeyDefinition;
        private HotkeyDefinition _toggleTimerHotkeyDefinition;
        private bool _pausedByIdle;
        private bool _initialized;
        private bool _hotkeysConfigured;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppController"/> class.
        /// </summary>
        /// <param name="settingsProvider">Provider for application settings.</param>
        /// <param name="hotkeyService">Service for handling global hotkeys.</param>
        /// <param name="idleDetectionService">Service for detecting user idle state.</param>
        /// <param name="notificationService">Service for displaying notifications.</param>
        /// <param name="themeService">Service for theme operations.</param>
        /// <param name="themeManager">Manager for theme-related functionality.</param>
        /// <param name="timerViewModelFactory">Factory for creating timer view model instances.</param>
        /// <param name="settingsViewModelFactory">Factory for creating settings window view model instances.</param>
        /// <param name="todayStatsService">Service for managing today's statistics.</param>
        /// <param name="trayIconController">Controller for managing the system tray icon.</param>
        /// <param name="logWriter">Logger for application logging.</param>
        /// <param name="eventBus">Event bus for subscribing to application-level events.</param>
        /// <param name="installationIdentity">Cached identity for worklog entries.</param>
        /// <param name="autoStartService">Optional start-on-login service for recovery.</param>
        /// <param name="worklogViewModelFactory">Optional factory for the Worklog window view model; without it the window cannot be opened.</param>
        /// <param name="projectRuleStore">Optional holder that receives the applied project rules for read-time project resolution.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Constructor injection of dependencies.")]
        public AppController(
            ISettingsProvider settingsProvider,
            IGlobalHotkeyService hotkeyService,
            IIdleDetectionService idleDetectionService,
            INotificationService notificationService,
            IThemeService themeService,
            ThemeManager themeManager,
            Func<TimerWidgetViewModel> timerViewModelFactory,
            Func<SettingsWindowViewModel> settingsViewModelFactory,
            TodayStatsService todayStatsService,
            ITrayIconController trayIconController,
            IAppLogger logWriter,
            IEventBus? eventBus,
            InstallationIdentity installationIdentity,
            IAutoStartService? autoStartService = null,
            Func<WorklogWindowViewModel>? worklogViewModelFactory = null,
            ProjectRuleStore? projectRuleStore = null)
        {
            this._projectRuleStore = projectRuleStore;
            this._settingsProvider = settingsProvider;
            this._autoStartService = autoStartService;
            this._hotkeyService = hotkeyService;
            this._notificationService = notificationService;
            this._themeService = themeService;
            this._themeManager = themeManager;
            this._timerViewModelFactory = timerViewModelFactory;
            this._settingsViewModelFactory = settingsViewModelFactory;
            this._worklogViewModelFactory = worklogViewModelFactory;
            this.CurrentSettings = new Settings();
            this._trayIconController = trayIconController;
            this._logWriter = logWriter;
            this._installationIdentity = installationIdentity;

            this._showHideHotkeyDefinition = this.ParseHotkeyOrDefault(this.CurrentSettings.HotkeyShowHide, "Ctrl+Alt+T");
            this._toggleTimerHotkeyDefinition = this.ParseHotkeyOrDefault(this.CurrentSettings.HotkeyToggleTimer, "Ctrl+Alt+P");
            this._hotkeyService.HotkeyPressed += this.OnHotkeyPressed;
            idleDetectionService.UserBecameIdle += this.OnUserBecameIdle;
            idleDetectionService.UserReturned += this.OnUserReturned;

            // Subscribe to EntriesLoggedEvent via event bus
            if (eventBus != null)
            {
                eventBus.Subscribe<EntriesLoggedEvent>(e => this.OnEntriesLogged(e.Entries));
            }
        }

        /// <summary>
        /// Gets get the current settings (for initialization purposes).
        /// </summary>
        public Settings CurrentSettings { get; private set; }

        /// <summary>Gets a value indicating whether startup settings recovery remains unfinished.</summary>
        public bool StartupRecoveryRequired { get; private set; }

        /// <summary>Gets a value indicating whether saved settings failed to load at startup.</summary>
        public bool StartupSettingsLoadFailed { get; private set; }

        /// <summary>
        /// Initialize the controller and load settings.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            // Load settings before exposing timer actions, even if theme resources fail.
            try
            {
                this._themeManager.InitializeThemeResources();
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to initialize theme resources.", ex);
            }

            try
            {
                if (this._autoStartService != null)
                {
                    var coordinator = new SettingsCommitCoordinator(
                        this._settingsProvider, this._autoStartService, _ => Task.CompletedTask);
                    if (!await coordinator.RecoverAsync())
                    {
                        this.StartupRecoveryRequired = true;
                        this._logWriter.LogError("Settings recovery is required before startup can activate settings.");
                        return;
                    }
                }

                this.CurrentSettings = await this._settingsProvider.LoadAsync();
                this._projectRuleStore?.Update(this.CurrentSettings.ProjectRules);
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to load settings.", ex);
                this.StartupSettingsLoadFailed = true;
                return;
            }

            this._installationIdentity.Initialize(this.CurrentSettings.DeviceId);
            this.StartupRecoveryRequired = false;
            this.StartupSettingsLoadFailed = false;

            try
            {
                if (!string.IsNullOrEmpty(this.CurrentSettings.ActiveThemeName))
                {
                    Theme? theme = this._themeService.GetBuiltInTheme(this.CurrentSettings.ActiveThemeName);

                    // A matching saved theme can contain edited appearance values.
                    // Use the factory preset only for first-run defaults or a name mismatch.
                    if (theme != null && !string.Equals(
                            this.CurrentSettings.Theme.ThemeName,
                            theme.ThemeName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        this.CurrentSettings.Theme = theme;
                    }
                }

                this._themeManager.ApplyTheme(this.CurrentSettings.Theme);
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to apply saved theme; using the default theme.", ex);
                this.CurrentSettings.ActiveThemeName = "Dark";
                this.CurrentSettings.Theme = new Theme { ThemeName = "Dark" };

                try
                {
                    this._themeManager.ApplyTheme(this.CurrentSettings.Theme);
                }
                catch (Exception fallbackEx)
                {
                    this._logWriter.LogError("Failed to apply the default theme.", fallbackEx);
                }
            }

            try
            {
                if (this._timerWindow == null)
                {
                    TimerWidgetViewModel viewModel = this._timerViewModelFactory();
                    viewModel.SetCompactModeDraftToggle(this.TryToggleCompactModeDraft);
                    viewModel.ApplySettings(this.CurrentSettings);
                    this._timerWindow = new TimerWidgetWindow(this._themeManager)
                    {
                        DataContext = viewModel,
                    };
                }
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to create timer widget.", ex);
            }

            this._initialized = true;
        }

        /// <summary>
        /// Register global hotkeys. Call this after timer window is created.
        /// </summary>
        public void RegisterHotkeys()
        {
            if (!this._initialized)
            {
                return;
            }

            try
            {
                this.RegisterHotkeysCore();
            }
            catch (Exception ex)
            {
                this._logWriter.LogError("Failed to register hotkeys.", ex);
            }
        }

        /// <summary>
        /// Show or create the timer widget window.
        /// </summary>
        public void ShowTimerWidget()
        {
            if (!this._initialized)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (this._timerWindow == null)
                    {
                        TimerWidgetViewModel viewModel = this._timerViewModelFactory();
                        viewModel.SetCompactModeDraftToggle(this.TryToggleCompactModeDraft);
                        this._timerWindow = new TimerWidgetWindow(this._themeManager)
                        {
                            DataContext = viewModel,
                        };

                        // If tray icon is available, set it in the controller
                        if (this._trayIcon != null && this._trayIconController != null)
                        {
                            (this._trayIconController as TrayStateController)?.SetTrayIcon(this._trayIcon);
                        }

                        viewModel.ApplySettings(this.CurrentSettings);
                        if (this._settingsWindow?.DataContext is SettingsWindowViewModel { CanEdit: true } editor)
                        {
                            viewModel.PreviewAppearance(editor.Settings);
                        }
                    }

                    if (this._timerWindow.WindowState == WindowState.Minimized)
                    {
                        this._timerWindow.WindowState = WindowState.Normal;
                    }

                    this._timerWindow.Show();
                    this._timerWindow.Activate();
                }
                catch (Exception ex)
                {
                    this._logWriter.LogError("Failed to show timer widget window.", ex);
                }
            });
        }

        /// <summary>
        /// Hide the timer widget window without closing it.
        /// </summary>
        public void HideTimerWidget()
        {
            Dispatcher.UIThread.Post(() =>
            {
                this._timerWindow?.Hide();
            });
        }

        /// <summary>
        /// Toggle timer widget visibility.
        /// </summary>
        public void ToggleTimerWidget()
        {
            if (!this._initialized)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (this._timerWindow == null || !this._timerWindow.IsVisible)
                {
                    this.ShowTimerWidget();
                }
                else
                {
                    this.HideTimerWidget();
                }
            });
        }

        /// <summary>
        /// Check if timer widget is visible.
        /// </summary>
        /// <returns>True if the timer widget is visible; otherwise, false.</returns>
        public bool IsTimerWidgetVisible()
        {
            return this._timerWindow?.IsVisible ?? false;
        }

        /// <summary>
        /// Start or pause the timer.
        /// </summary>
        public void ToggleTimer()
        {
            if (!this._initialized)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (this._timerWindow?.DataContext is TimerWidgetViewModel vm)
                {
                    vm.ToggleCommand.Execute(null);
                }
            });
        }

        /// <summary>
        /// Check if timer is running.
        /// </summary>
        /// <returns>True if the timer is running; otherwise, false.</returns>
        public bool IsTimerRunning()
        {
            return this._timerWindow?.DataContext is TimerWidgetViewModel vm && vm.IsRunning;
        }

        /// <summary>
        /// Show the settings window.
        /// </summary>
        public void ShowSettings()
        {
            if (!this._initialized && !this.StartupRecoveryRequired && !this.StartupSettingsLoadFailed)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (this._settingsWindow == null)
                {
                    SettingsWindowViewModel viewModel = this._settingsViewModelFactory();
                    this._settingsWindow = new SettingsWindow
                    {
                        DataContext = viewModel,
                    };

                    viewModel.SetRuntimeActivator(this.ActivateSettingsAsync);
                    viewModel.SetAppearancePreview(settings =>
                    {
                        (this._timerWindow?.DataContext as TimerWidgetViewModel)?.PreviewAppearance(settings);
                    });
                    viewModel.SetRecoveryCompleted(this.ContinueAfterRecoveryAsync);

                    // Handle window closed event
                    this._settingsWindow.Closed += (s, e) =>
                    {
                        (this._timerWindow?.DataContext as TimerWidgetViewModel)?.ClearAppearancePreview();
                        this._settingsWindow = null;
                    };
                }

                this._settingsWindow.Show();
                this._settingsWindow.Activate();
            });
        }

        /// <summary>
        /// Show the Worklog window: one instance, brought to the front when it is already open.
        /// </summary>
        public void ShowWorklog()
        {
            if (!this._initialized || this._worklogViewModelFactory is null)
            {
                return;
            }

            Dispatcher.UIThread.Post(async () =>
            {
                WorklogWindowViewModel? opening = null;
                if (this._worklogWindow == null)
                {
                    opening = this._worklogViewModelFactory();
                    this._worklogWindow = new WorklogWindow
                    {
                        DataContext = opening,
                    };
                    this._worklogWindow.Closed += (s, e) => this._worklogWindow = null;
                }

                this._worklogWindow.Show();
                this._worklogWindow.Activate();
                if (opening is null)
                {
                    return;
                }

                try
                {
                    await opening.OpenAsync();
                }
                catch (Exception ex)
                {
                    this._logWriter.LogError("Loading the Worklog window failed.", ex);
                }
            });
        }

        /// <summary>
        /// Exit the application cleanly.
        /// Stops timer, flushes logs, and disposes resources.
        /// </summary>
        public void ExitApplication()
        {
            try
            {
                // Unregister hotkeys
                this._hotkeyService.UnregisterAll();

                // Stop timer and flush entries
                if (this._timerWindow?.DataContext is TimerWidgetViewModel vm)
                {
                    vm.Dispose();
                }

                // Mark windows for actual closure (not hide)
                if (this._timerWindow != null)
                {
                    this._timerWindow.IsAppShuttingDown = true;
                }

                // Close all windows
                this._settingsWindow?.Close();
                this._worklogWindow?.Close();
                this._timerWindow?.Close();

                // Shutdown application
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
            catch (Exception ex)
            {
                this._logWriter.LogError($"Error during exit.", ex);

                // Force shutdown anyway
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown(1);
                }
            }
        }

        /// <summary>
        /// Call this after time entries are logged to update tray tooltip.
        /// </summary>
        /// <param name="entries">The time entries that were logged.</param>
        public void OnEntriesLogged(System.Collections.Generic.IEnumerable<FocusTimer.Core.Models.TimeEntry> entries)
        {
            this._trayIconController?.RaiseEntriesLogged(entries);
        }

        /// <summary>
        /// Set the tray icon instance.
        /// </summary>
        /// <param name="trayIcon">The tray icon instance to set.</param>
        public void SetTrayIcon(TrayIcon trayIcon)
        {
            this._trayIcon = trayIcon;
            this._trayIconController?.SetTrayIcon(this._trayIcon);
        }

        private static bool IsHotkeyMatch(HotkeyDefinition left, HotkeyDefinition right)
        {
            return left.KeyCode == right.KeyCode && left.Modifiers == right.Modifiers;
        }

        private HotkeyDefinition ParseHotkeyOrDefault(string? hotkey, string fallback)
        {
            if (HotkeyDefinition.Parse(hotkey) is HotkeyDefinition parsed)
            {
                return parsed;
            }

            if (HotkeyDefinition.Parse(fallback) is HotkeyDefinition fallbackParsed)
            {
                this._logWriter.LogWarning($"Invalid hotkey '{hotkey ?? "<null>"}'. Falling back to '{fallback}'.");
                return fallbackParsed;
            }

            throw new InvalidOperationException($"Failed to parse required fallback hotkey '{fallback}'.");
        }

        private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
        {
            if (!this._initialized)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (IsHotkeyMatch(e.Definition, this._showHideHotkeyDefinition))
                {
                    this.ToggleTimerWidget();
                    return;
                }

                if (IsHotkeyMatch(e.Definition, this._toggleTimerHotkeyDefinition))
                {
                    this.ToggleTimer();
                }
            });
        }

        private void OnUserBecameIdle(object? sender, UserIdleEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!this.IsTimerRunning())
                {
                    return;
                }

                this._pausedByIdle = true;
                if (this._timerWindow?.DataContext is TimerWidgetViewModel viewModel)
                {
                    viewModel.PauseForIdle();
                }

                _ = this._notificationService.ShowNotificationAsync("Focus Timer", "Timer paused because no activity was detected.");
                this._logWriter.LogInformation($"Timer paused due to user idle at {e.Timestamp:O}");
            });
        }

        private void OnUserReturned(object? sender, UserIdleEventArgs e)
        {
            if (!this._pausedByIdle)
            {
                return;
            }

            this._pausedByIdle = false;
            _ = this._notificationService.ShowNotificationAsync("Focus Timer", "Welcome back. Press Play to resume your focus session.");
            this._logWriter.LogInformation($"User activity resumed at {e.Timestamp:O}");
        }

        private bool TryToggleCompactModeDraft()
        {
            if (this._settingsWindow?.DataContext is not SettingsWindowViewModel editor)
            {
                return false;
            }

            editor.ToggleCompactModePreview();
            return true;
        }

        private void RegisterHotkeysCore(bool force = true)
        {
            HotkeyDefinition showHide = this.ParseHotkeyOrDefault(this.CurrentSettings.HotkeyShowHide ?? "Ctrl+Alt+T", "Ctrl+Alt+T");
            HotkeyDefinition toggleTimer = this.ParseHotkeyOrDefault(this.CurrentSettings.HotkeyToggleTimer ?? "Ctrl+Alt+P", "Ctrl+Alt+P");
            if (!force && this._hotkeysConfigured &&
                showHide.Modifiers == this._showHideHotkeyDefinition.Modifiers &&
                showHide.KeyCode == this._showHideHotkeyDefinition.KeyCode &&
                toggleTimer.Modifiers == this._toggleTimerHotkeyDefinition.Modifiers &&
                toggleTimer.KeyCode == this._toggleTimerHotkeyDefinition.KeyCode)
            {
                return;
            }

            this._hotkeysConfigured = false;
            this._hotkeyService.UnregisterAll();
            this._showHideHotkeyDefinition = showHide;
            this._hotkeyService.Register(this._showHideHotkeyDefinition);
            this._toggleTimerHotkeyDefinition = toggleTimer;
            this._hotkeyService.Register(this._toggleTimerHotkeyDefinition);
            this._hotkeysConfigured = true;
            this._logWriter.LogInformation(
                $"Hotkeys registered: {this._showHideHotkeyDefinition}, {this._toggleTimerHotkeyDefinition}");
        }

        private async Task ContinueAfterRecoveryAsync()
        {
            if (this._initialized)
            {
                return;
            }

            await this.InitializeAsync();
            if (!this._initialized)
            {
                return;
            }

            if (!this.CurrentSettings.StartMinimized)
            {
                this.ShowTimerWidget();
            }

            this.RegisterHotkeys();
        }

        /// <summary>
        /// Applies committed settings to the running widget and integrations.
        /// </summary>
        /// <param name="settings">Settings to activate.</param>
        /// <returns>A task representing activation.</returns>
        private async Task ActivateSettingsAsync(Settings settings)
        {
            this.CurrentSettings = settings.Clone();
            this._projectRuleStore?.Update(this.CurrentSettings.ProjectRules);
            this._themeService.ApplyTheme(this.CurrentSettings.Theme);
            this._themeManager.ApplyTheme(this.CurrentSettings.Theme);
            if (this._timerWindow?.DataContext is TimerWidgetViewModel vm)
            {
                await vm.ActivateSettingsAsync(this.CurrentSettings);
            }

            if (this._timerWindow != null)
            {
                this._timerWindow.Topmost = this.CurrentSettings.AlwaysOnTop;
            }

            this.RegisterHotkeysCore(force: false);
        }
    }
}
