using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

[TestFixture]
public class EventBusTests
{
    [SetUp]
    public void SetUp()
    {
        _eventBus = new EventBus();
    }

    private EventBus _eventBus = null!;

    [Test]
    public void Register_Should_Add_Handler()
    {
        var called = false;
        _eventBus.Register<EventBusTestsEvent>(@event => { called = true; });

        _eventBus.Send<EventBusTestsEvent>();

        Assert.That(called, Is.True);
    }

    [Test]
    public void UnRegister_Should_Remove_Handler()
    {
        var count = 0;

        Action<EventBusTestsEvent> handler = @event => { count++; };
        _eventBus.Register(handler);
        _eventBus.Send<EventBusTestsEvent>();
        Assert.That(count, Is.EqualTo(1));

        _eventBus.UnRegister(handler);
        _eventBus.Send<EventBusTestsEvent>();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void SendEvent_Should_Invoke_All_Handlers()
    {
        var count1 = 0;
        var count2 = 0;

        _eventBus.Register<EventBusTestsEvent>(@event => { count1++; });
        _eventBus.Register<EventBusTestsEvent>(@event => { count2++; });

        _eventBus.Send<EventBusTestsEvent>();

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }
}

public class EventBusTestsEvent
{
}