namespace FocusTimer.App.Tests;

using System.Reflection;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;
using FocusTimer.Persistence;

public class TimerWidgetSettingsActivationTests
{
    [Fact]
    public async Task ActivateSettingsAsync_UpdatesWidgetAndPollingCadence()
    {
        var provider = new SettingsProviderStub();
        var logger = new NullLogger();
        var notifications = new LinuxNotificationServiceStub();
        var tracker = new SessionTracker(new LinuxActiveWindowServiceStub(), logger);
        using var timer = new TimerService(tracker);
        using var reminders = new BreakReminderService(notifications, provider);
        using var vm = new TimerWidgetViewModel(
            provider,
            logger,
            new CsvSessionRepository(provider),
            notifications,
            tracker,
            reminders,
            timer,
            null,
            new EventBus());
        var candidate = new Settings
        {
            ActivityPollingIntervalSeconds = 5,
            WorkLoggingEnabled = false,
            UseCompactMode = true,
        };

        await vm.ActivateSettingsAsync(candidate);

        Assert.Same(candidate, vm.Settings);
        Assert.True(vm.Settings.UseCompactMode);
        Assert.Equal(5, typeof(SessionTracker)
            .GetField("_pollingIntervalSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(tracker));

        var previous = vm.Settings;
        await vm.ActivateSettingsAsync(candidate.Clone());
        int staleNotifications = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.UseCompactMode))
            {
                staleNotifications++;
            }
        };
        previous.UseCompactMode = false;
        Assert.Equal(0, staleNotifications);
        Assert.True(vm.UseCompactMode);
    }

    private sealed class NullLogger : IAppLogger
    {
        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) { }
    }
}
