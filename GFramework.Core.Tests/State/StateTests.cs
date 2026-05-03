// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.State;
using NUnit.Framework;

namespace GFramework.Core.Tests.State;

/// <summary>
///     测试状态模式实现的功能和行为
/// </summary>
[TestFixture]
public class StateTests
{
    /// <summary>
    ///     验证状态类是否正确实现了IState接口
    /// </summary>
    [Test]
    public void State_Should_Implement_IState_Interface()
    {
        var state = new ConcreteStateV2();

        Assert.That(state is IState);
    }

    /// <summary>
    ///     验证进入状态时OnEnter方法被正确调用并记录来源状态
    /// </summary>
    [Test]
    public void OnEnter_Should_BeCalled_When_State_Enters()
    {
        var state = new ConcreteStateV2();
        var otherState = new ConcreteStateV3();

        state.OnEnter(otherState);

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.SameAs(otherState));
    }

    /// <summary>
    ///     验证当传入null作为来源状态时的处理情况
    /// </summary>
    [Test]
    public void OnEnter_WithNull_Should_Set_EnterFrom_ToNull()
    {
        var state = new ConcreteStateV2();

        state.OnEnter(null);

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.Null);
    }

    /// <summary>
    ///     验证退出状态时OnExit方法被正确调用并记录目标状态
    /// </summary>
    [Test]
    public void OnExit_Should_BeCalled_When_State_Exits()
    {
        var state = new ConcreteStateV2();
        var otherState = new ConcreteStateV3();

        state.OnExit(otherState);

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.SameAs(otherState));
    }

    /// <summary>
    ///     验证当传入null作为目标状态时的处理情况
    /// </summary>
    [Test]
    public void OnExit_WithNull_Should_Set_ExitTo_ToNull()
    {
        var state = new ConcreteStateV2();

        state.OnExit(null);

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.Null);
    }

    /// <summary>
    ///     验证允许转换时CanTransitionTo方法返回true
    /// </summary>
    [Test]
    public void CanTransitionTo_WithAllowTrue_Should_ReturnTrue()
    {
        var state = new ConcreteStateV2 { AllowTransitions = true };
        var target = new ConcreteStateV3();

        var result = state.CanTransitionTo(target);

        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     验证不允许转换时CanTransitionTo方法返回false
    /// </summary>
    [Test]
    public void CanTransitionTo_WithAllowFalse_Should_ReturnFalse()
    {
        var state = new ConcreteStateV2 { AllowTransitions = false };
        var target = new ConcreteStateV3();

        var result = state.CanTransitionTo(target);

        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     验证CanTransitionTo方法正确接收目标状态参数
    /// </summary>
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

    /// <summary>
    ///     验证具有复杂转换规则的状态类功能
    /// </summary>
    [Test]
    public void State_WithComplexTransitionRules_Should_Work()
    {
        var state1 = new ConditionalStateV2 { AllowedTransitions = new[] { typeof(ConcreteStateV3) } };
        var state2 = new ConcreteStateV3();
        var state3 = new ConcreteStateV4();

        Assert.That(state1.CanTransitionTo(state2), Is.True);
        Assert.That(state1.CanTransitionTo(state3), Is.False);
    }

    /// <summary>
    ///     验证多个状态之间的协作功能
    /// </summary>
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

    /// <summary>
    ///     验证状态对多次转换的跟踪能力
    /// </summary>
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

    /// <summary>
    ///     验证相同类型状态间的转换处理
    /// </summary>
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

    /// <summary>
    ///     验证异步状态类是否正确实现了IAsyncState接口
    /// </summary>
    [Test]
    public void AsyncState_Should_Implement_IAsyncState_Interface()
    {
        var state = new ConcreteAsyncStateV2();

        Assert.That(state is IAsyncState);
    }

    /// <summary>
    ///     验证进入异步状态时OnEnterAsync方法被正确调用并记录来源状态
    /// </summary>
    [Test]
    public async Task OnEnterAsync_Should_BeCalled_When_AsyncState_Enters()
    {
        var state = new ConcreteAsyncStateV2();
        var otherState = new ConcreteStateV3();

        await state.OnEnterAsync(otherState);

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.SameAs(otherState));
    }

    /// <summary>
    ///     验证当传入null作为来源状态时异步状态的处理情况
    /// </summary>
    [Test]
    public async Task OnEnterAsync_WithNull_Should_Set_EnterFrom_ToNull()
    {
        var state = new ConcreteAsyncStateV2();

        await state.OnEnterAsync(null);

        Assert.That(state.EnterCalled, Is.True);
        Assert.That(state.EnterFrom, Is.Null);
    }

    /// <summary>
    ///     验证退出异步状态时OnExitAsync方法被正确调用并记录目标状态
    /// </summary>
    [Test]
    public async Task OnExitAsync_Should_BeCalled_When_AsyncState_Exits()
    {
        var state = new ConcreteAsyncStateV2();
        var otherState = new ConcreteStateV3();

        await state.OnExitAsync(otherState);

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.SameAs(otherState));
    }

    /// <summary>
    ///     验证当传入null作为目标状态时异步状态的处理情况
    /// </summary>
    [Test]
    public async Task OnExitAsync_WithNull_Should_Set_ExitTo_ToNull()
    {
        var state = new ConcreteAsyncStateV2();

        await state.OnExitAsync(null);

        Assert.That(state.ExitCalled, Is.True);
        Assert.That(state.ExitTo, Is.Null);
    }

    /// <summary>
    ///     验证允许转换时CanTransitionToAsync方法返回true
    /// </summary>
    [Test]
    public async Task CanTransitionToAsync_WithAllowTrue_Should_ReturnTrue()
    {
        var state = new ConcreteAsyncStateV2 { AllowTransitions = true };
        var target = new ConcreteStateV3();

        var result = await state.CanTransitionToAsync(target);

        Assert.That(result, Is.True);
    }

    /// <summary>
    ///     验证不允许转换时CanTransitionToAsync方法返回false
    /// </summary>
    [Test]
    public async Task CanTransitionToAsync_WithAllowFalse_Should_ReturnFalse()
    {
        var state = new ConcreteAsyncStateV2 { AllowTransitions = false };
        var target = new ConcreteStateV3();

        var result = await state.CanTransitionToAsync(target);

        Assert.That(result, Is.False);
    }

    /// <summary>
    ///     验证CanTransitionToAsync方法正确接收目标状态参数
    /// </summary>
    [Test]
    public async Task CanTransitionToAsync_Should_Receive_TargetState()
    {
        var state = new ConcreteAsyncStateV2 { AllowTransitions = true };
        var target = new ConcreteStateV3();
        IState? receivedTarget = null;

        state.CanTransitionToAsyncAction = s => receivedTarget = s;
        await state.CanTransitionToAsync(target);

        Assert.That(receivedTarget, Is.SameAs(target));
    }

    /// <summary>
    ///     验证异步状态对多次转换的跟踪能力
    /// </summary>
    [Test]
    public async Task AsyncState_Should_Track_MultipleTransitions()
    {
        var state = new ConcreteAsyncStateV2();
        var other = new ConcreteStateV3();

        await state.OnEnterAsync(other);
        await state.OnExitAsync(other);
        await state.OnEnterAsync(other);
        await state.OnExitAsync(null);

        Assert.That(state.EnterCallCount, Is.EqualTo(2));
        Assert.That(state.ExitCallCount, Is.EqualTo(2));
    }
}
