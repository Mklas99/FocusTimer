namespace FocusTimer.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Event published when time entries have been logged.
    /// </summary>
    public class EntriesLoggedEvent
    {
        /// <summary>
        /// Gets or sets the logged entries.
        /// </summary>
        public IEnumerable<TimeEntry> Entries { get; set; } = new List<TimeEntry>();
    }
}
