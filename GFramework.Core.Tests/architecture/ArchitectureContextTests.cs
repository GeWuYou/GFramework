using System.Reflection;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
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

[TestFixture]
public class ArchitectureContextTests
{
    private ArchitectureContext? _context;
    private IocContainer? _container;
    private EventBus? _eventBus;
    private CommandBus? _commandBus;
    private QueryBus? _queryBus;
    private DefaultEnvironment? _environment;

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
        
        _eventBus = new EventBus();
        _commandBus = new CommandBus();
        _queryBus = new QueryBus();
        _environment = new DefaultEnvironment();
        _context = new ArchitectureContext(_container, _eventBus, _commandBus, _queryBus, _environment);
    }

    [Test]
    public void Constructor_Should_NotThrow_When_AllParameters_AreValid()
    {
        Assert.That(() => new ArchitectureContext(_container!, _eventBus!, _commandBus!, _queryBus!, _environment!), 
            Throws.Nothing);
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_Container_IsNull()
    {
        Assert.That(() => new ArchitectureContext(null!, _eventBus!, _commandBus!, _queryBus!, _environment!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("container"));
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_EventBus_IsNull()
    {
        Assert.That(() => new ArchitectureContext(_container!, null!, _commandBus!, _queryBus!, _environment!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("eventBus"));
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_CommandBus_IsNull()
    {
        Assert.That(() => new ArchitectureContext(_container!, _eventBus!, null!, _queryBus!, _environment!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("commandBus"));
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_QueryBus_IsNull()
    {
        Assert.That(() => new ArchitectureContext(_container!, _eventBus!, _commandBus!, null!, _environment!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("queryBus"));
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_Environment_IsNull()
    {
        Assert.That(() => new ArchitectureContext(_container!, _eventBus!, _commandBus!, _queryBus!, null!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("environment"));
    }

    [Test]
    public void SendQuery_Should_ReturnResult_When_Query_IsValid()
    {
        var testQuery = new TestQueryV2 { Result = 42 };
        var result = _context!.SendQuery(testQuery);
        
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SendQuery_Should_ThrowArgumentNullException_When_Query_IsNull()
    {
        Assert.That(() => _context!.SendQuery<int>(null!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("query"));
    }

    [Test]
    public void SendCommand_Should_ExecuteCommand_When_Command_IsValid()
    {
        var testCommand = new TestCommandV2();
        Assert.That(() => _context!.SendCommand(testCommand), Throws.Nothing);
        Assert.That(testCommand.Executed, Is.True);
    }

    [Test]
    public void SendCommand_Should_ThrowArgumentNullException_When_Command_IsNull()
    {
        Assert.That(() => _context!.SendCommand((ICommand)null!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("command"));
    }

    [Test]
    public void SendCommand_WithResult_Should_ReturnResult_When_Command_IsValid()
    {
        var testCommand = new TestCommandWithResultV2 { Result = 123 };
        var result = _context!.SendCommand(testCommand);
        
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void SendCommand_WithResult_Should_ThrowArgumentNullException_When_Command_IsNull()
    {
        Assert.That(() => _context!.SendCommand((ICommand<int>)null!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("command"));
    }

    [Test]
    public void SendEvent_Should_SendEvent_When_EventType_IsValid()
    {
        bool eventReceived = false;
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent<TestEventV2>();
        
        Assert.That(eventReceived, Is.True);
    }

    [Test]
    public void SendEvent_WithInstance_Should_SendEvent_When_EventInstance_IsValid()
    {
        bool eventReceived = false;
        var testEvent = new TestEventV2();
        _context!.RegisterEvent<TestEventV2>(_ => eventReceived = true);
        _context.SendEvent(testEvent);
        
        Assert.That(eventReceived, Is.True);
    }

    [Test]
    public void SendEvent_WithInstance_Should_ThrowArgumentNullException_When_EventInstance_IsNull()
    {
        Assert.That(() => _context!.SendEvent<TestEventV2>(null!), 
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("e"));
    }

    [Test]
    public void GetSystem_Should_ReturnRegisteredSystem_When_SystemIsRegistered()
    {
        var testSystem = new TestSystemV2();
        _container!.RegisterPlurality(testSystem);
        
        var result = _context!.GetSystem<TestSystemV2>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testSystem));
    }

    [Test]
    public void GetSystem_Should_ReturnNull_When_SystemIsNotRegistered()
    {
        var result = _context!.GetSystem<TestSystemV2>();
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetModel_Should_ReturnRegisteredModel_When_ModelIsRegistered()
    {
        var testModel = new TestModelV2();
        _container!.RegisterPlurality(testModel);
        
        var result = _context!.GetModel<TestModelV2>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testModel));
    }

    [Test]
    public void GetModel_Should_ReturnNull_When_ModelIsNotRegistered()
    {
        var result = _context!.GetModel<TestModelV2>();
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetUtility_Should_ReturnRegisteredUtility_When_UtilityIsRegistered()
    {
        var testUtility = new TestUtilityV2();
        _container!.RegisterPlurality(testUtility);
        
        var result = _context!.GetUtility<TestUtilityV2>();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(testUtility));
    }

    [Test]
    public void GetUtility_Should_ReturnNull_When_UtilityIsNotRegistered()
    {
        var result = _context!.GetUtility<TestUtilityV2>();
        
        Assert.That(result, Is.Null);
    }

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
    public void Init() { }
    public void Destroy() { }
    public void OnArchitecturePhase(ArchitecturePhase phase) { }
}

public class TestModelV2 : IModel
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }
    
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
    public void Init() { }
    public void Destroy() { }
    public void OnArchitecturePhase(ArchitecturePhase phase) { }
}

public class TestUtilityV2 : IUtility
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }
    
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
    public void Init() { }
    public void Destroy() { }
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
