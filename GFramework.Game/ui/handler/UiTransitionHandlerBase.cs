using GFramework.Game.Abstractions.enums;
using GFramework.Game.Abstractions.ui;

namespace GFramework.Game.ui.handler;

/// <summary>
///     UI切换处理器抽象基类，提供一些默认实现
/// </summary>
public abstract class UiTransitionHandlerBase : IUiTransitionHandler
{
    /// <summary>
    ///     处理器适用的阶段，默认为所有阶段
    /// </summary>
    public virtual UiTransitionPhases Phases => UiTransitionPhases.All;

    /// <summary>
    ///     优先级，需要在子类中实现
    /// </summary>
    public abstract int Priority { get; }

    /// <summary>
    ///     判断是否应该处理当前事件，默认返回true
    /// </summary>
    public virtual bool ShouldHandle(UiTransitionEvent @event, UiTransitionPhases phases)
    {
        return true;
    }

    /// <summary>
    ///     处理UI切换事件，需要在子类中实现
    /// </summary>
    public abstract Task HandleAsync(UiTransitionEvent @event, CancellationToken cancellationToken);
}