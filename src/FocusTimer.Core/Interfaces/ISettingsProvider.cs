using FocusTimer.Core.Models;

namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for loading and saving application settings.
/// </summary>
public interface ISettingsProvider
{
    /// <summary>
    /// Loads settings from persistent storage.
    /// </summary>
    Task<Settings> LoadAsync();

    /// <summary>
    /// Saves settings to persistent storage.
    /// </summary>
    Task SaveAsync(Settings settings);
}
