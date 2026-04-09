namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Services;

public class EventBusTests
{
    [Fact]
    public void Publish_GivenSubscriber_DeliversMessage()
    {
        var bus = new EventBus();
        var calls = 0;

        using var sub = bus.Subscribe<string>(_ => calls++);

        bus.Publish("hello");

        Assert.Equal(1, calls);
    }

    [Fact]
    public void DisposeSubscription_GivenPublishedMessage_DoesNotDeliver()
    {
        var bus = new EventBus();
        var calls = 0;

        var sub = bus.Subscribe<int>(_ => calls++);
        sub.Dispose();

        bus.Publish(123);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Publish_GivenMultipleSubscribers_CallsAllHandlers()
    {
        var bus = new EventBus();
        var calls = 0;

        using var sub1 = bus.Subscribe<string>(_ => calls++);
        using var sub2 = bus.Subscribe<string>(_ => calls++);

        bus.Publish("value");

        Assert.Equal(2, calls);
    }

    [Fact]
    public void Publish_GivenOneThrowingSubscriber_ContinuesToOtherHandlers()
    {
        var bus = new EventBus();
        var calls = 0;

        using var sub1 = bus.Subscribe<string>(_ => throw new InvalidOperationException("boom"));
        using var sub2 = bus.Subscribe<string>(_ => calls++);

        bus.Publish("value");

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Publish_GivenDifferentTypes_DoesNotCrossInvokeHandlers()
    {
        var bus = new EventBus();
        var stringCalls = 0;
        var intCalls = 0;

        using var sub1 = bus.Subscribe<string>(_ => stringCalls++);
        using var sub2 = bus.Subscribe<int>(_ => intCalls++);

        bus.Publish("hello");

        Assert.Equal(1, stringCalls);
        Assert.Equal(0, intCalls);
    }
}
