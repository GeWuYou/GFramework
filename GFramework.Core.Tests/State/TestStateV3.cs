using GFramework.Core.Abstractions.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     为 <see cref="StateMachineTests" /> 提供始终允许切换的同步测试状态。
/// </summary>
public sealed class TestStateV3 : IState
{
    /// <summary>
    ///     获取进入状态是否已被调用。
    /// </summary>
    public bool EnterCalled { get; private set; }

    /// <summary>
    ///     获取离开状态是否已被调用。
    /// </summary>
    public bool ExitCalled { get; private set; }

    /// <summary>
    ///     获取进入回调被调用的次数。
    /// </summary>
    public int EnterCallCount { get; private set; }

    /// <summary>
    ///     获取离开回调被调用的次数。
    /// </summary>
    public int ExitCallCount { get; private set; }

    /// <summary>
    ///     获取最近一次进入时的来源状态。
    /// </summary>
    public IState? EnterFrom { get; private set; }

    /// <summary>
    ///     获取最近一次离开时的目标状态。
    /// </summary>
    public IState? ExitTo { get; private set; }

    /// <summary>
    ///     记录进入状态时的来源状态与调用次数。
    /// </summary>
    /// <param name="from">触发进入的来源状态。</param>
    public void OnEnter(IState? from)
    {
        EnterCalled = true;
        EnterCallCount++;
        EnterFrom = from;
    }

    /// <summary>
    ///     记录离开状态时的目标状态与调用次数。
    /// </summary>
    /// <param name="to">即将切换到的目标状态。</param>
    public void OnExit(IState? to)
    {
        ExitCalled = true;
        ExitCallCount++;
        ExitTo = to;
    }

    /// <summary>
    ///     始终允许切换到目标状态。
    /// </summary>
    /// <param name="target">目标状态。</param>
    /// <returns>始终返回 <see langword="true" />。</returns>
    public bool CanTransitionTo(IState target)
    {
        return true;
    }
}
