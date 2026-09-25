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
            IServiceProvider sp = BuildServiceProvider(
                Environment.GetEnvironmentVariable("FOCUSTIMER_LOG_DIR"),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                Settings.DefaultWorklogDirectory);
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

        /// <summary>
        /// Builds the dependency injection container.
        /// </summary>
        /// <param name="envLogDir">Optional log directory override; blank values fall back to the default.</param>
        /// <param name="isWindows">Whether to register the Windows platform services instead of the stubs.</param>
        /// <param name="worklogDirectory">The directory that is created for worklogs.</param>
        /// <returns>The configured service provider.</returns>
        internal static IServiceProvider BuildServiceProvider(string? envLogDir, bool isWindows, string worklogDirectory)
        {
            var services = new ServiceCollection();

            string logDirectory = Settings.DefaultApplicationLogDirectory;

            if (!string.IsNullOrWhiteSpace(envLogDir))
            {
                logDirectory = envLogDir;
            }

            Directory.CreateDirectory(logDirectory);
            Directory.CreateDirectory(worklogDirectory);

            Serilog.LoggerConfiguration serilogConfig = new Serilog.LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "focustimer-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));

            Serilog.Core.Logger serilogLogger = serilogConfig.CreateLogger();

            var appLogger = new SerilogAppLogger(serilogLogger);
            services.AddSingleton<IAppLogger>(appLogger);

            appLogger.LogInformation($"FocusTimer started. Log directory: {logDirectory}");

            if (isWindows)
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

            FocusTimer.Persistence.ServiceCollectionExtensions.AddPersistenceServices(services);

            services.AddSingleton<IThemeService, Core.Services.ThemeService>();
            services.AddSingleton<ThemeManager>();

            // Event bus for decoupled UI <> controller messaging
            services.AddSingleton<Core.Interfaces.IEventBus, Core.Services.EventBus>();
            services.AddSingleton<TimeProvider>(TimeProvider.System);
            services.AddSingleton<ISourcePlatformProvider, SourcePlatformProvider>();
            services.AddSingleton<SessionTracker>(sp => new SessionTracker(
                sp.GetRequiredService<IActiveWindowService>(),
                sp.GetRequiredService<IAppLogger>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ISourcePlatformProvider>(),
                () => sp.GetRequiredService<ISettingsProvider>().LoadAsync().GetAwaiter().GetResult().DeviceId));
            services.AddSingleton<ITimerService, TimerService>();
            services.AddSingleton<BreakReminderService>();
            services.AddSingleton<TodayStatsService>();

            // Worklog summary: add a grouping by registering another IWorklogGrouping, and replace
            // IProjectResolver to change how a project is decided (for example rule-based detection).
            services.AddSingleton<IWorklogGrouping, ApplicationGrouping>();
            services.AddSingleton<IWorklogGrouping, ProjectGrouping>();
            services.AddSingleton<WorklogGroupingRegistry>();
            services.AddSingleton<IProjectResolver, StoredProjectResolver>();
            services.AddSingleton<IWorklogSummaryService, WorklogSummaryService>();
            services.AddSingleton<AppController>();

            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TimerWidgetViewModel>();
            services.AddTransient<WorklogSummaryViewModel>();
            services.AddTransient<SettingsWindowViewModel>();

            services.AddTransient<Func<TimerWidgetViewModel>>(sp => () => sp.GetRequiredService<TimerWidgetViewModel>());
            services.AddTransient<Func<SettingsWindowViewModel>>(sp => () => sp.GetRequiredService<SettingsWindowViewModel>());

            return services.BuildServiceProvider();
        }
    }
}
