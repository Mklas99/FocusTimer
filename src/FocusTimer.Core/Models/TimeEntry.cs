namespace FocusTimer.Core.Models;

/// <summary>
/// Represents a single time tracking entry for an application/window.
/// </summary>
public class TimeEntry
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Gets the duration if the entry is closed (EndTime is set), otherwise null.
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    
    /// <summary>
    /// Gets the current duration of this entry, using the provided time (or current time) as the end point.
    /// Useful for computing live duration of open entries.
    /// </summary>
    public TimeSpan GetCurrentDuration(DateTime? now = null)
        => (EndTime ?? (now ?? DateTime.Now)) - StartTime;
    
    public string AppName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string? ProjectTag { get; set; }
}
