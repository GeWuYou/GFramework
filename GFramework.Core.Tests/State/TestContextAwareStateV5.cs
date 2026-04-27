using GFramework.Core.Abstractions.State;
using GFramework.Core.State;

namespace GFramework.Core.Tests.State;

/// <summary>
///     为 <see cref="StateMachineSystemTests" /> 提供最小化的上下文感知状态实现。
/// </summary>
public class TestContextAwareStateV5 : ContextAwareStateBase
{
    /// <summary>
    ///     进入状态时调用。该测试替身不需要额外行为。
    /// </summary>
    /// <param name="previous">前一个状态。</param>
    public override void OnEnter(IState? previous)
    {
    }

    /// <summary>
    ///     退出状态时调用。该测试替身不需要额外行为。
    /// </summary>
    /// <param name="next">下一个状态。</param>
    public override void OnExit(IState? next)
    {
    }
}
