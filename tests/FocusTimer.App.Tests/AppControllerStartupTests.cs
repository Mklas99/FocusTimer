namespace FocusTimer.App.Tests;

using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;

public sealed class AppControllerStartupTests
{
    [Theory]
    [InlineData("Dark", "Dark", "Soft", 0.38, "Off", 0.38)]
    [InlineData("Dark", "Custom", "Soft", 0.38, "Off", 0.80)]
    public async Task InitializeAsync_PreservesSavedThemeButUsesPresetForDefaults(
        string activeName,
        string savedName,
        string savedBlur,
        double savedOpacity,
        string expectedBlur,
        double expectedOpacity)
    {
        var settings = new Settings
        {
            ActiveThemeName = activeName,
            Theme = new Theme
            {
                ThemeName = savedName,
                WidgetBlurMode = savedBlur,
                BackgroundOpacity = savedOpacity,
            },
        };
        var controller = new AppController(
            new FixedSettingsProvider(settings),
            new CountingHotkeys(),
            new LinuxIdleDetectionServiceStub(),
            null!,
            new ThemeService(),
            new ThemeManager(),
            () => throw new InvalidOperationException("Window creation is unavailable in this test."),
            null!,
            null!,
            null!,
            new NullLogger(),
            null,
            new InstallationIdentity());

        await controller.InitializeAsync();

        Assert.Equal(expectedBlur, controller.CurrentSettings.Theme.WidgetBlurMode);
        Assert.Equal(expectedOpacity, controller.CurrentSettings.Theme.BackgroundOpacity);
    }

    [Fact]
    public async Task InitializeAsync_WhenSavedThemeFails_KeepsIdentityAndEnablesTrayActions()
    {
        var settings = new Settings
        {
            ActiveThemeName = "Custom",
            WorkLoggingEnabled = false,
        };
        var identity = new InstallationIdentity();
        var hotkeys = new CountingHotkeys();
        var controller = new AppController(
            new FixedSettingsProvider(settings),
            hotkeys,
            new LinuxIdleDetectionServiceStub(),
            null!,
            new ThrowingThemeService(),
            new ThemeManager(),
            () => throw new InvalidOperationException("Window creation is unavailable in this test."),
            null!,
            null!,
            null!,
            new NullLogger(),
            null,
            identity);

        await controller.InitializeAsync();
        controller.RegisterHotkeys();

        Assert.Equal(settings.DeviceId, identity.DeviceId);
        Assert.False(controller.CurrentSettings.WorkLoggingEnabled);
        Assert.Equal("Dark", controller.CurrentSettings.ActiveThemeName);
        Assert.Equal("Dark", controller.CurrentSettings.Theme.ThemeName);
        Assert.Equal(2, hotkeys.RegisterCount);
    }

    private sealed class FixedSettingsProvider(Settings settings) : ISettingsProvider
    {
        public Task<Settings> LoadAsync() => Task.FromResult(settings);

        public Task SaveAsync(Settings value) => Task.CompletedTask;
    }

    private sealed class ThrowingThemeService : IThemeService
    {
        public Theme CurrentTheme => new();

        public IReadOnlyList<Theme> BuiltInThemes => [];

        public Theme? GetBuiltInTheme(string themeName) => throw new InvalidOperationException("Invalid saved theme.");

        public void ApplyTheme(Theme theme) { }

        public Task<Theme> LoadThemeFromFileAsync(string filePath) => throw new NotImplementedException();

        public Task SaveThemeToFileAsync(Theme theme, string filePath) => throw new NotImplementedException();

        public void ResetToDefault() { }

        public bool ValidateTheme(Theme theme) => true;
    }

    private sealed class CountingHotkeys : IGlobalHotkeyService
    {
        public int RegisterCount { get; private set; }

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed
        {
            add { }
            remove { }
        }

        public void Register(HotkeyDefinition definition) => this.RegisterCount++;

        public void UnregisterAll() { }
    }

    private sealed class NullLogger : IAppLogger
    {
        public void LogCritical(string message, Exception? exception = null) { }

        public void LogError(string message, Exception? exception = null) { }

        public void LogWarning(string message) { }

        public void LogInformation(string message) { }

        public void LogDebug(string message) { }
    }
}
