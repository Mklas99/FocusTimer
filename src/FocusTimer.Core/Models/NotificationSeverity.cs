namespace FocusTimer.Core.Models;

/// <summary>The urgency of an app notification.</summary>
public enum NotificationSeverity
{
    /// <summary>Routine information.</summary>
    Information,

    /// <summary>A problem that needs attention.</summary>
    Warning,

    /// <summary>An operation failed.</summary>
    Error,
}
