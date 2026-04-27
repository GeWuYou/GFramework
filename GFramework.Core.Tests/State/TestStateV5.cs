using GFramework.Core.Abstractions.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     为 <see cref="StateMachineSystemTests" /> 提供最小化的普通状态实现。
/// </summary>
public class TestStateV5 : IState
{
    /// <summary>
    ///     获取或设置测试状态标识符。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     判断是否允许转换到下一个状态。
    /// </summary>
    /// <param name="next">目标状态。</param>
    /// <returns>始终返回 <see langword="true" /> 以简化状态机切换测试。</returns>
    public bool CanTransitionTo(IState next)
    {
        return true;
    }

    /// <summary>
    ///     进入状态时调用。该测试替身不需要额外行为。
    /// </summary>
    /// <param name="previous">前一个状态。</param>
    public void OnEnter(IState? previous)
    {
    }

    /// <summary>
    ///     退出状态时调用。该测试替身不需要额外行为。
    /// </summary>
    /// <param name="next">下一个状态。</param>
    public void OnExit(IState? next)
    {
    }
}
