using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
/// ArchitectureEvents类的单元测试
/// 测试内容包括：
/// - ArchitectureLifecycleReadyEvent事件触发
/// - ArchitectureDestroyingEvent事件触发
/// - ArchitectureDestroyedEvent事件触发
/// - ArchitectureFailedInitializationEvent事件触发
/// - 事件的参数传递
/// - 事件的订阅和取消订阅
/// - 事件顺序验证（LifecycleReady -> Destroying -> Destroyed）
/// </summary>
[TestFixture]
public class ArchitectureEventsTests
{
    [SetUp]
    public void SetUp()
    {
        _eventBus = new EventBus();
    }

    private EventBus? _eventBus;

    /// <summary>
    /// 测试ArchitectureLifecycleReadyEvent事件可以创建并发送
    /// </summary>
    [Test]
    public void ArchitectureLifecycleReadyEvent_Should_Be_Created_And_Sent()
    {
        bool eventReceived = false;

        _eventBus!.Register<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => eventReceived = true);
        _eventBus.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    /// 测试ArchitectureDestroyingEvent事件可以创建并发送
    /// </summary>
    [Test]
    public void ArchitectureDestroyingEvent_Should_Be_Created_And_Sent()
    {
        bool eventReceived = false;

        _eventBus!.Register<ArchitectureEvents.ArchitectureDestroyingEvent>(_ => eventReceived = true);
        _eventBus.Send<ArchitectureEvents.ArchitectureDestroyingEvent>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    /// 测试ArchitectureDestroyedEvent事件可以创建并发送
    /// </summary>
    [Test]
    public void ArchitectureDestroyedEvent_Should_Be_Created_And_Sent()
    {
        bool eventReceived = false;

        _eventBus!.Register<ArchitectureEvents.ArchitectureDestroyedEvent>(_ => eventReceived = true);
        _eventBus.Send<ArchitectureEvents.ArchitectureDestroyedEvent>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    /// 测试ArchitectureFailedInitializationEvent事件可以创建并发送
    /// </summary>
    [Test]
    public void ArchitectureFailedInitializationEvent_Should_Be_Created_And_Sent()
    {
        bool eventReceived = false;

        _eventBus!.Register<ArchitectureEvents.ArchitectureFailedInitializationEvent>(_ => eventReceived = true);
        _eventBus.Send<ArchitectureEvents.ArchitectureFailedInitializationEvent>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    /// 测试事件按正确顺序发送
    /// </summary>
    [Test]
    public void Events_Should_Be_Sent_In_Correct_Order()
    {
        var events = new List<string>();

        _eventBus!.Register<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => events.Add("Ready"));
        _eventBus!.Register<ArchitectureEvents.ArchitectureDestroyingEvent>(_ => events.Add("Destroying"));
        _eventBus!.Register<ArchitectureEvents.ArchitectureDestroyedEvent>(_ => events.Add("Destroyed"));

        _eventBus.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>();
        _eventBus.Send<ArchitectureEvents.ArchitectureDestroyingEvent>();
        _eventBus.Send<ArchitectureEvents.ArchitectureDestroyedEvent>();

        Assert.That(events.Count, Is.EqualTo(3));
        Assert.That(events[0], Is.EqualTo("Ready"));
        Assert.That(events[1], Is.EqualTo("Destroying"));
        Assert.That(events[2], Is.EqualTo("Destroyed"));
    }

    /// <summary>
    /// 测试多个订阅者都能接收到事件
    /// </summary>
    [Test]
    public void Multiple_Subscribers_Should_All_Receive_Events()
    {
        var count1 = 0;
        var count2 = 0;

        _eventBus!.Register<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => count1++);
        _eventBus!.Register<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => count2++);

        _eventBus.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>();

        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }

    /// <summary>
    /// 测试事件取消订阅后不再接收未来事件
    /// </summary>
    [Test]
    public void Event_UnRegister_Should_Prevent_Future_Events()
    {
        var count = 0;
        var unregister = _eventBus!.Register<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => count++);

        _eventBus.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>();
        Assert.That(count, Is.EqualTo(1));

        unregister.UnRegister();
        _eventBus.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>();

        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    /// 测试不同类型事件互不干扰
    /// </summary>
    [Test]
    public void Different_Events_Should_Not_Interfere()
    {
        bool readyReceived = false;
        bool destroyingReceived = false;

        _eventBus!.Register<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(_ => readyReceived = true);
        _eventBus!.Register<ArchitectureEvents.ArchitectureDestroyingEvent>(_ => destroyingReceived = true);

        _eventBus.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>();

        Assert.That(readyReceived, Is.True);
        Assert.That(destroyingReceived, Is.False);

        _eventBus.Send<ArchitectureEvents.ArchitectureDestroyingEvent>();

        Assert.That(destroyingReceived, Is.True);
    }

    /// <summary>
    /// 测试事件可以在没有订阅者时发送
    /// </summary>
    [Test]
    public void Event_Can_Be_Sent_Without_Subscribers()
    {
        Assert.That(() => _eventBus!.Send<ArchitectureEvents.ArchitectureLifecycleReadyEvent>(),
            Throws.Nothing);
    }
}