namespace FocusTimer.Core.Interfaces
{
    using FocusTimer.Core.Models;

    /// <summary>
    /// Service for loading and saving application settings.
    /// </summary>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Loads settings from persistent storage.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<Settings> LoadAsync();

        /// <summary>
        /// Saves settings to persistent storage.
        /// </summary>
        /// <param name="settings">The settings object to save.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveAsync(Settings settings);
    }
}
