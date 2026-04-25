using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Core.Query;
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
[TestFixture]
public class ArchitectureContextTests
{
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

        _context = new ArchitectureContext(_container);
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
        var workerStartupTimeout = TimeSpan.FromSeconds(5);
        var firstResolutionTimeout = TimeSpan.FromSeconds(5);
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

        Assert.That(
            workersReady.Wait(workerStartupTimeout),
            Is.True,
            "Expected all workers to be ready before releasing start gate.");
        startGate.Set();

        Assert.That(
            SpinWait.SpinUntil(() => Volatile.Read(ref resolutionCallCount) > 0, firstResolutionTimeout),
            Is.True,
            "Expected at least one CQRS runtime resolution attempt.");

        allowResolutionToComplete.Set();

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

    private sealed class TestCqrsRequest : IRequest<int>
    {
    }
}

#region Test Classes

public class TestSystemV2 : ISystem
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }

    public void Initialize()
    {
    }

    public void Destroy()
    {
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}

public class TestModelV2 : IModel
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }

    public void Initialize()
    {
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    public void Destroy()
    {
    }
}

public class TestUtilityV2 : IUtility
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

public class TestQueryV2 : IQuery<int>
{
    private IArchitectureContext _context = null!;
    public int Result { get; init; }

    public int Do()
    {
        return Result;
    }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

public class TestCommandV2 : ICommand
{
    private IArchitectureContext _context = null!;
    public bool Executed { get; private set; }

    public void Execute()
    {
        Executed = true;
    }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

public class TestCommandWithResultV2 : ICommand<int>
{
    private IArchitectureContext _context = null!;
    public int Result { get; init; }

    public int Execute()
    {
        return Result;
    }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }
}

public class TestEventV2
{
    public int Data { get; init; }
}

#endregion
