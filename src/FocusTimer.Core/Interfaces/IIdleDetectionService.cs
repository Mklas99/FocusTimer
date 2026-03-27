namespace FocusTimer.Core.Interfaces
{
    /// <summary>
    /// Service for detecting user idle state.
    /// </summary>
    public interface IIdleDetectionService
    {
        /// <summary>
        /// Occurs when the user becomes idle.
        /// </summary>
        event EventHandler<UserIdleEventArgs> UserBecameIdle;

        /// <summary>
        /// Occurs when the user returns from idle state.
        /// </summary>
        event EventHandler<UserIdleEventArgs> UserReturned;
    }

    /// <summary>
    /// Event arguments for user idle state changes.
    /// </summary>
    public class UserIdleEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserIdleEventArgs"/> class.
        /// </summary>
        /// <param name="timestamp">The timestamp of the idle state change.</param>
        public UserIdleEventArgs(DateTime timestamp)
        {
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// Gets or sets the timestamp of the idle state change.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
