namespace FocusTimer.Platform.Windows.Tests;

using FocusTimer.Core.Interfaces;

public class WindowsAutoStartServiceTests : IDisposable
{
    private readonly WindowsAutoStartService _service = new();

    public void Dispose()
    {
        // Leave the real HKCU Run key clean regardless of test outcome.
        this._service.SetAutoStart(enabled: false);
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
        var service = new WindowsAutoStartService(logger);

        service.SetAutoStart(enabled: true);
        service.SetAutoStart(enabled: false);

        Assert.Contains(logger.Information, m => m.Contains("Auto-start enabled"));
        Assert.Contains(logger.Information, m => m.Contains("Auto-start disabled"));
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
