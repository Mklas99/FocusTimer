namespace FocusTimer.Core.Interfaces
{
    using FocusTimer.Core.Models;

    /// <summary>
    /// Service for managing auto-start on login.
    /// </summary>
    public interface IAutoStartService
    {
        /// <summary>
        /// Enables or disables auto-start on login, throwing if registration cannot be changed.
        /// </summary>
        /// <param name="enabled">True to enable auto-start, false to disable.</param>
        void SetAutoStart(bool enabled);

        /// <summary>
        /// Checks if auto-start is currently enabled, throwing if registration cannot be read.
        /// </summary>
        /// <returns>True if auto-start is enabled, false otherwise.</returns>
        bool IsAutoStartEnabled();

        /// <summary>Captures the registration for commit rollback.</summary>
        /// <returns>The current registration.</returns>
        AutoStartRegistration CaptureRegistration() => new(this.IsAutoStartEnabled());

        /// <summary>Restores a registration captured before a commit.</summary>
        /// <param name="registration">The prior registration.</param>
        void RestoreRegistration(AutoStartRegistration registration) => this.SetAutoStart(registration.Enabled);
    }
}
