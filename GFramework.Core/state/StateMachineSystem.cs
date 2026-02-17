using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.state;
using GFramework.Core.extensions;
using IDisposable = GFramework.Core.Abstractions.lifecycle.IDisposable;

namespace GFramework.Core.state;

/// <summary>
///     上下文感知状态机，继承自StateMachine并实现ISystem接口
///     该状态机能够感知架构上下文，并在状态切换时发送状态变更事件
/// </summary>
public class StateMachineSystem : StateMachine, IStateMachineSystem
{
    /// <summary>
    ///     架构上下文对象，用于提供系统运行所需的上下文信息
    /// </summary>
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     设置架构上下文的方法
    /// </summary>
    /// <param name="context">要设置的架构上下文对象</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     获取当前架构上下文的方法
    /// </summary>
    /// <returns>当前的架构上下文对象</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }

    /// <summary>
    ///     处理架构生命周期阶段的方法
    /// </summary>
    /// <param name="phase">当前所处的架构生命周期阶段</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    /// <summary>
    ///     初始化方法，在系统启动时调用
    ///     遍历所有状态实例，为实现了IContextAware接口的状态设置上下文
    /// </summary>
    public virtual void Init()
    {
        foreach (var state in States.Values.OfType<IContextAware>()) state.SetContext(_context);
    }

    /// <summary>
    ///     销毁方法，在系统关闭时调用
    /// </summary>
    public virtual void Destroy()
    {
        // 退出当前状态
        if (Current != null)
        {
            if (Current is IAsyncState asyncState)
            {
                asyncState.OnExitAsync(null);
            }
            else
            {
                Current.OnExit(null);
            }

            Current = null;
        }

        // 清理所有状态
        foreach (var state in States.Values.OfType<IDisposable>()) state.Destroy();

        States.Clear();
    }


    /// <summary>
    ///     异步内部状态切换方法，重写基类方法以添加状态变更事件通知功能
    /// </summary>
    /// <param name="next">要切换到的下一个状态</param>
    protected override async Task ChangeInternalAsync(IState next)
    {
        var old = Current;
        await base.ChangeInternalAsync(next);

        // 发送状态变更事件，通知监听者状态已发生改变
        this.SendEvent(new StateChangedEvent
        {
            OldState = old,
            NewState = Current
        });
    }
}