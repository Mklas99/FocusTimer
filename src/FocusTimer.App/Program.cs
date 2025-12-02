using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.App.ViewModels;
using FocusTimer.App.Services;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace FocusTimer.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Set the main thread scheduler for ReactiveUI to Avalonia's UI thread
        RxApp.MainThreadScheduler = Avalonia.ReactiveUI.AvaloniaScheduler.Instance;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // Dependency injection container (exposed for App to use)
    public static IServiceProvider Services { get; private set; } = null!;

    static Program()
    {
        // Setup DI container with platform-specific implementations
        var services = new ServiceCollection();

        // Register platform-specific services based on OS
        // Windows: Full implementation using Win32 APIs for active window tracking
        // Linux: Stub implementation (TODO: X11/Wayland support in future milestone)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows-specific: P/Invoke based active window tracking
            services.AddSingleton<IActiveWindowService, Platform.Windows.WindowsActiveWindowService>();
            services.AddSingleton<ILogWriter, Persistence.CsvLogWriter>();
            
            // Windows-specific: Notifications, auto-start, and hotkeys
            services.AddSingleton<INotificationService, Platform.Windows.WindowsNotificationService>();
            services.AddSingleton<IAutoStartService, Platform.Windows.WindowsAutoStartService>();
            services.AddSingleton<IHotkeyService, Platform.Windows.WindowsHotkeyService>();
        }
        else
        {
            // Linux/Other: Stubs for now
            // TODO: Implement X11 (XGetInputFocus) or Wayland compositor APIs
            services.AddSingleton<IActiveWindowService, LinuxActiveWindowServiceStub>();
            services.AddSingleton<ILogWriter, Persistence.CsvLogWriter>(); // CsvLogWriter is cross-platform
            
            // Linux stubs: TODO implement using notify-send, .desktop files, and X11/Wayland APIs
            services.AddSingleton<INotificationService, LinuxNotificationServiceStub>();
            services.AddSingleton<IAutoStartService, LinuxAutoStartServiceStub>();
            services.AddSingleton<IHotkeyService, LinuxHotkeyServiceStub>();
        }

        // Settings provider - JSON-based persistence
        services.AddSingleton<ISettingsProvider, Persistence.JsonSettingsProvider>();

        // Session tracker - manages window change detection and TimeEntry segmentation
        // Registered as Transient because it's tied to the lifetime of TimerWidgetViewModel
        services.AddTransient<SessionTracker>();

        // Break reminder service - manages break reminder scheduling
        services.AddSingleton<BreakReminderService>();

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
