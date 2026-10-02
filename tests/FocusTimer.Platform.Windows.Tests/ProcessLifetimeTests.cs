namespace FocusTimer.Platform.Windows.Tests;

using System.Diagnostics;

public class ProcessLifetimeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void TryOpen_InvalidProcessId_ReturnsNull(int pid)
    {
        using var lifetime = ProcessLifetime.TryOpen(pid);

        Assert.Null(lifetime);
    }

    [Fact]
    public void CurrentProcess_LifetimeRemainsAliveAndHandleCanBeDisposedRepeatedly()
    {
        var lifetime = ProcessLifetime.TryOpen(Environment.ProcessId);
        Assert.NotNull(lifetime);
        try
        {
            Assert.True(lifetime.IsAlive);
            Assert.True(lifetime.IsAlive);
        }
        finally
        {
            lifetime.Dispose();
            lifetime.Dispose();
        }
    }

    [Fact]
    public async Task ProcessExit_IsDetectedThroughTheRetainedHandle()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
            Arguments = "/d /q",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        Assert.NotNull(process);
        try
        {
            using var lifetime = ProcessLifetime.TryOpen(process.Id);
            Assert.NotNull(lifetime);
            Assert.True(lifetime.IsAlive);

            await process.StandardInput.WriteLineAsync("exit");
            await process.StandardInput.FlushAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(lifetime.IsAlive);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
    }
}
