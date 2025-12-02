using FocusTimer.Core.Models;

namespace FocusTimer.Core.Interfaces;

/// <summary>
/// Service for writing time entries to log files.
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// Appends time entries to the appropriate log file.
    /// </summary>
    Task WriteEntriesAsync(IEnumerable<TimeEntry> entries, Settings settings);
}
