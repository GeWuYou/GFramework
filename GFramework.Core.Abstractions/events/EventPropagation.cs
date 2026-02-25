namespace GFramework.Core.Abstractions.events;

/// <summary>
///     事件传播模式
/// </summary>
public enum EventPropagation
{
    /// <summary>
    ///     传播到所有处理器
    /// </summary>
    All,

    /// <summary>
    ///     传播直到某个处理器标记为已处理
    /// </summary>
    UntilHandled,

    /// <summary>
    ///     仅传播到最高优先级的处理器
    /// </summary>
    Highest
}
