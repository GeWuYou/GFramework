using GFramework.Core.Abstractions.state;
using GFramework.Core.state;
using NUnit.Framework;

namespace GFramework.Core.Tests.state;

[TestFixture]
public class StateMachineTests
{
    [SetUp]
    public void SetUp()
    {
        _stateMachine = new StateMachine();
    }

    private StateMachine _stateMachine = null!;

    [Test]
    public void Current_Should_BeNull_When_NoState_Active()
    {
        Assert.That(_stateMachine.Current, Is.Null);
    }

    [Test]
    public void Register_Should_AddState_To_StatesDictionary()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);

        Assert.That(_stateMachine.ContainsState<TestStateV2>(), Is.True);
    }

    [Test]
    public void ChangeTo_Should_SetCurrentState()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        _stateMachine.ChangeTo<TestStateV2>();

        Assert.That(_stateMachine.Current, Is.SameAs(state));
    }

    [Test]
    public void ChangeTo_Should_Invoke_OnEnter()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        _stateMachine.ChangeTo<TestStateV2>();

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.Null);
    }

    [Test]
    public void ChangeTo_When_CurrentStateExists_Should_Invoke_OnExit()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.ChangeTo<TestStateV2>();
        _stateMachine.ChangeTo<TestStateV3>();

        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state1.ExitTo, Is.SameAs(state2));
    }

    [Test]
    public void ChangeTo_When_CurrentStateExists_Should_Invoke_OnEnter()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        _stateMachine.ChangeTo<TestStateV2>();
        _stateMachine.ChangeTo<TestStateV3>();

        Assert.That(state2.EnterCalled, Is.True);
        Assert.That(state2.EnterFrom, Is.SameAs(state1));
    }

    [Test]
    public void ChangeTo_ToSameState_Should_NotInvoke_Callbacks()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        _stateMachine.ChangeTo<TestStateV2>();

        var enterCount = state.EnterCallCount;
        var exitCount = state.ExitCallCount;

        _stateMachine.ChangeTo<TestStateV2>();

        Assert.That(state.EnterCallCount, Is.EqualTo(enterCount));
        Assert.That(state.ExitCallCount, Is.EqualTo(exitCount));
    }

    [Test]
    public void ChangeTo_ToUnregisteredState_Should_ThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _stateMachine.ChangeTo<TestStateV2>());
    }

    [Test]
    public void CanChangeTo_WhenStateNotRegistered_Should_ReturnFalse()
    {
        var result = _stateMachine.CanChangeTo<TestStateV2>();
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanChangeTo_WhenStateRegistered_Should_ReturnTrue()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);

        var result = _stateMachine.CanChangeTo<TestStateV2>();
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanChangeTo_WhenCurrentStateDeniesTransition_Should_ReturnFalse()
    {
        var state1 = new TestStateV2 { AllowTransition = false };
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.ChangeTo<TestStateV2>();

        var result = _stateMachine.CanChangeTo<TestStateV3>();
        Assert.That(result, Is.False);
    }

    [Test]
    public void ChangeTo_WhenCurrentStateDeniesTransition_Should_NotChange()
    {
        var state1 = new TestStateV2 { AllowTransition = false };
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.ChangeTo<TestStateV2>();

        var oldState = _stateMachine.Current;
        _stateMachine.ChangeTo<TestStateV3>();

        Assert.That(_stateMachine.Current, Is.SameAs(oldState));
        Assert.That(_stateMachine.Current, Is.SameAs(state1));
        Assert.That(state2.EnterCalled, Is.False);
    }

    [Test]
    public void Unregister_Should_RemoveState_FromDictionary()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        _stateMachine.Unregister<TestStateV2>();

        Assert.That(_stateMachine.ContainsState<TestStateV2>(), Is.False);
    }

    [Test]
    public void Unregister_WhenStateIsActive_Should_Invoke_OnExit_AndClearCurrent()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        _stateMachine.ChangeTo<TestStateV2>();
        _stateMachine.Unregister<TestStateV2>();

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.Null);
        Assert.That(_stateMachine.Current, Is.Null);
    }

    [Test]
    public void Unregister_WhenStateNotActive_Should_Not_Invoke_OnExit()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.ChangeTo<TestStateV2>();

        _stateMachine.Unregister<TestStateV3>();

        Assert.That(state1.ExitCalled, Is.False);
        Assert.That(_stateMachine.Current, Is.SameAs(state1));
    }

    [Test]
    public void MultipleStateChanges_Should_Invoke_Callbacks_Correctly()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        var state3 = new TestStateV4();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.Register(state3);

        _stateMachine.ChangeTo<TestStateV2>();
        _stateMachine.ChangeTo<TestStateV3>();
        _stateMachine.ChangeTo<TestStateV4>();

        Assert.That(state1.EnterCalled, Is.True);
        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state2.EnterCalled, Is.True);
        Assert.That(state2.ExitCalled, Is.True);
        Assert.That(state3.EnterCalled, Is.True);
        Assert.That(state3.ExitCalled, Is.False);
    }

    [Test]
    public void ChangeTo_Should_Respect_CanTransitionTo_Logic()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        var state3 = new TestStateV4();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.Register(state3);

        _stateMachine.ChangeTo<TestStateV2>();
        _stateMachine.ChangeTo<TestStateV3>();

        Assert.That(state1.EnterCalled, Is.True);
        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state2.EnterCalled, Is.True);
    }
}

public sealed class TestStateV2 : IState
{
    public bool AllowTransition { get; set; } = true;
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public int EnterCallCount { get; private set; }
    public int ExitCallCount { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterCallCount++;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitCallCount++;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return AllowTransition;
    }
}

public sealed class TestStateV3 : IState
{
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public int EnterCallCount { get; private set; }
    public int ExitCallCount { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterCallCount++;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitCallCount++;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}

public sealed class TestStateV4 : IState
{
    public bool EnterCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public int EnterCallCount { get; private set; }
    public int ExitCallCount { get; private set; }
    public IState? EnterFrom { get; private set; }
    public IState? ExitTo { get; private set; }

    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterCallCount++;
        EnterFrom = from;
    }

    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitCallCount++;
        ExitTo = to;
    }

    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}

public static class StateMachineExtensions
{
    public static bool ContainsState<T>(this StateMachine stateMachine) where T : IState
    {
        return stateMachine.GetType().GetField("States", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                   .GetValue(stateMachine) is System.Collections.Generic.Dictionary<System.Type, IState> states &&
               states.ContainsKey(typeof(T));
    }
}
