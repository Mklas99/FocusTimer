namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Models;
using System.Reflection;

public class JsonSettingsProviderTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsMultipleFields()
    {
        var provider = new JsonSettingsProvider(NullLogger.Instance);
        var settings = new Settings
        {
            ActiveThemeName = $"Theme_{Guid.NewGuid():N}",
            BreakIntervalMinutes = 37,
            WorklogDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
        };

        await provider.SaveAsync(settings);
        var loaded = await provider.LoadAsync();

        Assert.Equal(settings.ActiveThemeName, loaded.ActiveThemeName);
        Assert.Equal(37, loaded.BreakIntervalMinutes);
        Assert.Equal(settings.WorklogDirectory, loaded.WorklogDirectory);
    }

    [Fact]
    public async Task SaveAsync_GivenNull_ThrowsArgumentNullException()
    {
        var provider = new JsonSettingsProvider(NullLogger.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SaveAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_GivenInvalidJson_ReturnsDefaultSettings()
    {
        var provider = new JsonSettingsProvider(NullLogger.Instance);
        var path = GetSettingsPath(provider);
        var backupPath = path + ".bak";
        var hadExisting = File.Exists(path);

        try
        {
            if (hadExisting)
            {
                File.Copy(path, backupPath, overwrite: true);
            }

            await File.WriteAllTextAsync(path, "{ invalid-json }");

            var loaded = await provider.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Equal("Dark", loaded.ActiveThemeName);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (hadExisting && File.Exists(backupPath))
            {
                File.Move(backupPath, path, overwrite: true);
            }
        }
    }

    private static string GetSettingsPath(JsonSettingsProvider provider)
    {
        var field = typeof(JsonSettingsProvider).GetField("_settingsFilePath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var value = field!.GetValue(provider) as string;
        Assert.False(string.IsNullOrWhiteSpace(value));
        return value!;
    }
}
