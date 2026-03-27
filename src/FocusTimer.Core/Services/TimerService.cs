namespace FocusTimer.Core.Services
{
    using System;
    using System.Timers;
    using FocusTimer.Core.Interfaces;
    using FocusTimer.Core.Models;

    /// <summary>
    /// Manages timer state and elapsed time tracking for focus sessions.
    /// </summary>
    public class TimerService : ITimerService, IDisposable
    {
        private readonly SessionTracker _sessionTracker;
        private readonly System.Timers.Timer _timer;

        // Lock for thread safety
        private readonly object _lock = new object();

        private TimerState _currentState = TimerState.Idle;
        private TimeSpan _elapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerService"/> class.
        /// </summary>
        /// <param name="sessionTracker">The session tracker to use for tracking timer sessions.</param>
        public TimerService(SessionTracker sessionTracker)
        {
            this._sessionTracker = sessionTracker;
            this._timer = new System.Timers.Timer(1000);
            this._timer.Elapsed += this.OnTimerElapsed;
            this._timer.AutoReset = true;
        }

        /// <inheritdoc/>
        public event EventHandler<TimerState>? StateChanged;

        /// <inheritdoc/>
        public event EventHandler<TimeSpan>? Tick;

        /// <inheritdoc/>
        public TimerState CurrentState
        {
            get => this._currentState;
            private set
            {
                if (this._currentState != value)
                {
                    this._currentState = value;
                    this.StateChanged?.Invoke(this, this._currentState);
                }
            }
        }

        /// <inheritdoc/>
        public TimeSpan Elapsed => this._elapsed;

        /// <inheritdoc/>
        public void Start(string? projectTag = null)
        {
            if (this._currentState == TimerState.Running)
            {
                return;
            }

            this._sessionTracker.StartAsync(projectTag).ConfigureAwait(false);

            this._timer.Start();
            this.CurrentState = TimerState.Running;
        }

        /// <inheritdoc/>
        public void Pause()
        {
            if (this._currentState != TimerState.Running)
            {
                return;
            }

            this._timer.Stop();
            this.CurrentState = TimerState.Paused;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            this._timer.Stop();
            this.CurrentState = TimerState.Idle;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            this.Stop();
            lock (this._lock)
            {
                this._elapsed = TimeSpan.Zero;
            }

            this.Tick?.Invoke(this, this._elapsed);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this._timer.Dispose();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            lock (this._lock)
            {
                this._elapsed = this._elapsed.Add(TimeSpan.FromSeconds(1));
            }

            this.Tick?.Invoke(this, this._elapsed);

            // Fire and forget tracking update
            _ = this._sessionTracker.OnTimerTickAsync();
        }
    }
}
