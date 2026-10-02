namespace FocusTimer.Platform.Windows.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using Microsoft.Win32;

public class WindowsAutoStartServiceTests : IDisposable
{
    private readonly string _testKeyPath = $@"Software\FocusTimerTests\{Guid.NewGuid():N}";
    private readonly WindowsAutoStartService _service;

    public WindowsAutoStartServiceTests()
    {
        Registry.CurrentUser.CreateSubKey(this._testKeyPath)?.Dispose();
        this._service = new WindowsAutoStartService(this._testKeyPath, null);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);
    }

    [Fact]
    public void SetAutoStart_GivenEnabledTrue_MakesIsAutoStartEnabledReturnTrue()
    {
        this._service.SetAutoStart(enabled: true);

        Assert.True(this._service.IsAutoStartEnabled());
    }

    [Fact]
    public void SetAutoStart_GivenEnabledFalseAfterEnabled_MakesIsAutoStartEnabledReturnFalse()
    {
        this._service.SetAutoStart(enabled: true);
        this._service.SetAutoStart(enabled: false);

        Assert.False(this._service.IsAutoStartEnabled());
    }

    [Fact]
    public void SetAutoStart_GivenEnabledFalseWhenNotSet_DoesNotThrow()
    {
        this._service.SetAutoStart(enabled: false);

        var ex = Record.Exception(() => this._service.SetAutoStart(enabled: false));

        Assert.Null(ex);
        Assert.False(this._service.IsAutoStartEnabled());
    }

    [Fact]
    public void SetAutoStart_GivenEnabledTrue_LogsInformation()
    {
        var logger = new RecordingLogger();
        var service = new WindowsAutoStartService(this._testKeyPath, logger);

        service.SetAutoStart(enabled: true);
        service.SetAutoStart(enabled: false);

        Assert.Contains(logger.Information, m => m.Contains("Auto-start enabled"));
        Assert.Contains(logger.Information, m => m.Contains("Auto-start disabled"));
    }

    [Fact]
    public void RestoreRegistration_PreservesOriginalCommandAndValueKind()
    {
        const string originalCommand = "%LOCALAPPDATA%\\FocusTimer\\older.exe --start";
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(this._testKeyPath, writable: true)!)
        {
            key.SetValue("FocusTimer", originalCommand, RegistryValueKind.ExpandString);
        }

        var previous = this._service.CaptureRegistration();
        this._service.SetAutoStart(enabled: false);
        this._service.RestoreRegistration(previous);

        using RegistryKey restoredKey = Registry.CurrentUser.OpenSubKey(this._testKeyPath)!;
        Assert.Equal(originalCommand, restoredKey.GetValue(
            "FocusTimer", null, RegistryValueOptions.DoNotExpandEnvironmentNames));
        Assert.Equal(RegistryValueKind.ExpandString, restoredKey.GetValueKind("FocusTimer"));
    }

    [Fact]
    public void MissingRunKey_ReportsRegistrationFailure()
    {
        Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

        Assert.Throws<IOException>(() => this._service.SetAutoStart(enabled: true));
        Assert.Throws<IOException>(() => this._service.IsAutoStartEnabled());
    }

    [Fact]
    public void RegistryDenial_ReportsRegistrationFailure()
    {
        var service = new DeniedAutoStartService();

        Assert.Throws<UnauthorizedAccessException>(() => service.SetAutoStart(enabled: true));
        Assert.Throws<UnauthorizedAccessException>(() => service.IsAutoStartEnabled());
    }

    [Fact]
    public void CaptureMissingRegistration_CanRestoreAbsenceAfterEnabling()
    {
        var previous = this._service.CaptureRegistration();
        Assert.False(previous.Enabled);
        Assert.Null(previous.Command);
        this._service.SetAutoStart(true);

        this._service.RestoreRegistration(previous);

        using var key = Registry.CurrentUser.OpenSubKey(this._testKeyPath)!;
        Assert.Null(key.GetValue("FocusTimer"));
    }

    [Fact]
    public void CaptureEmptyCommand_RestoresTheExactEmptyValueRatherThanDeletingIt()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(this._testKeyPath, writable: true)!)
            key.SetValue("FocusTimer", string.Empty, RegistryValueKind.String);
        var previous = this._service.CaptureRegistration();
        Assert.False(previous.Enabled);
        Assert.False(this._service.IsAutoStartEnabled());
        this._service.SetAutoStart(true);

        this._service.RestoreRegistration(previous);

        using var restored = Registry.CurrentUser.OpenSubKey(this._testKeyPath)!;
        Assert.Equal(string.Empty, restored.GetValue("FocusTimer"));
    }

    [Fact]
    public void CaptureNonStringRegistration_RejectsItWithoutOverwritingTheValue()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(this._testKeyPath, writable: true)!)
            key.SetValue("FocusTimer", 123, RegistryValueKind.DWord);

        Assert.Throws<IOException>(() => this._service.CaptureRegistration());
        Assert.False(this._service.IsAutoStartEnabled());
        using var restored = Registry.CurrentUser.OpenSubKey(this._testKeyPath)!;
        Assert.Equal(123, restored.GetValue("FocusTimer"));
    }

    [Theory]
    [InlineData("unknown kind")]
    [InlineData("DWord")]
    [InlineData("999")]
    public void RestoreUnsupportedValueKind_DoesNotOverwriteTheExistingCommand(string kind)
    {
        const string original = "\"older.exe\" --start";
        using (var key = Registry.CurrentUser.OpenSubKey(this._testKeyPath, writable: true)!)
            key.SetValue("FocusTimer", original, RegistryValueKind.String);

        Assert.Throws<IOException>(() => this._service.RestoreRegistration(new AutoStartRegistration(true, "new.exe", kind)));

        using var restored = Registry.CurrentUser.OpenSubKey(this._testKeyPath)!;
        Assert.Equal(original, restored.GetValue("FocusTimer"));
    }

    [Theory]
    [InlineData(null, "String")]
    [InlineData("ignored.exe", null)]
    public void RestoreIncompleteSnapshot_UsesEnabledState(string? command, string? kind)
    {
        this._service.SetAutoStart(true);

        this._service.RestoreRegistration(new AutoStartRegistration(false, command, kind));

        Assert.False(this._service.IsAutoStartEnabled());
    }

    [Fact]
    public void CaptureOrRestoreWithMissingRunKey_ReportsFailure()
    {
        Registry.CurrentUser.DeleteSubKeyTree(this._testKeyPath, throwOnMissingSubKey: false);

        Assert.Throws<IOException>(() => this._service.CaptureRegistration());
        Assert.Throws<IOException>(() => this._service.RestoreRegistration(new AutoStartRegistration(true, "test.exe", "String")));
    }

    private sealed class DeniedAutoStartService : WindowsAutoStartService
    {
        protected override RegistryKey? OpenRunKey(bool writable) =>
            throw new UnauthorizedAccessException("Registry access denied.");
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Information { get; } = new();

        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) => this.Information.Add(message);

        public void LogWarning(string message) { }
    }
}
