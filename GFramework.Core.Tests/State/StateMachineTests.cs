// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;
using GFramework.Core.State;
using NUnit.Framework;

namespace GFramework.Core.Tests.State;

/// <summary>
///     测试状态机功能的单元测试类
/// </summary>
[TestFixture]
public class StateMachineTests
{
    /// <summary>
    ///     在每个测试方法执行前初始化状态机实例
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _stateMachine = new StateMachine();
    }

    private StateMachine _stateMachine = null!;

    /// <summary>
    ///     验证当没有活动状态时，当前状态应为null
    /// </summary>
    [Test]
    public void Current_Should_BeNull_When_NoState_Active()
    {
        Assert.That(_stateMachine.Current, Is.Null);
    }

    /// <summary>
    ///     验证注册状态后，状态会被添加到状态字典中
    /// </summary>
    [Test]
    public void Register_Should_AddState_To_StatesDictionary()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);

        Assert.That(_stateMachine.ContainsState<TestStateV2>(), Is.True);
    }

    /// <summary>
    ///     验证异步注销后状态应从字典中移除
    /// </summary>
    [Test]
    public async Task UnregisterAsync_Should_RemoveState_FromDictionary()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        await _stateMachine.UnregisterAsync<TestStateV2>();

        Assert.That(_stateMachine.ContainsState<TestStateV2>(), Is.False);
    }


    /// <summary>
    ///     验证异步注销活动异步状态时调用OnExitAsync
    /// </summary>
    [Test]
    public async Task UnregisterAsync_WhenStateIsActive_WithAsyncState_Should_Invoke_OnExitAsync()
    {
        var state = new TestAsyncState();
        _stateMachine.Register(state);
        await _stateMachine.ChangeToAsync<TestAsyncState>();
        await _stateMachine.UnregisterAsync<TestAsyncState>();

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.Null);
        Assert.That(_stateMachine.Current, Is.Null);
    }

    /// <summary>
    ///     验证异步切换检查未注册状态返回false
    /// </summary>
    [Test]
    public async Task CanChangeToAsync_WhenStateNotRegistered_Should_ReturnFalse()
    {
        var result = await _stateMachine.CanChangeToAsync<TestStateV2>();
        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     验证异步切换检查已注册状态返回true
    /// </summary>
    [Test]
    public async Task CanChangeToAsync_WhenStateRegistered_Should_ReturnTrue()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);

        var result = await _stateMachine.CanChangeToAsync<TestStateV2>();
        Assert.That(result, Is.True);
    }


    /// <summary>
    ///     验证异步切换检查使用异步状态时调用CanTransitionToAsync
    /// </summary>
    [Test]
    public async Task CanChangeToAsync_WithAsyncState_Should_Call_CanTransitionToAsync()
    {
        var state1 = new TestAsyncState();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        await _stateMachine.ChangeToAsync<TestAsyncState>();

        await _stateMachine.CanChangeToAsync<TestStateV3>();
        Assert.That(state1.CanTransitionToCallCount, Is.EqualTo(1));
    }


    /// <summary>
    ///     验证异步状态切换能够正确设置当前状态
    /// </summary>
    [Test]
    public async Task ChangeToAsync_Should_SetCurrentState()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        await _stateMachine.ChangeToAsync<TestStateV2>();

        Assert.That(_stateMachine.Current, Is.SameAs(state));
    }

    /// <summary>
    ///     验证异步状态切换对于异步状态调用OnEnterAsync
    /// </summary>
    [Test]
    public async Task ChangeToAsync_Should_Invoke_OnEnterAsync_ForAsyncState()
    {
        var state = new TestAsyncState();
        _stateMachine.Register(state);
        await _stateMachine.ChangeToAsync<TestAsyncState>();

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.Null);
    }

    /// <summary>
    ///     验证异步状态切换对于同步状态调用OnEnter
    /// </summary>
    [Test]
    public async Task ChangeToAsync_Should_Invoke_OnEnter_ForSyncState()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        await _stateMachine.ChangeToAsync<TestStateV2>();

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.Null);
    }

    /// <summary>
    ///     验证异步状态切换当存在当前异步状态时调用OnExitAsync
    /// </summary>
    [Test]
    public async Task ChangeToAsync_When_CurrentStateExists_WithAsyncState_Should_Invoke_OnExitAsync()
    {
        var state1 = new TestAsyncState();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        await _stateMachine.ChangeToAsync<TestAsyncState>();
        await _stateMachine.ChangeToAsync<TestStateV3>();

        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state1.ExitTo, Is.SameAs(state2));
    }

    /// <summary>
    ///     验证异步状态切换当存在当前同步状态时调用OnExit
    /// </summary>
    [Test]
    public async Task ChangeToAsync_When_CurrentStateExists_WithSyncState_Should_Invoke_OnExit()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        await _stateMachine.ChangeToAsync<TestStateV2>();
        await _stateMachine.ChangeToAsync<TestStateV3>();

        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state1.ExitTo, Is.SameAs(state2));
    }

    /// <summary>
    ///     验证异步状态切换到相同状态时不应调用回调方法
    /// </summary>
    [Test]
    public async Task ChangeToAsync_ToSameState_Should_NotInvoke_Callbacks()
    {
        var state = new TestStateV2();
        _stateMachine.Register(state);
        await _stateMachine.ChangeToAsync<TestStateV2>();

        var enterCount = state.EnterCallCount;
        var exitCount = state.ExitCallCount;

        await _stateMachine.ChangeToAsync<TestStateV2>();

        Assert.That(state.EnterCallCount, Is.EqualTo(enterCount));
        Assert.That(state.ExitCallCount, Is.EqualTo(exitCount));
    }

    /// <summary>
    ///     验证异步状态切换到未注册状态时应抛出InvalidOperationException
    /// </summary>
    [Test]
    public void ChangeToAsync_ToUnregisteredState_Should_ThrowInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => _stateMachine.ChangeToAsync<TestStateV2>());
    }

    /// <summary>
    ///     验证异步状态切换当当前状态拒绝转换时不应发生状态变化
    /// </summary>
    [Test]
    public async Task ChangeToAsync_WhenCurrentStateDeniesTransition_Should_NotChange()
    {
        var state1 = new TestStateV2 { AllowTransition = false };
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        await _stateMachine.ChangeToAsync<TestStateV2>();

        var oldState = _stateMachine.Current;
        var result = await _stateMachine.ChangeToAsync<TestStateV3>();

        Assert.That(result, Is.False);
        Assert.That(_stateMachine.Current, Is.SameAs(oldState));
        Assert.That(_stateMachine.Current, Is.SameAs(state1));
        Assert.That(state2.EnterCalled, Is.False);
    }

    /// <summary>
    ///     验证异步状态切换当当前异步状态拒绝转换时调用CanTransitionToAsync
    /// </summary>
    [Test]
    public async Task ChangeToAsync_WhenCurrentStateDeniesTransition_WithAsyncState_Should_Call_CanTransitionToAsync()
    {
        var state1 = new TestAsyncState { AllowTransitions = false };
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        await _stateMachine.ChangeToAsync<TestAsyncState>();

        await _stateMachine.ChangeToAsync<TestStateV3>();

        Assert.That(state1.CanTransitionToCallCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证异步回退能够返回到上一个状态
    /// </summary>
    [Test]
    public async Task GoBackAsync_Should_ReturnTo_PreviousState()
    {
        var state1 = new TestStateV2();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        await _stateMachine.ChangeToAsync<TestStateV2>();
        await _stateMachine.ChangeToAsync<TestStateV3>();
        var result = await _stateMachine.GoBackAsync();

        Assert.That(result, Is.True);
        Assert.That(_stateMachine.Current, Is.SameAs(state1));
    }

    /// <summary>
    ///     验证异步回退当没有历史记录时返回false
    /// </summary>
    [Test]
    public async Task GoBackAsync_WhenNoHistory_Should_ReturnFalse()
    {
        var result = await _stateMachine.GoBackAsync();
        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     验证异步回退调用正确的状态转换方法
    /// </summary>
    [Test]
    public async Task GoBackAsync_Should_Invoke_Correct_Transition_Methods()
    {
        var state1 = new TestAsyncState();
        var state2 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        await _stateMachine.ChangeToAsync<TestAsyncState>();
        await _stateMachine.ChangeToAsync<TestStateV3>();
        await _stateMachine.GoBackAsync();

        Assert.That(state2.ExitCalled, Is.True);
        Assert.That(state1.EnterCallCount, Is.EqualTo(2));
    }

    /// <summary>
    ///     验证从同步状态切换到异步状态能够正常工作
    /// </summary>
    [Test]
    public async Task ChangeToAsync_FromSyncToAsyncState_Should_Work()
    {
        var state1 = new TestStateV2();
        var state2 = new TestAsyncState();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        await _stateMachine.ChangeToAsync<TestStateV2>();
        await _stateMachine.ChangeToAsync<TestAsyncState>();

        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state2.EnterCalled, Is.True);
        Assert.That(_stateMachine.Current, Is.SameAs(state2));
    }

    /// <summary>
    ///     验证从异步状态切换到同步状态能够正常工作
    /// </summary>
    [Test]
    public async Task ChangeToAsync_FromAsyncToSyncState_Should_Work()
    {
        var state1 = new TestAsyncState();
        var state2 = new TestStateV2();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);

        await _stateMachine.ChangeToAsync<TestAsyncState>();
        await _stateMachine.ChangeToAsync<TestStateV2>();

        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state2.EnterCalled, Is.True);
        Assert.That(_stateMachine.Current, Is.SameAs(state2));
    }

    /// <summary>
    ///     验证多次异步状态转换应正确调用回调方法
    /// </summary>
    [Test]
    public async Task MultipleAsyncStateChanges_Should_Invoke_Callbacks_Correctly()
    {
        var state1 = new TestAsyncState();
        var state2 = new TestStateV2();
        var state3 = new TestStateV3();
        _stateMachine.Register(state1);
        _stateMachine.Register(state2);
        _stateMachine.Register(state3);

        await _stateMachine.ChangeToAsync<TestAsyncState>();
        await _stateMachine.ChangeToAsync<TestStateV2>();
        await _stateMachine.ChangeToAsync<TestStateV3>();

        Assert.That(state1.EnterCalled, Is.True);
        Assert.That(state1.ExitCalled, Is.True);
        Assert.That(state2.EnterCalled, Is.True);
        Assert.That(state2.ExitCalled, Is.True);
        Assert.That(state3.EnterCalled, Is.True);
        Assert.That(state3.ExitCalled, Is.False);
    }
}
