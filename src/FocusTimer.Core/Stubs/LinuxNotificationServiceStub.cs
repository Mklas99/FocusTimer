using FocusTimer.Core.Interfaces;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Linux stub for notification service.
/// TODO: Implement using DBus notifications or notify-send command.
/// </summary>
public class LinuxNotificationServiceStub : INotificationService
{
    public Task ShowBreakReminderAsync(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[Linux Stub] Break reminder: {message}");
        // TODO: Implement using notify-send or DBus
        // Example: Process.Start("notify-send", $"\"Break Reminder\" \"{message}\"");
        return Task.CompletedTask;
    }

    public Task ShowNotificationAsync(string title, string message)
    {
        System.Diagnostics.Debug.WriteLine($"[Linux Stub] Notification: {title} - {message}");
        // TODO: Implement using notify-send or DBus
        return Task.CompletedTask;
    }
}
