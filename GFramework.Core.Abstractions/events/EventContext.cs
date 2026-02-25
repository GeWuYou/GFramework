namespace GFramework.Core.Abstractions.events;

/// <summary>
///     事件上下文，包装事件数据并提供控制方法
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
public class EventContext<T>(T data)
{
    /// <summary>
    ///     事件数据
    /// </summary>
    public T Data { get; } = data;

    /// <summary>
    ///     事件是否已被处理
    /// </summary>
    public bool IsHandled { get; private set; }

    /// <summary>
    ///     标记事件为已处理，停止后续传播（仅对 UntilHandled 模式有效）
    /// </summary>
    public void MarkAsHandled()
    {
        IsHandled = true;
    }
}