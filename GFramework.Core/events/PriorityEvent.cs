using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
///     支持优先级的泛型事件类
/// </summary>
/// <typeparam name="T">事件回调函数的参数类型</typeparam>
public class PriorityEvent<T> : IEvent
{
    /// <summary>
    ///     存储已注册的上下文事件处理器列表
    /// </summary>
    private readonly List<ContextEventHandler> _contextHandlers = new();

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
    ///     注册一个上下文事件监听器，并指定优先级
    /// </summary>
    /// <param name="onEvent">要注册的事件处理方法，接收 EventContext 参数</param>
    /// <param name="priority">优先级，数值越大优先级越高</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器</returns>
    public IUnRegister RegisterWithContext(Action<EventContext<T>> onEvent, int priority = 0)
    {
        var handler = new ContextEventHandler(onEvent, priority);
        _contextHandlers.Add(handler);

        // 按优先级降序排序（高优先级在前）
        _contextHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return new DefaultUnRegister(() => UnRegisterContext(onEvent));
    }

    /// <summary>
    ///     取消指定的上下文事件监听器
    /// </summary>
    /// <param name="onEvent">需要被注销的事件处理方法</param>
    public void UnRegisterContext(Action<EventContext<T>> onEvent)
    {
        _contextHandlers.RemoveAll(h => h.Handler == onEvent);
    }

    /// <summary>
    ///     触发事件处理程序，并指定传播模式
    /// </summary>
    /// <param name="t">传递给事件处理程序的参数</param>
    /// <param name="propagation">事件传播模式</param>
    public void Trigger(T t, EventPropagation propagation = EventPropagation.All)
    {
        _handled = false;

        switch (propagation)
        {
            case EventPropagation.All:
                // 使用快照避免迭代期间修改
                // 合并所有处理器并按优先级排序
                var allHandlersForAll = _handlers
                    .Select(h => (h.Priority, Handler: (Action?)(() => h.Handler.Invoke(t)),
                        ContextHandler: (Action<EventContext<T>>?)null, IsContext: false))
                    .Concat(_contextHandlers
                        .Select(h => (h.Priority, Handler: (Action?)null,
                            ContextHandler: (Action<EventContext<T>>?)h.Handler, IsContext: true)))
                    .OrderByDescending(h => h.Priority)
                    .ToList();

                var contextAll = new EventContext<T>(t);
                foreach (var item in allHandlersForAll)
                {
                    if (item.IsContext)
                    {
                        item.ContextHandler?.Invoke(contextAll);
                    }
                    else
                    {
                        item.Handler?.Invoke();
                    }
                }

                break;

            case EventPropagation.UntilHandled:
                // 合并所有处理器并按优先级排序
                var allHandlers = _handlers
                    .Select(h => (h.Priority, Handler: (Action?)(() => h.Handler.Invoke(t)),
                        ContextHandler: (Action<EventContext<T>>?)null, IsContext: false))
                    .Concat(_contextHandlers
                        .Select(h => (h.Priority, Handler: (Action?)null,
                            ContextHandler: (Action<EventContext<T>>?)h.Handler, IsContext: true)))
                    .OrderByDescending(h => h.Priority)
                    .ToList();

                var context = new EventContext<T>(t);
                foreach (var item in allHandlers)
                {
                    if (item.IsContext)
                    {
                        item.ContextHandler?.Invoke(context);
                        if (context.IsHandled) break;
                    }
                    else
                    {
                        item.Handler?.Invoke();
                        if (_handled) break; // 保持向后兼容
                    }
                }

                break;

            case EventPropagation.Highest:
                // 使用快照避免迭代期间修改
                var normalSnapshot = _handlers.ToArray();
                var contextSnapshot = _contextHandlers.ToArray();

                // 找到最高优先级
                var highestPriority = int.MinValue;
                if (normalSnapshot.Length > 0)
                    highestPriority = Math.Max(highestPriority, normalSnapshot[0].Priority);
                if (contextSnapshot.Length > 0)
                    highestPriority = Math.Max(highestPriority, contextSnapshot[0].Priority);

                if (highestPriority != int.MinValue)
                {
                    // 触发最高优先级的普通处理器
                    foreach (var handler in normalSnapshot)
                    {
                        if (handler.Priority < highestPriority) break;
                        handler.Handler.Invoke(t);
                    }

                    // 触发最高优先级的上下文处理器
                    var contextHighest = new EventContext<T>(t);
                    foreach (var handler in contextSnapshot)
                    {
                        if (handler.Priority < highestPriority) break;
                        handler.Handler.Invoke(contextHighest);
                    }
                }

                break;
        }
    }

    /// <summary>
    ///     事件处理器包装类，包含处理器和优先级
    /// </summary>
    private sealed class EventHandler(Action<T> handler, int priority)
    {
        public Action<T> Handler { get; } = handler;
        public int Priority { get; } = priority;
    }

    /// <summary>
    ///     上下文事件处理器包装类，包含处理器和优先级
    /// </summary>
    private sealed class ContextEventHandler(Action<EventContext<T>> handler, int priority)
    {
        public Action<EventContext<T>> Handler { get; } = handler;
        public int Priority { get; } = priority;
    }
}