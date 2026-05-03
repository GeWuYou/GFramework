// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Core.Query;
using GFramework.Core.State;

namespace GFramework.Core.Tests.State;

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
        // 初始化 LoggerFactoryResolver 以支持 MicrosoftDiContainer
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        _eventBus = new EventBus();
        var container = new MicrosoftDiContainer();

        // 直接初始化 logger 字段
        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
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

        Assert.Throws<InvalidOperationException>(() => state1.GetContext());
        Assert.Throws<InvalidOperationException>(() => state2.GetContext());

        _stateMachine.Initialize();

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
        _stateMachine.Initialize();
    }

    /// <summary>
    ///     测试DestroyAsync方法不抛出异常
    /// </summary>
    [Test]
    public void DestroyAsync_Should_Not_Throw_Exception()
    {
        Assert.That(() => _stateMachine!.DestroyAsync(), Throws.Nothing);
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
    ///     测试异步ChangeToAsync发送StateChangedEvent事件
    ///     验证当状态机使用异步方法切换到新状态时，会正确触发StateChangedEvent事件
    /// </summary>
    [Test]
    public async Task ChangeToAsync_Should_Send_StateChangedEvent()
    {
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

        _stateMachine.Initialize();
        await _stateMachine.ChangeToAsync<TestStateV5>();

        Assert.That(eventReceived, Is.True);
        Assert.That(receivedEvent!.OldState, Is.Null);
        Assert.That(receivedEvent.NewState, Is.InstanceOf<TestStateV5>());
    }

    /// <summary>
    ///     测试异步ChangeToAsync发送StateChangedEvent事件（包含旧状态）
    ///     验证当状态机使用异步方法从一个状态切换到另一个状态时，会正确触发StateChangedEvent事件
    /// </summary>
    [Test]
    public async Task ChangeToAsync_Should_Send_StateChangedEvent_With_OldState()
    {
        var eventReceived = false;
        StateChangedEvent? receivedEvent = null;

        _eventBus!.Register<StateChangedEvent>(e =>
        {
            eventReceived = true;
            receivedEvent = e;
        });

        var state1 = new TestStateV5();
        var state2 = new TestStateV5_2();

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.Initialize();
        await _stateMachine.ChangeToAsync<TestStateV5>();

        eventReceived = false;
        await _stateMachine.ChangeToAsync<TestStateV5_2>();

        Assert.That(eventReceived, Is.True);
        Assert.That(receivedEvent!.OldState, Is.InstanceOf<TestStateV5>());
        Assert.That(receivedEvent.NewState, Is.InstanceOf<TestStateV5_2>());
    }

    /// <summary>
    ///     测试异步切换时多次状态变更都能正确触发事件
    /// </summary>
    [Test]
    public async Task ChangeToAsync_MultipleChanges_Should_Send_Events_Correctly()
    {
        var eventCount = 0;

        _eventBus!.Register<StateChangedEvent>(_ => { eventCount++; });

        var state1 = new TestStateV5();
        var state2 = new TestStateV5_2();

        _stateMachine!.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.Initialize();
        await _stateMachine.ChangeToAsync<TestStateV5>();
        await _stateMachine.ChangeToAsync<TestStateV5_2>();
        await _stateMachine.ChangeToAsync<TestStateV5>();

        Assert.That(eventCount, Is.EqualTo(3));
    }
}
