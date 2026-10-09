namespace FocusTimer.App.Services;

using FocusTimer.Core.Models;

/// <summary>Immutable notification preferences used when opening a toast.</summary>
public sealed class NotificationPolicy
{
    private readonly Settings _settings;

    /// <summary>Initializes a new instance of the <see cref="NotificationPolicy"/> class.</summary>
    /// <param name="settings">Committed settings to snapshot.</param>
    public NotificationPolicy(Settings settings) => this._settings = settings.Clone();

    /// <summary>Checks whether a general notification should appear. Errors always appear.</summary>
    /// <param name="severity">Notification severity.</param>
    /// <returns>Whether the toast is enabled.</returns>
    public bool IsEnabled(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Information => this._settings.InformationNotificationsEnabled,
        NotificationSeverity.Warning => this._settings.WarningNotificationsEnabled,
        _ => true,
    };

    /// <summary>Gets a bounded automatic dismissal duration.</summary>
    /// <param name="severity">Notification severity.</param>
    /// <returns>The configured duration or its default when invalid.</returns>
    public TimeSpan GetDismissalDelay(NotificationSeverity severity)
    {
        int seconds = severity switch
        {
            NotificationSeverity.Information => this._settings.InformationDismissalSeconds,
            NotificationSeverity.Warning => this._settings.WarningDismissalSeconds,
            _ => this._settings.ErrorDismissalSeconds,
        };
        return seconds is >= 1 and <= 120
            ? TimeSpan.FromSeconds(seconds)
            : DesktopNotificationService.GetDismissalDelay(severity);
    }
}
