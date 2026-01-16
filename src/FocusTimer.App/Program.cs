using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Services;
using Serilog;
using ReactiveUI;
using FocusTimer.Platform.Windows;

namespace FocusTimer.App
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();

        // Dependency injection container (exposed for App to use)
        public static IServiceProvider Services { get; private set; }

        static Program()
        {
            // Setup DI container with platform-specific implementations
            var services = new ServiceCollection();

            // Register platform-specific services based on OS
            // Windows: Full implementation using Win32 APIs for active window tracking
            // Linux: Stub implementation (TODO: X11/Wayland support in future milestone)
            // Configure Serilog
            var logger = new Serilog.LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.File("FocusTimer.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows-specific: P/Invoke based active window tracking
                services.AddSingleton<IActiveWindowService, Platform.Windows.WindowsActiveWindowService>();
                services.AddSingleton<ILogWriter>(sp => new SerilogLogWriter(logger));

                // Windows-specific: Notifications, auto-start, and hotkeys
                services.AddSingleton<INotificationService, Platform.Windows.WindowsNotificationService>();
                services.AddSingleton<IAutoStartService, Platform.Windows.WindowsAutoStartService>();
                services.AddSingleton<IGlobalHotkeyService, Platform.Windows.WindowsHotkeyService>();
                services.AddSingleton<ITrayIconController, TrayStateController>();
                services.AddSingleton<IIdleDetectionService, Platform.Windows.WindowsIdleDetectionService>();
            }
            else
            {
                // Linux/Other: Stubs for now
                // TODO: Implement X11 (XGetInputFocus) or Wayland compositor APIs
                services.AddSingleton<IActiveWindowService, LinuxActiveWindowServiceStub>();
                services.AddSingleton<ILogWriter>(sp => new SerilogLogWriter(logger));

                // Linux stubs: TODO implement using notify-send, .desktop files, and X11/Wayland APIs
                services.AddSingleton<INotificationService, LinuxNotificationServiceStub>();
                services.AddSingleton<IAutoStartService, LinuxAutoStartServiceStub>();
                services.AddSingleton<IGlobalHotkeyService, LinuxHotkeyServiceStub>();
            }

            // Settings provider - JSON-based persistence
            services.AddSingleton<ISettingsProvider, Persistence.JsonSettingsProvider>();

            // Theme services - manages theme loading, saving, and application
            services.AddSingleton<IThemeService, Core.Services.ThemeService>();
            services.AddSingleton<ThemeManager>();

            // Session tracker - manages window change detection and TimeEntry segmentation
            // Registered as Singleton to be shared between TimerService and ViewModels
            services.AddSingleton<SessionTracker>();

            // Timer Service - Central timer logic
            services.AddSingleton<ITimerService, TimerService>();

            // Break reminder service - manages break reminder scheduling
            services.AddSingleton<BreakReminderService>();
            
            // TodayStatsService - required by TrayStateController
            services.AddSingleton<TodayStatsService>();

            // App Controller - manages window lifecycle and coordination (Singleton)
            services.AddSingleton<AppController>();

            // ViewModels - Transient so each window instance gets its own ViewModel
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TimerWidgetViewModel>();
            services.AddTransient<SettingsWindowViewModel>();

            // Register factories for ViewModels (required for AppController constructor)
            services.AddTransient<Func<TimerWidgetViewModel>>(sp => () => sp.GetRequiredService<TimerWidgetViewModel>());
            services.AddTransient<Func<SettingsWindowViewModel>>(sp => () => sp.GetRequiredService<SettingsWindowViewModel>());

            Services = services.BuildServiceProvider();
        }
    }
}
