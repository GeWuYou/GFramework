using GFramework.Core.Abstractions.events;
using GFramework.Core.events;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.events;

/// <summary>
///     EventListenerScope的单元测试类
///     测试内容包括：
///     - 初始化和基本功能
///     - 事件触发处理
///     - 资源释放
///     - 多次触发事件
/// </summary>
[TestFixture]
public class EventListenerScopeTests
{
    /// <summary>
    ///     测试用的简单事件类
    /// </summary>
    private class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    ///     验证EventListenerScope初始状态为未触发
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Not_Be_Triggered_Initially()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        Assert.That(scope.IsTriggered, Is.False);
    }

    /// <summary>
    ///     验证EventListenerScope初始EventData为null
    /// </summary>
    [Test]
    public void EventListenerScope_EventData_Should_Be_Null_Initially()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        Assert.That(scope.EventData, Is.Null);
    }

    /// <summary>
    ///     验证EventListenerScope应该在事件触发后标记为已触发
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Be_Triggered_After_Event_Fired()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        Assert.That(scope.IsTriggered, Is.False);

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(scope.IsTriggered, Is.True);
    }

    /// <summary>
    ///     验证EventListenerScope应该保存事件数据
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Save_Event_Data()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        var testEvent = new TestEvent { Data = "SavedData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(scope.EventData, Is.Not.Null);
        Assert.That(scope.EventData?.Data, Is.EqualTo("SavedData"));
    }

    /// <summary>
    ///     验证EventListenerScope在Dispose后应该取消注册
    /// </summary>
    [Test]
    public void EventListenerScope_Should_UnRegister_On_Dispose()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        scope.Dispose();

        unRegisterMock.Verify(x => x.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     验证EventListenerScope应该在using块结束后自动取消注册
    /// </summary>
    [Test]
    public void EventListenerScope_Should_UnRegister_When_Using_Block_Ends()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        ExecuteUsingBlockTest(eventBusMock);

        // 验证using块结束后调用了UnRegister
        unRegisterMock.Verify(x => x.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     验证EventListenerScope应该在事件触发后保持已触发状态
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Remain_Triggered_After_Event_Fired()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        var testEvent = new TestEvent { Data = "FirstData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(scope.IsTriggered, Is.True);

        // 再次触发事件，状态应保持已触发
        registeredAction?.Invoke(new TestEvent { Data = "SecondData" });

        Assert.That(scope.IsTriggered, Is.True);
    }

    /// <summary>
    ///     验证EventListenerScope在多次触发时应该保存最后一次的事件数据
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Save_Last_Event_Data_On_Multiple_Triggers()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        // 第一次触发
        registeredAction?.Invoke(new TestEvent { Data = "FirstData" });
        Assert.That(scope.EventData?.Data, Is.EqualTo("FirstData"));

        // 第二次触发，应该覆盖数据
        registeredAction?.Invoke(new TestEvent { Data = "SecondData" });
        Assert.That(scope.EventData?.Data, Is.EqualTo("SecondData"));
    }

    /// <summary>
    ///     验证EventListenerScope应该在初始化时注册事件监听器
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Register_Event_Listener_On_Init()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        eventBusMock.Verify(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()), Times.Once);
    }

    /// <summary>
    ///     验证EventListenerScope应该处理值类型事件
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Handle_Value_Type_Event()
    {
        Action<int>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register(It.IsAny<Action<int>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<int>>(action => registeredAction = action);

        using var scope = new EventListenerScope<int>(eventBusMock.Object);

        Assert.That(scope.IsTriggered, Is.False);
        Assert.That(scope.EventData, Is.EqualTo(default(int)));

        registeredAction?.Invoke(42);

        Assert.That(scope.IsTriggered, Is.True);
        Assert.That(scope.EventData, Is.EqualTo(42));
    }

    /// <summary>
    ///     验证EventListenerScope应该处理结构体事件
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Handle_Struct_Event()
    {
        Action<StructEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register(It.IsAny<Action<StructEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<StructEvent>>(action => registeredAction = action);

        using var scope = new EventListenerScope<StructEvent>(eventBusMock.Object);

        Assert.That(scope.IsTriggered, Is.False);

        var structEvent = new StructEvent { Id = 123, Value = 456.78f };
        registeredAction?.Invoke(structEvent);

        Assert.That(scope.IsTriggered, Is.True);
        Assert.That(scope.EventData.Id, Is.EqualTo(123));
        Assert.That(scope.EventData.Value, Is.EqualTo(456.78f));
    }

    /// <summary>
    ///     验证EventListenerScope应该是线程安全的（IsTriggered使用volatile）
    /// </summary>
    [Test]
    public async Task EventListenerScope_IsTriggered_Should_Be_Thread_Safe()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        // 在另一个线程中触发事件
        await Task.Run(() => { registeredAction?.Invoke(new TestEvent { Data = "ThreadData" }); });

        // 主线程应该能看到更新后的值
        Assert.That(scope.IsTriggered, Is.True);
        Assert.That(scope.EventData?.Data, Is.EqualTo("ThreadData"));
    }

    /// <summary>
    ///     验证EventListenerScope可以多次Dispose而不抛出异常
    /// </summary>
    [Test]
    public void EventListenerScope_Should_Handle_Multiple_Dispose_Calls()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);

        // 多次调用Dispose不应抛出异常
        Assert.DoesNotThrow(() => scope.Dispose());
        Assert.DoesNotThrow(() => scope.Dispose());
    }

    /// <summary>
    ///     测试用的结构体事件
    /// </summary>
    private struct StructEvent
    {
        public int Id { get; set; }
        public float Value { get; set; }
    }

    /// <summary>
    ///     辅助方法：执行using块测试
    /// </summary>
    /// <param name="eventBusMock">事件总线模拟对象</param>
    private static void ExecuteUsingBlockTest(Mock<IEventBus> eventBusMock)
    {
        using var scope = new EventListenerScope<TestEvent>(eventBusMock.Object);
        // 作用域内部不验证
    }
}