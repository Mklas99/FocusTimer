namespace FocusTimer.Core.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Repository interface for managing session data and time entries.
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Saves a collection of time entries to the session repository.
        /// </summary>
        /// <param name="entries">The collection of time entries to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveSessionAsync(IEnumerable<TimeEntry> entries);

        /// <summary>
        /// Retrieves all sessions for a specific date.
        /// </summary>
        /// <param name="date">The date for which to retrieve sessions.</param>
        /// <returns>A task that returns a collection of time entries for the specified date.</returns>
        Task<IEnumerable<TimeEntry>> GetSessionsByDateAsync(DateTime date);
    }
}
