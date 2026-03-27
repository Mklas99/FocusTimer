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
        private readonly IGlobalHotkeyService _hotkeyService;
        private readonly IIdleDetectionService _idleDetectionService;
        private readonly INotificationService _notificationService;
        private readonly IThemeService _themeService;
        private readonly ThemeManager _themeManager;
        private readonly Func<TimerWidgetViewModel> _timerViewModelFactory;
        private readonly Func<SettingsWindowViewModel> _settingsViewModelFactory;
        private readonly TodayStatsService _todayStatsService;
        private readonly ITrayIconController? _trayIconController;
        private readonly IAppLogger _logWriter;
        private TrayIcon? _trayIcon;
        private TimerWidgetWindow? _timerWindow;
        private SettingsWindow? _settingsWindow;
        private Settings _currentSettings;
        private HotkeyDefinition _showHideHotkeyDefinition;
        private HotkeyDefinition _toggleTimerHotkeyDefinition;
        private bool _pausedByIdle;

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
            IAppLogger logWriter)
        {
            this._settingsProvider = settingsProvider;
            this._hotkeyService = hotkeyService;
            this._idleDetectionService = idleDetectionService;
            this._notificationService = notificationService;
            this._themeService = themeService;
            this._themeManager = themeManager;
            this._timerViewModelFactory = timerViewModelFactory;
            this._settingsViewModelFactory = settingsViewModelFactory;
            this._currentSettings = new Settings();
            this._todayStatsService = todayStatsService;
            this._trayIconController = trayIconController;
            this._logWriter = logWriter;

            this._showHideHotkeyDefinition = this.ParseHotkeyOrDefault(this._currentSettings.HotkeyShowHide, "Ctrl+Alt+T");
            this._toggleTimerHotkeyDefinition = this.ParseHotkeyOrDefault(this._currentSettings.HotkeyToggleTimer, "Ctrl+Alt+P");
            this._hotkeyService.HotkeyPressed += this.OnHotkeyPressed;
            this._idleDetectionService.UserBecameIdle += this.OnUserBecameIdle;
            this._idleDetectionService.UserReturned += this.OnUserReturned;
        }

        /// <summary>
        /// Gets get the current settings (for initialization purposes).
        /// </summary>
        public Settings CurrentSettings => this._currentSettings;

        /// <summary>
        /// Initialize the controller and load settings.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            try
            {
                // Initialize theme resources first
                this._themeManager.InitializeThemeResources();

                // Load settings
                this._currentSettings = await this._settingsProvider.LoadAsync();

                // Apply saved theme
                if (!string.IsNullOrEmpty(this._currentSettings.ActiveThemeName))
                {
                    var theme = this._themeService.GetBuiltInTheme(this._currentSettings.ActiveThemeName);
                    if (theme != null)
                    {
                        this._currentSettings.Theme = theme;
                    }
                }

                // Apply theme to UI
                this._themeManager.ApplyTheme(this._currentSettings.Theme);

                // Setup tray icon controller after timer window is created
                if (this._timerWindow == null)
                {
                    var viewModel = this._timerViewModelFactory();
                    this._timerWindow = new TimerWidgetWindow
                    {
                        DataContext = viewModel,
                    };

                    // Initialize settings async
                    _ = viewModel.InitializeSettingsAsync();
                }
            }
            catch (Exception ex)
            {
                this._logWriter.LogError($"Failed to load settings.", ex);

                // Continue with defaults
            }
        }

        /// <summary>
        /// Register global hotkeys. Call this after timer window is created.
        /// </summary>
        public void RegisterHotkeys()
        {
            try
            {
                this._hotkeyService.UnregisterAll();

                // Register show/hide hotkey (default: Ctrl+Alt+T)
                var showHideHotkey = this._currentSettings.HotkeyShowHide ?? "Ctrl+Alt+T";
                this._showHideHotkeyDefinition = this.ParseHotkeyOrDefault(showHideHotkey, "Ctrl+Alt+T");
                this._hotkeyService.Register(this._showHideHotkeyDefinition);

                // Register toggle timer hotkey (default: Ctrl+Alt+P)
                var toggleTimerHotkey = this._currentSettings.HotkeyToggleTimer ?? "Ctrl+Alt+P";
                this._toggleTimerHotkeyDefinition = this.ParseHotkeyOrDefault(toggleTimerHotkey, "Ctrl+Alt+P");
                this._hotkeyService.Register(this._toggleTimerHotkeyDefinition);

                this._logWriter.LogInformation($"Hotkeys registered: {this._showHideHotkeyDefinition}, {this._toggleTimerHotkeyDefinition}");
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
            Dispatcher.UIThread.Post(() =>
            {
                if (this._timerWindow == null)
                {
                    var viewModel = this._timerViewModelFactory();
                    this._timerWindow = new TimerWidgetWindow
                    {
                        DataContext = viewModel,
                    };

                    // If tray icon is available, set it in the controller
                    if (this._trayIcon != null && this._trayIconController != null)
                    {
                        (this._trayIconController as TrayStateController)?.SetTrayIcon(this._trayIcon);
                    }

                    // Initialize settings async
                    _ = viewModel.InitializeSettingsAsync();
                }

                this._timerWindow.Show();
                this._timerWindow.Activate();
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
            if (this._timerWindow?.DataContext is TimerWidgetViewModel vm)
            {
                return vm.IsRunning;
            }

            return false;
        }

        /// <summary>
        /// Show the settings window.
        /// </summary>
        public void ShowSettings()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this._settingsWindow == null)
                {
                    var viewModel = this._settingsViewModelFactory();
                    this._settingsWindow = new SettingsWindow
                    {
                        DataContext = viewModel,
                    };

                    // Subscribe to settings applied event
                    viewModel.SettingsApplied += this.OnSettingsApplied;

                    // Handle window closed event
                    this._settingsWindow.Closed += (s, e) =>
                    {
                        if (this._settingsWindow?.DataContext is SettingsWindowViewModel vm)
                        {
                            vm.SettingsApplied -= this.OnSettingsApplied;
                        }

                        this._settingsWindow = null;
                    };
                }

                this._settingsWindow.Show();
                this._settingsWindow.Activate();
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
                this.ToggleTimer();
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

        /// <summary>
        /// Handle settings changes and apply them to the timer widget.
        /// </summary>
        private async void OnSettingsApplied(object? sender, EventArgs e)
        {
            try
            {
                // Reload settings
                this._currentSettings = await this._settingsProvider.LoadAsync();

                // Apply theme
                this._themeManager.ApplyTheme(this._currentSettings.Theme);

                // Apply to timer widget if it exists
                if (this._timerWindow?.DataContext is TimerWidgetViewModel vm)
                {
                    await vm.ReloadSettingsAsync();
                }

                // Apply window-level settings
                if (this._timerWindow != null)
                {
                    this._timerWindow.Opacity = 1.0;
                    this._timerWindow.Topmost = this._currentSettings.AlwaysOnTop;

                    // Apply scale via RenderTransform
                    var scale = this._currentSettings.WidgetScale;
                    this._timerWindow.RenderTransform = new Avalonia.Media.ScaleTransform(scale, scale);
                }

                // Re-register hotkeys with new settings
                this._hotkeyService.UnregisterAll();
                this.RegisterHotkeys();
            }
            catch (Exception ex)
            {
                this._logWriter.LogError($"Failed to apply settings.", ex);
            }
        }
    }
}
