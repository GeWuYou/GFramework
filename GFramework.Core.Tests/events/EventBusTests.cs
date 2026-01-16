using GFramework.Core.events;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
/// EventBus测试类，用于验证事件总线的各种功能
/// </summary>
[TestFixture]
public class EventBusTests
{
    /// <summary>
    /// 测试设置方法，在每个测试方法执行前初始化EventBus实例
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _eventBus = new EventBus();
    }

    private EventBus _eventBus = null!;

    /// <summary>
    /// 测试注册事件处理器的功能
    /// 验证注册的处理器能够在发送对应事件时被正确调用
    /// </summary>
    [Test]
    public void Register_Should_Add_Handler()
    {
        var called = false;
        _eventBus.Register<EventBusTestsEvent>(@event => { called = true; });

        _eventBus.Send<EventBusTestsEvent>();

        Assert.That(called, Is.True);
    }

    /// <summary>
    /// 测试注销事件处理器的功能
    /// 验证已注册的处理器在注销后不会再被调用
    /// </summary>
    [Test]
    public void UnRegister_Should_Remove_Handler()
    {
        var count = 0;

        Action<EventBusTestsEvent> handler = @event => { count++; };
        _eventBus.Register(handler);
        _eventBus.Send<EventBusTestsEvent>();
        // 验证处理器被调用一次
        Assert.That(count, Is.EqualTo(1));

        _eventBus.UnRegister(handler);
        _eventBus.Send<EventBusTestsEvent>();
        // 验证处理器在注销后不再被调用
        Assert.That(count, Is.EqualTo(1));
    }

    /// <summary>
    /// 测试发送事件时调用所有处理器的功能
    /// 验证同一事件类型的多个处理器都能被正确调用
    /// </summary>
    [Test]
    public void SendEvent_Should_Invoke_All_Handlers()
    {
        var count1 = 0;
        var count2 = 0;

        _eventBus.Register<EventBusTestsEvent>(@event => { count1++; });
        _eventBus.Register<EventBusTestsEvent>(@event => { count2++; });

        _eventBus.Send<EventBusTestsEvent>();

        // 验证所有处理器都被调用一次
        Assert.That(count1, Is.EqualTo(1));
        Assert.That(count2, Is.EqualTo(1));
    }
}

/// <summary>
/// EventBus测试专用事件类
/// </summary>
public class EventBusTestsEvent
{
}