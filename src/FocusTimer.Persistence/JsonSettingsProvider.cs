namespace FocusTimer.Persistence
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// JSON-based implementation of ISettingsProvider.
    /// Stores settings in user's AppData folder.
    /// </summary>
    public class JsonSettingsProvider : ISettingsProvider
    {
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAppLogger? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSettingsProvider"/> class.
        /// </summary>
        public JsonSettingsProvider()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSettingsProvider"/> class with an optional logger.
        /// </summary>
        /// <param name="logger">An optional logger for diagnostics.</param>
        public JsonSettingsProvider(IAppLogger? logger)
        {
            this._logger = logger;

            // Store settings in user's AppData\Roaming\FocusTimer folder
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsFolder = Path.Combine(appDataFolder, "FocusTimer");

            Directory.CreateDirectory(settingsFolder);

            this._settingsFilePath = Path.Combine(settingsFolder, "settings.json");

            this._jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        /// <summary>
        /// Load settings from JSON file. Returns defaults if file doesn't exist or is invalid.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Settings> LoadAsync()
        {
            try
            {
                if (!File.Exists(this._settingsFilePath))
                {
                    this._logger?.LogDebug($"Settings file not found at {this._settingsFilePath}, using defaults.");
                    return new Settings();
                }

                var json = await File.ReadAllTextAsync(this._settingsFilePath);
                var settings = JsonSerializer.Deserialize<Settings>(json, this._jsonOptions);

                if (settings == null)
                {
                    this._logger?.LogWarning("Failed to deserialize settings; using defaults.");
                    return new Settings();
                }

                this._logger?.LogDebug($"Settings loaded from {this._settingsFilePath}.");
                return settings;
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Error loading settings.", ex);
                return new Settings();
            }
        }

        /// <summary>
        /// Save settings to JSON file.
        /// </summary>
        /// <param name="settings">The settings object to save.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SaveAsync(Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                var json = JsonSerializer.Serialize(settings, this._jsonOptions);
                await File.WriteAllTextAsync(this._settingsFilePath, json);
                this._logger?.LogDebug($"Settings saved to {this._settingsFilePath}.");
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Error saving settings.", ex);
                throw;
            }
        }
    }
}
