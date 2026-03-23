using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.StateManagement;
using GFramework.Core.Events;

namespace GFramework.Core.StateManagement;

/// <summary>
///     集中式状态容器的默认实现，用于统一管理复杂状态树的读取、归约和订阅通知。
///     该类型定位于现有 BindableProperty 之上的可选能力，适合跨模块共享、需要统一变更入口
///     或需要中间件/诊断能力的状态场景，而不是替代所有简单字段级响应式属性。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class Store<TState> : IStore<TState>, IStoreDiagnostics<TState>
{
    /// <summary>
    ///     Dispatch 串行化门闩。
    ///     该锁保证任意时刻只有一个 action 管线在运行，从而保持状态演进顺序确定，
    ///     同时避免让耗时 middleware / reducer 长时间占用状态锁。
    /// </summary>
    private readonly object _dispatchGate = new();

    /// <summary>
    ///     当前状态变化订阅者列表。
    ///     使用显式订阅对象而不是委托链，便于处理原子初始化订阅、挂起补发和精确解绑。
    /// </summary>
    private readonly List<ListenerSubscription> _listeners = [];

    /// <summary>
    ///     Store 内部所有可变状态的同步锁。
    ///     该锁仅保护状态快照、订阅集合、缓存选择视图和注册表本身的短临界区访问。
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    ///     已注册的中间件链，按添加顺序执行。
    ///     每个条目都持有稳定身份，便于通过注销句柄精确移除而不影响其他同类中间件。
    ///     Dispatch 开始时会抓取快照，因此运行中的分发不会受到后续注册变化影响。
    /// </summary>
    private readonly List<MiddlewareRegistration> _middlewares = [];

    /// <summary>
    ///     按 action 具体运行时类型组织的 reducer 注册表。
    ///     Store 采用精确类型匹配策略，保证 reducer 执行顺序和行为保持确定性。
    ///     每个 reducer 通过注册条目获得稳定身份，以支持运行时精确注销。
    ///     Dispatch 开始时会抓取对应 action 类型的 reducer 快照。
    /// </summary>
    private readonly Dictionary<Type, List<ReducerRegistration>> _reducers = [];

    /// <summary>
    ///     已缓存的局部状态选择视图。
    ///     该缓存用于避免高频访问的 Model 属性在每次 getter 调用时都创建新的选择对象。
    /// </summary>
    private readonly Dictionary<string, object> _selectionCache = [];

    /// <summary>
    ///     用于判断状态是否发生有效变化的比较器。
    /// </summary>
    private readonly IEqualityComparer<TState> _stateComparer;

    /// <summary>
    ///     标记当前 Store 是否正在执行分发。
    ///     该标记用于阻止同一 Store 的重入分发，避免产生难以推导的执行顺序和状态回滚问题。
    /// </summary>
    private bool _isDispatching;

    /// <summary>
    ///     最近一次分发的 action 类型。
    /// </summary>
    private Type? _lastActionType;

    /// <summary>
    ///     最近一次分发记录。
    /// </summary>
    private StoreDispatchRecord<TState>? _lastDispatchRecord;

    /// <summary>
    ///     最近一次真正改变状态的时间戳。
    /// </summary>
    private DateTimeOffset? _lastStateChangedAt;

    /// <summary>
    ///     当前 Store 持有的状态快照。
    /// </summary>
    private TState _state;

    /// <summary>
    ///     初始化一个新的 Store。
    /// </summary>
    /// <param name="initialState">Store 的初始状态。</param>
    /// <param name="comparer">状态比较器；未提供时使用 <see cref="EqualityComparer{T}.Default"/>。</param>
    public Store(TState initialState, IEqualityComparer<TState>? comparer = null)
    {
        _state = initialState;
        _stateComparer = comparer ?? EqualityComparer<TState>.Default;
    }

    /// <summary>
    ///     获取当前状态快照。
    /// </summary>
    public TState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    ///     分发一个 action 并按顺序执行匹配的 reducer。
    /// </summary>
    /// <typeparam name="TAction">action 的具体类型。</typeparam>
    /// <param name="action">要分发的 action。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="InvalidOperationException">当同一 Store 发生重入分发时抛出。</exception>
    public void Dispatch<TAction>(TAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Action<TState>[] listenersSnapshot = Array.Empty<Action<TState>>();
        IStoreMiddleware<TState>[] middlewaresSnapshot = Array.Empty<IStoreMiddleware<TState>>();
        IStoreReducerAdapter[] reducersSnapshot = Array.Empty<IStoreReducerAdapter>();
        IEqualityComparer<TState> stateComparerSnapshot = _stateComparer;
        StoreDispatchContext<TState>? context = null;
        var enteredDispatchScope = false;

        lock (_dispatchGate)
        {
            try
            {
                lock (_lock)
                {
                    EnsureNotDispatching();
                    _isDispatching = true;
                    enteredDispatchScope = true;
                    context = new StoreDispatchContext<TState>(action!, _state);
                    stateComparerSnapshot = _stateComparer;
                    middlewaresSnapshot = CreateMiddlewareSnapshot();
                    reducersSnapshot = CreateReducerSnapshot(context.ActionType);
                }

                // middleware 和 reducer 可能包含较重的同步逻辑，因此仅持有 dispatch 串行门，
                // 不占用状态锁，让读取、订阅和注册操作只在需要访问共享状态时短暂阻塞。
                ExecuteDispatchPipeline(context, middlewaresSnapshot, reducersSnapshot, stateComparerSnapshot);

                lock (_lock)
                {
                    _lastActionType = context.ActionType;
                    _lastDispatchRecord = new StoreDispatchRecord<TState>(
                        context.Action,
                        context.PreviousState,
                        context.NextState,
                        context.HasStateChanged,
                        context.DispatchedAt);

                    if (!context.HasStateChanged)
                    {
                        return;
                    }

                    _state = context.NextState;
                    _lastStateChangedAt = context.DispatchedAt;
                    listenersSnapshot = SnapshotListenersForNotification(context.NextState);
                }
            }
            finally
            {
                if (enteredDispatchScope)
                {
                    lock (_lock)
                    {
                        _isDispatching = false;
                    }
                }
            }
        }

        // 始终在锁外通知订阅者，避免监听器内部读取 Store 或执行额外逻辑时产生死锁。
        foreach (var listener in listenersSnapshot)
        {
            listener(context!.NextState);
        }
    }

    /// <summary>
    ///     订阅状态变化通知。
    /// </summary>
    /// <param name="listener">状态变化时的监听器。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="listener"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister Subscribe(Action<TState> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        var subscription = new ListenerSubscription(listener);

        lock (_lock)
        {
            _listeners.Add(subscription);
        }

        return new DefaultUnRegister(() => UnSubscribe(subscription));
    }

    /// <summary>
    ///     订阅状态变化通知，并立即回放当前状态。
    /// </summary>
    /// <param name="listener">状态变化时的监听器。</param>
    /// <returns>用于取消订阅的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="listener"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister SubscribeWithInitValue(Action<TState> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        var subscription = new ListenerSubscription(listener)
        {
            IsActive = false
        };
        TState currentState;
        TState? pendingState = default;
        var hasPendingState = false;

        lock (_lock)
        {
            currentState = _state;
            _listeners.Add(subscription);
        }

        try
        {
            listener(currentState);
        }
        catch
        {
            UnSubscribe(subscription);
            throw;
        }

        lock (_lock)
        {
            if (!subscription.IsSubscribed)
            {
                return new DefaultUnRegister(() => { });
            }

            subscription.IsActive = true;
            if (subscription.HasPendingState)
            {
                pendingState = subscription.PendingState;
                hasPendingState = true;
                subscription.HasPendingState = false;
                subscription.PendingState = default!;
            }
        }

        if (hasPendingState)
        {
            listener(pendingState!);
        }

        return new DefaultUnRegister(() => UnSubscribe(subscription));
    }

    /// <summary>
    ///     取消订阅指定监听器。
    /// </summary>
    /// <param name="listener">需要移除的监听器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="listener"/> 为 <see langword="null"/> 时抛出。</exception>
    public void UnSubscribe(Action<TState> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (_lock)
        {
            var index = _listeners.FindIndex(subscription => subscription.Listener == listener);
            if (index < 0)
            {
                return;
            }

            _listeners[index].IsSubscribed = false;
            _listeners.RemoveAt(index);
        }
    }

    /// <summary>
    ///     获取最近一次分发的 action 类型。
    /// </summary>
    public Type? LastActionType
    {
        get
        {
            lock (_lock)
            {
                return _lastActionType;
            }
        }
    }

    /// <summary>
    ///     获取最近一次真正改变状态的时间戳。
    /// </summary>
    public DateTimeOffset? LastStateChangedAt
    {
        get
        {
            lock (_lock)
            {
                return _lastStateChangedAt;
            }
        }
    }

    /// <summary>
    ///     获取当前订阅者数量。
    /// </summary>
    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _listeners.Count;
            }
        }
    }

    /// <summary>
    ///     获取最近一次分发记录。
    /// </summary>
    public StoreDispatchRecord<TState>? LastDispatchRecord
    {
        get
        {
            lock (_lock)
            {
                return _lastDispatchRecord;
            }
        }
    }

    /// <summary>
    ///     创建一个用于当前状态类型的 Store 构建器。
    /// </summary>
    /// <returns>新的 Store 构建器实例。</returns>
    public static StoreBuilder<TState> CreateBuilder()
    {
        return new StoreBuilder<TState>();
    }

    /// <summary>
    ///     注册一个强类型 reducer。
    ///     同一 action 类型可注册多个 reducer，它们会按照注册顺序依次归约状态。
    ///     该重载保留现有链式配置体验；若需要在运行时注销，请改用 <see cref="RegisterReducerHandle{TAction}(IReducer{TState, TAction})"/>。
    /// </summary>
    /// <typeparam name="TAction">reducer 处理的 action 类型。</typeparam>
    /// <param name="reducer">要注册的 reducer 实例。</param>
    /// <returns>当前 Store 实例，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="reducer"/> 为 <see langword="null"/> 时抛出。</exception>
    public Store<TState> RegisterReducer<TAction>(IReducer<TState, TAction> reducer)
    {
        RegisterReducerHandle(reducer);
        return this;
    }

    /// <summary>
    ///     使用委托快速注册一个 reducer。
    ///     该重载保留现有链式配置体验；若需要在运行时注销，请改用 <see cref="RegisterReducerHandle{TAction}(Func{TState, TAction, TState})"/>。
    /// </summary>
    /// <typeparam name="TAction">reducer 处理的 action 类型。</typeparam>
    /// <param name="reducer">执行归约的委托。</param>
    /// <returns>当前 Store 实例，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="reducer"/> 为 <see langword="null"/> 时抛出。</exception>
    public Store<TState> RegisterReducer<TAction>(Func<TState, TAction, TState> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);
        return RegisterReducer(new DelegateReducer<TAction>(reducer));
    }

    /// <summary>
    ///     注册一个强类型 reducer，并返回可用于注销该 reducer 的句柄。
    ///     该句柄只会移除当前这次注册，不会影响同一 action 类型下的其他 reducer。
    ///     若在 dispatch 进行中调用注销，当前这次 dispatch 仍会使用开始时抓取的 reducer 快照，
    ///     注销仅影响之后的新 dispatch。
    /// </summary>
    /// <typeparam name="TAction">reducer 处理的 action 类型。</typeparam>
    /// <param name="reducer">要注册的 reducer 实例。</param>
    /// <returns>用于注销当前 reducer 注册的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="reducer"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister RegisterReducerHandle<TAction>(IReducer<TState, TAction> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);

        var actionType = typeof(TAction);
        var registration = new ReducerRegistration(new ReducerAdapter<TAction>(reducer));

        lock (_lock)
        {
            if (!_reducers.TryGetValue(actionType, out var reducers))
            {
                reducers = [];
                _reducers[actionType] = reducers;
            }

            reducers.Add(registration);
        }

        return new DefaultUnRegister(() => UnRegisterReducer(actionType, registration));
    }

    /// <summary>
    ///     使用委托快速注册一个 reducer，并返回可用于注销该 reducer 的句柄。
    ///     适合测试代码或按场景临时挂载的状态逻辑。
    /// </summary>
    /// <typeparam name="TAction">reducer 处理的 action 类型。</typeparam>
    /// <param name="reducer">执行归约的委托。</param>
    /// <returns>用于注销当前 reducer 注册的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="reducer"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister RegisterReducerHandle<TAction>(Func<TState, TAction, TState> reducer)
    {
        ArgumentNullException.ThrowIfNull(reducer);
        return RegisterReducerHandle(new DelegateReducer<TAction>(reducer));
    }

    /// <summary>
    ///     添加一个 Store 中间件。
    ///     中间件按添加顺序包裹 reducer 执行，可用于日志、审计或调试。
    ///     该重载保留现有链式配置体验；若需要在运行时注销，请改用 <see cref="RegisterMiddleware"/>.
    /// </summary>
    /// <param name="middleware">要添加的中间件实例。</param>
    /// <returns>当前 Store 实例，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="middleware"/> 为 <see langword="null"/> 时抛出。</exception>
    public Store<TState> UseMiddleware(IStoreMiddleware<TState> middleware)
    {
        RegisterMiddleware(middleware);
        return this;
    }

    /// <summary>
    ///     注册一个 Store 中间件，并返回可用于注销该中间件的句柄。
    ///     中间件按注册顺序包裹 reducer 执行；注销只会移除当前这次注册。
    ///     若在 dispatch 进行中调用注销，当前这次 dispatch 仍会使用开始时抓取的中间件快照，
    ///     注销仅影响之后的新 dispatch。
    /// </summary>
    /// <param name="middleware">要注册的中间件实例。</param>
    /// <returns>用于注销当前中间件注册的句柄。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="middleware"/> 为 <see langword="null"/> 时抛出。</exception>
    public IUnRegister RegisterMiddleware(IStoreMiddleware<TState> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        var registration = new MiddlewareRegistration(middleware);

        lock (_lock)
        {
            _middlewares.Add(registration);
        }

        return new DefaultUnRegister(() => UnRegisterMiddleware(registration));
    }

    /// <summary>
    ///     获取或创建一个带缓存的局部状态选择视图。
    ///     对于会被频繁读取的 Model 只读属性，推荐使用该方法复用同一个选择实例。
    /// </summary>
    /// <typeparam name="TSelected">局部状态类型。</typeparam>
    /// <param name="key">缓存键，调用方应保证同一个键始终表示同一局部状态语义。</param>
    /// <param name="selector">状态选择委托。</param>
    /// <param name="comparer">用于比较局部状态是否变化的比较器。</param>
    /// <returns>稳定复用的选择视图实例。</returns>
    public StoreSelection<TState, TSelected> GetOrCreateSelection<TSelected>(
        string key,
        Func<TState, TSelected> selector,
        IEqualityComparer<TSelected>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(selector);

        lock (_lock)
        {
            if (_selectionCache.TryGetValue(key, out var existing))
            {
                if (existing is StoreSelection<TState, TSelected> cachedSelection)
                {
                    return cachedSelection;
                }

                throw new InvalidOperationException(
                    $"A cached selection with key '{key}' already exists with a different selected type.");
            }

            var selection = new StoreSelection<TState, TSelected>(this, selector, comparer);
            _selectionCache[key] = selection;
            return selection;
        }
    }

    /// <summary>
    ///     获取或创建一个带缓存的只读 BindableProperty 风格视图。
    /// </summary>
    /// <typeparam name="TSelected">局部状态类型。</typeparam>
    /// <param name="key">缓存键，调用方应保证同一个键始终表示同一局部状态语义。</param>
    /// <param name="selector">状态选择委托。</param>
    /// <param name="comparer">用于比较局部状态是否变化的比较器。</param>
    /// <returns>稳定复用的只读绑定视图。</returns>
    public StoreSelection<TState, TSelected> GetOrCreateBindableProperty<TSelected>(
        string key,
        Func<TState, TSelected> selector,
        IEqualityComparer<TSelected>? comparer = null)
    {
        return GetOrCreateSelection(key, selector, comparer);
    }

    /// <summary>
    ///     执行一次完整分发管线。
    /// </summary>
    /// <param name="context">当前分发上下文。</param>
    /// <param name="middlewares">本次分发使用的中间件快照。</param>
    /// <param name="reducers">本次分发使用的 reducer 快照。</param>
    /// <param name="stateComparer">本次分发使用的状态比较器快照。</param>
    private static void ExecuteDispatchPipeline(
        StoreDispatchContext<TState> context,
        IReadOnlyList<IStoreMiddleware<TState>> middlewares,
        IReadOnlyList<IStoreReducerAdapter> reducers,
        IEqualityComparer<TState> stateComparer)
    {
        Action pipeline = () => ApplyReducers(context, reducers, stateComparer);

        for (var i = middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = middlewares[i];
            var next = pipeline;
            pipeline = () => middleware.Invoke(context, next);
        }

        pipeline();
    }

    /// <summary>
    ///     对当前 action 应用所有匹配的 reducer。
    ///     reducer 使用 action 的精确运行时类型进行查找，以保证匹配结果和执行顺序稳定。
    /// </summary>
    /// <param name="context">当前分发上下文。</param>
    /// <param name="reducers">本次分发使用的 reducer 快照。</param>
    /// <param name="stateComparer">本次分发使用的状态比较器快照。</param>
    private static void ApplyReducers(
        StoreDispatchContext<TState> context,
        IReadOnlyList<IStoreReducerAdapter> reducers,
        IEqualityComparer<TState> stateComparer)
    {
        if (reducers.Count == 0)
        {
            context.NextState = context.PreviousState;
            context.HasStateChanged = false;
            return;
        }

        var nextState = context.PreviousState;

        // 多个 reducer 共享同一 action 类型时，后一个 reducer 以前一个 reducer 的输出作为输入，
        // 从而支持按模块拆分归约逻辑，同时保持总体状态演进顺序明确。
        foreach (var reducer in reducers)
        {
            nextState = reducer.Reduce(nextState, context.Action);
        }

        context.NextState = nextState;
        context.HasStateChanged = !stateComparer.Equals(context.PreviousState, nextState);
    }

    /// <summary>
    ///     确保当前 Store 没有发生重入分发。
    /// </summary>
    /// <exception cref="InvalidOperationException">当检测到重入分发时抛出。</exception>
    private void EnsureNotDispatching()
    {
        if (_isDispatching)
        {
            throw new InvalidOperationException("Nested dispatch on the same store is not allowed.");
        }
    }

    /// <summary>
    ///     从当前订阅集合中提取需要立即通知的监听器快照，并为尚未激活的初始化订阅保存待补发状态。
    /// </summary>
    /// <param name="nextState">本次分发后的最新状态。</param>
    /// <returns>需要在锁外立即调用的监听器快照。</returns>
    private Action<TState>[] SnapshotListenersForNotification(TState nextState)
    {
        if (_listeners.Count == 0)
        {
            return Array.Empty<Action<TState>>();
        }

        var activeListeners = new List<Action<TState>>(_listeners.Count);
        foreach (var subscription in _listeners)
        {
            if (!subscription.IsSubscribed)
            {
                continue;
            }

            if (subscription.IsActive)
            {
                activeListeners.Add(subscription.Listener);
                continue;
            }

            subscription.PendingState = nextState;
            subscription.HasPendingState = true;
        }

        return activeListeners.Count > 0 ? activeListeners.ToArray() : Array.Empty<Action<TState>>();
    }

    /// <summary>
    ///     为当前中间件链创建快照。
    ///     该方法自行获取状态锁，避免调用方必须记住“只能在已加锁条件下调用”这一隐含约束，
    ///     从而降低未来重构时把它误用到锁外路径中的风险。
    /// </summary>
    /// <returns>当前中间件链的快照；若未注册则返回空数组。</returns>
    private IStoreMiddleware<TState>[] CreateMiddlewareSnapshot()
    {
        lock (_lock)
        {
            if (_middlewares.Count == 0)
            {
                return Array.Empty<IStoreMiddleware<TState>>();
            }

            var snapshot = new IStoreMiddleware<TState>[_middlewares.Count];
            for (var i = 0; i < _middlewares.Count; i++)
            {
                snapshot[i] = _middlewares[i].Middleware;
            }

            return snapshot;
        }
    }

    /// <summary>
    ///     为当前 action 类型创建 reducer 快照。
    ///     该方法自行获取状态锁，避免让快照安全性依赖调用方的锁顺序知识。
    /// </summary>
    /// <param name="actionType">当前分发的 action 类型。</param>
    /// <returns>对应 action 类型的 reducer 快照；若未注册则返回空数组。</returns>
    private IStoreReducerAdapter[] CreateReducerSnapshot(Type actionType)
    {
        lock (_lock)
        {
            if (!_reducers.TryGetValue(actionType, out var reducers) || reducers.Count == 0)
            {
                return Array.Empty<IStoreReducerAdapter>();
            }

            var snapshot = new IStoreReducerAdapter[reducers.Count];
            for (var i = 0; i < reducers.Count; i++)
            {
                snapshot[i] = reducers[i].Adapter;
            }

            return snapshot;
        }
    }

    /// <summary>
    ///     解绑一个精确的订阅对象。
    /// </summary>
    /// <param name="subscription">要解绑的订阅对象。</param>
    private void UnSubscribe(ListenerSubscription subscription)
    {
        lock (_lock)
        {
            subscription.IsSubscribed = false;
            _listeners.Remove(subscription);
        }
    }

    /// <summary>
    ///     注销一个中间件注册条目。
    ///     仅精确移除与当前句柄关联的条目，避免误删同一实例的其他重复注册。
    /// </summary>
    /// <param name="registration">要移除的中间件注册条目。</param>
    private void UnRegisterMiddleware(MiddlewareRegistration registration)
    {
        lock (_lock)
        {
            _middlewares.Remove(registration);
        }
    }

    /// <summary>
    ///     注销一个 reducer 注册条目。
    ///     若该 action 类型下已无其他 reducer，则同时清理空注册桶，保持注册表紧凑。
    /// </summary>
    /// <param name="actionType">reducer 对应的 action 类型。</param>
    /// <param name="registration">要移除的 reducer 注册条目。</param>
    private void UnRegisterReducer(Type actionType, ReducerRegistration registration)
    {
        lock (_lock)
        {
            if (!_reducers.TryGetValue(actionType, out var reducers))
            {
                return;
            }

            reducers.Remove(registration);
            if (reducers.Count == 0)
            {
                _reducers.Remove(actionType);
            }
        }
    }

    /// <summary>
    ///     适配不同 action 类型 reducer 的内部统一接口。
    ///     Store 通过该接口在运行时按 action 具体类型执行 reducer，而不暴露内部装配细节。
    /// </summary>
    private interface IStoreReducerAdapter
    {
        /// <summary>
        ///     使用当前 action 对状态进行一次归约。
        /// </summary>
        /// <param name="currentState">当前状态。</param>
        /// <param name="action">分发中的 action。</param>
        /// <returns>归约后的下一状态。</returns>
        TState Reduce(TState currentState, object action);
    }

    /// <summary>
    ///     基于强类型 reducer 的适配器实现。
    ///     该适配器仅负责安全地完成 object 到 action 类型的转换，然后委托给真实 reducer。
    /// </summary>
    /// <typeparam name="TAction">当前适配器负责处理的 action 类型。</typeparam>
    private sealed class ReducerAdapter<TAction>(IReducer<TState, TAction> reducer) : IStoreReducerAdapter
    {
        /// <summary>
        ///     包装后的强类型 reducer 实例。
        /// </summary>
        private readonly IReducer<TState, TAction> _reducer =
            reducer ?? throw new ArgumentNullException(nameof(reducer));

        /// <summary>
        ///     将运行时 action 转换为强类型 action 后执行归约。
        /// </summary>
        /// <param name="currentState">当前状态。</param>
        /// <param name="action">运行时 action。</param>
        /// <returns>归约后的下一状态。</returns>
        public TState Reduce(TState currentState, object action)
        {
            return _reducer.Reduce(currentState, (TAction)action);
        }
    }

    /// <summary>
    ///     表示一条 reducer 注册记录。
    ///     该包装对象为运行时注销提供稳定身份，同时不改变 reducer 的执行顺序语义。
    /// </summary>
    private sealed class ReducerRegistration(IStoreReducerAdapter adapter)
    {
        /// <summary>
        ///     获取真正执行归约的内部适配器。
        /// </summary>
        public IStoreReducerAdapter Adapter { get; } = adapter;
    }

    /// <summary>
    ///     基于委托的 reducer 适配器实现，便于快速在测试和应用代码中声明 reducer。
    /// </summary>
    /// <typeparam name="TAction">当前适配器负责处理的 action 类型。</typeparam>
    private sealed class DelegateReducer<TAction>(Func<TState, TAction, TState> reducer) : IReducer<TState, TAction>
    {
        /// <summary>
        ///     真正执行归约的委托。
        /// </summary>
        private readonly Func<TState, TAction, TState> _reducer =
            reducer ?? throw new ArgumentNullException(nameof(reducer));

        /// <summary>
        ///     执行一次委托归约。
        /// </summary>
        /// <param name="currentState">当前状态。</param>
        /// <param name="action">当前 action。</param>
        /// <returns>归约后的下一状态。</returns>
        public TState Reduce(TState currentState, TAction action)
        {
            return _reducer(currentState, action);
        }
    }

    /// <summary>
    ///     表示一条中间件注册记录。
    ///     通过显式注册对象而不是直接存储中间件实例，可在重复注册同一实例时保持精确注销。
    /// </summary>
    private sealed class MiddlewareRegistration(IStoreMiddleware<TState> middleware)
    {
        /// <summary>
        ///     获取注册的中间件实例。
        /// </summary>
        public IStoreMiddleware<TState> Middleware { get; } = middleware;
    }

    /// <summary>
    ///     表示一个 Store 状态监听订阅。
    ///     该对象用于支持初始化回放与正式订阅之间的原子衔接，避免 SubscribeWithInitValue 漏掉状态变化。
    /// </summary>
    private sealed class ListenerSubscription(Action<TState> listener)
    {
        /// <summary>
        ///     获取订阅回调。
        /// </summary>
        public Action<TState> Listener { get; } = listener;

        /// <summary>
        ///     获取或设置订阅是否已激活。
        ///     非激活状态表示正在执行初始化回放，此时新的状态变化会被暂存为待补发值。
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        ///     获取或设置订阅是否仍然有效。
        /// </summary>
        public bool IsSubscribed { get; set; } = true;

        /// <summary>
        ///     获取或设置是否存在待补发的最新状态。
        /// </summary>
        public bool HasPendingState { get; set; }

        /// <summary>
        ///     获取或设置初始化阶段积累的最新状态。
        /// </summary>
        public TState PendingState { get; set; } = default!;
    }
}