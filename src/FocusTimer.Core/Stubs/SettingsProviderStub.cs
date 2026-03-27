using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Stub implementation of ISettingsProvider that returns default settings.
/// </summary>
public class SettingsProviderStub : ISettingsProvider
{
    private readonly Settings _defaultSettings = new();
    private readonly IAppLogger? _logger;

    public SettingsProviderStub()
        : this(null)
    {
    }

    public SettingsProviderStub(IAppLogger? logger)
    {
        _logger = logger;
    }

    public Task<Settings> LoadAsync()
    {
        // Return default settings for now
        return Task.FromResult(_defaultSettings);
    }

    public Task SaveAsync(Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Stub: just log
        _logger?.LogInformation("[SettingsProviderStub] Would save settings to disk.");
        return Task.CompletedTask;
    }
}
