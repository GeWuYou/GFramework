using GFramework.Core.Abstractions.state;

namespace GFramework.Core.state;

/// <summary>
///     状态机实现类，用于管理状态的注册、切换和生命周期
///     同时支持同步状态(IState)和异步状态(IAsyncState)
/// </summary>
public class StateMachine(int maxHistorySize = 10) : IStateMachine
{
    private readonly object _lock = new();

    private readonly HashSet<IState> _registeredStates = [];
    private readonly Stack<IState> _stateHistory = new();
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    /// <summary>
    ///     存储所有已注册状态的字典，键为状态类型，值为状态实例
    /// </summary>
    protected readonly Dictionary<Type, IState> States = new();

    /// <summary>
    ///     获取当前激活的状态
    /// </summary>
    public IState? Current { get; protected set; }

    /// <summary>
    ///     注册一个状态到状态机中
    /// </summary>
    /// <param name="state">要注册的状态实例</param>
    public IStateMachine Register(IState state)
    {
        lock (_lock)
        {
            States[state.GetType()] = state;
            _registeredStates.Add(state);
        }

        return this;
    }

    /// <summary>
    ///     从状态机中注销指定类型的状态
    /// </summary>
    /// <typeparam name="T">要注销的状态类型</typeparam>
    public IStateMachine Unregister<T>() where T : IState
    {
        return UnregisterAsync<T>().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步注销指定类型的状态
    /// </summary>
    /// <typeparam name="T">要注销的状态类型</typeparam>
    public async Task<IStateMachine> UnregisterAsync<T>() where T : IState
    {
        await _transitionLock.WaitAsync();
        try
        {
            var stateToUnregister = PrepareUnregister<T>(out var isCurrentState);
            if (stateToUnregister == null) return this;

            if (isCurrentState)
            {
                await ExecuteExitAsync(Current!, null);
                Current = null;
            }

            CompleteUnregister(stateToUnregister);
            return this;
        }
        finally
        {
            _transitionLock.Release();
        }
    }


    /// <summary>
    ///     检查是否可以切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型</typeparam>
    /// <returns>如果可以切换则返回true，否则返回false</returns>
    public bool CanChangeTo<T>() where T : IState
    {
        return CanChangeToAsync<T>().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步检查是否可以切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型</typeparam>
    /// <returns>如果可以切换则返回true，否则返回false</returns>
    public async Task<bool> CanChangeToAsync<T>() where T : IState
    {
        await _transitionLock.WaitAsync();
        try
        {
            if (!States.TryGetValue(typeof(T), out var target))
                return false;

            if (Current == null) return true;

            return await CanTransitionToAsync(Current, target);
        }
        finally
        {
            _transitionLock.Release();
        }
    }


    /// <summary>
    ///     切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型</typeparam>
    /// <returns>如果成功切换则返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationException">当目标状态未注册时抛出</exception>
    public bool ChangeTo<T>() where T : IState
    {
        return ChangeToAsync<T>().GetAwaiter().GetResult();
    }


    /// <summary>
    ///     异步切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型</typeparam>
    /// <returns>如果成功切换则返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationException">当目标状态未注册时抛出</exception>
    public async Task<bool> ChangeToAsync<T>() where T : IState
    {
        await _transitionLock.WaitAsync();
        try
        {
            IState target;
            IState? currentSnapshot;

            lock (_lock)
            {
                if (!States.TryGetValue(typeof(T), out target!))
                    throw new InvalidOperationException($"State {typeof(T).Name} not registered.");

                currentSnapshot = Current;
            }

            if (currentSnapshot != null)
            {
                var canTransition = await CanTransitionToAsync(currentSnapshot, target);
                if (!canTransition)
                {
                    await OnTransitionRejectedAsync(currentSnapshot, target);
                    return false;
                }
            }

            await ChangeInternalAsync(target);
            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    ///     检查指定类型的状态是否已注册
    /// </summary>
    /// <typeparam name="T">要检查的状态类型</typeparam>
    /// <returns>如果状态已注册则返回true，否则返回false</returns>
    public bool IsRegistered<T>() where T : IState
    {
        return States.ContainsKey(typeof(T));
    }

    /// <summary>
    ///     获取指定类型的已注册状态实例
    /// </summary>
    /// <typeparam name="T">要获取的状态类型</typeparam>
    /// <returns>如果状态存在则返回对应实例，否则返回null</returns>
    public T? GetState<T>() where T : class, IState
    {
        return States.TryGetValue(typeof(T), out var state) ? state as T : null;
    }

    /// <summary>
    ///     获取所有已注册状态的类型集合
    /// </summary>
    /// <returns>包含所有已注册状态类型的枚举器</returns>
    public IEnumerable<Type> GetRegisteredStateTypes()
    {
        return States.Keys;
    }

    /// <summary>
    ///     获取上一个状态
    /// </summary>
    /// <returns>如果历史记录存在则返回上一个状态，否则返回null</returns>
    public IState? GetPreviousState()
    {
        lock (_lock)
        {
            return _stateHistory.Count > 0 ? _stateHistory.Peek() : null;
        }
    }

    /// <summary>
    ///     获取状态历史记录
    /// </summary>
    /// <returns>状态历史记录的只读副本，从最近到最远排序</returns>
    public IReadOnlyList<IState> GetStateHistory()
    {
        lock (_lock)
        {
            return _stateHistory.ToList().AsReadOnly();
        }
    }

    /// <summary>
    ///     回退到上一个状态
    /// </summary>
    /// <returns>如果成功回退则返回true，否则返回false</returns>
    public bool GoBack()
    {
        return GoBackAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步回退到上一个状态
    /// </summary>
    /// <returns>如果成功回退则返回true，否则返回false</returns>
    public async Task<bool> GoBackAsync()
    {
        await _transitionLock.WaitAsync();
        try
        {
            var previousState = FindValidPreviousState();
            if (previousState == null) return false;

            await ChangeInternalWithoutHistoryAsync(previousState);
            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    ///     清空状态历史记录
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            _stateHistory.Clear();
        }
    }

    /// <summary>
    ///     准备注销操作，返回要注销的状态
    /// </summary>
    private IState? PrepareUnregister<T>(out bool isCurrentState) where T : IState
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!States.TryGetValue(type, out var state))
            {
                isCurrentState = false;
                return null;
            }

            isCurrentState = Current == state;
            return state;
        }
    }

    /// <summary>
    ///     完成注销操作，清理历史记录和状态字典
    /// </summary>
    private void CompleteUnregister(IState stateToUnregister)
    {
        lock (_lock)
        {
            // 从历史记录中移除该状态的所有引用
            var tempStack = new Stack<IState>(_stateHistory.Reverse());
            _stateHistory.Clear();
            foreach (var historyState in tempStack.Where(s => s != stateToUnregister))
                _stateHistory.Push(historyState);

            States.Remove(stateToUnregister.GetType());
            _registeredStates.Remove(stateToUnregister);
        }
    }

    /// <summary>
    ///     查找有效的上一个状态（跳过已注销的状态）
    /// </summary>
    private IState? FindValidPreviousState()
    {
        lock (_lock)
        {
            while (_stateHistory.Count > 0)
            {
                var candidate = _stateHistory.Pop();

                // 使用 HashSet 快速检查，O(1) 复杂度
                if (_registeredStates.Contains(candidate))
                    return candidate;
            }

            return null;
        }
    }

    /// <summary>
    ///     异步内部状态切换方法（不记录历史），用于回退操作
    /// </summary>
    /// <param name="next">下一个状态实例</param>
    protected virtual async Task ChangeInternalWithoutHistoryAsync(IState next)
    {
        if (Current == next) return;

        var old = Current;
        await OnStateChangingAsync(old, next);

        await ExecuteExitAsync(old, next);
        Current = next;
        await ExecuteEnterAsync(Current, old);

        await OnStateChangedAsync(old, Current);
    }

    /// <summary>
    ///     异步内部状态切换方法，处理状态切换的核心逻辑
    /// </summary>
    /// <param name="next">下一个状态实例</param>
    protected virtual async Task ChangeInternalAsync(IState next)
    {
        if (Current == next) return;

        var old = Current;
        await OnStateChangingAsync(old, next);

        await ExecuteExitAsync(old, next);

        AddToHistory(old);

        Current = next;

        await ExecuteEnterAsync(Current, old);


        await OnStateChangedAsync(old, Current);
    }

    /// <summary>
    ///     将状态添加到历史记录
    /// </summary>
    private void AddToHistory(IState? state)
    {
        if (state == null) return;

        lock (_lock)
        {
            _stateHistory.Push(state);

            // 限制历史记录大小
            if (_stateHistory.Count > maxHistorySize)
            {
                var tempStack = new Stack<IState>(_stateHistory.Reverse().Skip(1));
                _stateHistory.Clear();
                foreach (var s in tempStack.Reverse())
                    _stateHistory.Push(s);
            }
        }
    }

    /// <summary>
    ///     执行状态进入逻辑（智能判断同步/异步）
    /// </summary>
    private static async Task ExecuteEnterAsync(IState? state, IState? from)
    {
        if (state == null) return;

        if (state is IAsyncState asyncState)
            await asyncState.OnEnterAsync(from);
        else
            state.OnEnter(from);
    }

    /// <summary>
    ///     执行状态退出逻辑（智能判断同步/异步）
    /// </summary>
    private static async Task ExecuteExitAsync(IState? state, IState? to)
    {
        if (state == null) return;

        if (state is IAsyncState asyncState)
            await asyncState.OnExitAsync(to);
        else
            state.OnExit(to);
    }

    /// <summary>
    ///     检查是否可以转换到目标状态（智能判断同步/异步）
    /// </summary>
    private static async Task<bool> CanTransitionToAsync(IState current, IState target)
    {
        if (current is IAsyncState asyncState)
            return await asyncState.CanTransitionToAsync(target);

        return current.CanTransitionTo(target);
    }

    /// <summary>
    ///     当状态转换被拒绝时的回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual void OnTransitionRejected(IState from, IState to)
    {
    }

    /// <summary>
    ///     当状态转换被拒绝时的异步回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual Task OnTransitionRejectedAsync(IState from, IState to)
    {
        OnTransitionRejected(from, to);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     当状态即将发生改变时的回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual void OnStateChanging(IState? from, IState to)
    {
    }

    /// <summary>
    ///     当状态即将发生改变时的异步回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual Task OnStateChangingAsync(IState? from, IState to)
    {
        OnStateChanging(from, to);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     当状态改变完成后的回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual void OnStateChanged(IState? from, IState? to)
    {
    }

    /// <summary>
    ///     当状态改变完成后的异步回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual Task OnStateChangedAsync(IState? from, IState? to)
    {
        OnStateChanged(from, to);
        return Task.CompletedTask;
    }
}