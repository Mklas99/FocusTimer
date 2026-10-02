namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Adds, edits, and deletes worklog entries for the user.</summary>
public interface IWorklogEditingService
{
    /// <summary>Adds a manual entry.</summary>
    /// <param name="request">The entry to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result; failures carry a message for the user.</returns>
    Task<WorklogEditResult> AddManualAsync(ManualEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the window title, project, or duration of an entry the user loaded.</summary>
    /// <param name="entry">The entry as loaded; its revision is sent with the change.</param>
    /// <param name="edit">The new values.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result; failures carry a message for the user.</returns>
    Task<WorklogEditResult> UpdateAsync(TimeEntry entry, WorklogEntryEdit edit, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entry the user loaded.</summary>
    /// <param name="entry">The entry as loaded; its revision is sent with the delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result; failures carry a message for the user.</returns>
    Task<WorklogEditResult> DeleteAsync(TimeEntry entry, CancellationToken cancellationToken = default);
}
