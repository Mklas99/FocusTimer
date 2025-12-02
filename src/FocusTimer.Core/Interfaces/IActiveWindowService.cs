using FocusTimer.Core.Models;

namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for detecting the currently active application window.
/// </summary>
public interface IActiveWindowService
{
    /// <summary>
    /// Gets information about the currently active (foreground) window.
    /// </summary>
    /// <returns>ActiveWindowInfo if a window is detected, null otherwise.</returns>
    Task<ActiveWindowInfo?> GetForegroundWindowAsync();
}
