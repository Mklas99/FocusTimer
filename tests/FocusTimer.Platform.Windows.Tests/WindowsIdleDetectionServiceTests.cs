namespace FocusTimer.Platform.Windows.Tests;

public class WindowsIdleDetectionServiceTests
{
    [Theory]
    [InlineData(299999, false)]
    [InlineData(300000, true)]
    [InlineData(300001, true)]
    public void ThresholdBoundary_OnlyPausesAtOrAboveFiveMinutes(int milliseconds, bool expectedIdle)
    {
        using var service = new WindowsIdleDetectionService(startPolling: false);
        int idleEvents = 0;
        int returnEvents = 0;
        service.UserBecameIdle += (_, _) => idleEvents++;
        service.UserReturned += (_, _) => returnEvents++;

        service.ProcessIdleDuration(TimeSpan.FromMilliseconds(milliseconds));
        service.ProcessIdleDuration(TimeSpan.FromMilliseconds(milliseconds));

        Assert.Equal(expectedIdle ? 1 : 0, idleEvents);
        Assert.Equal(0, returnEvents);
    }

    [Fact]
    public void RepeatedPolls_EmitOnlyOneEventForEachIdleAndActiveTransition()
    {
        using var service = new WindowsIdleDetectionService(startPolling: false);
        var transitions = new List<string>();
        service.UserBecameIdle += (sender, _) => { Assert.Same(service, sender); transitions.Add("idle"); };
        service.UserReturned += (sender, _) => { Assert.Same(service, sender); transitions.Add("active"); };

        service.ProcessIdleDuration(TimeSpan.Zero);
        service.ProcessIdleDuration(TimeSpan.FromMinutes(5));
        service.ProcessIdleDuration(TimeSpan.FromMinutes(8));
        service.ProcessIdleDuration(TimeSpan.Zero);
        service.ProcessIdleDuration(TimeSpan.FromSeconds(1));
        service.ProcessIdleDuration(TimeSpan.FromMinutes(5));
        service.ProcessIdleDuration(TimeSpan.FromMinutes(5));
        service.ProcessIdleDuration(TimeSpan.FromMinutes(4));

        Assert.Equal(new[] { "idle", "active", "idle", "active" }, transitions);
    }

    [Fact]
    public void PollingWithoutSubscribers_StillTracksTheIdleState()
    {
        using var service = new WindowsIdleDetectionService(startPolling: false);
        service.ProcessIdleDuration(TimeSpan.FromMinutes(5));
        service.ProcessIdleDuration(TimeSpan.Zero);
        int idleEvents = 0;
        service.UserBecameIdle += (_, _) => idleEvents++;

        service.ProcessIdleDuration(TimeSpan.FromMinutes(5));

        Assert.Equal(1, idleEvents);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dispose_IgnoresPendingPollsWithoutPublishingAnotherTransition(bool initiallyIdle)
    {
        using var service = new WindowsIdleDetectionService(startPolling: false);
        if (initiallyIdle)
            service.ProcessIdleDuration(TimeSpan.FromMinutes(5));
        service.Dispose();
        int events = 0;
        service.UserBecameIdle += (_, _) => events++;
        service.UserReturned += (_, _) => events++;

        service.ProcessIdleDuration(initiallyIdle ? TimeSpan.Zero : TimeSpan.FromMinutes(5));

        Assert.Equal(0, events);
    }

    [Fact]
    public void DisposalFromAnIdleSubscriber_CompletesAndSuppressesFurtherEvents()
    {
        using var service = new WindowsIdleDetectionService(startPolling: false);
        int idleEvents = 0;
        int returnEvents = 0;
        service.UserBecameIdle += (_, _) => { idleEvents++; service.Dispose(); };
        service.UserReturned += (_, _) => returnEvents++;

        service.ProcessIdleDuration(TimeSpan.FromMinutes(5));
        service.ProcessIdleDuration(TimeSpan.Zero);

        Assert.Equal(1, idleEvents);
        Assert.Equal(0, returnEvents);
    }
}
