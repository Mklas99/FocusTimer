using FocusTimer.Core.Interfaces;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Linux stub for notification service.
/// TODO: Implement using DBus notifications or notify-send command.
/// </summary>
public class LinuxNotificationServiceStub : INotificationService
{
    private readonly IAppLogger? _logger;

    public LinuxNotificationServiceStub()
        : this(null)
    {
    }

    public LinuxNotificationServiceStub(IAppLogger? logger)
    {
        _logger = logger;
    }

    public Task ShowBreakReminderAsync(string message)
    {
        _logger?.LogInformation($"[Linux Stub] Break reminder: {message}");
        // TODO: Implement using notify-send or DBus
        // Example: Process.Start("notify-send", $"\"Break Reminder\" \"{message}\"");
        return Task.CompletedTask;
    }

    public Task ShowNotificationAsync(string title, string message)
    {
        _logger?.LogInformation($"[Linux Stub] Notification: {title} - {message}");
        // TODO: Implement using notify-send or DBus
        return Task.CompletedTask;
    }
}
