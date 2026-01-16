using GFramework.Core.Abstractions.state;
using GFramework.Core.state;
using NUnit.Framework;

namespace GFramework.Core.Tests.state;

[TestFixture]
public class StateTests
{
    [Test]
    public void State_Should_Implement_IState_Interface()
    {
        var state = new ConcreteStateV2();

        Assert.That(state is IState);
    }

    [Test]
    public void OnEnter_Should_BeCalled_When_State_Enters()
    {
        var state = new ConcreteStateV2();
        var otherState = new ConcreteStateV3();

        state.OnEnter(otherState);

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.SameAs(otherState));
    }

    [Test]
    public void OnEnter_WithNull_Should_Set_EnterFrom_ToNull()
    {
        var state = new ConcreteStateV2();

        state.OnEnter(null);

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.Null);
    }

    [Test]
    public void OnExit_Should_BeCalled_When_State_Exits()
    {
        var state = new ConcreteStateV2();
        var otherState = new ConcreteStateV3();

        state.OnExit(otherState);

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.SameAs(otherState));
    }

    [Test]
    public void OnExit_WithNull_Should_Set_ExitTo_ToNull()
    {
        var state = new ConcreteStateV2();

        state.OnExit(null);

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.Null);
    }

    [Test]
    public void CanTransitionTo_WithAllowTrue_Should_ReturnTrue()
    {
        var state = new ConcreteStateV2 { AllowTransitions = true };
        var target = new ConcreteStateV3();

        var result = state.CanTransitionTo(target);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanTransitionTo_WithAllowFalse_Should_ReturnFalse()
    {
        var state = new ConcreteStateV2 { AllowTransitions = false };
        var target = new ConcreteStateV3();

        var result = state.CanTransitionTo(target);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanTransitionTo_Should_Receive_TargetState()
    {
        var state = new ConcreteStateV2 { AllowTransitions = true };
        var target = new ConcreteStateV3();
        IState? receivedTarget = null;

        state.CanTransitionToAction = s => receivedTarget = s;
        state.CanTransitionTo(target);

        Assert.That(receivedTarget, Is.SameAs(target));
    }

    [Test]
    public void State_WithComplexTransitionRules_Should_Work()
    {
        var state1 = new ConditionalStateV2 { AllowedTransitions = new[] { typeof(ConcreteStateV3) } };
        var state2 = new ConcreteStateV3();
        var state3 = new ConcreteStateV4();

        Assert.That(state1.CanTransitionTo(state2), Is.True);
        Assert.That(state1.CanTransitionTo(state3), Is.False);
    }

    [Test]
    public void MultipleStates_Should_WorkTogether()
    {
        var state1 = new ConcreteStateV2();
        var state2 = new ConcreteStateV3();
        var state3 = new ConcreteStateV4();

        state1.OnEnter(null);
        state2.OnEnter(state1);
        state3.OnEnter(state2);

        Assert.That(state1.EnterCalled, Is.True);
        Assert.That(state2.EnterCalled, Is.True);
        Assert.That(state3.EnterCalled, Is.True);

        Assert.That(state2.EnterFrom, Is.SameAs(state1));
        Assert.That(state3.EnterFrom, Is.SameAs(state2));
    }

    [Test]
    public void State_Should_Track_MultipleTransitions()
    {
        var state = new TrackingStateV2();
        var other = new ConcreteStateV3();

        state.OnEnter(other);
        state.OnExit(other);
        state.OnEnter(other);
        state.OnExit(null);

        Assert.That(state.EnterCallCount, Is.EqualTo(2));
        Assert.That(state.ExitCallCount, Is.EqualTo(2));
    }

    [Test]
    public void State_Should_Handle_SameState_Transition()
    {
        var state1 = new ConcreteStateV2();
        var state2 = new ConcreteStateV3();
        var state3 = new ConcreteStateV2();

        state1.OnEnter(null);
        state2.OnEnter(state1);
        state3.OnEnter(state2);

        Assert.That(state1.EnterFrom, Is.Null);
        Assert.That(state2.EnterFrom, Is.SameAs(state1));
        Assert.That(state3.EnterFrom, Is.SameAs(state2));
    }
}

public sealed class ConcreteStateV2 : IState
{
    public bool AllowTransitions { get; set; } = true;
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }
    public Action<IState>? CanTransitionToAction { get; set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        CanTransitionToAction?.Invoke(target);
        return AllowTransitions;
    }
}

public sealed class ConcreteStateV3 : IState
{
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}

public sealed class ConcreteStateV4 : IState
{
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}

public sealed class ConditionalStateV2 : IState
{
    public Type[] AllowedTransitions { get; set; } = Array.Empty<Type>();
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return AllowedTransitions.Contains(target.GetType());
    }
}

public sealed class TrackingStateV2 : IState
{
    public int EnterCallCount { get; private set; }
    public int ExitCallCount { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCallCount++;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCallCount++;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}
