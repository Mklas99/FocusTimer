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
        
        // Stub: just log to console/debug for now
        Console.WriteLine($"[LogWriterStub] Would write {entries.Count()} entries to {settings.LogDirectory}");
        return Task.CompletedTask;
    }
}
