using System;
using System.Timers;
using System.Threading.Tasks;
using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Services
{
    public class TimerService : ITimerService, IDisposable
    {
        private readonly SessionTracker _sessionTracker;
        private readonly System.Timers.Timer _timer;
        private TimerState _currentState = TimerState.Idle;
        private TimeSpan _elapsed;
        
        // Lock for thread safety
        private readonly object _lock = new object();

        public TimerState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    StateChanged?.Invoke(this, _currentState);
                }
            }
        }

        public TimeSpan Elapsed => _elapsed;

        public event EventHandler<TimerState>? StateChanged;
        public event EventHandler<TimeSpan>? Tick;

        public TimerService(SessionTracker sessionTracker)
        {
            _sessionTracker = sessionTracker;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
            }
            
            Tick?.Invoke(this, _elapsed);
            
            // Fire and forget tracking update
            _ = _sessionTracker.OnTimerTickAsync();
        }

        public void Start(string? projectTag = null)
        {
            if (_currentState == TimerState.Running) return;

            if (_currentState == TimerState.Idle)
            {
                // Starting fresh
                _sessionTracker.StartAsync(projectTag).ConfigureAwait(false);
            }
            // If Paused, we just resume timer, session tracking continues (it wasn't stopped)

            _timer.Start();
            CurrentState = TimerState.Running;
        }

        public void Pause()
        {
            if (_currentState != TimerState.Running) return;

            _timer.Stop();
            CurrentState = TimerState.Paused;
        }

        public void Stop()
        {
            _timer.Stop();
            CurrentState = TimerState.Idle;
        }

        public void Reset()
        {
            Stop();
            lock (_lock)
            {
                _elapsed = TimeSpan.Zero;
            }
            Tick?.Invoke(this, _elapsed);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
