namespace FocusTimer.Core.Models
{
    /// <summary>
    /// Represents the state of the timer.
    /// </summary>
    public enum TimerState
    {
        /// <summary>
        /// Timer is idle and not running.
        /// </summary>
        Idle,

        /// <summary>
        /// Timer is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Timer is paused.
        /// </summary>
        Paused,
    }
}
