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

    public JsonSettingsProvider()
    {
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
                System.Diagnostics.Debug.WriteLine($"Settings file not found at {_settingsFilePath}, using defaults");
                return new Settings();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<Settings>(json, _jsonOptions);
            
            if (settings == null)
            {
                System.Diagnostics.Debug.WriteLine("Failed to deserialize settings, using defaults");
                return new Settings();
            }

            System.Diagnostics.Debug.WriteLine($"Settings loaded from {_settingsFilePath}");
            return settings;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Settings saved to {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            throw;
        }
    }
}
