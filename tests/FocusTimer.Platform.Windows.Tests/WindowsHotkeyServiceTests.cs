namespace FocusTimer.Platform.Windows.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

public class WindowsHotkeyServiceTests
{
    [Fact]
    public void RegisterHotkey_GivenNoWindowHandle_LogsWarningAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);
        var raised = false;
        service.HotkeyPressed += (_, _) => raised = true;

        var ex = Record.Exception(() => service.RegisterHotkey("Ctrl+Alt+T", () => { }));

        Assert.Null(ex);
        Assert.False(raised);
        Assert.Contains(logger.Warnings, w => w.Contains("Window handle not set"));
    }

    [Fact]
    public void Register_GivenNoWindowHandle_LogsWarningAndDoesNotThrow()
    {
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);

        var ex = Record.Exception(() =>
            service.Register(new HotkeyDefinition(HotkeyModifiers.Control, 'K')));

        Assert.Null(ex);
        Assert.Contains(logger.Warnings, w => w.Contains("Window handle not set"));
    }

    [Fact]
    public void SetWindowHandle_GivenZero_IsIgnoredAndLeavesServiceUsable()
    {
        var logger = new RecordingLogger();
        using var service = new WindowsHotkeyService(logger);

        var ex = Record.Exception(() =>
        {
            service.SetWindowHandle(IntPtr.Zero);
            service.RegisterHotkey("Ctrl+Alt+T", () => { });
        });

        Assert.Null(ex);

        // Falling back to "no handle" behavior confirms SetWindowHandle(Zero) never took effect.
        Assert.Contains(logger.Warnings, w => w.Contains("Window handle not set"));
    }

    [Fact]
    public void UnregisterAll_GivenNoWindowHandle_IsNoOp()
    {
        using var service = new WindowsHotkeyService();

        var ex = Record.Exception(service.UnregisterAll);

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_GivenNoWindowHandleEverSet_DoesNotThrowEvenWhenCalledTwice()
    {
        var service = new WindowsHotkeyService();

        var ex = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void ProcessHotkeyMessage_GivenUnknownId_DoesNotRaiseEvent()
    {
        using var service = new WindowsHotkeyService();
        var raised = false;
        service.HotkeyPressed += (_, _) => raised = true;

        service.ProcessHotkeyMessage(int.MaxValue);

        Assert.False(raised);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<string> Warnings { get; } = new();

        public void LogCritical(string message, Exception? ex = null) { }

        public void LogDebug(string message) { }

        public void LogError(string message, Exception? ex = null) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) => this.Warnings.Add(message);
    }
}
