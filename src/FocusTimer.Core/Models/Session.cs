namespace FocusTimer.Core.Models;

/// <summary>
/// Represents a work session containing multiple time entries.
/// </summary>
public class Session
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<TimeEntry> TimeEntries { get; set; } = new();
    public string? ProjectTag { get; set; }
}
