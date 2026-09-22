namespace FocusTimer.Core.Services
{
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using Timer = System.Timers.Timer;

    /// <summary>
    /// Manages break reminders during focus timer sessions.
    /// </summary>
    public class BreakReminderService : IDisposable
    {
        private readonly INotificationService _notificationService;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IAppLogger? _logger;
        private Timer? _reminderTimer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BreakReminderService"/> class.
        /// </summary>
        /// <param name="notificationService">The notification service to use for reminders.</param>
        /// <param name="settingsProvider">The settings provider to load break reminder settings.</param>
        public BreakReminderService(INotificationService notificationService, ISettingsProvider settingsProvider)
            : this(notificationService, settingsProvider, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BreakReminderService"/> class.
        /// </summary>
        /// <param name="notificationService">The notification service to use for reminders.</param>
        /// <param name="settingsProvider">The settings provider to load break reminder settings.</param>
        /// <param name="logger">The optional app logger for debugging and error logging.</param>
        public BreakReminderService(INotificationService notificationService, ISettingsProvider settingsProvider, IAppLogger? logger)
        {
            this._notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            this._settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            this._logger = logger;
        }

        /// <summary>
        /// Starts tracking for break reminders when timer starts/resumes.
        /// </summary>
        public async void OnTimerStarted()
        {
            await this.ScheduleReminderAsync();
        }

        /// <summary>
        /// Cancels any pending reminders when timer is paused/stopped.
        /// </summary>
        public void OnTimerPaused()
        {
            this.ResetReminderTimer();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources used by the service.
        /// </summary>
        /// <param name="disposing">True when called from <see cref="Dispose()"/>; false when called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
            {
                return;
            }

            if (disposing)
            {
                this.ResetReminderTimer();
            }

            this._disposed = true;
        }

        private async Task ScheduleReminderAsync()
        {
            try
            {
                Settings settings = await this._settingsProvider.LoadAsync();
                if (!settings.BreakRemindersEnabled || settings.BreakIntervalMinutes <= 0)
                {
                    return;
                }

                this.ResetReminderTimer();

                int intervalMs = settings.BreakIntervalMinutes * 60 * 1000;
                this._reminderTimer = new Timer(intervalMs) { AutoReset = false };
                this._reminderTimer.Elapsed += async (s, e) => await this.OnReminderElapsedAsync(settings.BreakIntervalMinutes);
                this._reminderTimer.Start();
                this._logger?.LogDebug($"Scheduled break reminder in {settings.BreakIntervalMinutes} minutes.");
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Failed to schedule break reminder.", ex);
            }
        }

        private async Task OnReminderElapsedAsync(int breakInterval)
        {
            try
            {
                Settings settings = await this._settingsProvider.LoadAsync();
                if (!settings.BreakRemindersEnabled)
                {
                    return;
                }

                await this._notificationService.ShowBreakReminderAsync(
                    $"You've been working for {breakInterval} minutes. Time to take a break!",
                    settings.RequireBreakReminderAcknowledgement);

                if (settings.BreakRemindersEnabled)
                {
                    int nextReminderMinutes = settings.RequireBreakReminderAcknowledgement
                        ? settings.BreakIntervalMinutes
                        : 10;

                    this._logger?.LogDebug($"Scheduling next break reminder in {nextReminderMinutes} minutes.");
                    this.ScheduleNextReminder(nextReminderMinutes);
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Break reminder failed.", ex);
            }
        }

        private void ResetReminderTimer()
        {
            this._reminderTimer?.Stop();
            this._reminderTimer?.Dispose();
            this._reminderTimer = null;
        }

        private void ScheduleNextReminder(int minutes)
        {
            if (minutes <= 0)
            {
                return;
            }

            this.ResetReminderTimer();
            this._reminderTimer = new Timer(minutes * 60 * 1000) { AutoReset = false };
            this._reminderTimer.Elapsed += async (s, e) => await this.OnReminderElapsedAsync(minutes);
            this._reminderTimer.Start();
        }
    }
}
