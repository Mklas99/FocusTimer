namespace FocusTimer.Persistence.Tests;

using System;
using System.IO;
using System.Threading.Tasks;
using FocusTimer.Core.Models;
using Xunit;

public class JsonSettingsProviderTests : IDisposable
{
    private readonly string _testDirectory;

    public JsonSettingsProviderTests()
    {
        this._testDirectory = Path.Combine(Path.GetTempPath(), $"focustimer_settings_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this._testDirectory);
    }

    [Fact]
    public void Constructor_GivenCustomPath_UsesSpecifiedPath()
    {
        var customPath = Path.Combine(this._testDirectory, "custom_settings.json");
        var provider = new JsonSettingsProvider(customPath, NullLogger.Instance);

        Assert.Equal(customPath, provider.SettingsFilePath);
    }

    [Fact]
    public void Constructor_GivenNullOrWhitespace_DefaultsToAppDataPath()
    {
        var provider = new JsonSettingsProvider(NullLogger.Instance);
        var expectedFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusTimer");
        var expectedPath = Path.Combine(expectedFolder, "settings.json");

        Assert.Equal(expectedPath, provider.SettingsFilePath);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsMultipleFields()
    {
        var customPath = Path.Combine(this._testDirectory, "roundtrip.json");
        var provider = new JsonSettingsProvider(customPath, NullLogger.Instance);
        var settings = new Settings
        {
            ActiveThemeName = $"Theme_{Guid.NewGuid():N}",
            BreakIntervalMinutes = 37,
            WorklogDirectory = Path.Combine(this._testDirectory, "worklogs"),
        };
        settings.Theme.WidgetBlurMode = WidgetBlurModes.Solid;
        settings.Theme.BackgroundOpacity = 0.37;

        await provider.SaveAsync(settings);
        var loaded = await provider.LoadAsync();

        Assert.Equal(settings.ActiveThemeName, loaded.ActiveThemeName);
        Assert.Equal(37, loaded.BreakIntervalMinutes);
        Assert.Equal(settings.WorklogDirectory, loaded.WorklogDirectory);
        Assert.Equal(settings.DeviceId, loaded.DeviceId);
        Assert.Equal(WidgetBlurModes.Solid, loaded.Theme.WidgetBlurMode);
        Assert.Equal(0.37, loaded.Theme.BackgroundOpacity);
        Assert.False(string.IsNullOrWhiteSpace(loaded.DeviceId));
        Assert.True(File.Exists(customPath));
    }

    [Fact]
    public async Task SaveAsync_GivenNull_ThrowsArgumentNullException()
    {
        var customPath = Path.Combine(this._testDirectory, "null_test.json");
        var provider = new JsonSettingsProvider(customPath, NullLogger.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SaveAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_GivenMissingFile_ReturnsDefaultSettings()
    {
        var customPath = Path.Combine(this._testDirectory, "non_existent.json");
        var provider = new JsonSettingsProvider(customPath, NullLogger.Instance);

        var loaded = await provider.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("Dark", loaded.ActiveThemeName);
        Assert.True(File.Exists(customPath));
        var restarted = new JsonSettingsProvider(customPath, NullLogger.Instance);
        Assert.Equal(loaded.DeviceId, (await restarted.LoadAsync()).DeviceId);
    }

    [Fact]
    public async Task LoadAsync_GivenInvalidJson_ReturnsDefaultSettings()
    {
        var customPath = Path.Combine(this._testDirectory, "invalid.json");
        await File.WriteAllTextAsync(customPath, "{ invalid-json }");

        var provider = new JsonSettingsProvider(customPath, NullLogger.Instance);
        var loaded = await provider.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("Dark", loaded.ActiveThemeName);
        Assert.Equal(loaded.DeviceId, (await provider.LoadAsync()).DeviceId);
        Assert.Equal("{ invalid-json }", await File.ReadAllTextAsync(customPath));
    }

    [Fact]
    public async Task LoadAsync_GivenConcurrentFirstLoads_PersistsOneIdentity()
    {
        var path = Path.Combine(this._testDirectory, "parallel.json");
        var provider = new JsonSettingsProvider(path, NullLogger.Instance);
        var loaded = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => provider.LoadAsync()));

        Assert.Single(loaded.Select(s => s.DeviceId).Distinct());
        Assert.Equal(loaded[0].DeviceId, (await new JsonSettingsProvider(path, NullLogger.Instance).LoadAsync()).DeviceId);
    }

    [Fact]
    public async Task LoadAsync_GivenOlderSettingsWithoutDeviceId_PersistsGeneratedIdentity()
    {
        var path = Path.Combine(this._testDirectory, "older.json");
        await File.WriteAllTextAsync(path, "{\"activeThemeName\":\"Light\"}");
        var first = await new JsonSettingsProvider(path, NullLogger.Instance).LoadAsync();
        var restarted = await new JsonSettingsProvider(path, NullLogger.Instance).LoadAsync();

        Assert.Equal("Light", restarted.ActiveThemeName);
        Assert.Equal(first.DeviceId, restarted.DeviceId);
        Assert.Equal(WidgetBlurModes.Off, restarted.Theme.WidgetBlurMode);
    }

    [Theory]
    [InlineData("Soft")]
    [InlineData("Strong")]
    [InlineData("Blur")]
    public async Task LoadAsync_GivenLegacyBackdropChoice_PreservesTintAndMigratesChoice(string legacyMode)
    {
        var path = Path.Combine(this._testDirectory, "legacy-backdrop.json");
        await File.WriteAllTextAsync(path, $"{{ \"theme\": {{ \"widgetBlurMode\": \"{legacyMode}\", \"backgroundOpacity\": 0.29 }} }}");

        var loaded = await new JsonSettingsProvider(path, NullLogger.Instance).LoadAsync();

        Assert.Equal(WidgetBlurModes.Off, loaded.Theme.WidgetBlurMode);
        Assert.Equal(0.29, loaded.Theme.BackgroundOpacity);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(this._testDirectory))
            {
                Directory.Delete(this._testDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup in test temp directory
        }

        GC.SuppressFinalize(this);
    }
}
