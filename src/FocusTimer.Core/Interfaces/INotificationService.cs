namespace FocusTimer.Core.Interfaces
{
    using FocusTimer.Core.Models;

    /// <summary>
    /// Service for showing notifications (break reminders, etc.).
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Shows a break reminder notification.
        /// </summary>
        /// <param name="message">The notification message to display.</param>
        /// <param name="requireAcknowledgement">Whether the reminder should remain visible until acknowledged by the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ShowBreakReminderAsync(string message, bool requireAcknowledgement);

        /// <summary>
        /// Shows a general notification.
        /// </summary>
        /// <param name="title">The notification title to display.</param>
        /// <param name="message">The notification message to display.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ShowNotificationAsync(string title, string message);

        /// <summary>Shows a notification with an explicit severity.</summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The message.</param>
        /// <param name="severity">The urgency of the notification.</param>
        /// <returns>A task representing the display operation.</returns>
        Task ShowNotificationAsync(string title, string message, NotificationSeverity severity) =>
            this.ShowNotificationAsync(title, message);
    }
}
