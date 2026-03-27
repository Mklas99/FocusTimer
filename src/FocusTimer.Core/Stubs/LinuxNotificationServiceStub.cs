namespace FocusTimer.Core.Stubs
{
    using FocusTimer.Core.Interfaces;

    /// <summary>
    /// Linux stub for notification service.
    /// TODO: Implement using DBus notifications or notify-send command.
    /// </summary>
    public class LinuxNotificationServiceStub : INotificationService
    {
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxNotificationServiceStub"/> class.
        /// </summary>
        public LinuxNotificationServiceStub()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxNotificationServiceStub"/> class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        public LinuxNotificationServiceStub(IAppLogger? logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public Task ShowBreakReminderAsync(string message)
        {
            this._logger?.LogInformation($"[Linux Stub] Break reminder: {message}");

            // TODO: Implement using notify-send or DBus
            // Example: Process.Start("notify-send", $"\"Break Reminder\" \"{message}\"");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ShowNotificationAsync(string title, string message)
        {
            this._logger?.LogInformation($"[Linux Stub] Notification: {title} - {message}");

            // TODO: Implement using notify-send or DBus
            return Task.CompletedTask;
        }
    }
}
