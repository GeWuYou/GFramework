using GFramework.Core.Abstractions.state;
using NUnit.Framework;

namespace GFramework.Core.Tests.state;

/// <summary>
/// 测试状态模式实现的功能和行为
/// </summary>
[TestFixture]
public class StateTests
{
    /// <summary>
    /// 验证状态类是否正确实现了IState接口
    /// </summary>
    [Test]
    public void State_Should_Implement_IState_Interface()
    {
        var state = new ConcreteStateV2();

        Assert.That(state is IState);
    }

    /// <summary>
    /// 验证进入状态时OnEnter方法被正确调用并记录来源状态
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
    /// 验证当传入null作为来源状态时的处理情况
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
    /// 验证退出状态时OnExit方法被正确调用并记录目标状态
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
    /// 验证当传入null作为目标状态时的处理情况
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
    /// 验证允许转换时CanTransitionTo方法返回true
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
    /// 验证不允许转换时CanTransitionTo方法返回false
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
    /// 验证CanTransitionTo方法正确接收目标状态参数
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
    /// 验证具有复杂转换规则的状态类功能
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
    /// 验证多个状态之间的协作功能
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
    /// 验证状态对多次转换的跟踪能力
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
    /// 验证相同类型状态间的转换处理
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
}

/// <summary>
/// 具体状态实现类V2版本，用于测试状态的基本功能
/// </summary>
public sealed class ConcreteStateV2 : IState
{
    /// <summary>
    /// 获取或设置是否允许转换
    /// </summary>
    public bool AllowTransitions { get; set; } = true;

    /// <summary>
    /// 获取进入状态是否被调用的标志
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    /// 获取退出状态是否被调用的标志
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    /// 获取进入此状态的来源状态
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    /// 获取从此状态退出的目标状态
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    /// 获取或设置转换到目标状态时执行的动作
    /// </summary>
    public Action<IState>? CanTransitionToAction { get; set; }

    /// <summary>
    /// 进入当前状态时调用的方法
    /// </summary>
    /// <param name="from">从哪个状态进入</param>
    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    /// <summary>
    /// 退出当前状态时调用的方法
    /// </summary>
    /// <param name="to">退出到哪个状态</param>
    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    /// <summary>
    /// 判断是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>如果可以转换则返回true，否则返回false</returns>
    public bool CanTransitionTo(IState target)
    {
        CanTransitionToAction?.Invoke(target);
        return AllowTransitions;
    }
}

/// <summary>
/// 具体状态实现类V3版本，用于测试状态的基本功能
/// </summary>
public sealed class ConcreteStateV3 : IState
{
    /// <summary>
    /// 获取进入状态是否被调用的标志
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    /// 获取退出状态是否被调用的标志
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    /// 获取进入此状态的来源状态
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    /// 获取从此状态退出的目标状态
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    /// 进入当前状态时调用的方法
    /// </summary>
    /// <param name="from">从哪个状态进入</param>
    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    /// <summary>
    /// 退出当前状态时调用的方法
    /// </summary>
    /// <param name="to">退出到哪个状态</param>
    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    /// <summary>
    /// 判断是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>如果可以转换则返回true，否则返回false</returns>
    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}

/// <summary>
/// 具体状态实现类V4版本，用于测试状态的基本功能
/// </summary>
public sealed class ConcreteStateV4 : IState
{
    /// <summary>
    /// 获取进入状态是否被调用的标志
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    /// 获取退出状态是否被调用的标志
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    /// 获取进入此状态的来源状态
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    /// 获取从此状态退出的目标状态
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    /// 进入当前状态时调用的方法
    /// </summary>
    /// <param name="from">从哪个状态进入</param>
    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    /// <summary>
    /// 退出当前状态时调用的方法
    /// </summary>
    /// <param name="to">退出到哪个状态</param>
    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    /// <summary>
    /// 判断是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>如果可以转换则返回true，否则返回false</returns>
    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}

/// <summary>
/// 条件状态实现类V2版本，支持基于类型的条件转换规则
/// </summary>
public sealed class ConditionalStateV2 : IState
{
    /// <summary>
    /// 获取或设置允许转换到的状态类型数组
    /// </summary>
    public Type[] AllowedTransitions { get; set; } = Array.Empty<Type>();

    /// <summary>
    /// 获取进入状态是否被调用的标志
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    /// 获取退出状态是否被调用的标志
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    /// 获取进入此状态的来源状态
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    /// 获取从此状态退出的目标状态
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    /// 进入当前状态时调用的方法
    /// </summary>
    /// <param name="from">从哪个状态进入</param>
    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterFrom = from;
    }

    /// <summary>
    /// 退出当前状态时调用的方法
    /// </summary>
    /// <param name="to">退出到哪个状态</param>
    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitTo = to;
    }

    /// <summary>
    /// 判断是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>如果目标状态类型在允许列表中则返回true，否则返回false</returns>
    public bool CanTransitionTo(IState target)
    {
        return AllowedTransitions.Contains(target.GetType());
    }
}

/// <summary>
/// 跟踪状态实现类V2版本，用于跟踪状态转换次数
/// </summary>
public sealed class TrackingStateV2 : IState
{
    /// <summary>
    /// 获取进入状态被调用的次数
    /// </summary>
    public int EnterCallCount { get; private set; }

    /// <summary>
    /// 获取退出状态被调用的次数
    /// </summary>
    public int ExitCallCount { get; private set; }

    /// <summary>
    /// 获取进入此状态的来源状态
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    /// 获取从此状态退出的目标状态
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    /// 进入当前状态时调用的方法
    /// </summary>
    /// <param name="from">从哪个状态进入</param>
    public void OnEnter(IState? from)
    {
        EnterCallCount++;
        EnterFrom = from;
    }

    /// <summary>
    /// 退出当前状态时调用的方法
    /// </summary>
    /// <param name="to">退出到哪个状态</param>
    public void OnExit(IState? to)
    {
        ExitCallCount++;
        ExitTo = to;
    }

    /// <summary>
    /// 判断是否可以转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>总是返回true</returns>
    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}