using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.events;
using GFramework.Core.coroutine.instructions;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForEvent的单元测试类
///     测试内容包括：
///     - 初始化和基本功能
///     - 事件触发处理
///     - 资源释放
///     - 异常处理
/// </summary>
[TestFixture]
public class WaitForEventTests
{
    /// <summary>
    ///     测试用的简单事件类
    /// </summary>
    private class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    ///     验证WaitForEvent初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Not_Be_Done_Initially()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForEvent应该在事件触发后完成
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Be_Done_After_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        Assert.That(wait.IsDone, Is.False);

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEvent应该保存事件数据
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Save_Event_Data()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.EventData, Is.Not.Null);
        Assert.That(wait.EventData?.Data, Is.EqualTo("TestData"));
    }

    /// <summary>
    ///     验证WaitForEvent应该在事件触发后保持完成状态
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Remain_Done_After_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.IsDone, Is.True);

        // 再次触发事件，确认状态不变
        registeredAction?.Invoke(new TestEvent { Data = "AnotherData" });

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEvent应该在Dispose后释放资源
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Release_Resources_On_Dispose()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        wait.Dispose();

        unRegisterMock.Verify(x => x.UnRegister(), Times.Once);
    }

    /// <summary>
    ///     验证WaitForEvent应该处理多次Dispose调用
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Handle_Multiple_Dispose_Calls()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        wait.Dispose();
        // 第二次调用不应引发异常
        Assert.DoesNotThrow(() => wait.Dispose());
    }

    /// <summary>
    ///     验证WaitForEvent应该抛出ArgumentNullException当eventBus为null
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Throw_ArgumentNullException_When_EventBus_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForEvent<TestEvent>(null!));
    }

    /// <summary>
    ///     验证WaitForEvent的Update方法不影响状态
    /// </summary>
    [Test]
    public void WaitForEvent_Update_Should_Not_Affect_State()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEvent实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Implement_IYieldInstruction()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    ///     验证WaitForEvent在事件触发后自动注销监听器
    /// </summary>
    [Test]
    public void WaitForEvent_Should_Unregister_After_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        // 事件触发后，Update方法应该注销监听器
        wait.Update(0.1);

        unRegisterMock.Verify(x => x.UnRegister(), Times.AtLeastOnce);
    }

    /// <summary>
    ///     验证WaitForEventEventData为null当没有事件触发
    /// </summary>
    [Test]
    public void WaitForEvent_EventData_Should_Be_Null_When_No_Event_Triggered()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        Assert.That(wait.EventData, Is.Null);
    }

    /// <summary>
    ///     验证WaitForEventEventData在事件触发后不为null
    /// </summary>
    [Test]
    public void WaitForEvent_EventData_Should_Not_Be_Null_After_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var wait = new WaitForEvent<TestEvent>(eventBusMock.Object);

        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.EventData, Is.Not.Null);
    }
}