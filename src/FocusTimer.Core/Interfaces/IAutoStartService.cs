namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for managing auto-start on login.
/// </summary>
public interface IAutoStartService
{
    /// <summary>
    /// Enables or disables auto-start on login.
    /// </summary>
    void SetAutoStart(bool enabled);

    /// <summary>
    /// Checks if auto-start is currently enabled.
    /// </summary>
    bool IsAutoStartEnabled();
}
