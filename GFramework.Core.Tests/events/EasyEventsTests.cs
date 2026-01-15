using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

[TestFixture]
public class EasyEventsTests
{
    [SetUp]
    public void SetUp()
    {
        _easyEvents = new EasyEvents();
    }

    private EasyEvents _easyEvents = null!;

    [Test]
    public void Get_EventT_Should_Trigger_With_Parameter()
    {
        var receivedValue = 0;
        var @event = EasyEvents.GetOrAdd<Event<int>>();

        @event.Register(value => { receivedValue = value; });
        @event.Trigger(42);

        Assert.That(receivedValue, Is.EqualTo(42));
    }

    [Test]
    public void Get_EventTTK_Should_Trigger_With_Two_Parameters()
    {
        var receivedInt = 0;
        var receivedString = string.Empty;
        var @event = EasyEvents.GetOrAdd<Event<int, string>>();

        @event.Register((i, s) =>
        {
            receivedInt = i;
            receivedString = s;
        });
        @event.Trigger(100, "hello");

        Assert.That(receivedInt, Is.EqualTo(100));
        Assert.That(receivedString, Is.EqualTo("hello"));
    }
}