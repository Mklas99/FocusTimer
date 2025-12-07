using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FocusTimer.App.Services;
using FocusTimer.App.Views;
using FocusTimer.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Threading;
using System;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace FocusTimer.App;

public partial class App : Application
{
    private AppController? _appController;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get the AppController from DI
            _appController = Program.Services.GetRequiredService<AppController>();
            
            // Initialize settings asynchronously
            _ = InitializeAppAsync(desktop);

            // Ensure cleanup on shutdown
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async System.Threading.Tasks.Task InitializeAppAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            // Initialize the AppController
            await _appController!.InitializeAsync();

            // Show widget based on settings
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_appController.CurrentSettings.StartMinimized)
                {
                    _appController.ShowTimerWidget();
                }
                // Tray icon is always shown via XAML
                
                // Register global hotkeys after showing window (Windows needs window handle)
                _appController.RegisterHotkeys();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing app: {ex.Message}");
            // Show widget anyway as fallback
            _appController?.ShowTimerWidget();
        }
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // Dispose ViewModels and flush any pending data
        try
        {
            var timerViewModel = Program.Services.GetService<TimerWidgetViewModel>();
            timerViewModel?.Dispose();
            
            (Program.Services as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
        }
    }

    #region TrayIcon Event Handlers

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        // Toggle widget visibility on tray icon click
        _appController?.ToggleTimerWidget();
    }

    private void TrayMenu_ShowHide(object? sender, EventArgs e)
    {
        _appController?.ToggleTimerWidget();
    }

    private void TrayMenu_ToggleTimer(object? sender, EventArgs e)
    {
        _appController?.ToggleTimer();
    }

    private void TrayMenu_Settings(object? sender, EventArgs e)
    {
        _appController?.ShowSettings();
    }

    private void TrayMenu_Exit(object? sender, EventArgs e)
    {
        // Exit cleanly via AppController
        if (_appController != null)
        {
            _appController.ExitApplication();
        }
    }

    #endregion
}
