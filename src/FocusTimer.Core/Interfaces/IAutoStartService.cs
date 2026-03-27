namespace FocusTimer.Core.Interfaces
{
    /// <summary>
    /// Service for managing auto-start on login.
    /// </summary>
    public interface IAutoStartService
    {
        /// <summary>
        /// Enables or disables auto-start on login.
        /// </summary>
        /// <param name="enabled">True to enable auto-start, false to disable.</param>
        void SetAutoStart(bool enabled);

        /// <summary>
        /// Checks if auto-start is currently enabled.
        /// </summary>
        /// <returns>True if auto-start is enabled, false otherwise.</returns>
        bool IsAutoStartEnabled();
    }
}
