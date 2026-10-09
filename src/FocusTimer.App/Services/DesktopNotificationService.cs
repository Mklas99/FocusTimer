namespace FocusTimer.App.Services;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using FocusTimer.App.Views;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

/// <summary>Displays themed app-owned toasts without creating another tray icon.</summary>
public sealed class DesktopNotificationService : INotificationService
{
    private readonly IAppLogger? _logger;

    /// <summary>Initializes a new instance of the <see cref="DesktopNotificationService"/> class.</summary>
    /// <param name="logger">Optional diagnostic logger.</param>
    public DesktopNotificationService(IAppLogger? logger = null) => this._logger = logger;

    /// <summary>Gets the automatic dismissal delay for a notification.</summary>
    /// <param name="severity">The urgency of the notification.</param>
    /// <returns>Fifteen seconds for warnings/errors, five seconds for information.</returns>
    public static TimeSpan GetDismissalDelay(NotificationSeverity severity) =>
        TimeSpan.FromSeconds(severity == NotificationSeverity.Information ? 5 : 15);

    /// <inheritdoc/>
    public async Task ShowBreakReminderAsync(string message, bool requireAcknowledgement)
    {
        try
        {
            Task closed = Task.CompletedTask;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                closed = this.ShowWindow("Break Reminder", message, requireAcknowledgement);
            });
            await closed;
        }
        catch (Exception ex)
        {
            this._logger?.LogError("Break reminder failed.", ex);
        }
    }

    /// <inheritdoc/>
    public Task ShowNotificationAsync(string title, string message) =>
        this.ShowNotificationAsync(title, message, NotificationSeverity.Information);

    /// <inheritdoc/>
    public async Task ShowNotificationAsync(string title, string message, NotificationSeverity severity)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => { _ = this.ShowWindow(title, message, false, severity); });
        }
        catch (Exception ex)
        {
            this._logger?.LogError("Notification failed.", ex);
        }
    }

    private Task ShowWindow(string title, string message, bool requireAcknowledgement, NotificationSeverity severity = NotificationSeverity.Information)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var window = new NotificationWindow(title, message, requireAcknowledgement, severity);
        var timer = new DispatcherTimer { Interval = GetDismissalDelay(severity) };
        window.Closed += (_, _) =>
        {
            timer.Stop();
            completion.TrySetResult();
        };
        timer.Tick += (_, _) => window.Close();
        window.Opened += (_, _) =>
        {
            // Working areas are physical pixels; window bounds are logical pixels.
            Screen? screen = window.Screens.Primary;
            if (screen != null)
            {
                double scale = screen.Scaling;
                window.Position = new PixelPoint(
                    screen.WorkingArea.Right - (int)Math.Ceiling((window.Bounds.Width + 20) * scale),
                    screen.WorkingArea.Bottom - (int)Math.Ceiling((window.Bounds.Height + 20) * scale));
            }
        };
        window.Show();
        if (!requireAcknowledgement)
        {
            timer.Start();
        }

        this._logger?.LogDebug($"Notification displayed (requires acknowledgement: {requireAcknowledgement}).");
        return completion.Task;
    }
}
