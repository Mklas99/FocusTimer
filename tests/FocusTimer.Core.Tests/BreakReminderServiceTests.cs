namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class BreakReminderServiceTests
{
    [Fact]
    public void Constructor_GivenNullNotificationService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BreakReminderService(null!, new FixedSettingsProvider(new Settings())));
    }

    [Fact]
    public void Constructor_GivenNullSettingsProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BreakReminderService(new RecordingNotificationService(), null!));
    }

    [Fact]
    public void OnTimerPaused_GivenNoTimer_DoesNotThrow()
    {
        using var service = new BreakReminderService(
            new RecordingNotificationService(),
            new FixedSettingsProvider(new Settings()));

        var ex = Record.Exception(service.OnTimerPaused);

        Assert.Null(ex);
    }

    [Fact]
    public void OnTimerPaused_CalledTwice_DoesNotThrow()
    {
        using var service = new BreakReminderService(
            new RecordingNotificationService(),
            new FixedSettingsProvider(new Settings()));

        var ex = Record.Exception(() =>
        {
            service.OnTimerPaused();
            service.OnTimerPaused();
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task OnTimerStarted_GivenBreakRemindersDisabled_DoesNotNotify()
    {
        var notifications = new RecordingNotificationService();
        var settings = new Settings
        {
            BreakRemindersEnabled = false,
            BreakIntervalMinutes = 1,
        };

        using var service = new BreakReminderService(notifications, new FixedSettingsProvider(settings), NullLogger.Instance);

        service.OnTimerStarted();
        await Task.Delay(50);

        Assert.Equal(0, notifications.BreakReminderCount);
    }

    [Fact]
    public async Task OnTimerStarted_GivenZeroInterval_DoesNotNotify()
    {
        var notifications = new RecordingNotificationService();
        var settings = new Settings
        {
            BreakRemindersEnabled = true,
            BreakIntervalMinutes = 0,
        };

        using var service = new BreakReminderService(notifications, new FixedSettingsProvider(settings), NullLogger.Instance);

        service.OnTimerStarted();
        await Task.Delay(50);

        Assert.Equal(0, notifications.BreakReminderCount);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = new BreakReminderService(
            new RecordingNotificationService(),
            new FixedSettingsProvider(new Settings()));

        var ex = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(ex);
    }
}