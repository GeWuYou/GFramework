using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.coroutine.extensions;
using GFramework.Core.coroutine.instructions;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     CommandCoroutineExtensions的单元测试类
///     测试内容包括：
///     - SendCommandCoroutineWithErrorHandler扩展方法
///     - SendCommandAndWaitEventCoroutine扩展方法
/// </summary>
[TestFixture]
public class CommandCoroutineExtensionsTests
{
    /// <summary>
    ///     测试用的简单命令类
    /// </summary>
    private class TestCommand : IAsyncCommand
    {
        private IArchitectureContext? _context;

        public void SetContext(IArchitectureContext context)
        {
            _context = context;
        }

        public IArchitectureContext GetContext()
        {
            return _context!;
        }

        public Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     测试用的简单事件类
    /// </summary>
    private class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    ///     上下文感知基类的模拟实现
    /// </summary>
    private class TestContextAware : IContextAware
    {
        public readonly Mock<IArchitectureContext> _mockContext = new();

        public IArchitectureContext GetContext()
        {
            return _mockContext.Object;
        }

        public void SetContext(IArchitectureContext context)
        {
        }
    }

    /// <summary>
    ///     验证SendCommandCoroutineWithErrorHandler应该能正常执行成功的命令
    /// </summary>
    [Test]
    public async Task SendCommandCoroutineWithErrorHandler_Should_Execute_Successful_Command()
    {
        var command = new TestCommand();
        Exception? capturedException = null;
        var contextAware = new TestContextAware();

        // 设置上下文发送命令的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.CompletedTask);

        var coroutine = contextAware.SendCommandCoroutineWithErrorHandler(command, ex => capturedException = ex);

        // 迭代协程直到完成
        while (coroutine.MoveNext())
            if (coroutine.Current is WaitForTask)
                // 等待任务完成
                await Task.Delay(10);

        Assert.That(capturedException, Is.Null);
    }

    /// <summary>
    ///     验证SendCommandCoroutineWithErrorHandler应该捕获命令执行中的异常
    /// </summary>
    [Test]
    public async Task SendCommandCoroutineWithErrorHandler_Should_Capture_Command_Exception()
    {
        var command = new TestCommand();
        Exception? capturedException = null;
        var contextAware = new TestContextAware();
        var expectedException = new InvalidOperationException("Test exception");

        // 设置上下文发送命令的模拟行为，返回失败的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.FromException(expectedException));

        var coroutine = contextAware.SendCommandCoroutineWithErrorHandler(command, ex => capturedException = ex);

        // 迭代协程直到完成
        while (coroutine.MoveNext())
            if (coroutine.Current is WaitForTask)
                // 等待任务完成
                await Task.Delay(10);

        Assert.That(capturedException, Is.Not.Null);
        // 异常被包装为 AggregateException
        Assert.That(capturedException, Is.TypeOf<AggregateException>());
        var aggregateException = (AggregateException)capturedException!;
        Assert.That(aggregateException.InnerException, Is.EqualTo(expectedException));
    }

    /// <summary>
    ///     验证SendCommandCoroutineWithErrorHandler应该处理null错误回调
    /// </summary>
    [Test]
    public async Task SendCommandCoroutineWithErrorHandler_Should_Handle_Null_Error_Handler()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();
        var expectedException = new InvalidOperationException("Test exception");

        // 设置上下文发送命令的模拟行为，返回失败的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.FromException(expectedException));

        // 使用null作为错误处理程序
        var coroutine = contextAware.SendCommandCoroutineWithErrorHandler(command);

        // 迭代协程直到完成
        while (coroutine.MoveNext())
            if (coroutine.Current is WaitForTask)
                // 等待任务完成
                await Task.Delay(10);

        // 应该不会抛出异常
        Assert.Pass();
    }

    /// <summary>
    ///     验证SendCommandAndWaitEventCoroutine应该在事件触发后完成
    /// </summary>
    [Test]
    public async Task SendCommandAndWaitEventCoroutine_Should_Complete_After_Event_Triggered()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();
        TestEvent? receivedEvent = null;
        var eventTriggered = false;

        // 创建事件总线模拟
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        Action<TestEvent>? eventCallback = null;
        eventBusMock.Setup(bus => bus.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(callback => eventCallback = callback);

        // 设置上下文服务以返回事件总线
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IEventBus>())
            .Returns(eventBusMock.Object);

        // 设置命令执行返回完成的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.CompletedTask);

        var coroutine = contextAware.SendCommandAndWaitEventCoroutine<TestCommand, TestEvent>(
            command,
            ev =>
            {
                receivedEvent = ev;
                eventTriggered = true;
            });

        // 启动协程并等待命令执行完成
        coroutine.MoveNext(); // 进入命令发送阶段
        if (coroutine.Current is WaitForTask) await Task.Delay(10); // 等待命令任务完成

        // 此时协程应该在等待事件
        Assert.That(coroutine.MoveNext(), Is.True); // 等待事件阶段

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        eventCallback?.Invoke(testEvent);

        // 现在协程应该完成
        Assert.That(coroutine.MoveNext(), Is.False);

        Assert.That(eventTriggered, Is.True);
        Assert.That(receivedEvent, Is.Not.Null);
        Assert.That(receivedEvent?.Data, Is.EqualTo("TestData"));
    }

    /// <summary>
    ///     验证SendCommandAndWaitEventCoroutine应该处理超时情况
    /// </summary>
    [Test]
    public void SendCommandAndWaitEventCoroutine_Should_Throw_Timeout_Exception()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();

        // 创建事件总线模拟
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(bus => bus.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        // 设置上下文服务以返回事件总线
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IEventBus>())
            .Returns(eventBusMock.Object);

        // 设置命令执行返回完成的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.CompletedTask);

        // 对于超时情况，我们期望抛出TimeoutException
        Assert.Throws<TimeoutException>(() =>
        {
            var coroutine = contextAware.SendCommandAndWaitEventCoroutine<TestCommand, TestEvent>(
                command,
                null,
                0.1f); // 0.1秒超时

            // 运行协程直到完成
            while (coroutine.MoveNext())
                if (coroutine.Current is WaitForEventWithTimeout<TestEvent> timeoutWait)
                    // 模拟超时
                    timeoutWait.Update(0.2); // 更新时间超过超时限制
        });
    }

    /// <summary>
    ///     验证SendCommandAndWaitEventCoroutine应该处理null事件回调
    /// </summary>
    [Test]
    public async Task SendCommandAndWaitEventCoroutine_Should_Handle_Null_Event_Callback()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();

        // 创建事件总线模拟
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        Action<TestEvent>? eventCallback = null;
        eventBusMock.Setup(bus => bus.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object)
            .Callback<Action<TestEvent>>(callback => eventCallback = callback);

        // 设置上下文服务以返回事件总线
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IEventBus>())
            .Returns(eventBusMock.Object);

        // 设置命令执行返回完成的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.CompletedTask);

        // 使用null作为事件回调
        var coroutine = contextAware.SendCommandAndWaitEventCoroutine<TestCommand, TestEvent>(
            command); // null回调

        // 启动协程
        coroutine.MoveNext(); // 进入命令发送阶段
        if (coroutine.Current is WaitForTask) await Task.Delay(10); // 等待命令任务完成

        // 触发事件
        var testEvent = new TestEvent { Data = "TestData" };
        eventCallback?.Invoke(testEvent);

        // 协程应该能正常完成
        Assert.That(() => coroutine.MoveNext(), Throws.Nothing);
    }

    /// <summary>
    ///     验证SendCommandAndWaitEventCoroutine应该处理命令执行异常
    /// </summary>
    [Test]
    public async Task SendCommandAndWaitEventCoroutine_Should_Handle_Command_Exception()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();
        var expectedException = new InvalidOperationException("Command execution failed");

        // 创建事件总线模拟
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(bus => bus.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        // 设置上下文服务以返回事件总线
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IEventBus>())
            .Returns(eventBusMock.Object);

        // 设置命令执行返回失败的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.FromException(expectedException));

        var coroutine = contextAware.SendCommandAndWaitEventCoroutine<TestCommand, TestEvent>(
            command,
            _ => { });

        // 启动协程 - 命令失败时协程仍然继续
        coroutine.MoveNext(); // 进入命令发送阶段
        if (coroutine.Current is WaitForTask) await Task.Delay(10); // 等待命令任务完成

        // 命令执行失败后，协程继续执行
        Assert.Pass();
    }

    /// <summary>
    ///     验证SendCommandCoroutineWithErrorHandler应该返回IEnumerator<IYieldInstruction>
    /// </summary>
    [Test]
    public void SendCommandCoroutineWithErrorHandler_Should_Return_IEnumerator_Of_YieldInstruction()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();

        // 设置上下文发送命令的模拟行为
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.CompletedTask);

        var coroutine = contextAware.SendCommandCoroutineWithErrorHandler(command, ex => { });

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证SendCommandAndWaitEventCoroutine应该返回IEnumerator<IYieldInstruction>
    /// </summary>
    [Test]
    public void SendCommandAndWaitEventCoroutine_Should_Return_IEnumerator_Of_YieldInstruction()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();

        // 创建事件总线模拟
        var eventBusMock = new Mock<IEventBus>();
        var unRegisterMock = new Mock<IUnRegister>();

        eventBusMock.Setup(bus => bus.Register(It.IsAny<Action<TestEvent>>()))
            .Returns(unRegisterMock.Object);

        // 设置上下文服务以返回事件总线
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IEventBus>())
            .Returns(eventBusMock.Object);

        // 设置命令执行返回完成的任务
        contextAware._mockContext
            .Setup(ctx => ctx.SendCommandAsync(It.IsAny<IAsyncCommand>()))
            .Returns(Task.CompletedTask);

        var coroutine = contextAware.SendCommandAndWaitEventCoroutine<TestCommand, TestEvent>(
            command,
            ev => { });

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证SendCommandAndWaitEventCoroutine应该在事件总线为null时处理异常
    /// </summary>
    [Test]
    public void SendCommandAndWaitEventCoroutine_Should_Handle_Null_EventBus()
    {
        var command = new TestCommand();
        var contextAware = new TestContextAware();

        // 设置上下文服务以返回null事件总线
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IEventBus>())
            .Returns((IEventBus?)null);

        // 创建协程
        var coroutine = contextAware.SendCommandAndWaitEventCoroutine<TestCommand, TestEvent>(
            command,
            ev => { });

        // 调用 MoveNext 时应该抛出异常，因为事件总线为null
        Assert.Throws<ArgumentNullException>(() => coroutine.MoveNext());
    }
}