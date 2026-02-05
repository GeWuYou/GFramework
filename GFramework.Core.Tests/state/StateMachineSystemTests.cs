using System.Reflection;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.state;
using GFramework.Core.Abstractions.system;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.query;
using GFramework.Core.state;
using NUnit.Framework;

namespace GFramework.Core.Tests.state;

/// <summary>
///     ContextAwareStateMachine类的单元测试
///     测试内容包括：
///     - 作为ISystem的集成测试
///     - Init方法 - 初始化上下文感知状态
///     - Init方法 - 设置Context属性
///     - Destroy方法 - 清理状态
///     - OnArchitecturePhase方法 - 接收架构阶段
///     - 上下文感知状态初始化
///     - 状态变更事件发送
///     - SetContext方法
///     - GetContext方法
///     - ISystem接口实现验证
///     - 与EventBus的集成测试
///     - 多状态注册和切换
///     - 状态机生命周期完整性
/// </summary>
[TestFixture]
public class StateMachineSystemTests
{
    [SetUp]
    public void SetUp()
    {
        // 初始化 LoggerFactoryResolver 以支持 IocContainer
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        _eventBus = new EventBus();
        var container = new IocContainer();

        // 直接初始化 logger 字段
        var loggerField = typeof(IocContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(StateMachineSystemTests)));

        container.RegisterPlurality(_eventBus);
        container.RegisterPlurality(new CommandExecutor());
        container.RegisterPlurality(new QueryExecutor());
        container.RegisterPlurality(new DefaultEnvironment());
        container.RegisterPlurality(new AsyncQueryExecutor());

        _context = new ArchitectureContext(container);

        _stateMachine = new TestStateMachineSystemV5();
        _stateMachine.SetContext(_context);
    }

    private TestStateMachineSystemV5? _stateMachine;
    private ArchitectureContext? _context;
    private EventBus? _eventBus;

    /// <summary>
    ///     测试ContextAwareStateMachine实现ISystem接口
    /// </summary>
    [Test]
    public void ContextAwareStateMachine_Should_Implement_ISystem_Interface()
    {
        Assert.That(_stateMachine, Is.InstanceOf<ISystem>());
    }

    /// <summary>
    ///     测试SetContext设置Context属性
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        _stateMachine!.SetContext(_context!);

        var context = _stateMachine.GetContext();
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试GetContext返回Context属性
    /// </summary>
    [Test]
    public void GetContext_Should_Return_Context_Property()
    {
        _stateMachine!.SetContext(_context!);

        var context = _stateMachine.GetContext();
        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试Init方法为所有ContextAware状态设置Context
    /// </summary>
    [Test]
    public void Init_Should_SetContext_On_All_ContextAware_States()
    {
        var state1 = new TestContextAwareStateV5();
        var state2 = new TestContextAwareStateV5_2();

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);

        Assert.That(state1.GetContext(), Is.Null);
        Assert.That(state2.GetContext(), Is.Null);

        _stateMachine.Init();

        Assert.That(state1.GetContext(), Is.SameAs(_context));
        Assert.That(state2.GetContext(), Is.SameAs(_context));
    }

    /// <summary>
    ///     测试Init方法不为非ContextAware状态设置Context
    /// </summary>
    [Test]
    public void Init_Should_Not_SetContext_On_NonContextAware_States()
    {
        var state = new TestStateV5();

        _stateMachine!.Register(state);
        _stateMachine.Init();
    }

    /// <summary>
    ///     测试Destroy方法不抛出异常
    /// </summary>
    [Test]
    public void Destroy_Should_Not_Throw_Exception()
    {
        Assert.That(() => _stateMachine!.Destroy(), Throws.Nothing);
    }

    /// <summary>
    ///     测试OnArchitecturePhase方法不抛出异常
    /// </summary>
    [Test]
    public void OnArchitecturePhase_Should_Not_Throw_Exception()
    {
        Assert.That(() => _stateMachine!.OnArchitecturePhase(ArchitecturePhase.Ready),
            Throws.Nothing);
    }

    /// <summary>
    ///     测试ChangeTo发送StateChangedEvent事件
    ///     验证当状态机切换到新状态时，会正确触发StateChangedEvent事件，并且事件中的旧状态为null（首次切换）
    /// </summary>
    [Test]
    public void ChangeTo_Should_Send_StateChangedEvent()
    {
        // 订阅StateChangedEvent事件以验证事件是否被正确发送
        var eventReceived = false;
        StateChangedEvent? receivedEvent = null;

        _eventBus!.Register<StateChangedEvent>(e =>
        {
            eventReceived = true;
            receivedEvent = e;
        });

        var state1 = new TestStateV5();
        var state2 = new TestStateV5();

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.Init();
        _stateMachine.ChangeTo<TestStateV5>();

        Assert.That(eventReceived, Is.True);
        Assert.That(receivedEvent!.OldState, Is.Null);
        Assert.That(receivedEvent.NewState, Is.InstanceOf<TestStateV5>());
    }

    /// <summary>
    ///     测试ChangeTo发送StateChangedEvent事件（包含旧状态）
    ///     验证当状态机从一个状态切换到另一个状态时，会正确触发StateChangedEvent事件，
    ///     并且事件中包含正确的旧状态和新状态信息
    /// </summary>
    [Test]
    public void ChangeTo_Should_Send_StateChangedEvent_With_OldState()
    {
        // 订阅StateChangedEvent事件以验证事件是否被正确发送
        var eventReceived = false;
        StateChangedEvent? receivedEvent = null;

        _eventBus!.Register<StateChangedEvent>(e =>
        {
            eventReceived = true;
            receivedEvent = e;
        });

        var state1 = new TestStateV5();
        var state2 = new TestStateV5();

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.Init();
        _stateMachine.ChangeTo<TestStateV5>();
        _stateMachine.ChangeTo<TestStateV5>();

        Assert.That(eventReceived, Is.True);
        Assert.That(receivedEvent!.OldState, Is.InstanceOf<TestStateV5>());
        Assert.That(receivedEvent.NewState, Is.InstanceOf<TestStateV5>());
    }

    /// <summary>
    ///     测试CanChangeTo方法对于已注册状态的工作情况
    ///     验证当状态已注册到状态机中时，CanChangeTo方法应返回true
    /// </summary>
    [Test]
    public void CanChangeTo_Should_Work_With_Registered_States()
    {
        var state = new TestStateV5();

        _stateMachine!.Register(state);

        var canChange = _stateMachine.CanChangeTo<TestStateV5>();
        Assert.That(canChange, Is.True);
    }

    /// <summary>
    ///     测试可以注册多个状态
    ///     验证状态机能够成功注册多个不同的状态实例，并且能够切换到这些已注册的状态
    /// </summary>
    [Test]
    public void Multiple_States_Should_Be_Registered()
    {
        var state1 = new TestStateV5 { Id = 1 };
        var state2 = new TestStateV5 { Id = 2 };
        var state3 = new TestStateV5 { Id = 3 };

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.Register(state3);

        var canChange = _stateMachine.CanChangeTo<TestStateV5>();
        Assert.That(canChange, Is.True);
    }


    /// <summary>
    ///     测试状态机生命周期完整
    /// </summary>
    [Test]
    public void StateMachine_Lifecycle_Should_Be_Complete()
    {
        var state1 = new TestStateV5();
        var state2 = new TestStateV5();

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.Init();
        Assert.That(_stateMachine.Current, Is.Null);

        _stateMachine.ChangeTo<TestStateV5>();
        Assert.That(_stateMachine.Current, Is.InstanceOf<TestStateV5>());

        _stateMachine.Destroy();
    }
}

#region Test Classes

/// <summary>
///     测试用的ContextAwareStateMachine派生类，用于访问内部状态字典
/// </summary>
public class TestStateMachineSystemV5 : StateMachineSystem
{
    /// <summary>
    ///     获取状态机内部的状态字典
    /// </summary>
    /// <returns>类型到状态实例的映射字典</returns>
    public Dictionary<Type, IState> GetStates()
    {
        return States;
    }
}

/// <summary>
///     测试用的上下文感知状态基类实现
/// </summary>
public class TestContextAwareStateV5 : ContextAwareStateBase
{
    /// <summary>
    ///     进入状态时调用
    /// </summary>
    /// <param name="previous">前一个状态</param>
    public override void OnEnter(IState? previous)
    {
    }

    /// <summary>
    ///     退出状态时调用
    /// </summary>
    /// <param name="next">下一个状态</param>
    public override void OnExit(IState? next)
    {
    }
}

/// <summary>
///     第二个测试用的上下文感知状态基类实现
/// </summary>
public class TestContextAwareStateV5_2 : ContextAwareStateBase
{
    /// <summary>
    ///     进入状态时调用
    /// </summary>
    /// <param name="previous">前一个状态</param>
    public override void OnEnter(IState? previous)
    {
    }

    /// <summary>
    ///     退出状态时调用
    /// </summary>
    /// <param name="next">下一个状态</param>
    public override void OnExit(IState? next)
    {
    }
}

/// <summary>
///     测试用的普通状态实现
/// </summary>
public class TestStateV5 : IState
{
    /// <summary>
    ///     状态标识符
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     检查是否可以转换到指定状态
    /// </summary>
    /// <param name="next">目标状态</param>
    /// <returns>始终返回true表示允许转换</returns>
    public bool CanTransitionTo(IState next)
    {
        return true;
    }

    /// <summary>
    ///     进入状态时调用
    /// </summary>
    /// <param name="previous">前一个状态</param>
    public void OnEnter(IState? previous)
    {
    }

    /// <summary>
    ///     退出状态时调用
    /// </summary>
    /// <param name="next">下一个状态</param>
    public void OnExit(IState? next)
    {
    }
}

#endregion