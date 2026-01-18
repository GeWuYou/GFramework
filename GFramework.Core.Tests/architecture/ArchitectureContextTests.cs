using System.Reflection;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.query;
using NUnit.Framework;

namespace GFramework.Core.Tests.architecture;

/// <summary>
/// ArchitectureContext类的单元测试
/// 测试内容包括：
/// - 构造函数参数验证（所有5个参数）
/// - 构造函数空参数异常
/// - SendQuery方法 - 正常查询发送
/// - SendQuery方法 - 空查询异常
/// - SendCommand方法 - 正常命令发送
/// - SendCommand方法 - 空命令异常
/// - SendCommand_WithResult方法 - 正常命令发送
/// - SendCommand_WithResult方法 - 空命令异常
/// - SendEvent方法 - 正常事件发送
/// - SendEvent_WithInstance方法 - 正常事件发送
/// - SendEvent_WithInstance方法 - 空事件异常
/// - GetSystem方法 - 获取已注册系统
/// - GetSystem方法 - 获取未注册系统
/// - GetModel方法 - 获取已注册模型
/// - GetModel方法 - 获取未注册模型
/// - GetUtility方法 - 获取已注册工具
/// - GetUtility方法 - 获取未注册工具
/// - GetEnvironment方法 - 获取环境对象
/// </summary>
[TestFixture]
public class ArchitectureContextTests
{
    [SetUp]
    public void SetUp()
    {
        // 初始化 LoggerFactoryResolver 以支持 IocContainer
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        _container = new IocContainer();

        // 直接初始化 logger 字段
        var loggerField = typeof(IocContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(ArchitectureContextTests)));

        // 创建服务实例
        _eventBus = new EventBus();
        _commandBus = new CommandBus();
        _queryBus = new QueryBus();
        _asyncQueryBus = new AsyncQueryBus();
        _environment = new DefaultEnvironment();

        // 将服务注册到容器
        _container.RegisterPlurality(_eventBus);
        _container.RegisterPlurality(_commandBus);
        _container.RegisterPlurality(_queryBus);
        _container.RegisterPlurality(_asyncQueryBus);
        _container.RegisterPlurality(_environment);

        _context = new ArchitectureContext(_container);
    }

    private ArchitectureContext? _context;
    private IocContainer? _container;
    private EventBus? _eventBus;
    private CommandBus? _commandBus;
    private QueryBus? _queryBus;
    private AsyncQueryBus? _asyncQueryBus;
    private DefaultEnvironment? _environment;

    /// <summary>
    /// 测试构造函数在所有参数都有效时不应抛出异常
    /// </summary>
    [Test]
    public void Constructor_Should_NotThrow_When_AllParameters_AreValid()
    {
        Assert.That(() => new ArchitectureContext(_container!), Throws.Nothing);
    }

    /// <summary>
    /// 测试构造函数在 container 为 null 时应抛出 ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_Should_Throw_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!), Throws.ArgumentNullException);
    }

    /// <summary>
    /// 测试构造函数在Container为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("container"));
    }

    /// <summary>
    /// 测试SendQuery方法在查询有效时返回正确结果
    /// </summary>
    [Test]
    public void SendQuery_Should_ReturnResult_When_Query_IsValid()
    {
        var testQuery = new TestQueryV2 { Result = 42 };
        var result = _context!.SendQuery(testQuery);

        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    /// 测试SendQuery方法在查询为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendQuery_Should_ThrowArgumentNullException_When_Query_IsNull()
    {
        Assert.That(() => _context!.SendQuery<int>(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("query"));
    }

    /// <summary>
    /// 测试SendCommand方法在命令有效时正确执行
    /// </summary>
    [Test]
    public void SendCommand_Should_ExecuteCommand_When_Command_IsValid()
    {
        var testCommand = new TestCommandV2();
        Assert.That(() => _context!.SendCommand(testCommand), Throws.Nothing);
        Assert.That(testCommand.Executed, Is.True);
    }

    /// <summary>
    /// 测试SendCommand方法在命令为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendCommand_Should_ThrowArgumentNullException_When_Command_IsNull()
    {
        Assert.That(() => _context!.SendCommand((ICommand)null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("command"));
    }

    /// <summary>
    /// 测试SendCommand方法（带返回值）在命令有效时返回正确结果
    /// </summary>
    [Test]
    public void SendCommand_WithResult_Should_ReturnResult_When_Command_IsValid()
    {
        var testCommand = new TestCommandWithResultV2 { Result = 123 };
        var result = _context!.SendCommand(testCommand);

        Assert.That(result, Is.EqualTo(123));
    }

    /// <summary>
    /// 测试SendCommand方法（带返回值）在命令为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendCommand_WithResult_Should_ThrowArgumentNullException_When_Command_IsNull()
    {
        Assert.That(() => _context!.SendCommand((ICommand<int>)null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("command"));
    }

    /// <summary>
    /// 测试SendEvent方法在事件类型有效时正确发送事件
    /// </summary>
    [Test]
    public void SendEvent_Should_SendEvent_When_EventType_IsValid()
    {
        bool eventReceived = false;
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent<TestEventV2>();

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    /// 测试SendEvent方法（带实例）在事件实例有效时正确发送事件
    /// </summary>
    [Test]
    public void SendEvent_WithInstance_Should_SendEvent_When_EventInstance_IsValid()
    {
        bool eventReceived = false;
        var testEvent = new TestEventV2();
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent(testEvent);

        Assert.That(eventReceived, Is.True);
    }

    /// <summary>
    /// 测试SendEvent方法（带实例）在事件实例为null时应抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendEvent_WithInstance_Should_ThrowArgumentNullException_When_EventInstance_IsNull()
    {
        Assert.That(() => _context!.SendEvent<TestEventV2>(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("e"));
    }

    /// <summary>
    /// 测试GetSystem方法在系统已注册时返回注册的系统
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
    /// 测试GetSystem方法在系统未注册时返回null
    /// </summary>
    [Test]
    public void GetSystem_Should_ReturnNull_When_SystemIsNotRegistered()
    {
        var result = _context!.GetSystem<TestSystemV2>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// 测试GetModel方法在模型已注册时返回注册的模型
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
    /// 测试GetModel方法在模型未注册时返回null
    /// </summary>
    [Test]
    public void GetModel_Should_ReturnNull_When_ModelIsNotRegistered()
    {
        var result = _context!.GetModel<TestModelV2>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// 测试GetUtility方法在工具已注册时返回注册的工具
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
    /// 测试GetUtility方法在工具未注册时返回null
    /// </summary>
    [Test]
    public void GetUtility_Should_ReturnNull_When_UtilityIsNotRegistered()
    {
        var result = _context!.GetUtility<TestUtilityV2>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// 测试GetEnvironment方法返回环境实例
    /// </summary>
    [Test]
    public void GetEnvironment_Should_Return_EnvironmentInstance()
    {
        var environment = _context!.GetEnvironment();

        Assert.That(environment, Is.Not.Null);
        Assert.That(environment, Is.InstanceOf<IEnvironment>());
    }
}

#region Test Classes

public class TestSystemV2 : ISystem
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;

    public void Init()
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

    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;

    public void Init()
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

    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

public class TestQueryV2 : IQuery<int>
{
    private IArchitectureContext _context = null!;
    public int Result { get; init; }

    public int Do() => Result;
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

public class TestCommandV2 : ICommand
{
    private IArchitectureContext _context = null!;
    public bool Executed { get; private set; }

    public void Execute() => Executed = true;
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

public class TestCommandWithResultV2 : ICommand<int>
{
    private IArchitectureContext _context = null!;
    public int Result { get; init; }

    public int Execute() => Result;
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

public class TestEventV2
{
    public int Data { get; init; }
}

#endregion