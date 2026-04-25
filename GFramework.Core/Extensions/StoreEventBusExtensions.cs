using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.StateManagement;
using GFramework.Core.Events;
using GFramework.Core.StateManagement;

namespace GFramework.Core.Extensions;

/// <summary>
///     为 Store 提供到 EventBus 的兼容桥接扩展。
///     该扩展面向旧模块渐进迁移场景，使现有事件消费者可以继续观察 Store 的 action 分发和状态变化。
/// </summary>
public static class StoreEventBusExtensions
{
    /// <summary>
    ///     将 Store 的 dispatch 和状态变化同时桥接到 EventBus。
    ///     dispatch 事件会逐次发布；状态变化事件会复用 Store 自身的通知折叠语义，因此批处理中只发布最终状态。
    /// </summary>
    /// <typeparam name="TState">状态树的根状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="eventBus">目标事件总线。</param>
    /// <param name="publishDispatches">是否发布每次 action 分发事件。</param>
    /// <param name="publishStateChanges">是否发布状态变化事件。</param>
    /// <returns>用于拆除桥接的句柄。</returns>
    public static IUnRegister BridgeToEventBus<TState>(
        this Store<TState> store,
        IEventBus eventBus,
        bool publishDispatches = true,
        bool publishStateChanges = true)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        if (eventBus is null)
        {
            throw new ArgumentNullException(nameof(eventBus));
        }

        IUnRegister? dispatchBridge = null;
        IUnRegister? stateBridge = null;

        if (publishDispatches)
        {
            dispatchBridge = store.BridgeDispatchesToEventBus(eventBus);
        }

        if (publishStateChanges)
        {
            stateBridge = store.BridgeStateChangesToEventBus(eventBus);
        }

        return new DefaultUnRegister(() =>
        {
            dispatchBridge?.UnRegister();
            stateBridge?.UnRegister();
        });
    }

    /// <summary>
    ///     将 Store 的每次 dispatch 结果桥接到 EventBus。
    ///     该桥接通过中间件实现，因此即使某次分发未改变状态，也会发布对应的 dispatch 事件。
    /// </summary>
    /// <typeparam name="TState">状态树的根状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="eventBus">目标事件总线。</param>
    /// <returns>用于移除 dispatch 桥接中间件的句柄。</returns>
    public static IUnRegister BridgeDispatchesToEventBus<TState>(this Store<TState> store, IEventBus eventBus)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        if (eventBus is null)
        {
            throw new ArgumentNullException(nameof(eventBus));
        }

        return store.RegisterMiddleware(new DispatchEventBusMiddleware<TState>(eventBus));
    }

    /// <summary>
    ///     将 Store 的状态变化桥接到 EventBus。
    ///     该桥接复用 Store 的订阅通知语义，因此只会在状态真正变化时发布事件。
    /// </summary>
    /// <typeparam name="TState">状态树的根状态类型。</typeparam>
    /// <param name="store">源 Store。</param>
    /// <param name="eventBus">目标事件总线。</param>
    /// <returns>用于移除状态变化桥接的句柄。</returns>
    public static IUnRegister BridgeStateChangesToEventBus<TState>(this IReadonlyStore<TState> store,
        IEventBus eventBus)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        if (eventBus is null)
        {
            throw new ArgumentNullException(nameof(eventBus));
        }

        return store.Subscribe(state =>
            eventBus.Send(new StoreStateChangedEvent<TState>(state, DateTimeOffset.UtcNow)));
    }

    /// <summary>
    ///     用于把 dispatch 结果桥接到 EventBus 的内部中间件。
    ///     选择中间件而不是改写 Store 核心提交流程，是为了把兼容层成本保持在可选扩展中。
    /// </summary>
    /// <typeparam name="TState">状态树的根状态类型。</typeparam>
    private sealed class DispatchEventBusMiddleware<TState>(IEventBus eventBus) : IStoreMiddleware<TState>
    {
        /// <summary>
        ///     目标事件总线。
        /// </summary>
        private readonly IEventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

        /// <summary>
        ///     执行后续 dispatch 管线，并在结束后把分发结果发送到 EventBus。
        /// </summary>
        /// <param name="context">当前分发上下文。</param>
        /// <param name="next">后续管线。</param>
        public void Invoke(StoreDispatchContext<TState> context, Action next)
        {
            next();

            var dispatchRecord = new StoreDispatchRecord<TState>(
                context.Action,
                context.PreviousState,
                context.NextState,
                context.HasStateChanged,
                context.DispatchedAt);

            _eventBus.Send(new StoreDispatchedEvent<TState>(dispatchRecord));
        }
    }
}
