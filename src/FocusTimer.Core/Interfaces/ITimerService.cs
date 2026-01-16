using System;
using FocusTimer.Core.Models;

namespace FocusTimer.Core.Interfaces
{
    public interface ITimerService
    {
        TimerState CurrentState { get; }
        TimeSpan Elapsed { get; }
        event EventHandler<TimerState> StateChanged;
        event EventHandler<TimeSpan> Tick;

        void Start(string? projectTag = null);
        void Pause();
        void Stop();
        void Reset();
    }
}
