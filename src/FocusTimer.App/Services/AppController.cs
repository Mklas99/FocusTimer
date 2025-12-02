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

namespace FocusTimer.App.Services;

/// <summary>
/// Central coordinator for managing application windows and lifecycle.
/// Handles show/hide logic, settings changes, and clean shutdown.
/// </summary>
public class AppController
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IHotkeyService _hotkeyService;
    private readonly INotificationService _notificationService;
    private readonly Func<TimerWidgetViewModel> _timerViewModelFactory;
    private readonly Func<SettingsWindowViewModel> _settingsViewModelFactory;
    
    private TimerWidgetWindow? _timerWindow;
    private SettingsWindow? _settingsWindow;
    private Settings _currentSettings;

    public AppController(
        ISettingsProvider settingsProvider,
        IHotkeyService hotkeyService,
        INotificationService notificationService,
        Func<TimerWidgetViewModel> timerViewModelFactory,
        Func<SettingsWindowViewModel> settingsViewModelFactory)
    {
        _settingsProvider = settingsProvider;
        _hotkeyService = hotkeyService;
        _notificationService = notificationService;
        _timerViewModelFactory = timerViewModelFactory;
        _settingsViewModelFactory = settingsViewModelFactory;
        _currentSettings = new Settings();
    }

    /// <summary>
    /// Initialize the controller and load settings.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _currentSettings = await _settingsProvider.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
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
            // Register show/hide hotkey (default: Ctrl+Alt+T)
            var showHideHotkey = _currentSettings.HotkeyShowHide ?? "Ctrl+Alt+T";
            _hotkeyService.RegisterHotkey(showHideHotkey, ToggleTimerWidget);

            // Register toggle timer hotkey (default: Ctrl+Alt+P)
            var toggleTimerHotkey = _currentSettings.HotkeyToggleTimer ?? "Ctrl+Alt+P";
            _hotkeyService.RegisterHotkey(toggleTimerHotkey, ToggleTimer);

            System.Diagnostics.Debug.WriteLine($"Hotkeys registered: {showHideHotkey}, {toggleTimerHotkey}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register hotkeys: {ex.Message}");
        }
    }

    /// <summary>
    /// Show or create the timer widget window.
    /// </summary>
    public void ShowTimerWidget()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_timerWindow == null)
            {
                var viewModel = _timerViewModelFactory();
                _timerWindow = new TimerWidgetWindow
                {
                    DataContext = viewModel
                };
                
                // Initialize settings async
                _ = viewModel.InitializeSettingsAsync();
            }

            _timerWindow.Show();
            _timerWindow.Activate();
        });
    }

    /// <summary>
    /// Hide the timer widget window without closing it.
    /// </summary>
    public void HideTimerWidget()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _timerWindow?.Hide();
        });
    }

    /// <summary>
    /// Toggle timer widget visibility.
    /// </summary>
    public void ToggleTimerWidget()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_timerWindow == null || !_timerWindow.IsVisible)
            {
                ShowTimerWidget();
            }
            else
            {
                HideTimerWidget();
            }
        });
    }

    /// <summary>
    /// Check if timer widget is visible.
    /// </summary>
    public bool IsTimerWidgetVisible()
    {
        return _timerWindow?.IsVisible ?? false;
    }

    /// <summary>
    /// Start or pause the timer.
    /// </summary>
    public void ToggleTimer()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_timerWindow?.DataContext is TimerWidgetViewModel vm)
            {
                vm.ToggleCommand.Execute(null);
            }
        });
    }

    /// <summary>
    /// Check if timer is running.
    /// </summary>
    public bool IsTimerRunning()
    {
        if (_timerWindow?.DataContext is TimerWidgetViewModel vm)
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
            if (_settingsWindow == null)
            {
                var viewModel = _settingsViewModelFactory();
                _settingsWindow = new SettingsWindow
                {
                    DataContext = viewModel
                };

                // Subscribe to settings applied event
                viewModel.SettingsApplied += OnSettingsApplied;

                // Handle window closed event
                _settingsWindow.Closed += (s, e) =>
                {
                    if (_settingsWindow?.DataContext is SettingsWindowViewModel vm)
                    {
                        vm.SettingsApplied -= OnSettingsApplied;
                    }
                    _settingsWindow = null;
                };
            }

            _settingsWindow.Show();
            _settingsWindow.Activate();
        });
    }

    /// <summary>
    /// Handle settings changes and apply them to the timer widget.
    /// </summary>
    private async void OnSettingsApplied(object? sender, EventArgs e)
    {
        try
        {
            // Reload settings
            _currentSettings = await _settingsProvider.LoadAsync();

            // Apply to timer widget if it exists
            if (_timerWindow?.DataContext is TimerWidgetViewModel vm)
            {
                await vm.ReloadSettingsAsync();
            }

            // Apply window-level settings
            if (_timerWindow != null)
            {
                _timerWindow.Opacity = _currentSettings.WidgetOpacity;
                _timerWindow.Topmost = _currentSettings.AlwaysOnTop;
                
                // Apply scale via RenderTransform
                var scale = _currentSettings.WidgetScale;
                _timerWindow.RenderTransform = new Avalonia.Media.ScaleTransform(scale, scale);
            }
            
            // Re-register hotkeys with new settings
            _hotkeyService.UnregisterAll();
            RegisterHotkeys();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Exit the application cleanly.
    /// Stops timer, flushes logs, and disposes resources.
    /// </summary>
    public async Task ExitApplicationAsync()
    {
        try
        {
            // Unregister hotkeys
            _hotkeyService.UnregisterAll();

            // Stop timer and flush entries
            if (_timerWindow?.DataContext is TimerWidgetViewModel vm)
            {
                vm.Dispose();
            }

            // Mark windows for actual closure (not hide)
            if (_timerWindow != null)
            {
                _timerWindow.IsAppShuttingDown = true;
            }

            // Close all windows
            _settingsWindow?.Close();
            _timerWindow?.Close();

            // Shutdown application
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during exit: {ex.Message}");
            // Force shutdown anyway
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown(1);
            }
        }
    }

    /// <summary>
    /// Get the current settings (for initialization purposes).
    /// </summary>
    public Settings CurrentSettings => _currentSettings;
}
