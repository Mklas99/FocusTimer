namespace FocusTimer.Core.Interfaces
{
    using System;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Defines the interface for a timer service.
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// Occurs when the timer state changes.
        /// </summary>
        event EventHandler<TimerState>? StateChanged;

        /// <summary>
        /// Occurs on each timer tick.
        /// </summary>
        event EventHandler<TimeSpan> Tick;

        /// <summary>
        /// Gets the current state of the timer.
        /// </summary>
        TimerState CurrentState { get; }

        /// <summary>
        /// Gets the elapsed time since the timer was started.
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Starts the timer with an optional project tag.
        /// </summary>
        /// <param name="projectTag">The optional project tag to associate with the timer.</param>
        void Start(string? projectTag = null);

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        void Pause();

        /// <summary>
        /// Stops the timer.
        /// </summary>
        void Stop();

        /// <summary>
        /// Resets the timer.
        /// </summary>
        void Reset();
    }
}
