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
using FocusTimer.Core.Interfaces;
using System.Linq;
using Avalonia.LogicalTree;

namespace FocusTimer.App;

public partial class App : Application
{
    private AppController? _appController;
    private TrayIcon? _trayIcon;


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            _trayIcon = FindTrayIcon();
        }

        
        if (_trayIcon != null)
        {
            _appController.SetTrayIcon(_trayIcon);
            _trayIcon.Clicked += TrayIcon_Clicked;
        }
          /// _trayIcon.MenuShowHide += TrayMenu_ShowHide;
          /// _trayIcon.MenuToggleTimer += TrayMenu_ToggleTimer;
          /// _trayIcon.MenuSettings += TrayMenu_Settings;
          /// _trayIcon.MenuExit += TrayMenu_Exit;
    }

    private TrayIcon? FindTrayIcon()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            // Search for TrayIcon in MainWindow's logical tree
            return desktop.MainWindow.GetLogicalChildren()
                .OfType<TrayIcon>()
                .FirstOrDefault();
        }
        return null;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get the AppController from DI
            _appController = Program.Services.GetRequiredService<AppController>();

            // Create and configure the tray icon in code
            _trayIcon = new TrayIcon
            {
                ToolTipText = "Focus Timer: Idle",
                Icon = new WindowIcon(
                    Avalonia.Platform.AssetLoader.Open(
                        new Uri("avares://FocusTimer.App/Assets/FocusTimer-idle.png"))
                ),
                Menu = new NativeMenu
                {
                    CreateMenuItem("Show/Hide Timer", TrayMenu_ShowHide),
                    CreateMenuItem("Start/Pause Timer", TrayMenu_ToggleTimer),
                    new NativeMenuItemSeparator(),
                    CreateMenuItem("Settings...", TrayMenu_Settings),
                    new NativeMenuItemSeparator(),
                    CreateMenuItem("Exit", TrayMenu_Exit)
                }
            };
            _trayIcon.Clicked += TrayIcon_Clicked;

            // Register tray icon with controllers
            _appController.SetTrayIcon(_trayIcon);
            var trayController = Program.Services.GetRequiredService<ITrayIconController>();
            trayController.SetTrayIcon(_trayIcon);

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

    private NativeMenuItem CreateMenuItem(string header, EventHandler? onClick)
    {
        var item = new NativeMenuItem(header);
        if (onClick != null)
            item.Click += onClick;
        return item;
    }

    #region TrayIcon Event Handlers

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        // Store TrayIcon instance statically on first event
        if (_trayIcon == null && sender is TrayIcon tray)
            _trayIcon = tray;
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
