#pragma warning disable
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;
using Microsoft.Extensions.DependencyInjection;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(root);
        var logger = new Logger(Path.Combine(root, "smoke.log"));
        var settings = new JsonSettingsProvider(Path.Combine(root, "settings.json"), logger);
        if (!File.Exists(settings.SettingsFilePath)) settings.SaveAsync(new Settings
        {
            DeveloperModeEnabled = true, WorkLoggingEnabled = true, BreakRemindersEnabled = false,
            WorklogDirectory = Path.Combine(root, "worklogs"), LogDirectory = Path.Combine(root, "logs"),
        }).GetAwaiter().GetResult();
        var services = new ServiceCollection();
        services.AddSingleton<IAppLogger>(logger);
        services.AddSingleton<ISettingsProvider>(settings);
        services.AddSingleton<IWorklogStore, CsvSessionRepository>();
        services.AddSingleton<IActiveWindowService, ActiveWindowServiceStub>();
        services.AddSingleton<INotificationService, LinuxNotificationServiceStub>();
        services.AddSingleton<IAutoStartService, LinuxAutoStartServiceStub>();
        services.AddSingleton<IGlobalHotkeyService, Hotkeys>();
        services.AddSingleton<IIdleDetectionService, LinuxIdleDetectionServiceStub>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<SessionTracker>();
        services.AddSingleton<ITimerService, TimerService>();
        services.AddSingleton<BreakReminderService>();
        services.AddSingleton<TodayStatsService>();
        services.AddSingleton<ITrayIconController, TrayStateController>();
        services.AddSingleton<AppController>();
        services.AddTransient<TimerWidgetViewModel>();
        services.AddTransient<SettingsWindowViewModel>();
        services.AddTransient<Func<TimerWidgetViewModel>>(sp => () => sp.GetRequiredService<TimerWidgetViewModel>());
        services.AddTransient<Func<SettingsWindowViewModel>>(sp => () => sp.GetRequiredService<SettingsWindowViewModel>());
        AppHost.Services = services.BuildServiceProvider();
        AppBuilder.Configure<FocusTimer.App.App>().UsePlatformDetect().WithInterFont().UseReactiveUI()
            .AfterSetup(builder => DispatcherTimer.RunOnce(() => _ = VerifyAsync(root), TimeSpan.FromSeconds(2)))
            .StartWithClassicDesktopLifetime([]);
        (AppHost.Services as IDisposable)?.Dispose();
        File.WriteAllText(Path.Combine(root, "cleanup.json"), "{\"ServicesDisposed\":true}");
    }

    private static async Task VerifyAsync(string root)
    {
        try
        {
            var controller = AppHost.Services.GetRequiredService<AppController>();
            controller.ShowSettings();
            await Task.Delay(200);
            var window = (Window)typeof(AppController).GetField("_settingsWindow", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller)!;
            window.Title = "FocusTimer - isolated polling smoke";
            var vm = (SettingsWindowViewModel)window.DataContext!;
            var tracker = AppHost.Services.GetRequiredService<SessionTracker>();
            var widget = (TimerWidgetViewModel)((Window)typeof(AppController).GetField("_timerWindow", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller)!).DataContext!;
            await widget.InitializeSettingsAsync();
            Require(Interval(tracker) == vm.Settings.ActivityPollingIntervalSeconds, "startup interval not applied");
            var initial = vm.ActivityPollingIntervalInput;
            vm.ActivityPollingIntervalInput = 1.5m;
            await Apply(vm);
            Require(!vm.LastApplySucceeded, "invalid draft accepted");
            vm.ActivityPollingIntervalInput = 1;
            await Apply(vm); Require(vm.LastApplySucceeded, "Apply failed");
            await widget.ReloadSettingsAsync(); Require(Interval(tracker) == 1, "live interval not applied");
            var timer = AppHost.Services.GetRequiredService<ITimerService>();
            timer.Start(); await Task.Delay(2200);
            Require(timer.Elapsed >= TimeSpan.FromSeconds(2), "display tick slowed");
            vm.ActivityPollingIntervalInput = 5;
            await Apply(vm); await widget.ReloadSettingsAsync(); Require(Interval(tracker) == 5, "1-to-5 reload failed");
            var revision = Revision(tracker);
            await widget.ReloadSettingsAsync(); Require(Revision(tracker) == revision, "unchanged reload reset deadline");
            vm.ActivityPollingIntervalInput = 1;
            await Apply(vm); await widget.ReloadSettingsAsync(); Require(Interval(tracker) == 1, "5-to-1 reload failed");
            vm.Settings.DeveloperModeEnabled = false;
            vm.Settings.WorkLoggingEnabled = false;
            await Apply(vm); await widget.ReloadSettingsAsync();
            Require(tracker.CompletedEntryCount == 0, "disabled logging retained work");
            vm.Settings.WorkLoggingEnabled = true;
            await Apply(vm); await widget.ReloadSettingsAsync();
            Require(Interval(tracker) == 1 && tracker.CompletedEntryCount > 0, "hidden developer setting or re-enable failed");
            vm.ActivityPollingIntervalInput = 60;
            await Apply(vm); Require(vm.LastApplySucceeded, "live interval failed");
            await widget.ReloadSettingsAsync(); Require(Interval(tracker) == 60, "maximum interval not applied");
            var elapsed = timer.Elapsed; await Task.Delay(1100);
            Require(timer.Elapsed > elapsed, "60-second capture slowed timer");
            vm.ActivityPollingIntervalInput = 5; window.Close();
            controller.ShowSettings();
            await Task.Delay(200);
            window = (Window)typeof(AppController).GetField("_settingsWindow", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller)!;
            window.Title = "FocusTimer - isolated polling smoke";
            vm = (SettingsWindowViewModel)window.DataContext!;
            await Task.Delay(100);
            Require(vm.ActivityPollingIntervalInput == 60, "unapplied draft persisted");
            timer.Pause();
            Require(typeof(FocusTimer.App.App).GetField("_trayIcon", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Application.Current) != null, "tray missing");
            await File.WriteAllTextAsync(Path.Combine(root, "result.json"), JsonSerializer.Serialize(new
            {
                Passed = true, Initial = initial, Persisted = vm.ActivityPollingIntervalInput,
                TimerSeconds = timer.Elapsed.TotalSeconds, InvalidRejected = true, TrayCreated = true,
                Timestamp = DateTimeOffset.UtcNow,
            }));
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(Path.Combine(root, "result.json"), JsonSerializer.Serialize(new { Passed = false, Error = ex.ToString() }));
        }
        finally
        {
            AppHost.Services.GetRequiredService<AppController>().ExitApplication();
        }
    }

    private static Task Apply(SettingsWindowViewModel vm) => (Task)typeof(SettingsWindowViewModel)
        .GetMethod("ApplyAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!;
    private static int Interval(SessionTracker t) => (int)typeof(SessionTracker).GetField("_pollingIntervalSeconds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(t)!;
    private static long Revision(SessionTracker t) => (long)typeof(SessionTracker).GetField("_scheduleRevision", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(t)!;
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    private sealed class Hotkeys : IGlobalHotkeyService
    {
        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed { add { } remove { } }
        public void Register(HotkeyDefinition definition) { }
        public void UnregisterAll() { }
    }
    private sealed class Logger(string path) : IAppLogger
    {
        public void LogCritical(string m, Exception? e = null) => LogError(m, e);
        public void LogError(string m, Exception? e = null) => File.AppendAllText(path, m + " " + e + Environment.NewLine);
        public void LogWarning(string m) { }
        public void LogInformation(string m) { }
        public void LogDebug(string m) { }
    }
}
