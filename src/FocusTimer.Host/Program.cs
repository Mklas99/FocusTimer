namespace FocusTimer.Host
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Avalonia;
    using Avalonia.ReactiveUI;
    using FocusTimer.App;
    using FocusTimer.App.Services;
    using FocusTimer.App.ViewModels;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using FocusTimer.Core.Services;
    using FocusTimer.Core.Stubs;
    using Microsoft.Extensions.DependencyInjection;
    using ReactiveUI;
    using Serilog;

    /// <summary>
    /// Application entry point and dependency injection configuration.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Initializes static members of the <see cref="Program"/> class.
        /// </summary>
        static Program()
        {
            var services = new ServiceCollection();

            var logDirectory = Settings.DefaultApplicationLogDirectory;
            var worklogDirectory = Settings.DefaultWorklogDirectory;

            var envLogDir = Environment.GetEnvironmentVariable("FOCUSTIMER_LOG_DIR");
            if (!string.IsNullOrWhiteSpace(envLogDir))
            {
                logDirectory = envLogDir;
            }

            Directory.CreateDirectory(logDirectory);
            Directory.CreateDirectory(worklogDirectory);

            var serilogConfig = new Serilog.LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "focustimer-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));

            var serilogLogger = serilogConfig.CreateLogger();

            var appLogger = new SerilogAppLogger(serilogLogger);
            services.AddSingleton<IAppLogger>(appLogger);

            appLogger.LogInformation($"FocusTimer started. Log directory: {logDirectory}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IActiveWindowService, Platform.Windows.WindowsActiveWindowService>();
                services.AddSingleton<INotificationService, Platform.Windows.WindowsNotificationService>();
                services.AddSingleton<IAutoStartService, Platform.Windows.WindowsAutoStartService>();
                services.AddSingleton<IGlobalHotkeyService, Platform.Windows.WindowsHotkeyService>();
                services.AddSingleton<ITrayIconController, TrayStateController>();
                services.AddSingleton<IIdleDetectionService, Platform.Windows.WindowsIdleDetectionService>();
            }
            else
            {
                services.AddSingleton<IActiveWindowService, LinuxActiveWindowServiceStub>();
                services.AddSingleton<INotificationService, LinuxNotificationServiceStub>();
                services.AddSingleton<IAutoStartService, LinuxAutoStartServiceStub>();
                services.AddSingleton<IGlobalHotkeyService, LinuxHotkeyServiceStub>();
                services.AddSingleton<IIdleDetectionService, LinuxIdleDetectionServiceStub>();
            }

            // Persistence registrations (consolidated helper) - use extension if available
            try
            {
                // Prefer extension if project provides it
                var addPersistence = typeof(FocusTimer.Persistence.ServiceCollectionExtensions).GetMethod("AddPersistenceServices");
                if (addPersistence != null)
                {
                    FocusTimer.Persistence.ServiceCollectionExtensions.AddPersistenceServices(services);
                }
                else
                {
                    services.AddSingleton<ISettingsProvider, Persistence.JsonSettingsProvider>();
                    services.AddSingleton<ISessionRepository>(sp =>
                    {
                        var settingsProvider = sp.GetRequiredService<ISettingsProvider>();
                        var logger = sp.GetRequiredService<IAppLogger>();
                        return new Persistence.CsvSessionRepository(settingsProvider, logger);
                    });
                }
            }
            catch
            {
                services.AddSingleton<ISettingsProvider, Persistence.JsonSettingsProvider>();
                services.AddSingleton<ISessionRepository>(sp =>
                {
                    var settingsProvider = sp.GetRequiredService<ISettingsProvider>();
                    var logger = sp.GetRequiredService<IAppLogger>();
                    return new Persistence.CsvSessionRepository(settingsProvider, logger);
                });
            }

            services.AddSingleton<IThemeService, Core.Services.ThemeService>();
            services.AddSingleton<ThemeManager>();

            // Event bus for decoupled UI <> controller messaging
            services.AddSingleton<Core.Interfaces.IEventBus, Core.Services.EventBus>();
            services.AddSingleton<SessionTracker>();
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<BreakReminderService>();
            services.AddSingleton<TodayStatsService>();
            services.AddSingleton<AppController>();

            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TimerWidgetViewModel>();
            services.AddTransient<SettingsWindowViewModel>();

            services.AddTransient<Func<TimerWidgetViewModel>>(sp => () => sp.GetRequiredService<TimerWidgetViewModel>());
            services.AddTransient<Func<SettingsWindowViewModel>>(sp => () => sp.GetRequiredService<SettingsWindowViewModel>());

            var sp = services.BuildServiceProvider();
            Services = sp;
            FocusTimer.Core.AppHost.Services = sp;
        }

        /// <summary>
        /// Gets the application's root <see cref="IServiceProvider"/>.
        /// </summary>
        public static IServiceProvider Services { get; private set; }

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        /// <summary>
        /// Configures and returns an Avalonia <see cref="AppBuilder"/> for startup.
        /// </summary>
        /// <returns>A configured <see cref="AppBuilder"/> instance.</returns>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .UseReactiveUI();
    }
}
