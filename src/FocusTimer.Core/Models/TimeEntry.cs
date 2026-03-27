namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Represents a single time tracking entry for an application/window.
    /// </summary>
    public class TimeEntry
    {
        /// <summary>
        /// Gets or sets the start time of this time entry.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of this time entry. Null if the entry is still open.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets the duration if the entry is closed (EndTime is set), otherwise null.
        /// </summary>
        public TimeSpan? Duration => this.EndTime.HasValue ? this.EndTime.Value - this.StartTime : null;

        /// <summary>
        /// Gets or sets the application name associated with this time entry.
        /// </summary>
        public string AppName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the window title associated with this time entry.
        /// </summary>
        public string WindowTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project tag associated with this time entry.
        /// </summary>
        public string? ProjectTag { get; set; }

        /// <summary>
        /// Gets the current duration of this entry, using the provided time (or current time) as the end point.
        /// Useful for computing live duration of open entries.
        /// </summary>
        /// <param name="now">Optional time to use as the end point. If null, uses DateTime.Now.</param>
        /// <returns>The current duration as a TimeSpan.</returns>
        public TimeSpan GetCurrentDuration(DateTime? now = null)
            => (this.EndTime ?? (now ?? DateTime.Now)) - this.StartTime;
    }
}
