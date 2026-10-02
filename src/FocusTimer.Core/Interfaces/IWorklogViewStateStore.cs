namespace FocusTimer.Core.Interfaces;

using FocusTimer.Core.Models;

/// <summary>Remembers the Worklog window's view preferences. A failure never affects tracking or the worklog.</summary>
public interface IWorklogViewStateStore
{
    /// <summary>Loads the remembered view state.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The state; an empty state when nothing was saved or the file cannot be read.</returns>
    Task<WorklogViewState> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Remembers the view state.</summary>
    /// <param name="state">The state to keep.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the state is stored; failures are logged, not thrown.</returns>
    Task SaveAsync(WorklogViewState state, CancellationToken cancellationToken = default);
}
