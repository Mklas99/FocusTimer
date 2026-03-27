using System.Timers;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using Timer = System.Timers.Timer;

namespace FocusTimer.Core.Services;

public class BreakReminderService : IDisposable
{
    private readonly INotificationService _notificationService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IAppLogger? _logger;
    private Timer? _reminderTimer;
    private bool _disposed;

    public BreakReminderService(INotificationService notificationService, ISettingsProvider settingsProvider)
        : this(notificationService, settingsProvider, null)
    {
    }

    public BreakReminderService(INotificationService notificationService, ISettingsProvider settingsProvider, IAppLogger? logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        _logger = logger;
    }

    /// <summary>
    /// Starts tracking for break reminders when timer starts/resumes.
    /// </summary>
    public async void OnTimerStarted()
    {
        await ScheduleReminderAsync();
    }

    /// <summary>
    /// Cancels any pending reminders when timer is paused/stopped.
    /// </summary>
    public void OnTimerPaused()
    {
        _reminderTimer?.Stop();
        _reminderTimer?.Dispose();
        _reminderTimer = null;
    }

    private async Task ScheduleReminderAsync()
    {
        try
        {
            var settings = await _settingsProvider.LoadAsync();
            if (!settings.BreakRemindersEnabled || settings.BreakIntervalMinutes <= 0)
                return;

            _reminderTimer?.Stop();
            _reminderTimer?.Dispose();
            _reminderTimer = null;

            var intervalMs = settings.BreakIntervalMinutes * 60 * 1000;
            _reminderTimer = new Timer(intervalMs) { AutoReset = false };
            _reminderTimer.Elapsed += async (s, e) => await OnReminderElapsedAsync(settings.BreakIntervalMinutes);
            _reminderTimer.Start();
            _logger?.LogDebug($"Scheduled break reminder in {settings.BreakIntervalMinutes} minutes.");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to schedule break reminder.", ex);
        }
    }

    private async Task OnReminderElapsedAsync(int breakInterval)
    {
        try
        {
            await _notificationService.ShowBreakReminderAsync(
                $"You've been working for {breakInterval} minutes. Time to take a break!");

            // Auto-snooze: schedule another reminder in 10 minutes if still enabled
            var settings = await _settingsProvider.LoadAsync();
            if (settings.BreakRemindersEnabled)
            {
                _logger?.LogDebug("Scheduling auto-snooze break reminder in 10 minutes.");

                _reminderTimer?.Dispose();
                _reminderTimer = new Timer(10 * 60 * 1000) { AutoReset = false };
                _reminderTimer.Elapsed += async (s, e) => await OnReminderElapsedAsync(settings.BreakIntervalMinutes);
                _reminderTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Break reminder failed.", ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _reminderTimer?.Stop();
        _reminderTimer?.Dispose();
        _reminderTimer = null;
        _disposed = true;
    }
}
