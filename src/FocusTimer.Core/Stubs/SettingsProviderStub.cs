namespace FocusTimer.Core.Stubs
{
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Stub implementation of ISettingsProvider that returns default settings.
    /// </summary>
    public class SettingsProviderStub : ISettingsProvider
    {
        /// <summary>
        /// Gets the default settings instance.
        /// </summary>
        private readonly Settings _defaultSettings = new();

        /// <summary>
        /// Gets the optional app logger instance.
        /// </summary>
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsProviderStub"/> class.
        /// </summary>
        public SettingsProviderStub()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsProviderStub"/> class.
        /// </summary>
        /// <param name="logger">The optional app logger instance.</param>
        public SettingsProviderStub(IAppLogger? logger)
        {
            this._logger = logger;
        }

        /// <inheritdoc/>
        public Task<Settings> LoadAsync()
        {
            // Return default settings for now
            return Task.FromResult(this._defaultSettings);
        }

        /// <inheritdoc/>
        public Task SaveAsync(Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            // Stub: just log
            this._logger?.LogInformation("[SettingsProviderStub] Would save settings to disk.");
            return Task.CompletedTask;
        }
    }
}
