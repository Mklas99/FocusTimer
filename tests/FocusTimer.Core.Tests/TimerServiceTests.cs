namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using FocusTimer.Core.Services;

public class TimerServiceTests
{
    [Fact]
    public void Start_GivenIdle_SetsStateToRunning()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));

        service.Start("P1");

        Assert.Equal(TimerState.Running, service.CurrentState);
    }

    [Fact]
    public void Start_GivenAlreadyRunning_DoesNotRaiseExtraStateChange()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));
        var changed = 0;
        service.StateChanged += (_, _) => changed++;

        service.Start("P1");
        service.Start("P1");

        Assert.Equal(1, changed);
        Assert.Equal(TimerState.Running, service.CurrentState);
    }

    [Fact]
    public void Pause_GivenRunning_SetsStateToPaused()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));
        service.Start();

        service.Pause();

        Assert.Equal(TimerState.Paused, service.CurrentState);
    }

    [Fact]
    public void Pause_GivenIdle_DoesNotChangeState()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));

        service.Pause();

        Assert.Equal(TimerState.Idle, service.CurrentState);
    }

    [Fact]
    public void Stop_GivenAnyState_SetsIdle()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));
        service.Start();

        service.Stop();

        Assert.Equal(TimerState.Idle, service.CurrentState);
    }

    [Fact]
    public void Reset_GivenElapsedTime_ResetsToZeroAndRaisesTick()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));
        var ticks = 0;
        service.Tick += (_, elapsed) =>
        {
            if (elapsed == TimeSpan.Zero)
            {
                ticks++;
            }
        };

        service.Start();
        Thread.Sleep(1050);
        service.Reset();

        Assert.Equal(TimeSpan.Zero, service.Elapsed);
        Assert.Equal(1, ticks);
        Assert.Equal(TimerState.Idle, service.CurrentState);
    }

    [Fact]
    public void Tick_GivenRunning_AdvancesElapsed()
    {
        using var service = new TimerService(new SessionTracker(new StaticWindowService(), NullLogger.Instance));
        using var signal = new ManualResetEventSlim(false);
        service.Tick += (_, elapsed) =>
        {
            if (elapsed >= TimeSpan.FromSeconds(1))
            {
                signal.Set();
            }
        };

        service.Start();

        Assert.True(signal.Wait(TimeSpan.FromSeconds(3)));
        Assert.True(service.Elapsed >= TimeSpan.FromSeconds(1));
    }

    private sealed class StaticWindowService : IActiveWindowService
    {
        public Task<ActiveWindowInfo?> GetForegroundWindowAsync()
            => Task.FromResult<ActiveWindowInfo?>(new ActiveWindowInfo
            {
                ProcessName = "test",
                WindowTitle = "window",
            });
    }
}