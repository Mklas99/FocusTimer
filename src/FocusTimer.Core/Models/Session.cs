namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Represents a work session containing multiple time entries.
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Gets or sets the start time of the session.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of the session.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the collection of time entries in the session.
        /// </summary>
        public List<TimeEntry> TimeEntries { get; set; } = new();

        /// <summary>
        /// Gets or sets the project tag associated with the session.
        /// </summary>
        public string? ProjectTag { get; set; }
    }
}
