using GFramework.Core.Abstractions.state;

namespace GFramework.Core.state;

/// <summary>
///     表示状态变更事件的数据类
/// </summary>
public sealed class StateChangedEvent
{
    /// <summary>
    ///     获取变更前的旧状态
    /// </summary>
    public IState? OldState { get; init; }

    /// <summary>
    ///     获取变更后的新状态
    /// </summary>
    public IState? NewState { get; init; }
}