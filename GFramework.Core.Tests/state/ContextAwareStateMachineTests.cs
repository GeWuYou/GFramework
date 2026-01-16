using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.state;
using GFramework.Core.Abstractions.system;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.query;
using GFramework.Core.state;
using NUnit.Framework;

namespace GFramework.Core.Tests.state;

[TestFixture]
public class ContextAwareStateMachineTests
{
    private TestContextAwareStateMachineV5? _stateMachine;
    private ArchitectureContext? _context;
    private EventBus? _eventBus;

    [SetUp]
    public void SetUp()
    {
        _eventBus = new EventBus();
        _context = new ArchitectureContext(
            new IocContainer(),
            _eventBus,
            new CommandBus(),
            new QueryBus(),
            new DefaultEnvironment());
        
        _stateMachine = new TestContextAwareStateMachineV5();
        _stateMachine.SetContext(_context);
    }

    [Test]
    public void ContextAwareStateMachine_Should_Implement_ISystem_Interface()
    {
        Assert.That(_stateMachine, Is.InstanceOf<ISystem>());
    }

    [Test]
    public void SetContext_Should_Set_Context_Property()
    {
        _stateMachine!.SetContext(_context!);
        
        var context = _stateMachine.GetContext();
        Assert.That(context, Is.SameAs(_context));
    }

    [Test]
    public void GetContext_Should_Return_Context_Property()
    {
        _stateMachine!.SetContext(_context!);
        
        var context = _stateMachine.GetContext();
        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.SameAs(_context));
    }

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

    [Test]
    public void Init_Should_Not_SetContext_On_NonContextAware_States()
    {
        var state = new TestStateV5();
        
        _stateMachine!.Register(state);
        _stateMachine.Init();
    }

    [Test]
    public void Destroy_Should_Not_Throw_Exception()
    {
        Assert.That(() => _stateMachine!.Destroy(), Throws.Nothing);
    }

    [Test]
    public void OnArchitecturePhase_Should_Not_Throw_Exception()
    {
        Assert.That(() => _stateMachine!.OnArchitecturePhase(ArchitecturePhase.Ready), 
            Throws.Nothing);
    }

    [Test]
    public void ChangeTo_Should_Send_StateChangedEvent()
    {
        bool eventReceived = false;
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

    [Test]
    public void ChangeTo_Should_Send_StateChangedEvent_With_OldState()
    {
        bool eventReceived = false;
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

    [Test]
    public void CanChangeTo_Should_Work_With_Registered_States()
    {
        var state = new TestStateV5();
        
        _stateMachine!.Register(state);
        
        var canChange = _stateMachine.CanChangeTo<TestStateV5>();
        Assert.That(canChange, Is.True);
    }

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

public class TestContextAwareStateMachineV5 : ContextAwareStateMachine
{
    public Dictionary<Type, IState> GetStates() => States;
}

public class TestContextAwareStateV5 : ContextAwareStateBase
{
    public override void OnEnter(IState? previous)
    {
    }

    public override void OnExit(IState? next)
    {
    }
}

public class TestContextAwareStateV5_2 : ContextAwareStateBase
{
    public override void OnEnter(IState? previous)
    {
    }

    public override void OnExit(IState? next)
    {
    }
}

public class TestStateV5 : IState
{
    public int Id { get; set; }
    
    public bool CanTransitionTo(IState next) => true;
    
    public void OnEnter(IState? previous)
    {
    }

    public void OnExit(IState? next)
    {
    }
}

#endregion
