namespace FocusTimer.App
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.LogicalTree;
    using Avalonia.Markup.Xaml;
    using Avalonia.Threading;
    using FocusTimer.App.Services;
    using FocusTimer.App.ViewModels;
    using FocusTimer.App.Views;
    using FocusTimer.Core;
    using FocusTimer.Core.Interfaces;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The main application class for the Focus Timer application.
    /// </summary>
    public partial class App : Application, IAppInitializer
    {
        private AppController? _appController;
        private IAppLogger? _logger;
        private TrayIcon? _trayIcon;
        private object? _serviceProvider;

        /// <inheritdoc/>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                this._trayIcon = this.FindTrayIcon();
            }

            if (this._trayIcon != null)
            {
                this._trayIcon.Clicked += this.TrayIcon_Clicked;
            }

            // _trayIcon.MenuShowHide += TrayMenu_ShowHide;
            // _trayIcon.MenuToggleTimer += TrayMenu_ToggleTimer;
            // _trayIcon.MenuSettings += TrayMenu_Settings;
            // _trayIcon.MenuExit += TrayMenu_Exit;
        }

        /// <summary>
        /// Implements IAppInitializer to receive injected services from Host.
        /// </summary>
        /// <param name="appController">The application controller instance from DI.</param>
        /// <param name="logger">The logger instance from DI.</param>
        /// <param name="trayIconController">The tray icon controller instance from DI.</param>
        /// <param name="serviceProvider">The root service provider for cleanup on shutdown.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous initialization.</returns>
        async Task IAppInitializer.InitializeAsync(
            object? appController,
            IAppLogger? logger,
            object? trayIconController,
            object? serviceProvider)
        {
            this._appController = appController as AppController;
            this._logger = logger;
            this._serviceProvider = serviceProvider;

            // Create and configure the tray icon in code
            this._trayIcon = new TrayIcon
            {
                ToolTipText = "Focus Timer: Idle",
                Icon = new WindowIcon(
                    Avalonia.Platform.AssetLoader.Open(
                        new Uri("avares://FocusTimer.App/Assets/FocusTimer-idle.png"))),
                Menu = new NativeMenu
                {
                    CreateMenuItem("Show/Hide Timer", this.TrayMenu_ShowHide),
                    CreateMenuItem("Start/Pause Timer", this.TrayMenu_ToggleTimer),
                    new NativeMenuItemSeparator(),
                    CreateMenuItem("Settings...", this.TrayMenu_Settings),
                    new NativeMenuItemSeparator(),
                    CreateMenuItem("Exit", this.TrayMenu_Exit),
                },
            };
            this._trayIcon.Clicked += this.TrayIcon_Clicked;

            // Register tray icon with controllers
            if (this._appController != null)
            {
                this._appController.SetTrayIcon(this._trayIcon);
            }

            if (trayIconController is ITrayIconController trayCtrl)
            {
                trayCtrl.SetTrayIcon(this._trayIcon);
            }

            // Initialize settings asynchronously
            await this.InitializeAppAsync();

            // Ensure cleanup on shutdown
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownRequested += this.OnShutdownRequested;
            }
        }

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            base.OnFrameworkInitializationCompleted();

            // Initialize app with injected services from AppHost
            if (AppHost.Services != null)
            {
                var appController = AppHost.Services.GetService<AppController>();
                var logger = AppHost.Services.GetService<IAppLogger>();
                var trayController = AppHost.Services.GetService<ITrayIconController>();

                // InvokeAsync avoids blocking the UI thread (which would deadlock Avalonia's dispatcher)
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ((IAppInitializer)this).InitializeAsync(appController, logger, trayController, AppHost.Services);
                });
            }
        }

        private static NativeMenuItem CreateMenuItem(string header, EventHandler? onClick)
        {
            var item = new NativeMenuItem(header);
            if (onClick != null)
            {
                item.Click += onClick;
            }

            return item;
        }

        private TrayIcon? FindTrayIcon()
        {
            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                // Search for TrayIcon in MainWindow's logical tree
                return desktop.MainWindow.GetLogicalChildren()
                    .OfType<TrayIcon>()
                    .FirstOrDefault();
            }

            return null;
        }

        private async System.Threading.Tasks.Task InitializeAppAsync()
        {
            try
            {
                // Initialize the AppController
                await this._appController!.InitializeAsync();

                // Show widget based on settings
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!this._appController.CurrentSettings.StartMinimized)
                    {
                        this._appController.ShowTimerWidget();
                    }

                    // Tray icon is always shown via XAML

                    // Register global hotkeys after showing window (Windows needs window handle)
                    this._appController.RegisterHotkeys();
                });
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Error initializing application.", ex);

                // Show widget anyway as fallback
                this._appController?.ShowTimerWidget();
            }
        }

        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            // Dispose ViewModels and flush any pending data
            try
            {
                // Dispose service provider and any resources
                (this._serviceProvider as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Error during application shutdown.", ex);
            }
        }

        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            // Store TrayIcon instance statically on first event
            if (this._trayIcon == null && sender is TrayIcon tray)
            {
                this._trayIcon = tray;
            }

            // Toggle widget visibility on tray icon click
            this._appController?.ToggleTimerWidget();
            this._logger?.LogDebug("Tray icon clicked - toggling timer widget.");
        }

        private void TrayMenu_ShowHide(object? sender, EventArgs e)
        {
            this._appController?.ToggleTimerWidget();
            this._logger?.LogDebug("Tray menu item 'Show/Hide Timer' clicked - toggling timer widget.");
        }

        private void TrayMenu_ToggleTimer(object? sender, EventArgs e)
        {
            this._appController?.ToggleTimer();
            this._logger?.LogDebug("Tray menu item 'Toggle Timer' clicked - toggling timer.");
        }

        private void TrayMenu_Settings(object? sender, EventArgs e)
        {
            this._appController?.ShowSettings();
            this._logger?.LogDebug("Tray menu item 'Settings' clicked - showing settings window.");
        }

        private void TrayMenu_Exit(object? sender, EventArgs e)
        {
            // Exit cleanly via AppController
            if (this._appController != null)
            {
                this._logger?.LogInformation("Tray menu item 'Exit' clicked - exiting application.");
                this._appController.ExitApplication();
            }
        }
    }
}
