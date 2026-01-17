using GFramework.Core.Abstractions.state;

namespace GFramework.Core.state;

/// <summary>
/// 状态机实现类，用于管理状态的注册、切换和生命周期
/// </summary>
public class StateMachine(int maxHistorySize = 10) : IStateMachine
{
    private readonly object _lock = new();
    private readonly Stack<IState> _stateHistory = new();

    /// <summary>
    /// 存储所有已注册状态的字典，键为状态类型，值为状态实例
    /// </summary>
    protected readonly Dictionary<Type, IState> States = new();

    /// <summary>
    /// 获取当前激活的状态
    /// </summary>
    public IState? Current { get; protected set; }

    /// <summary>
    /// 注册一个状态到状态机中
    /// </summary>
    /// <param name="state">要注册的状态实例</param>
    public void Register(IState state)
    {
        lock (_lock)
        {
            States[state.GetType()] = state;
        }
    }

    /// <summary>
    /// 从状态机中注销指定类型的状态
    /// </summary>
    /// <typeparam name="T">要注销的状态类型</typeparam>
    public void Unregister<T>() where T : IState
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!States.TryGetValue(type, out var state)) return;

            // 如果当前状态是要注销的状态，则先执行退出逻辑
            if (Current == state)
            {
                Current.OnExit(null);
                Current = null;
            }

            // 从历史记录中移除该状态的所有引用
            var tempStack = new Stack<IState>(_stateHistory.Reverse());
            _stateHistory.Clear();
            foreach (var historyState in tempStack.Where(s => s != state))
            {
                _stateHistory.Push(historyState);
            }

            States.Remove(type);
        }
    }

    /// <summary>
    /// 检查是否可以切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型</typeparam>
    /// <returns>如果可以切换则返回true，否则返回false</returns>
    public bool CanChangeTo<T>() where T : IState
    {
        if (!States.TryGetValue(typeof(T), out var target))
            return false;

        return Current?.CanTransitionTo(target) ?? true;
    }

    /// <summary>
    /// 切换到指定类型的状态
    /// </summary>
    /// <typeparam name="T">目标状态类型</typeparam>
    /// <exception cref="InvalidOperationException">当目标状态未注册时抛出</exception>
    public void ChangeTo<T>() where T : IState
    {
        lock (_lock)
        {
            // 检查目标状态是否已注册
            if (!States.TryGetValue(typeof(T), out var target))
                throw new InvalidOperationException("State not registered.");

            // 验证当前状态是否可以转换到目标状态
            if (Current != null && !Current.CanTransitionTo(target))
                throw new InvalidOperationException(
                    $"Cannot transition from {Current.GetType().Name} to {typeof(T).Name}");

            ChangeInternal(target);
        }
    }

    /// <summary>
    /// 检查指定类型的状态是否已注册
    /// </summary>
    /// <typeparam name="T">要检查的状态类型</typeparam>
    /// <returns>如果状态已注册则返回true，否则返回false</returns>
    public bool IsRegistered<T>() where T : IState => States.ContainsKey(typeof(T));

    /// <summary>
    /// 获取指定类型的已注册状态实例
    /// </summary>
    /// <typeparam name="T">要获取的状态类型</typeparam>
    /// <returns>如果状态存在则返回对应实例，否则返回null</returns>
    public T? GetState<T>() where T : class, IState => States.TryGetValue(typeof(T), out var state) ? state as T : null;

    /// <summary>
    /// 获取所有已注册状态的类型集合
    /// </summary>
    /// <returns>包含所有已注册状态类型的枚举器</returns>
    public IEnumerable<Type> GetRegisteredStateTypes() => States.Keys;

    /// <summary>
    /// 获取上一个状态
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
    /// 获取状态历史记录
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
    /// 回退到上一个状态
    /// </summary>
    /// <returns>如果成功回退则返回true，否则返回false</returns>
    public bool GoBack()
    {
        lock (_lock)
        {
            if (_stateHistory.Count == 0) return false;

            var previousState = _stateHistory.Pop();

            // 检查上一个状态是否仍然注册
            if (!States.ContainsValue(previousState))
            {
                // 如果状态已被注销，继续尝试更早的状态
                return GoBack();
            }

            // 回退时不添加到历史记录
            ChangeInternalWithoutHistory(previousState);
            return true;
        }
    }

    /// <summary>
    /// 清空状态历史记录
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            _stateHistory.Clear();
        }
    }

    /// <summary>
    /// 内部状态切换方法（不记录历史），用于回退操作
    /// </summary>
    /// <param name="next">下一个状态实例</param>
    protected virtual void ChangeInternalWithoutHistory(IState next)
    {
        if (Current == next) return;

        var old = Current;
        OnStateChanging(old, next);

        old?.OnExit(next);
        Current = next;
        Current.OnEnter(old);

        OnStateChanged(old, Current);
    }

    /// <summary>
    /// 内部状态切换方法，处理状态切换的核心逻辑
    /// </summary>
    /// <param name="next">下一个状态实例</param>
    protected virtual void ChangeInternal(IState next)
    {
        // 检查是否为相同状态，避免不必要的切换
        if (Current == next) return;

        // 验证当前状态是否允许切换到目标状态
        if (Current != null && !Current.CanTransitionTo(next))
        {
            OnTransitionRejected(Current, next);
            return;
        }

        var old = Current;
        OnStateChanging(old, next);

        // 将当前状态添加到历史记录
        if (Current != null)
        {
            _stateHistory.Push(Current);

            // 限制历史记录大小
            if (_stateHistory.Count > maxHistorySize)
            {
                // 移除最旧的记录（栈底元素）
                var tempStack = new Stack<IState>(_stateHistory.Reverse().Skip(1));
                _stateHistory.Clear();
                foreach (var state in tempStack.Reverse())
                {
                    _stateHistory.Push(state);
                }
            }
        }

        old?.OnExit(next);
        Current = next;
        Current.OnEnter(old);

        OnStateChanged(old, Current);
    }

    /// <summary>
    /// 当状态转换被拒绝时的回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual void OnTransitionRejected(IState from, IState to)
    {
    }

    /// <summary>
    /// 当状态即将发生改变时的回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual void OnStateChanging(IState? from, IState to)
    {
    }

    /// <summary>
    /// 当状态改变完成后的回调方法
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    protected virtual void OnStateChanged(IState? from, IState? to)
    {
    }
}