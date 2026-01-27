using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
///     测试OrEvent类的功能，验证其在多个事件中的逻辑或操作行为
/// </summary>
[TestFixture]
public class OrEventTests
{
    /// <summary>
    ///     测试当任意一个事件触发时，OrEvent应该被触发
    ///     验证基本的OR逻辑功能
    /// </summary>
    [Test]
    public void OrEvent_Should_Trigger_When_Any_Event_Fires()
    {
        var event1 = new Event<int>();
        var event2 = new Event<int>();
        var orEvent = new OrEvent();

        var triggered = false;
        orEvent.Register(() => { triggered = true; });

        // 将两个事件添加到OrEvent中
        orEvent.Or(event1).Or(event2);

        event1.Trigger(0);

        Assert.That(triggered, Is.True);
    }

    /// <summary>
    ///     测试当第二个事件触发时，OrEvent应该被触发
    ///     验证OR逻辑对所有注册事件都有效
    /// </summary>
    [Test]
    public void OrEvent_Should_Trigger_When_Second_Event_Fires()
    {
        var event1 = new Event<int>();
        var event2 = new Event<int>();
        var orEvent = new OrEvent();

        var triggered = false;
        orEvent.Register(() => { triggered = true; });

        // 将两个事件添加到OrEvent中
        orEvent.Or(event1).Or(event2);

        event2.Trigger(0);

        Assert.That(triggered, Is.True);
    }

    /// <summary>
    ///     测试OrEvent支持多个处理程序
    ///     验证单个OrEvent可以注册多个回调函数
    /// </summary>
    [Test]
    public void OrEvent_Should_Support_Multiple_Handlers()
    {
        var @event = new Event<int>();
        var orEvent = new OrEvent();

        var count1 = 0;
        var count2 = 0;

        orEvent.Register(() => { count1++; });
        orEvent.Register(() => { count2++; });

        // 将事件添加到OrEvent中
        orEvent.Or(@event);
        @event.Trigger(0);

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试UnRegister方法应该移除处理程序
    ///     验证注销功能能够正确移除已注册的回调函数
    /// </summary>
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

        // 注销处理程序
        orEvent.UnRegister(handler);
        @event.Trigger(0);
        Assert.That(count, Is.EqualTo(1));
    }
}