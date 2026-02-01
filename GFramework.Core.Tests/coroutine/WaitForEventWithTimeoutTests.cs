using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.events;
using GFramework.Core.coroutine.instructions;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForEventWithTimeout的单元测试类
///     测试内容包括：
///     - 初始化和基本功能
///     - 超时处理
///     - 事件提前触发
///     - 异常处理
/// </summary>
[TestFixture]
public class WaitForEventWithTimeoutTests
{
    /// <summary>
    ///     测试用的简单事件类
    /// </summary>
    private class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout初始状态为未完成且未超时
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Not_Be_Done_Initially()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 2.0f);

        Assert.That(wait.IsDone, Is.False);
        Assert.That(wait.IsTimeout, Is.False);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在超时时完成
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Be_Done_When_Timeout()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 1.0f);

        // 更新时间超过超时时间
        wait.Update(1.5);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.True);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在事件触发时完成
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Be_Done_When_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 2.0f);

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在事件触发后保存事件数据
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Save_Event_Data()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 2.0f);

        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.EventData, Is.Not.Null);
        Assert.That(wait.EventData?.Data, Is.EqualTo("TestData"));
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在超时后返回null事件数据
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Return_Null_EventData_When_Timeout()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 1.0f);

        wait.Update(1.5);

        Assert.That(wait.EventData, Is.Null);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在事件触发前正确计算超时
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Calculate_Timeout_Correctly()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 2.0f);

        wait.Update(1.0);
        Assert.That(wait.IsTimeout, Is.False);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.5);
        Assert.That(wait.IsTimeout, Is.False);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.6); // 总共2.1秒，超过2.0秒超时时间
        Assert.That(wait.IsTimeout, Is.True);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在事件触发后忽略后续超时
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Ignore_Timeout_After_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 1.0f);

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);

        // 即使时间超过了超时限制，也不应标记为超时
        wait.Update(2.0);
        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该抛出ArgumentNullException当waitForEvent为null
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Throw_ArgumentNullException_When_WaitForEvent_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForEventWithTimeout<TestEvent>(null!, 1.0f));
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该正确处理Update方法
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Update_Should_Work_Correctly()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 2.0f);

        // 更新时间但未超过超时时间
        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);
        Assert.That(wait.IsTimeout, Is.False);

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Implement_IYieldInstruction()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 2.0f);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该处理小超时时间
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Handle_Small_Timeout()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 0.1f);

        wait.Update(0.2);

        Assert.That(wait.IsTimeout, Is.True);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该处理大超时时间
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Handle_Large_Timeout()
    {
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();
        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 10.0f);

        wait.Update(5.0);

        Assert.That(wait.IsTimeout, Is.False);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForEventWithTimeout应该在事件触发后忽略后续超时并保持状态
    /// </summary>
    [Test]
    public void WaitForEventWithTimeout_Should_Ignore_Timeout_And_Maintain_State_After_Event_Triggered()
    {
        Action<TestEvent>? registeredAction = null;
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(x => x.Register<TestEvent>(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(action => registeredAction = action);

        var waitForEvent = new WaitForEvent<TestEvent>(eventBusMock.Object);
        var wait = new WaitForEventWithTimeout<TestEvent>(waitForEvent, 1.0f);

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        registeredAction?.Invoke(testEvent);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);
        Assert.That(wait.EventData, Is.Not.Null);
        Assert.That(wait.EventData?.Data, Is.EqualTo("TestData"));

        // 即使时间超过了超时限制，也不应标记为超时，状态应保持不变
        wait.Update(2.0);
        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);
        Assert.That(wait.EventData, Is.Not.Null);
        Assert.That(wait.EventData?.Data, Is.EqualTo("TestData"));

        // 再次更新，状态仍应保持不变
        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimeout, Is.False);
        Assert.That(wait.EventData, Is.Not.Null);
        Assert.That(wait.EventData?.Data, Is.EqualTo("TestData"));
    }
}