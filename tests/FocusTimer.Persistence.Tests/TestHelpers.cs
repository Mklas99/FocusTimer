namespace FocusTimer.Persistence.Tests;

using FocusTimer.Core.Interfaces;

internal static class TestHelpers
{
    internal static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "FocusTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

internal sealed class NullLogger : IAppLogger
{
    internal static readonly NullLogger Instance = new();

    public void LogCritical(string message, Exception? ex = null) { }

    public void LogDebug(string message) { }

    public void LogError(string message, Exception? ex = null) { }

    public void LogInformation(string message) { }

    public void LogWarning(string message) { }
}