using GFramework.Core.Abstractions.state;

namespace GFramework.Core.state;

/// <summary>
/// 状态机实现类，用于管理状态的注册、切换和生命周期
/// </summary>
public class StateMachine : IStateMachine
{
    /// <summary>
    /// 存储所有已注册状态的字典，键为状态类型，值为状态实例
    /// </summary>
    protected readonly Dictionary<Type, IState> States = new();

    /// <summary>
    /// 获取当前激活的状态
    /// </summary>
    public IState? Current { get; private set; }

    /// <summary>
    /// 注册一个状态到状态机中
    /// </summary>
    /// <param name="state">要注册的状态实例</param>
    public void Register(IState state)
        => States[state.GetType()] = state;

    /// <summary>
    /// 从状态机中注销指定类型的状态
    /// </summary>
    /// <typeparam name="T">要注销的状态类型</typeparam>
    public void Unregister<T>() where T : IState
    {
        var type = typeof(T);
        if (!States.TryGetValue(type, out var state)) return;

        // 如果当前状态是要注销的状态，则先执行退出逻辑
        if (Current == state)
        {
            Current.OnExit(null);
            Current = null;
        }

        States.Remove(type);
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
        if (!States.TryGetValue(typeof(T), out var target))
            throw new InvalidOperationException("State not registered.");

        ChangeInternal(target);
    }

    /// <summary>
    /// 内部状态切换方法，处理状态切换的核心逻辑
    /// </summary>
    /// <param name="next">下一个状态实例</param>
    protected virtual void ChangeInternal(IState next)
    {
        if (Current == next) return;
        if (Current != null && !Current.CanTransitionTo(next)) return;

        var old = Current;
        old?.OnExit(next);

        Current = next;
        Current.OnEnter(old);
    }
}