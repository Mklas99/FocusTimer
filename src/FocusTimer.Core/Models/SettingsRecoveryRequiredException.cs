namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Indicates that an unfinished settings commit must be recovered before settings can be loaded.
    /// </summary>
    public sealed class SettingsRecoveryRequiredException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="SettingsRecoveryRequiredException"/> class.</summary>
        public SettingsRecoveryRequiredException()
            : base("An unfinished settings commit requires recovery.")
        {
        }
    }
}
