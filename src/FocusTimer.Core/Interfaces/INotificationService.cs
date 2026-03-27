namespace FocusTimer.Core.Interfaces
{
    /// <summary>
    /// Service for showing notifications (break reminders, etc.).
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Shows a break reminder notification.
        /// </summary>
        /// <param name="message">The notification message to display.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ShowBreakReminderAsync(string message);

        /// <summary>
        /// Shows a general notification.
        /// </summary>
        /// <param name="title">The notification title to display.</param>
        /// <param name="message">The notification message to display.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ShowNotificationAsync(string title, string message);
    }
}
