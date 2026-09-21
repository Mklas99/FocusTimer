namespace FocusTimer.Platform.Windows.Tests;

using System.ComponentModel;
using FocusTimer.Core.Interfaces;

public class WindowsActiveWindowServiceTests
{
    private static readonly IntPtr Handle = new(1234);

    private static WindowsActiveWindowService Create(
        Func<IntPtr>? foreground = null,
        Func<IntPtr, string>? title = null,
        Func<IntPtr, uint>? pid = null,
        Func<int, string>? name = null,
        IAppLogger? logger = null) =>
        new(
            logger,
            foreground ?? (() => Handle),
            title ?? (_ => "Editor"),
            pid ?? (_ => 42),
            name ?? (_ => "code"));

    [Fact]
    public async Task GetForegroundWindowAsync_GivenHealthyWindow_ReturnsProcessAndTitle()
    {
        var info = await Create().GetForegroundWindowAsync();

        Assert.NotNull(info);
        Assert.Equal("code", info!.ProcessName);
        Assert.Equal("Editor", info.WindowTitle);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenNoForegroundWindow_ReturnsNull()
    {
        var info = await Create(foreground: () => IntPtr.Zero).GetForegroundWindowAsync();

        Assert.Null(info);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenProcessIdZero_KeepsTitleWithUnknownProcess()
    {
        var info = await Create(pid: _ => 0).GetForegroundWindowAsync();

        Assert.NotNull(info);
        Assert.Equal("Unknown", info!.ProcessName);
        Assert.Equal("Editor", info.WindowTitle);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenProcessIdZeroAndBlankTitle_ReturnsNull()
    {
        var info = await Create(title: _ => "  ", pid: _ => 0).GetForegroundWindowAsync();

        Assert.Null(info);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenProcessGone_ReturnsUnknownProcessWithTitle()
    {
        var info = await Create(name: _ => throw new ArgumentException("gone")).GetForegroundWindowAsync();

        Assert.NotNull(info);
        Assert.Equal("Unknown", info!.ProcessName);
        Assert.Equal("Editor", info.WindowTitle);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenProcessGoneAndBlankTitle_ReturnsNull()
    {
        var info = await Create(title: _ => string.Empty, name: _ => throw new ArgumentException("gone"))
            .GetForegroundWindowAsync();

        Assert.Null(info);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenAccessDenied_UsesProcessIdFallbackName()
    {
        var info = await Create(name: _ => throw new Win32Exception(5)).GetForegroundWindowAsync();

        Assert.Equal("Process_42", info!.ProcessName);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenProcessExitedDuringLookup_UsesProcessIdFallbackName()
    {
        var info = await Create(name: _ => throw new InvalidOperationException("exited")).GetForegroundWindowAsync();

        Assert.Equal("Process_42", info!.ProcessName);
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenUnexpectedFailure_ReturnsNullAndLogsWarning()
    {
        var logger = new RecordingLogger();

        var info = await Create(title: _ => throw new InvalidOperationException("boom"), logger: logger)
            .GetForegroundWindowAsync();

        Assert.Null(info);
        Assert.Contains(logger.Warnings, w => w.Contains("boom"));
    }

    [Fact]
    public async Task GetForegroundWindowAsync_GivenUnexpectedFailureAndNoLogger_ReturnsNull()
    {
        var info = await Create(foreground: () => throw new InvalidOperationException("boom")).GetForegroundWindowAsync();

        Assert.Null(info);
    }

    [Fact]
    public async Task DefaultConstructors_QueryRealSystemWithoutThrowing()
    {
        var withLogger = new WindowsActiveWindowService(new RecordingLogger());

        var ex = await Record.ExceptionAsync(async () =>
        {
            await new WindowsActiveWindowService().GetForegroundWindowAsync();
            await withLogger.GetForegroundWindowAsync();
        });

        Assert.Null(ex);
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
