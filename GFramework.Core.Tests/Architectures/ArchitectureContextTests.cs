// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Cqrs;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Core.Query;
using GFramework.Core.Services.Modules;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     ArchitectureContext类的单元测试
///     测试内容包括：
///     - 构造函数参数验证（所有5个参数）
///     - 构造函数空参数异常
///     - SendQuery方法 - 正常查询发送
///     - SendQuery方法 - 空查询异常
///     - SendCommand方法 - 正常命令发送
///     - SendCommand方法 - 空命令异常
///     - SendCommand_WithResult方法 - 正常命令发送
///     - SendCommand_WithResult方法 - 空命令异常
///     - SendEvent方法 - 正常事件发送
///     - SendEvent_WithInstance方法 - 正常事件发送
///     - SendEvent_WithInstance方法 - 空事件异常
///     - GetSystem方法 - 获取已注册系统
///     - GetSystem方法 - 获取未注册系统时抛出异常
///     - GetModel方法 - 获取已注册模型
///     - GetModel方法 - 获取未注册模型时抛出异常
///     - GetUtility方法 - 获取已注册工具
///     - GetUtility方法 - 获取未注册工具时抛出异常
///     - GetEnvironment方法 - 获取环境对象
/// </summary>
[NonParallelizable]
[TestFixture]
public class ArchitectureContextTests
{
    /// <summary>
    ///     初始化测试所需的容器与默认服务实例。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // 初始化 LoggerFactoryResolver 以支持 MicrosoftDiContainer
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        _container = new MicrosoftDiContainer();

        // 直接初始化 logger 字段
        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(ArchitectureContextTests)));

        // 创建服务实例
        _eventBus = new EventBus();
        _commandBus = new CommandExecutor();
        _queryBus = new QueryExecutor();
        _asyncQueryBus = new AsyncQueryExecutor();
        _environment = new DefaultEnvironment();

        // 将服务注册到容器
        _container.RegisterPlurality(_eventBus);
        _container.RegisterPlurality(_commandBus);
        _container.RegisterPlurality(_queryBus);
        _container.RegisterPlurality(_asyncQueryBus);
        _container.RegisterPlurality(_environment);
        new CqrsRuntimeModule().Register(_container);
        RegisterLegacyBridgeHandlers(_container);

        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    ///     释放当前测试创建的容器，并清理 legacy bridge 共享计数状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        LegacyBridgePipelineTracker.Reset();
        _container?.Dispose();
    }

    private AsyncQueryExecutor? _asyncQueryBus;
    private CommandExecutor? _commandBus;
    private MicrosoftDiContainer? _container;

    private ArchitectureContext? _context;
    private DefaultEnvironment? _environment;
    private EventBus? _eventBus;
    private QueryExecutor? _queryBus;

    /// <summary>
    ///     测试构造函数在所有参数都有效时不应抛出异常
    /// </summary>
    [Test]
    public void Constructor_Should_NotThrow_When_AllParameters_AreValid()
    {
        Assert.That(() => new ArchitectureContext(_container!), Throws.Nothing);
    }

    /// <summary>
    ///     测试构造函数在 container 为 null 时应抛出 ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!), Throws.ArgumentNullException);
    }

    /// <summary>
    ///     测试构造函数在Container为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("container"));
    }

    /// <summary>
    ///     测试SendQuery方法在查询有效时返回正确结果
    /// </summary>
    [Test]
    public void SendQuery_Should_ReturnResult_When_Query_IsValid()
    {
        var testQuery = new TestQueryV2 { Result = 42 };
        var result = _context!.SendQuery(testQuery);

        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试 legacy 查询通过 <see cref="ArchitectureContext" /> 发送时会进入统一 CQRS pipeline，
    ///     并把当前架构上下文注入到查询对象。
    /// </summary>
    [Test]
    public void SendQuery_Should_Bridge_Through_CqrsRuntime_And_Preserve_Context()
    {
        LegacyBridgePipelineTracker.Reset();
        var testQuery = new LegacyArchitectureBridgeQuery();
        var bridgeContext = CreateFrozenBridgeContext(out var bridgeContainer);

        try
        {
            var result = bridgeContext.SendQuery(testQuery);

            Assert.That(result, Is.EqualTo(24));
            Assert.That(testQuery.ObservedContext, Is.SameAs(bridgeContext));
            Assert.That(LegacyBridgePipelineTracker.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            bridgeContainer.Dispose();
        }
    }

    /// <summary>
    ///     测试SendQuery方法在查询为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendQuery_Should_ThrowArgumentNullException_When_Query_IsNull()
    {
        // 明确指定调用旧的 IQuery<int> 重载
        Assert.That(() => _context!.SendQuery((IQuery<int>)null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("query"));
    }

    /// <summary>
    ///     测试SendCommand方法在命令有效时正确执行
    /// </summary>
    [Test]
    public void SendCommand_Should_ExecuteCommand_When_Command_IsValid()
    {
        var testCommand = new TestCommandV2();
        Assert.That(() => _context!.SendCommand(testCommand), Throws.Nothing);
        Assert.That(testCommand.Executed, Is.True);
    }

    /// <summary>
    ///     测试 legacy 命令通过 <see cref="ArchitectureContext" /> 发送时会进入统一 CQRS pipeline，
    ///     并把当前架构上下文注入到命令对象。
    /// </summary>
    [Test]
    public void SendCommand_Should_Bridge_Through_CqrsRuntime_And_Preserve_Context()
    {
        LegacyBridgePipelineTracker.Reset();
        var testCommand = new LegacyArchitectureBridgeCommand();
        var bridgeContext = CreateFrozenBridgeContext(out var bridgeContainer);

        try
        {
            bridgeContext.SendCommand(testCommand);

            Assert.That(testCommand.Executed, Is.True);
            Assert.That(testCommand.ObservedContext, Is.SameAs(bridgeContext));
            Assert.That(LegacyBridgePipelineTracker.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            bridgeContainer.Dispose();
        }
    }

    /// <summary>
    ///     测试SendCommand方法在命令为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendCommand_Should_ThrowArgumentNullException_When_Command_IsNull()
    {
        Assert.That(() => _context!.SendCommand(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("command"));
    }

    /// <summary>
    ///     测试SendCommand方法（带返回值）在命令有效时返回正确结果
    /// </summary>
    [Test]
    public void SendCommand_WithResult_Should_ReturnResult_When_Command_IsValid()
    {
        var testCommand = new TestCommandWithResultV2 { Result = 123 };
        var result = _context!.SendCommand(testCommand);

        Assert.That(result, Is.EqualTo(123));
    }

    /// <summary>
    ///     测试 legacy 带返回值命令通过 <see cref="ArchitectureContext" /> 发送时会进入统一 CQRS pipeline，
    ///     并保持原始返回值语义。
    /// </summary>
    [Test]
    public void SendCommand_WithResult_Should_Bridge_Through_CqrsRuntime()
    {
        LegacyBridgePipelineTracker.Reset();
        var testCommand = new LegacyArchitectureBridgeCommandWithResult();
        var bridgeContext = CreateFrozenBridgeContext(out var bridgeContainer);

        try
        {
            var result = bridgeContext.SendCommand(testCommand);

            Assert.That(result, Is.EqualTo(42));
            Assert.That(testCommand.ObservedContext, Is.SameAs(bridgeContext));
            Assert.That(LegacyBridgePipelineTracker.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            bridgeContainer.Dispose();
        }
    }

    /// <summary>
    ///     测试 legacy 异步查询通过 <see cref="ArchitectureContext" /> 发送时也会进入统一 CQRS pipeline。
    /// </summary>
    [Test]
    public async Task SendQueryAsync_Should_Bridge_Through_CqrsRuntime_And_Preserve_Context()
    {
        LegacyBridgePipelineTracker.Reset();
        var testQuery = new LegacyArchitectureBridgeAsyncQuery();
        var bridgeContext = CreateFrozenBridgeContext(out var bridgeContainer);

        try
        {
            var result = await bridgeContext.SendQueryAsync(testQuery).ConfigureAwait(false);

            Assert.That(result, Is.EqualTo(64));
            Assert.That(testQuery.ObservedContext, Is.SameAs(bridgeContext));
            Assert.That(LegacyBridgePipelineTracker.InvocationCount, Is.EqualTo(1));
        }
        finally
        {
            bridgeContainer.Dispose();
        }
    }

    /// <summary>
    ///     为需要验证统一 CQRS pipeline 的用例创建一个已冻结的最小 bridge 上下文。
    /// </summary>
    /// <param name="container">返回承载当前 bridge 上下文的冻结容器，供测试在 finally 中显式释放。</param>
    /// <returns>能够执行 legacy bridge request 且会 materialize open-generic pipeline behavior 的上下文。</returns>
    private static ArchitectureContext CreateFrozenBridgeContext(out MicrosoftDiContainer container)
    {
        container = new MicrosoftDiContainer();
        RegisterLegacyBridgeHandlers(container);
        new CqrsRuntimeModule().Register(container);
        container.ExecuteServicesHook(services =>
            services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LegacyBridgeTrackingPipelineBehavior<,>)));
        container.Freeze();
        return new ArchitectureContext(container);
    }

    /// <summary>
    ///     把 GFramework.Core 内部的 legacy bridge handler 实例预先注册成可见的实例绑定。
    /// </summary>
    /// <param name="container">目标测试容器。</param>
    private static void RegisterLegacyBridgeHandlers(MicrosoftDiContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.RegisterPlurality(new LegacyCommandDispatchRequestHandler());
        container.RegisterPlurality(new LegacyCommandResultDispatchRequestHandler());
        container.RegisterPlurality(new LegacyAsyncCommandDispatchRequestHandler());
        container.RegisterPlurality(new LegacyAsyncCommandResultDispatchRequestHandler());
        container.RegisterPlurality(new LegacyQueryDispatchRequestHandler());
        container.RegisterPlurality(new LegacyAsyncQueryDispatchRequestHandler());
    }

    /// <summary>
    ///     测试SendCommand方法（带返回值）在命令为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendCommand_WithResult_Should_ThrowArgumentNullException_When_Command_IsNull()
    {
        Assert.That(() => _context!.SendCommand((ICommand<int>)null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("command"));
    }

    /// <summary>
    ///     测试SendEvent方法在事件类型有效时正确发送事件
    /// </summary>
    [Test]
    public void SendEvent_Should_SendEvent_When_EventType_IsValid()
    {
        var eventReceived = false;
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent<TestEventV2>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    ///     测试SendEvent方法（带实例）在事件实例有效时正确发送事件
    /// </summary>
    [Test]
    public void SendEvent_WithInstance_Should_SendEvent_When_EventInstance_IsValid()
    {
        var eventReceived = false;
        var testEvent = new TestEventV2();
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent(testEvent);

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    ///     测试SendEvent方法（带实例）在事件实例为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendEvent_WithInstance_Should_ThrowArgumentNullException_When_EventInstance_IsNull()
    {
        Assert.That(() => _context!.SendEvent<TestEventV2>(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("e"));
    }

    /// <summary>
    ///     测试GetSystem方法在系统已注册时返回注册的系统
    /// </summary>
    [Test]
    public void GetSystem_Should_ReturnRegisteredSystem_When_SystemIsRegistered()
    {
        var testSystem = new TestSystemV2();
        _container!.RegisterPlurality(testSystem);

        var result = _context!.GetSystem<TestSystemV2>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testSystem));
    }

    /// <summary>
    ///     测试GetSystem方法在系统未注册时应抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void GetSystem_Should_ThrowInvalidOperationException_When_SystemIsNotRegistered()
    {
        Assert.That(() => _context!.GetSystem<TestSystemV2>(),
            Throws.InvalidOperationException);
    }

    /// <summary>
    ///     测试GetModel方法在模型已注册时返回注册的模型
    /// </summary>
    [Test]
    public void GetModel_Should_ReturnRegisteredModel_When_ModelIsRegistered()
    {
        var testModel = new TestModelV2();
        _container!.RegisterPlurality(testModel);

        var result = _context!.GetModel<TestModelV2>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testModel));
    }

    /// <summary>
    ///     测试GetModel方法在模型未注册时应抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void GetModel_Should_ThrowInvalidOperationException_When_ModelIsNotRegistered()
    {
        Assert.That(() => _context!.GetModel<TestModelV2>(),
            Throws.InvalidOperationException);
    }

    /// <summary>
    ///     测试GetUtility方法在工具已注册时返回注册的工具
    /// </summary>
    [Test]
    public void GetUtility_Should_ReturnRegisteredUtility_When_UtilityIsRegistered()
    {
        var testUtility = new TestUtilityV2();
        _container!.RegisterPlurality(testUtility);

        var result = _context!.GetUtility<TestUtilityV2>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testUtility));
    }

    /// <summary>
    ///     测试GetUtility方法在工具未注册时应抛出 InvalidOperationException
    /// </summary>
    [Test]
    public void GetUtility_Should_ThrowInvalidOperationException_When_UtilityIsNotRegistered()
    {
        Assert.That(() => _context!.GetUtility<TestUtilityV2>(),
            Throws.InvalidOperationException);
    }

    /// <summary>
    ///     测试GetEnvironment方法返回环境实例
    /// </summary>
    [Test]
    public void GetEnvironment_Should_Return_EnvironmentInstance()
    {
        var environment = _context!.GetEnvironment();

        Assert.That(environment, Is.Not.Null);
        Assert.That(environment, Is.InstanceOf<IEnvironment>());
    }

    /// <summary>
    ///     测试 CQRS runtime 在并发首次访问时只会从容器解析一次。
    /// </summary>
    [Test]
    public async Task SendRequestAsync_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently()
    {
        const int workerCount = 8;
        using var startGate = new ManualResetEventSlim(false);
        using var allowResolutionToComplete = new ManualResetEventSlim(false);
        using var workersReady = new CountdownEvent(workerCount);
        var resolutionCallCount = 0;
        var runtime = new Mock<ICqrsRuntime>(MockBehavior.Strict);
        var container = new Mock<IIocContainer>(MockBehavior.Strict);

        runtime.Setup(mockRuntime => mockRuntime.SendAsync(
                It.IsAny<IArchitectureContext>(),
                It.IsAny<IRequest<int>>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(42));

        container.Setup(mockContainer => mockContainer.Get<ICqrsRuntime>())
            .Returns(() =>
            {
                Interlocked.Increment(ref resolutionCallCount);
                allowResolutionToComplete.Wait();
                return runtime.Object;
            });

        var context = new ArchitectureContext(container.Object);
        var requests = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                workersReady.Signal();
                startGate.Wait();
                return await context.SendRequestAsync(new TestCqrsRequest()).ConfigureAwait(false);
            }))
            .ToArray();

        ReleaseWorkersAfterFirstResolutionAttempt(
            workersReady,
            startGate,
            allowResolutionToComplete,
            () => Volatile.Read(ref resolutionCallCount) > 0);

        var responses = await Task.WhenAll(requests);

        Assert.That(responses, Has.All.EqualTo(42));
        Assert.That(resolutionCallCount, Is.EqualTo(1));
        container.Verify(mockContainer => mockContainer.Get<ICqrsRuntime>(), Times.Once);
        runtime.Verify(
            mockRuntime => mockRuntime.SendAsync(
                It.IsAny<IArchitectureContext>(),
                It.IsAny<IRequest<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(requests.Length));
    }

    /// <summary>
    ///     测试 CQRS runtime 在并发首次发布通知时只会从容器解析一次。
    /// </summary>
    [Test]
    public async Task PublishAsync_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently()
    {
        const int workerCount = 8;
        using var startGate = new ManualResetEventSlim(false);
        using var allowResolutionToComplete = new ManualResetEventSlim(false);
        using var workersReady = new CountdownEvent(workerCount);
        var resolutionCallCount = 0;
        var runtime = new Mock<ICqrsRuntime>(MockBehavior.Strict);
        var container = new Mock<IIocContainer>(MockBehavior.Strict);

        runtime.Setup(mockRuntime => mockRuntime.PublishAsync(
                It.IsAny<IArchitectureContext>(),
                It.IsAny<TestCqrsNotification>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        container.Setup(mockContainer => mockContainer.Get<ICqrsRuntime>())
            .Returns(() =>
            {
                Interlocked.Increment(ref resolutionCallCount);
                allowResolutionToComplete.Wait();
                return runtime.Object;
            });

        var context = new ArchitectureContext(container.Object);
        var notifications = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                workersReady.Signal();
                startGate.Wait();
                await context.PublishAsync(new TestCqrsNotification()).ConfigureAwait(false);
            }))
            .ToArray();

        ReleaseWorkersAfterFirstResolutionAttempt(
            workersReady,
            startGate,
            allowResolutionToComplete,
            () => Volatile.Read(ref resolutionCallCount) > 0);

        await Task.WhenAll(notifications).ConfigureAwait(false);

        Assert.That(resolutionCallCount, Is.EqualTo(1));
        container.Verify(mockContainer => mockContainer.Get<ICqrsRuntime>(), Times.Once);
        runtime.Verify(
            mockRuntime => mockRuntime.PublishAsync(
                It.IsAny<IArchitectureContext>(),
                It.IsAny<TestCqrsNotification>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(notifications.Length));
    }

    /// <summary>
    ///     测试 CQRS runtime 在并发首次创建流时只会从容器解析一次。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_ResolveCqrsRuntime_OnlyOnce_When_AccessedConcurrently()
    {
        const int workerCount = 8;
        using var startGate = new ManualResetEventSlim(false);
        using var allowResolutionToComplete = new ManualResetEventSlim(false);
        using var workersReady = new CountdownEvent(workerCount);
        var resolutionCallCount = 0;
        var runtime = new Mock<ICqrsRuntime>(MockBehavior.Strict);
        var container = new Mock<IIocContainer>(MockBehavior.Strict);

        runtime.Setup(mockRuntime => mockRuntime.CreateStream(
                It.IsAny<IArchitectureContext>(),
                It.IsAny<TestCqrsStreamRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(static () => CreateTestCqrsStream());

        container.Setup(mockContainer => mockContainer.Get<ICqrsRuntime>())
            .Returns(() =>
            {
                Interlocked.Increment(ref resolutionCallCount);
                allowResolutionToComplete.Wait();
                return runtime.Object;
            });

        var context = new ArchitectureContext(container.Object);
        var streamTasks = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(async () =>
            {
                workersReady.Signal();
                startGate.Wait();
                await DrainAsync(context.CreateStream(new TestCqrsStreamRequest())).ConfigureAwait(false);
            }))
            .ToArray();

        ReleaseWorkersAfterFirstResolutionAttempt(
            workersReady,
            startGate,
            allowResolutionToComplete,
            () => Volatile.Read(ref resolutionCallCount) > 0);

        await Task.WhenAll(streamTasks).ConfigureAwait(false);

        Assert.That(resolutionCallCount, Is.EqualTo(1));
        container.Verify(mockContainer => mockContainer.Get<ICqrsRuntime>(), Times.Once);
        runtime.Verify(
            mockRuntime => mockRuntime.CreateStream(
                It.IsAny<IArchitectureContext>(),
                It.IsAny<TestCqrsStreamRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(streamTasks.Length));
    }

    /// <summary>
    ///     枚举完整个测试流，确保 `CreateStream` 路径真正执行到底。
    /// </summary>
    /// <param name="stream">要消费的异步流。</param>
    /// <returns>表示消费完成的任务。</returns>
    private static async Task DrainAsync(IAsyncEnumerable<int> stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await foreach (var _ in stream.ConfigureAwait(false))
        {
        }
    }

    /// <summary>
    ///     释放并发 worker，并确保在断言失败时也能放行首次 runtime 解析。
    /// </summary>
    /// <param name="workersReady">用于确认 worker 已就绪的倒计时器。</param>
    /// <param name="startGate">用于同时放行 worker 的门闩。</param>
    /// <param name="allowResolutionToComplete">用于解除首次 runtime 解析阻塞的门闩。</param>
    /// <param name="hasObservedResolutionAttempt">用于判断当前是否已观察到首次 runtime 解析尝试。</param>
    private static void ReleaseWorkersAfterFirstResolutionAttempt(
        CountdownEvent workersReady,
        ManualResetEventSlim startGate,
        ManualResetEventSlim allowResolutionToComplete,
        Func<bool> hasObservedResolutionAttempt)
    {
        ArgumentNullException.ThrowIfNull(workersReady);
        ArgumentNullException.ThrowIfNull(startGate);
        ArgumentNullException.ThrowIfNull(allowResolutionToComplete);
        ArgumentNullException.ThrowIfNull(hasObservedResolutionAttempt);

        var workerStartupTimeout = TimeSpan.FromSeconds(5);
        var firstResolutionTimeout = TimeSpan.FromSeconds(5);

        Assert.That(
            workersReady.Wait(workerStartupTimeout),
            Is.True,
            "Expected all workers to be ready before releasing start gate.");
        startGate.Set();

        try
        {
            Assert.That(
                SpinWait.SpinUntil(hasObservedResolutionAttempt, firstResolutionTimeout),
                Is.True,
                "Expected at least one CQRS runtime resolution attempt.");
        }
        finally
        {
            allowResolutionToComplete.Set();
        }
    }

    /// <summary>
    ///     为 `CreateStream` 并发解析测试提供最小异步流。
    /// </summary>
    /// <returns>只包含单个元素的异步流。</returns>
    private static async IAsyncEnumerable<int> CreateTestCqrsStream()
    {
        yield return 42;
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private sealed class TestCqrsRequest : IRequest<int>
    {
    }

    private sealed record TestCqrsNotification : INotification;

    private sealed record TestCqrsStreamRequest : IStreamRequest<int>;
}
