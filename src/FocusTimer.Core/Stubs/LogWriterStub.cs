using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Stubs;

/// <summary>
/// Stub implementation of ILogWriter for initial testing.
/// </summary>
public class LogWriterStub : ILogWriter
{
    public Task WriteEntriesAsync(IEnumerable<TimeEntry> entries, Settings settings)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(settings);
        Console.WriteLine($"[LogWriterStub] Would write {entries.Count()} entries to {settings.LogDirectory}");
        return Task.CompletedTask;
    }

    public void LogDebug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }

    public void LogInformation(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void LogWarning(string message)
    {
        Console.WriteLine($"[WARN] {message}");
    }

    public void LogError(string message, Exception? ex = null)
    {
        Console.WriteLine($"[ERROR] {message} " + (ex != null ? ex.ToString() : ""));
    }

    public void LogCritical(string message, Exception? ex = null)
    {
        Console.WriteLine($"[CRITICAL] {message} " + (ex != null ? ex.ToString() : ""));
    }
}
