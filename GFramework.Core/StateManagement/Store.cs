using System.Diagnostics;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.StateManagement;
using GFramework.Core.Events;

namespace GFramework.Core.StateManagement;

/// <summary>
///     集中式状态容器的默认实现，用于统一管理复杂状态树的读取、归约和订阅通知。
///     该类型定位于现有 BindableProperty 之上的可选能力，适合跨模块共享、需要统一变更入口、
///     支持调试历史或需要中间件/诊断能力的状态场景，而不是替代所有简单字段级响应式属性。
/// </summary>
/// <typeparam name="TState">状态树的根状态类型。</typeparam>
public sealed class Store<TState> : IStore<TState>, IStoreDiagnostics<TState>
{
    /// <summary>
    ///     当前 Store 使用的 action 匹配策略。
    /// </summary>
    private readonly StoreActionMatchingMode _actionMatchingMode;

    /// <summary>
    ///     Dispatch 串行化门闩。
    ///     该锁保证任意时刻只有一个 action 管线或历史跳转在提交状态，从而保持状态演进顺序确定，
    ///     同时避免让耗时 middleware / reducer 长时间占用状态锁。
    /// </summary>
    private readonly object _dispatchGate = new();

    /// <summary>
    ///     历史快照缓冲区。
    ///     当容量为 0 时该集合始终为空；启用后会保留当前时间线上的最近若干个状态快照。
    /// </summary>
    private readonly List<StoreHistoryEntry<TState>> _history = [];

    /// <summary>
    ///     历史缓冲区容量。
    ///     该容量包含当前状态锚点，因此容量越小，可撤销的步数也越少。
    /// </summary>
    private readonly int _historyCapacity;

    /// <summary>
    ///     当前状态变化订阅者列表。
    ///     使用显式订阅对象而不是委托链，便于处理原子初始化订阅、挂起补发和精确解绑。
    /// </summary>
    private readonly List<ListenerSubscription> _listeners = [];

    /// <summary>
    ///     Store 内部所有可变状态的同步锁。
    ///     该锁仅保护状态快照、订阅集合、缓存选择视图、历史记录和注册表本身的短临界区访问。
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    ///     已注册的中间件链，按添加顺序执行。
    ///     每个条目都持有稳定身份，便于通过注销句柄精确移除而不影响其他同类中间件。
    ///     Dispatch 开始时会抓取快照，因此运行中的分发不会受到后续注册变化影响。
    /// </summary>
    private readonly List<MiddlewareRegistration> _middlewares = [];

    /// <summary>
    ///     按 action 注册类型组织的 reducer 注册表。
    ///     默认使用精确类型匹配；启用多态匹配时，会在分发时按确定性的优先级扫描可赋值类型。
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
    ///     当前批处理嵌套深度。
    ///     大于 0 时会推迟状态通知，直到最外层批处理结束。
    /// </summary>
    private int _batchDepth;

    /// <summary>
    ///     当前批处理中是否有待发送的最终状态通知。
    /// </summary>
    private bool _hasPendingBatchNotification;

    /// <summary>
    ///     当前历史游标位置。
    ///     当未启用历史记录时，该值保持为 -1。
    /// </summary>
    private int _historyIndex = -1;

    /// <summary>
    ///     标记当前 Store 是否正在执行分发。
    ///     该标记用于阻止同一 Store 的重入分发或在 dispatch 中执行历史跳转，避免产生难以推导的执行顺序。
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
    ///     reducer 注册序号。
    ///     该序号用于在多态匹配时为不同注册桶提供跨类型的稳定排序键。
    /// </summary>
    private long _nextReducerSequence;

    /// <summary>
    ///     当前批处理中最后一个应通知给订阅者的状态快照。
    /// </summary>
    private TState _pendingBatchState = default!;

    /// <summary>
    ///     当前 Store 持有的状态快照。
    /// </summary>
    private TState _state;

    /// <summary>
    ///     初始化一个新的 Store。
    /// </summary>
    /// <param name="initialState">Store 的初始状态。</param>
    /// <param name="comparer">状态比较器；未提供时使用 <see cref="EqualityComparer{T}.Default"/>。</param>
    /// <param name="historyCapacity">历史缓冲区容量；0 表示不启用历史记录。</param>
    /// <param name="actionMatchingMode">reducer 的 action 匹配策略。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="historyCapacity"/> 小于 0 时抛出。</exception>
    public Store(
        TState initialState,
        IEqualityComparer<TState>? comparer = null,
        int historyCapacity = 0,
        StoreActionMatchingMode actionMatchingMode = StoreActionMatchingMode.ExactTypeOnly)
    {
        if (historyCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(historyCapacity), historyCapacity,
                "History capacity cannot be negative.");
        }

        _state = initialState;
        _stateComparer = comparer ?? EqualityComparer<TState>.Default;
        _historyCapacity = historyCapacity;
        _actionMatchingMode = actionMatchingMode;

        if (_historyCapacity > 0)
        {
            ResetHistoryToCurrentState(DateTimeOffset.UtcNow);
        }
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
    ///     获取当前是否可以撤销到更早的历史状态。
    /// </summary>
    public bool CanUndo
    {
        get
        {
            lock (_lock)
            {
                return _historyCapacity > 0 && _historyIndex > 0;
            }
        }
    }

    /// <summary>
    ///     获取当前是否可以重做到更晚的历史状态。
    /// </summary>
    public bool CanRedo
    {
        get
        {
            lock (_lock)
            {
                return _historyCapacity > 0 && _historyIndex >= 0 && _historyIndex < _history.Count - 1;
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
        TState notificationState = default!;
        var hasNotification = false;
        var enteredDispatchScope = false;

        lock (_dispatchGate)
        {
            try
            {
                var context = EnterDispatchScope(
                    action,
                    out var middlewaresSnapshot,
                    out var reducersSnapshot,
                    out var stateComparerSnapshot);

                enteredDispatchScope = true;

                // middleware 和 reducer 可能包含较重的同步逻辑，因此仅持有 dispatch 串行门，
                // 不占用状态锁，让读取、订阅和注册操作只在需要访问共享状态时短暂阻塞。
                ExecuteDispatchPipeline(context, middlewaresSnapshot, reducersSnapshot, stateComparerSnapshot);

                hasNotification = TryCommitDispatchResult(context, out listenersSnapshot, out notificationState);
            }
            finally
            {
                if (enteredDispatchScope)
                {
                    ExitDispatchScope();
                }
            }
        }

        if (!hasNotification)
        {
            return;
        }

        NotifyListeners(listenersSnapshot, notificationState);
    }

    /// <summary>
    ///     将当前状态回退到上一个历史点。
    /// </summary>
    /// <exception cref="InvalidOperationException">当历史缓冲区未启用，或当前已经没有可撤销的历史点时抛出。</exception>
    public void Undo()
    {
        MoveToHistoryIndex(-1, isRelative: true, nameof(Undo), "No earlier history entry is available for undo.");
    }

    /// <summary>
    ///     将当前状态前进到下一个历史点。
    /// </summary>
    /// <exception cref="InvalidOperationException">当历史缓冲区未启用，或当前已经没有可重做的历史点时抛出。</exception>
    public void Redo()
    {
        MoveToHistoryIndex(1, isRelative: true, nameof(Redo), "No later history entry is available for redo.");
    }

    /// <summary>
    ///     跳转到指定索引的历史点。
    /// </summary>
    /// <param name="historyIndex">目标历史索引，从 0 开始。</param>
    /// <exception cref="InvalidOperationException">当历史缓冲区未启用时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="historyIndex"/> 超出历史范围时抛出。</exception>
    public void TimeTravelTo(int historyIndex)
    {
        MoveToHistoryIndex(historyIndex, isRelative: false, nameof(historyIndex), null);
    }

    /// <summary>
    ///     清空当前撤销/重做历史，并以当前状态作为新的历史锚点。
    /// </summary>
    public void ClearHistory()
    {
        lock (_dispatchGate)
        {
            lock (_lock)
            {
                EnsureNotDispatching();
                if (_historyCapacity == 0)
                {
                    return;
                }

                ResetHistoryToCurrentState(DateTimeOffset.UtcNow);
            }
        }
    }

    /// <summary>
    ///     将多个状态操作合并到一个批处理中执行。
    ///     批处理内的状态变化会立即提交，但通知会在最外层批处理结束后折叠为一次最终回放。
    /// </summary>
    /// <param name="batchAction">批处理主体。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="batchAction"/> 为 <see langword="null"/> 时抛出。</exception>
    public void RunInBatch(Action batchAction)
    {
        ArgumentNullException.ThrowIfNull(batchAction);

        lock (_lock)
        {
            _batchDepth++;
        }

        Action<TState>[] listenersSnapshot = Array.Empty<Action<TState>>();
        TState notificationState = default!;

        try
        {
            batchAction();
        }
        finally
        {
            lock (_lock)
            {
                if (_batchDepth == 0)
                {
                    Debug.Fail("Batch depth is already zero during RunInBatch cleanup.");
                }
                else
                {
                    _batchDepth--;
                    if (_batchDepth == 0 && _hasPendingBatchNotification)
                    {
                        notificationState = _pendingBatchState;
                        _pendingBatchState = default!;
                        _hasPendingBatchNotification = false;
                        listenersSnapshot = SnapshotListenersForNotification(notificationState);
                    }
                }
            }
        }

        if (listenersSnapshot.Length > 0)
        {
            NotifyListeners(listenersSnapshot, notificationState);
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
    ///     获取当前 Store 使用的 action 匹配策略。
    /// </summary>
    public StoreActionMatchingMode ActionMatchingMode => _actionMatchingMode;

    /// <summary>
    ///     获取历史缓冲区容量。
    /// </summary>
    public int HistoryCapacity => _historyCapacity;

    /// <summary>
    ///     获取当前可见历史记录数量。
    /// </summary>
    public int HistoryCount
    {
        get
        {
            lock (_lock)
            {
                return _history.Count;
            }
        }
    }

    /// <summary>
    ///     获取当前状态在历史缓冲区中的索引。
    /// </summary>
    public int HistoryIndex
    {
        get
        {
            lock (_lock)
            {
                return _historyIndex;
            }
        }
    }

    /// <summary>
    ///     获取当前是否处于批处理阶段。
    /// </summary>
    public bool IsBatching
    {
        get
        {
            lock (_lock)
            {
                return _batchDepth > 0;
            }
        }
    }

    /// <summary>
    ///     获取当前历史快照列表的只读快照。
    ///     该方法以方法语义显式表达会分配并返回集合副本，避免把快照克隆隐藏在属性访问中。
    /// </summary>
    /// <returns>当前历史快照列表；若未启用历史记录或当前没有历史，则返回空数组。</returns>
    public IReadOnlyList<StoreHistoryEntry<TState>> GetHistoryEntriesSnapshot()
    {
        lock (_lock)
        {
            return _history.Count == 0 ? Array.Empty<StoreHistoryEntry<TState>>() : _history.ToArray();
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
        ReducerRegistration registration;

        lock (_lock)
        {
            registration = new ReducerRegistration(new ReducerAdapter<TAction>(reducer), _nextReducerSequence++);

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
    ///     reducer 会按照预先计算好的稳定顺序执行，从而在多态匹配模式下仍保持确定性的状态演进。
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
    ///     进入一次新的 dispatch 作用域，并在状态锁内抓取本次执行所需的上下文快照。
    ///     该方法只做最短路径的共享状态访问，把 middleware/reducer 的实际执行留到锁外完成。
    /// </summary>
    /// <typeparam name="TAction">action 的具体类型。</typeparam>
    /// <param name="action">本次分发的 action。</param>
    /// <param name="middlewaresSnapshot">返回本次 dispatch 使用的中间件快照。</param>
    /// <param name="reducersSnapshot">返回本次 dispatch 使用的 reducer 快照。</param>
    /// <param name="stateComparerSnapshot">返回本次 dispatch 使用的状态比较器快照。</param>
    /// <returns>已初始化的 dispatch 上下文。</returns>
    private StoreDispatchContext<TState> EnterDispatchScope<TAction>(
        TAction action,
        out IStoreMiddleware<TState>[] middlewaresSnapshot,
        out IStoreReducerAdapter[] reducersSnapshot,
        out IEqualityComparer<TState> stateComparerSnapshot)
    {
        Debug.Assert(
            Monitor.IsEntered(_dispatchGate),
            "Caller must hold _dispatchGate before entering a dispatch scope.");

        lock (_lock)
        {
            EnsureNotDispatching();
            _isDispatching = true;

            try
            {
                var context = new StoreDispatchContext<TState>(action!, _state);
                stateComparerSnapshot = _stateComparer;
                middlewaresSnapshot = CreateMiddlewareSnapshotCore();
                reducersSnapshot = CreateReducerSnapshotCore(context.ActionType);
                return context;
            }
            catch
            {
                // 进入 dispatch 标记早于快照构建；如果这里抛异常，必须同步回滚标记，避免后续调用被永久判定为嵌套分发。
                _isDispatching = false;
                throw;
            }
        }
    }

    /// <summary>
    ///     在 dispatch 管线执行完成后提交诊断信息和状态变更。
    ///     状态与订阅集合的更新统一在该阶段完成，从而保证 dispatch 与 time-travel 共享同一提交流程。
    /// </summary>
    /// <param name="context">刚完成 middleware/reducer 管线的 dispatch 上下文。</param>
    /// <param name="listenersSnapshot">若需要立即通知，则返回锁外回放的监听器快照。</param>
    /// <param name="notificationState">若需要立即通知，则返回要通知的状态。</param>
    /// <returns>本次 dispatch 是否需要在锁外执行监听器通知。</returns>
    private bool TryCommitDispatchResult(
        StoreDispatchContext<TState> context,
        out Action<TState>[] listenersSnapshot,
        out TState notificationState)
    {
        Debug.Assert(
            Monitor.IsEntered(_dispatchGate),
            "Caller must hold _dispatchGate before committing a dispatch result.");

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
                listenersSnapshot = Array.Empty<Action<TState>>();
                notificationState = default!;
                return false;
            }

            ApplyCommittedStateChange(context.NextState, context.DispatchedAt, context.Action);
            listenersSnapshot = CaptureListenersOrDeferNotification(context.NextState, out notificationState);
            return listenersSnapshot.Length > 0;
        }
    }

    /// <summary>
    ///     退出当前 dispatch 作用域，允许后续 dispatch 或历史控制继续进入。
    /// </summary>
    private void ExitDispatchScope()
    {
        lock (_lock)
        {
            _isDispatching = false;
        }
    }

    /// <summary>
    ///     确保当前 Store 没有发生重入分发或在 dispatch 中执行历史控制。
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
    ///     将新的已提交状态应用到 Store。
    ///     该方法负责统一更新当前状态、最后变更时间和历史缓冲区，避免 dispatch 与 time-travel 路径产生分叉语义。
    /// </summary>
    /// <param name="nextState">要提交的新状态。</param>
    /// <param name="changedAt">状态生效时间。</param>
    /// <param name="action">触发该状态的 action；若本次变化不是由 dispatch 触发，则为 <see langword="null"/>。</param>
    private void ApplyCommittedStateChange(TState nextState, DateTimeOffset changedAt, object? action)
    {
        _state = nextState;
        _lastStateChangedAt = changedAt;
        RecordHistoryEntry(nextState, changedAt, action);
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
    ///     根据当前批处理状态决定是立即提取监听器快照，还是把通知折叠到批处理尾部。
    /// </summary>
    /// <param name="nextState">最新状态快照。</param>
    /// <param name="notificationState">若需要立即通知，则返回要回放给监听器的状态。</param>
    /// <returns>需要立即通知的监听器快照；若处于批处理阶段则返回空数组。</returns>
    private Action<TState>[] CaptureListenersOrDeferNotification(TState nextState, out TState notificationState)
    {
        if (_batchDepth > 0)
        {
            _pendingBatchState = nextState;
            _hasPendingBatchNotification = true;
            notificationState = default!;
            return Array.Empty<Action<TState>>();
        }

        notificationState = nextState;
        return SnapshotListenersForNotification(nextState);
    }

    /// <summary>
    ///     为当前中间件链创建快照。
    ///     调用该方法时必须已经持有状态锁，从而避免额外的锁重入和快照时序歧义。
    /// </summary>
    /// <returns>当前中间件链的快照；若未注册则返回空数组。</returns>
    private IStoreMiddleware<TState>[] CreateMiddlewareSnapshotCore()
    {
        Debug.Assert(
            Monitor.IsEntered(_lock),
            "Caller must hold _lock before invoking CreateMiddlewareSnapshotCore to avoid concurrency bugs.");

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

    /// <summary>
    ///     为当前 action 类型创建 reducer 快照。
    ///     在精确匹配模式下只读取一个注册桶；在多态模式下会按稳定排序规则合并可赋值的基类和接口注册。
    /// </summary>
    /// <param name="actionType">当前分发的 action 类型。</param>
    /// <returns>对应 action 类型的 reducer 快照；若未注册则返回空数组。</returns>
    private IStoreReducerAdapter[] CreateReducerSnapshotCore(Type actionType)
    {
        Debug.Assert(
            Monitor.IsEntered(_lock),
            "Caller must hold _lock before invoking CreateReducerSnapshotCore to avoid concurrency bugs.");

        if (_actionMatchingMode == StoreActionMatchingMode.ExactTypeOnly)
        {
            return CreateExactReducerSnapshot(actionType);
        }

        return CreateAssignableReducerSnapshot(actionType);
    }

    /// <summary>
    ///     为精确类型匹配模式创建 reducer 快照。
    /// </summary>
    /// <param name="actionType">当前 action 的运行时类型。</param>
    /// <returns>精确匹配到的 reducer 快照；若未注册则返回空数组。</returns>
    private IStoreReducerAdapter[] CreateExactReducerSnapshot(Type actionType)
    {
        if (!_reducers.TryGetValue(actionType, out var exactReducers) || exactReducers.Count == 0)
        {
            return Array.Empty<IStoreReducerAdapter>();
        }

        var exactSnapshot = new IStoreReducerAdapter[exactReducers.Count];
        for (var i = 0; i < exactReducers.Count; i++)
        {
            exactSnapshot[i] = exactReducers[i].Adapter;
        }

        return exactSnapshot;
    }

    /// <summary>
    ///     为多态匹配模式创建 reducer 快照。
    ///     该路径会收集所有可赋值的注册桶，并按“精确类型 -> 基类距离 -> 接口 -> 注册顺序”的稳定规则排序。
    /// </summary>
    /// <param name="actionType">当前 action 的运行时类型。</param>
    /// <returns>多态模式下的 reducer 快照；若未注册则返回空数组。</returns>
    private IStoreReducerAdapter[] CreateAssignableReducerSnapshot(Type actionType)
    {
        var matches = CollectReducerMatches(actionType);
        if (matches is null || matches.Count == 0)
        {
            return Array.Empty<IStoreReducerAdapter>();
        }

        matches.Sort(CompareReducerMatch);

        var snapshot = new IStoreReducerAdapter[matches.Count];
        for (var i = 0; i < matches.Count; i++)
        {
            snapshot[i] = matches[i].Adapter;
        }

        return snapshot;
    }

    /// <summary>
    ///     收集当前 action 类型可命中的 reducer 注册，并附带稳定排序所需的匹配元数据。
    /// </summary>
    /// <param name="actionType">当前 action 的运行时类型。</param>
    /// <returns>匹配结果列表；若没有任何匹配则返回 <see langword="null"/>。</returns>
    private List<ReducerMatch>? CollectReducerMatches(Type actionType)
    {
        List<ReducerMatch>? matches = null;

        foreach (var reducerBucket in _reducers)
        {
            if (!TryCreateReducerMatch(actionType, reducerBucket.Key, out var matchCategory,
                    out var inheritanceDistance))
            {
                continue;
            }

            matches ??= new List<ReducerMatch>();
            foreach (var registration in reducerBucket.Value)
            {
                matches.Add(new ReducerMatch(
                    registration.Adapter,
                    registration.Sequence,
                    matchCategory,
                    inheritanceDistance));
            }
        }

        return matches;
    }

    /// <summary>
    ///     比较两个 reducer 匹配结果的优先级，保证多态匹配下的执行顺序稳定可预测。
    /// </summary>
    /// <param name="left">左侧匹配结果。</param>
    /// <param name="right">右侧匹配结果。</param>
    /// <returns>排序比较结果。</returns>
    private static int CompareReducerMatch(ReducerMatch left, ReducerMatch right)
    {
        var categoryComparison = left.MatchCategory.CompareTo(right.MatchCategory);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        var distanceComparison = left.InheritanceDistance.CompareTo(right.InheritanceDistance);
        if (distanceComparison != 0)
        {
            return distanceComparison;
        }

        return left.Sequence.CompareTo(right.Sequence);
    }

    /// <summary>
    ///     判断指定 reducer 注册类型是否能匹配当前 action 类型，并给出用于稳定排序的分类信息。
    /// </summary>
    /// <param name="actionType">当前 action 的运行时类型。</param>
    /// <param name="registeredActionType">reducer 注册时声明的 action 类型。</param>
    /// <param name="matchCategory">匹配分类：0 为精确类型，1 为基类，2 为接口。</param>
    /// <param name="inheritanceDistance">继承距离，值越小表示越接近当前 action 类型。</param>
    /// <returns>若可以匹配则返回 <see langword="true"/>。</returns>
    private static bool TryCreateReducerMatch(
        Type actionType,
        Type registeredActionType,
        out int matchCategory,
        out int inheritanceDistance)
    {
        if (registeredActionType == actionType)
        {
            matchCategory = 0;
            inheritanceDistance = 0;
            return true;
        }

        if (!registeredActionType.IsAssignableFrom(actionType))
        {
            matchCategory = default;
            inheritanceDistance = default;
            return false;
        }

        if (registeredActionType.IsInterface)
        {
            matchCategory = 2;
            inheritanceDistance = 0;
            return true;
        }

        matchCategory = 1;
        inheritanceDistance = GetInheritanceDistance(actionType, registeredActionType);
        return true;
    }

    /// <summary>
    ///     计算当前 action 类型到目标基类的继承距离。
    ///     距离越小表示基类越接近当前 action 类型，在多态匹配排序中优先级越高。
    /// </summary>
    /// <param name="actionType">当前 action 的运行时类型。</param>
    /// <param name="registeredActionType">reducer 注册时声明的基类类型。</param>
    /// <returns>从当前 action 到目标基类的继承层级数。</returns>
    private static int GetInheritanceDistance(Type actionType, Type registeredActionType)
    {
        var distance = 0;
        var currentType = actionType;

        while (currentType != registeredActionType && currentType.BaseType is not null)
        {
            currentType = currentType.BaseType;
            distance++;
        }

        return distance;
    }

    /// <summary>
    ///     记录一条新的历史快照。
    ///     当当前游标不在时间线末尾时，会先裁掉 redo 分支，再追加新的状态快照。
    /// </summary>
    /// <param name="state">要记录的状态快照。</param>
    /// <param name="recordedAt">历史记录时间。</param>
    /// <param name="action">触发该状态的 action；若为空则表示当前变化不应写入历史。</param>
    private void RecordHistoryEntry(TState state, DateTimeOffset recordedAt, object? action)
    {
        if (_historyCapacity == 0)
        {
            return;
        }

        if (action is null)
        {
            return;
        }

        if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        _history.Add(new StoreHistoryEntry<TState>(state, recordedAt, action));
        _historyIndex = _history.Count - 1;

        if (_history.Count <= _historyCapacity)
        {
            return;
        }

        var overflow = _history.Count - _historyCapacity;
        _history.RemoveRange(0, overflow);
        _historyIndex = Math.Max(0, _historyIndex - overflow);
    }

    /// <summary>
    ///     将当前状态重置为新的历史锚点。
    ///     该操作用于 Store 初始化和显式清空历史后重新建立时间线起点。
    /// </summary>
    /// <param name="recordedAt">锚点记录时间。</param>
    private void ResetHistoryToCurrentState(DateTimeOffset recordedAt)
    {
        _history.Clear();
        _history.Add(new StoreHistoryEntry<TState>(_state, recordedAt));
        _historyIndex = 0;
    }

    /// <summary>
    ///     将当前状态移动到指定历史索引。
    ///     该方法统一承载 Undo、Redo 和显式时间旅行路径，确保通知与批处理语义保持一致。
    /// </summary>
    /// <param name="historyIndexOrOffset">目标索引或相对偏移量。</param>
    /// <param name="isRelative">是否按相对偏移量解释 <paramref name="historyIndexOrOffset"/>。</param>
    /// <param name="argumentName">参数名，用于异常信息。</param>
    /// <param name="emptyHistoryMessage">当相对跳转无可用历史时的错误信息；绝对跳转场景传 <see langword="null"/>。</param>
    private void MoveToHistoryIndex(
        int historyIndexOrOffset,
        bool isRelative,
        string argumentName,
        string? emptyHistoryMessage)
    {
        Action<TState>[] listenersSnapshot = Array.Empty<Action<TState>>();
        TState notificationState = default!;

        lock (_dispatchGate)
        {
            lock (_lock)
            {
                EnsureNotDispatching();
                EnsureHistoryEnabled();

                var targetIndex = isRelative ? _historyIndex + historyIndexOrOffset : historyIndexOrOffset;
                if (targetIndex < 0 || targetIndex >= _history.Count)
                {
                    if (isRelative)
                    {
                        throw new InvalidOperationException(emptyHistoryMessage);
                    }

                    throw new ArgumentOutOfRangeException(argumentName, historyIndexOrOffset,
                        "History index is out of range.");
                }

                if (targetIndex == _historyIndex)
                {
                    return;
                }

                _historyIndex = targetIndex;
                notificationState = _history[targetIndex].State;
                _state = notificationState;
                _lastStateChangedAt = DateTimeOffset.UtcNow;
                listenersSnapshot = CaptureListenersOrDeferNotification(notificationState, out notificationState);
            }
        }

        if (listenersSnapshot.Length > 0)
        {
            NotifyListeners(listenersSnapshot, notificationState);
        }
    }

    /// <summary>
    ///     确保当前 Store 已启用历史缓冲区。
    /// </summary>
    /// <exception cref="InvalidOperationException">当历史记录未启用时抛出。</exception>
    private void EnsureHistoryEnabled()
    {
        if (_historyCapacity == 0)
        {
            throw new InvalidOperationException("History is not enabled for this store.");
        }
    }

    /// <summary>
    ///     在锁外顺序通知监听器。
    ///     始终在锁外通知可避免监听器内部读取 Store 或执行额外逻辑时产生死锁。
    /// </summary>
    /// <param name="listenersSnapshot">监听器快照。</param>
    /// <param name="state">要回放给监听器的状态。</param>
    private static void NotifyListeners(Action<TState>[] listenersSnapshot, TState state)
    {
        foreach (var listener in listenersSnapshot)
        {
            listener(state);
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
    ///     该包装对象为运行时注销提供稳定身份，并携带全局序号以支撑多态匹配时的稳定排序。
    /// </summary>
    private sealed class ReducerRegistration(IStoreReducerAdapter adapter, long sequence)
    {
        /// <summary>
        ///     获取真正执行归约的内部适配器。
        /// </summary>
        public IStoreReducerAdapter Adapter { get; } = adapter;

        /// <summary>
        ///     获取该 reducer 的全局注册序号。
        /// </summary>
        public long Sequence { get; } = sequence;
    }

    /// <summary>
    ///     表示一次多态 reducer 匹配结果。
    ///     该结构在创建快照时缓存排序所需元数据，避免排序阶段重复计算类型关系。
    /// </summary>
    private sealed class ReducerMatch(
        IStoreReducerAdapter adapter,
        long sequence,
        int matchCategory,
        int inheritanceDistance)
    {
        /// <summary>
        ///     获取匹配到的 reducer 适配器。
        /// </summary>
        public IStoreReducerAdapter Adapter { get; } = adapter;

        /// <summary>
        ///     获取 reducer 的全局注册序号。
        /// </summary>
        public long Sequence { get; } = sequence;

        /// <summary>
        ///     获取匹配分类：0 为精确类型，1 为基类，2 为接口。
        /// </summary>
        public int MatchCategory { get; } = matchCategory;

        /// <summary>
        ///     获取继承距离。
        ///     该值越小表示注册类型越接近当前 action 类型。
        /// </summary>
        public int InheritanceDistance { get; } = inheritanceDistance;
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
