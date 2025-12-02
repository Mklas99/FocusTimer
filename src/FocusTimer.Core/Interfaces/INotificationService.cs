namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for showing notifications (break reminders, etc.).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a break reminder notification.
    /// </summary>
    Task ShowBreakReminderAsync(string message);

    /// <summary>
    /// Shows a general notification.
    /// </summary>
    Task ShowNotificationAsync(string title, string message);
}
