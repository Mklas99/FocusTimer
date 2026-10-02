namespace FocusTimer.App.Tests;

using System.Reflection;
using FocusTimer.App.Services;
using FocusTimer.App.ViewModels;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;
using FocusTimer.Core.Stubs;

public sealed class AppControllerStartupTests
{
    [Fact]
    public async Task InitializeAsync_LoadFailureDoesNotActivateDefaultsAndCanRetry()
    {
        var provider = new RetrySettingsProvider();
        var manager = new ThemeManager();
        var controller = new AppController(
            provider,
            new CountingHotkeys(),
            new LinuxIdleDetectionServiceStub(),
            null!,
            new ThemeService(),
            manager,
            () => throw new InvalidOperationException("Window creation is unavailable in this test."),
            null!,
            null!,
            null!,
            new NullLogger(),
            null,
            new InstallationIdentity());

        await controller.InitializeAsync();
        Assert.True(controller.StartupSettingsLoadFailed);
        Assert.Null(manager.ActiveTheme);

        provider.FailLoad = false;
        await controller.InitializeAsync();
        Assert.False(controller.StartupSettingsLoadFailed);
        Assert.Equal(25, controller.CurrentSettings.BreakIntervalMinutes);
    }

    [Fact]
    public async Task RuntimeActivation_UpdatesThemeAndHotkeysFromCommittedCandidate()
    {
        var manager = new ThemeManager();
        var themeService = new ThemeService();
        var hotkeys = new CountingHotkeys();
        var controller = new AppController(
            new FixedSettingsProvider(new Settings()),
            hotkeys,
            new LinuxIdleDetectionServiceStub(),
            null!,
            themeService,
            manager,
            () => throw new InvalidOperationException("Window creation is unavailable in this test."),
            null!,
            null!,
            null!,
            new NullLogger(),
            null,
            new InstallationIdentity());
        var candidate = new Settings
        {
            AlwaysOnTop = false,
            ActivityPollingIntervalSeconds = 5,
            HotkeyShowHide = "Ctrl+Alt+K",
            Theme = new Theme { ThemeName = "Custom", AccentPrimary = "#123456" },
        };

        Task activation = (Task)typeof(AppController)
            .GetMethod("ActivateSettingsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(controller, [candidate])!;
        await activation;

        Assert.Equal(5, controller.CurrentSettings.ActivityPollingIntervalSeconds);
        Assert.Equal("#123456", manager.ActiveTheme!.AccentPrimary);
        Assert.Equal("#123456", themeService.CurrentTheme.AccentPrimary);
        Assert.Equal(2, hotkeys.RegisterCount);
    }

    [Fact]
    public async Task RuntimeActivation_AppearanceOnlyApplyKeepsHotkeyRegistrations()
    {
        var hotkeys = new CountingHotkeys();
        var controller = new AppController(
            new FixedSettingsProvider(new Settings()),
            hotkeys,
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
        var activationMethod = typeof(AppController)
            .GetMethod("ActivateSettingsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var candidate = new Settings { HotkeyShowHide = "Ctrl+Alt+K" };
        await (Task)activationMethod.Invoke(controller, [candidate])!;
        Assert.Equal(2, hotkeys.RegisterCount);

        candidate.Theme.BackgroundOpacity = 0.25;
        await (Task)activationMethod.Invoke(controller, [candidate])!;
        Assert.Equal(2, hotkeys.RegisterCount);

        candidate.HotkeyShowHide = "Ctrl+Alt+J";
        await (Task)activationMethod.Invoke(controller, [candidate])!;
        Assert.Equal(4, hotkeys.RegisterCount);
    }

    [Fact]
    public async Task InitializeAsync_RetriesPendingRecoveryBeforeLoadingCandidate()
    {
        var provider = new RecoverySettingsProvider();
        var registration = new Registration();
        var controller = new AppController(
            provider,
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
            new InstallationIdentity(),
            registration);

        await controller.InitializeAsync();
        Assert.True(controller.StartupRecoveryRequired);
        Assert.Equal(0, provider.LoadCalls);

        provider.FailRestore = false;
        await controller.InitializeAsync();
        Assert.False(controller.StartupRecoveryRequired);
        Assert.Equal(50, controller.CurrentSettings.BreakIntervalMinutes);
        Assert.Equal(1, provider.LoadCalls);
    }

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

    private sealed class RetrySettingsProvider : ISettingsProvider
    {
        public bool FailLoad { get; set; } = true;

        public Task<Settings> LoadAsync() => this.FailLoad
            ? Task.FromException<Settings>(new IOException("Existing settings are unreadable."))
            : Task.FromResult(new Settings { BreakIntervalMinutes = 25 });

        public Task SaveAsync(Settings settings) => Task.CompletedTask;
    }

    private sealed class RecoverySettingsProvider : ISettingsProvider, ISettingsCommitStore
    {
        private bool _pending = true;
        private Settings _saved = new() { BreakIntervalMinutes = 25 };

        public bool FailRestore { get; set; } = true;

        public int LoadCalls { get; private set; }

        public Task<Settings> LoadAsync()
        {
            this.LoadCalls++;
            return this._pending
                ? Task.FromException<Settings>(new SettingsRecoveryRequiredException())
                : Task.FromResult(this._saved.Clone());
        }

        public Task SaveAsync(Settings value) => throw new NotImplementedException();

        public Task<AutoStartRegistration?> GetPendingRecoveryAsync() =>
            Task.FromResult(this._pending ? new AutoStartRegistration(false) : null);

        public Task BeginCommitAsync(Settings settings, AutoStartRegistration previousAutoStart) => throw new NotImplementedException();

        public Task RestorePreviousAsync()
        {
            if (this.FailRestore)
            {
                throw new IOException("Previous settings unavailable.");
            }

            this._saved = new Settings { BreakIntervalMinutes = 50 };
            return Task.CompletedTask;
        }

        public Task CompleteCommitAsync()
        {
            this._pending = false;
            return Task.CompletedTask;
        }
    }

    private sealed class Registration : IAutoStartService
    {
        public bool IsAutoStartEnabled() => false;

        public void SetAutoStart(bool enabled) { }
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
