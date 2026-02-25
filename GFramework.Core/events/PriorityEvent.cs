using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
///     支持优先级的泛型事件类
/// </summary>
/// <typeparam name="T">事件回调函数的参数类型</typeparam>
public class PriorityEvent<T> : IEvent
{
    /// <summary>
    ///     事件处理器包装类，包含处理器和优先级
    /// </summary>
    private class EventHandler
    {
        public Action<T> Handler { get; }
        public int Priority { get; }

        public EventHandler(Action<T> handler, int priority)
        {
            Handler = handler;
            Priority = priority;
        }
    }

    /// <summary>
    ///     存储已注册的事件处理器列表
    /// </summary>
    private readonly List<EventHandler> _handlers = new();

    /// <summary>
    ///     标记事件是否已被处理（用于 UntilHandled 传播模式）
    /// </summary>
    private bool _handled;

    /// <summary>
    ///     显式实现 IEvent 接口中的 Register 方法
    /// </summary>
    /// <param name="onEvent">无参事件处理方法</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器</returns>
    IUnRegister IEvent.Register(Action onEvent)
    {
        return Register(_ => onEvent(), 0);
    }

    /// <summary>
    ///     注册一个事件监听器，默认优先级为 0
    /// </summary>
    /// <param name="onEvent">要注册的事件处理方法</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器</returns>
    public IUnRegister Register(Action<T> onEvent)
    {
        return Register(onEvent, 0);
    }

    /// <summary>
    ///     注册一个事件监听器，并指定优先级
    /// </summary>
    /// <param name="onEvent">要注册的事件处理方法</param>
    /// <param name="priority">优先级，数值越大优先级越高</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器</returns>
    public IUnRegister Register(Action<T> onEvent, int priority)
    {
        var handler = new EventHandler(onEvent, priority);
        _handlers.Add(handler);

        // 按优先级降序排序（高优先级在前）
        _handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return new DefaultUnRegister(() => UnRegister(onEvent));
    }

    /// <summary>
    ///     取消指定的事件监听器
    /// </summary>
    /// <param name="onEvent">需要被注销的事件处理方法</param>
    public void UnRegister(Action<T> onEvent)
    {
        _handlers.RemoveAll(h => h.Handler == onEvent);
    }

    /// <summary>
    ///     触发所有已注册的事件处理程序（默认传播模式：All）
    /// </summary>
    /// <param name="t">传递给事件处理程序的参数</param>
    public void Trigger(T t)
    {
        Trigger(t, EventPropagation.All);
    }

    /// <summary>
    ///     触发事件处理程序，并指定传播模式
    /// </summary>
    /// <param name="t">传递给事件处理程序的参数</param>
    /// <param name="propagation">事件传播模式</param>
    public void Trigger(T t, EventPropagation propagation)
    {
        _handled = false;

        switch (propagation)
        {
            case EventPropagation.All:
                // 触发所有处理器
                foreach (var handler in _handlers)
                {
                    handler.Handler.Invoke(t);
                }
                break;

            case EventPropagation.UntilHandled:
                // 触发直到某个处理器标记为已处理
                foreach (var handler in _handlers)
                {
                    handler.Handler.Invoke(t);
                    if (_handled) break;
                }
                break;

            case EventPropagation.Highest:
                // 仅触发最高优先级的处理器
                if (_handlers.Count > 0)
                {
                    var highestPriority = _handlers[0].Priority;
                    foreach (var handler in _handlers)
                    {
                        if (handler.Priority < highestPriority) break;
                        handler.Handler.Invoke(t);
                    }
                }
                break;
        }
    }

    /// <summary>
    ///     标记事件为已处理（用于 UntilHandled 传播模式）
    /// </summary>
    public void MarkAsHandled()
    {
        _handled = true;
    }
}
