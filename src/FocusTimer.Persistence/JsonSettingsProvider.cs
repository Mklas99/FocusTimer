using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Persistence;

/// <summary>
/// JSON-based implementation of ISettingsProvider.
/// Stores settings in user's AppData folder.
/// </summary>
public class JsonSettingsProvider : ISettingsProvider
{
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAppLogger? _logger;

    public JsonSettingsProvider()
        : this(null)
    {
    }

    public JsonSettingsProvider(IAppLogger? logger)
    {
        _logger = logger;

        // Store settings in user's AppData\Roaming\FocusTimer folder
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsFolder = Path.Combine(appDataFolder, "FocusTimer");
        
        Directory.CreateDirectory(settingsFolder);
        
        _settingsFilePath = Path.Combine(settingsFolder, "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Load settings from JSON file. Returns defaults if file doesn't exist or is invalid.
    /// </summary>
    public async Task<Settings> LoadAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger?.LogDebug($"Settings file not found at {_settingsFilePath}, using defaults.");
                return new Settings();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<Settings>(json, _jsonOptions);
            
            if (settings == null)
            {
                _logger?.LogWarning("Failed to deserialize settings; using defaults.");
                return new Settings();
            }

            _logger?.LogDebug($"Settings loaded from {_settingsFilePath}.");
            return settings;
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error loading settings.", ex);
            return new Settings();
        }
    }

    /// <summary>
    /// Save settings to JSON file.
    /// </summary>
    public async Task SaveAsync(Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger?.LogDebug($"Settings saved to {_settingsFilePath}.");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error saving settings.", ex);
            throw;
        }
    }
}
