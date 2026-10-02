namespace FocusTimer.Persistence
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// JSON-based implementation of ISettingsProvider.
    /// Stores settings in user's AppData folder or a custom specified file path.
    /// </summary>
    public class JsonSettingsProvider : ISettingsProvider, ISettingsCommitStore
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAppLogger? _logger;
        private readonly SemaphoreSlim _fileGate = new(1, 1);
        private readonly string _fallbackDeviceId = new Settings().DeviceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSettingsProvider"/> class.
        /// </summary>
        public JsonSettingsProvider()
            : this((string?)null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSettingsProvider"/> class with an optional logger.
        /// </summary>
        /// <param name="logger">An optional logger for diagnostics.</param>
        [ActivatorUtilitiesConstructor]
        public JsonSettingsProvider(IAppLogger? logger)
            : this((string?)null, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSettingsProvider"/> class with a custom settings file path and optional logger.
        /// </summary>
        /// <param name="settingsFilePath">The full path to the settings JSON file, or null to use the default AppData path.</param>
        /// <param name="logger">An optional logger for diagnostics.</param>
        public JsonSettingsProvider(string? settingsFilePath, IAppLogger? logger = null)
        {
            this._logger = logger;

            if (string.IsNullOrWhiteSpace(settingsFilePath))
            {
                // Store settings in user's AppData\Roaming\FocusTimer folder
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string settingsFolder = Path.Combine(appDataFolder, "FocusTimer");

                Directory.CreateDirectory(settingsFolder);

                this.SettingsFilePath = Path.Combine(settingsFolder, "settings.json");
            }
            else
            {
                string? directory = Path.GetDirectoryName(settingsFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                this.SettingsFilePath = settingsFilePath;
            }

            this._jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        /// <summary>
        /// Gets the full file path used by this settings provider.
        /// </summary>
        public string SettingsFilePath { get; }

        private string CandidatePath => this.SettingsFilePath + ".candidate";

        private string PreviousPath => this.SettingsFilePath + ".previous";

        private string JournalPath => this.SettingsFilePath + ".pending";

        /// <summary>
        /// Load settings from JSON file. Returns defaults only if the file does not exist.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Settings> LoadAsync()
        {
            await this._fileGate.WaitAsync();
            try
            {
                if (File.Exists(this.JournalPath))
                {
                    throw new SettingsRecoveryRequiredException();
                }

                if (!File.Exists(this.SettingsFilePath))
                {
                    var defaults = new Settings { DeviceId = this._fallbackDeviceId };
                    await this.WriteAsync(defaults);
                    this._logger?.LogDebug($"Settings file not found at {this.SettingsFilePath}; saved defaults.");
                    return defaults;
                }

                string json = await File.ReadAllTextAsync(this.SettingsFilePath);
                using JsonDocument document = JsonDocument.Parse(json);
                Settings? settings = JsonSerializer.Deserialize<Settings>(json, this._jsonOptions);

                if (settings == null)
                {
                    throw new JsonException("The settings file does not contain a settings object.");
                }

                if (document.RootElement.TryGetProperty("exclusionRules", out JsonElement savedRules)
                    && savedRules.ValueKind == JsonValueKind.Array
                    && savedRules.GetArrayLength() != settings.ExclusionRules.Count)
                {
                    this._logger?.LogWarning(
                        $"Ignored {savedRules.GetArrayLength() - settings.ExclusionRules.Count} malformed exclusion rule(s) in {this.SettingsFilePath}.");
                }

                if (!document.RootElement.TryGetProperty("deviceId", out JsonElement deviceId)
                    || deviceId.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(deviceId.GetString()))
                {
                    await this.WriteAsync(settings);
                }

                this._logger?.LogDebug($"Settings loaded from {this.SettingsFilePath}.");
                return settings;
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Error loading settings.", ex);
                throw;
            }
            finally
            {
                this._fileGate.Release();
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

            await this._fileGate.WaitAsync();
            try
            {
                if (File.Exists(this.JournalPath))
                {
                    throw new SettingsRecoveryRequiredException();
                }

                await this.WriteAsync(settings);
                this._logger?.LogDebug($"Settings saved to {this.SettingsFilePath}.");
            }
            catch (Exception ex)
            {
                this._logger?.LogError("Error saving settings.", ex);
                throw;
            }
            finally
            {
                this._fileGate.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<AutoStartRegistration?> GetPendingRecoveryAsync()
        {
            await this._fileGate.WaitAsync();
            try
            {
                if (!File.Exists(this.JournalPath))
                {
                    return null;
                }

                RecoveryJournal journal = await this.ReadJournalAsync();
                return journal.PreviousAutoStart;
            }
            finally
            {
                this._fileGate.Release();
            }
        }

        /// <inheritdoc/>
        public async Task BeginCommitAsync(Settings settings, AutoStartRegistration previousAutoStart)
        {
            ArgumentNullException.ThrowIfNull(settings);
            await this._fileGate.WaitAsync();
            try
            {
                if (File.Exists(this.JournalPath))
                {
                    throw new SettingsRecoveryRequiredException();
                }

                string json = JsonSerializer.Serialize(settings, this._jsonOptions);
                await File.WriteAllTextAsync(this.CandidatePath, json);
                bool previousFileExists = File.Exists(this.SettingsFilePath);
                if (previousFileExists)
                {
                    File.Copy(this.SettingsFilePath, this.PreviousPath, overwrite: true);
                }

                var journal = new RecoveryJournal(previousFileExists, previousAutoStart);
                await this.WriteAtomicallyAsync(this.JournalPath, JsonSerializer.Serialize(journal));
                File.Move(this.CandidatePath, this.SettingsFilePath, overwrite: true);
            }
            finally
            {
                this._fileGate.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RestorePreviousAsync()
        {
            await this._fileGate.WaitAsync();
            try
            {
                RecoveryJournal journal = await this.ReadJournalAsync();
                if (journal.PreviousFileExists)
                {
                    if (!File.Exists(this.PreviousPath))
                    {
                        throw new IOException("The previous settings copy is missing.");
                    }

                    File.Copy(this.PreviousPath, this.CandidatePath, overwrite: true);
                    File.Move(this.CandidatePath, this.SettingsFilePath, overwrite: true);
                }
                else if (File.Exists(this.SettingsFilePath))
                {
                    File.Delete(this.SettingsFilePath);
                }
            }
            finally
            {
                this._fileGate.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CompleteCommitAsync()
        {
            await this._fileGate.WaitAsync();
            try
            {
                if (File.Exists(this.JournalPath))
                {
                    File.Delete(this.JournalPath);
                }

                try
                {
                    if (File.Exists(this.PreviousPath))
                    {
                        File.Delete(this.PreviousPath);
                    }

                    if (File.Exists(this.CandidatePath))
                    {
                        File.Delete(this.CandidatePath);
                    }
                }
                catch (IOException ex)
                {
                    this._logger?.LogWarning($"Settings commit succeeded but recovery-file cleanup failed: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    this._logger?.LogWarning($"Settings commit succeeded but recovery-file cleanup failed: {ex.Message}");
                }
            }
            finally
            {
                this._fileGate.Release();
            }
        }

        private async Task<RecoveryJournal> ReadJournalAsync()
        {
            string json = await File.ReadAllTextAsync(this.JournalPath);
            return JsonSerializer.Deserialize<RecoveryJournal>(json)
                ?? throw new IOException("The settings recovery journal is invalid.");
        }

        private Task WriteAsync(Settings settings)
        {
            string json = JsonSerializer.Serialize(settings, this._jsonOptions);
            return this.WriteAtomicallyAsync(this.SettingsFilePath, json);
        }

        private async Task WriteAtomicallyAsync(string path, string content)
        {
            string temporaryPath = path + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, content);
            File.Move(temporaryPath, path, overwrite: true);
        }

        private sealed record RecoveryJournal(bool PreviousFileExists, AutoStartRegistration PreviousAutoStart);
    }
}
