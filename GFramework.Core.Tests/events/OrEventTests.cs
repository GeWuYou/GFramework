using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

[TestFixture]
public class OrEventTests
{
    [Test]
    public void OrEvent_Should_Trigger_When_Any_Event_Fires()
    {
        var event1 = new Event<int>();
        var event2 = new Event<int>();
        var orEvent = new OrEvent();

        var triggered = false;
        orEvent.Register(() => { triggered = true; });

        orEvent.Or(event1).Or(event2);

        event1.Trigger(0);

        Assert.That(triggered, Is.True);
    }

    [Test]
    public void OrEvent_Should_Trigger_When_Second_Event_Fires()
    {
        var event1 = new Event<int>();
        var event2 = new Event<int>();
        var orEvent = new OrEvent();

        var triggered = false;
        orEvent.Register(() => { triggered = true; });

        orEvent.Or(event1).Or(event2);

        event2.Trigger(0);

        Assert.That(triggered, Is.True);
    }

    [Test]
    public void OrEvent_Should_Support_Multiple_Handlers()
    {
        var @event = new Event<int>();
        var orEvent = new OrEvent();

        var count1 = 0;
        var count2 = 0;

        orEvent.Register(() => { count1++; });
        orEvent.Register(() => { count2++; });

        orEvent.Or(@event);
        @event.Trigger(0);

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    [Test]
    public void OrEvent_UnRegister_Should_Remove_Handler()
    {
        var @event = new Event<int>();
        var orEvent = new OrEvent();

        var count = 0;
        var handler = () => { count++; };

        orEvent.Register(handler);
        orEvent.Or(@event);

        @event.Trigger(0);
        Assert.That(count, Is.EqualTo(1));

        orEvent.UnRegister(handler);
        @event.Trigger(0);
        Assert.That(count, Is.EqualTo(1));
    }
}