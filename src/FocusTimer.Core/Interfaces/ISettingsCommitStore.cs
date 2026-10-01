namespace FocusTimer.Core.Interfaces
{
    using FocusTimer.Core.Models;

    /// <summary>
    /// Persists a Settings candidate with a recoverable previous state.
    /// </summary>
    public interface ISettingsCommitStore
    {
        /// <summary>Gets the previous registration value from an unfinished commit, if any.</summary>
        /// <returns>The previous value, or null when no recovery is pending.</returns>
        Task<AutoStartRegistration?> GetPendingRecoveryAsync();

        /// <summary>Records the previous state and replaces the settings file.</summary>
        /// <param name="settings">Candidate settings.</param>
        /// <param name="previousAutoStart">Previous registration state.</param>
        /// <returns>A task representing the operation.</returns>
        Task BeginCommitAsync(Settings settings, AutoStartRegistration previousAutoStart);

        /// <summary>Removes the pending recovery record after all commit stages succeed.</summary>
        /// <returns>A task representing the operation.</returns>
        Task CompleteCommitAsync();

        /// <summary>Restores the previous file and removes the pending recovery record.</summary>
        /// <returns>A task representing the operation.</returns>
        Task RestorePreviousAsync();
    }
}
