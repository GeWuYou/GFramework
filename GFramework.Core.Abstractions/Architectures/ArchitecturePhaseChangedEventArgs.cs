using GFramework.Core.Abstractions.Enums;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     表示架构阶段变化事件的数据。
///     该类型用于向事件订阅者传递当前已进入的阶段值。
/// </summary>
public sealed class ArchitecturePhaseChangedEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 <see cref="ArchitecturePhaseChangedEventArgs" /> 的新实例。
    /// </summary>
    /// <param name="phase">当前已进入的架构阶段。</param>
    public ArchitecturePhaseChangedEventArgs(ArchitecturePhase phase)
    {
        Phase = phase;
    }

    /// <summary>
    ///     获取当前已进入的架构阶段。
    /// </summary>
    public ArchitecturePhase Phase { get; }
}
