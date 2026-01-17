using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.state;
using IDisposable = GFramework.Core.Abstractions.lifecycle.IDisposable;

namespace GFramework.Core.state;

/// <summary>
/// 上下文感知状态基类
/// 提供基础的状态管理功能和架构上下文访问能力
/// 实现了IState和IContextAware接口
/// </summary>
public class ContextAwareStateBase : IState, IContextAware, IDisposable
{
    /// <summary>
    /// 架构上下文引用，用于访问架构相关的服务和数据
    /// </summary>
    private IArchitectureContext? _context;

    /// <summary>
    /// 设置架构上下文
    /// </summary>
    /// <param name="context">架构上下文实例</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取架构上下文
    /// </summary>
    /// <returns>架构上下文实例</returns>
    public IArchitectureContext GetContext()
    {
        return _context!;
    }

    /// <summary>
    /// 销毁当前状态，释放相关资源
    /// 子类可重写此方法以执行特定的清理操作
    /// </summary>
    public virtual void Destroy()
    {
    }

    /// <summary>
    /// 进入状态时调用的方法
    /// 子类可重写此方法以实现特定的状态进入逻辑
    /// </summary>
    /// <param name="from">从哪个状态转换而来，可能为null表示初始状态</param>
    public virtual void OnEnter(IState? from)
    {
    }

    /// <summary>
    /// 退出状态时调用的方法
    /// 子类可重写此方法以实现特定的状态退出逻辑
    /// </summary>
    /// <param name="to">将要转换到的目标状态，可能为null表示结束状态</param>
    public virtual void OnExit(IState? to)
    {
    }

    /// <summary>
    /// 判断当前状态是否可以转换到目标状态
    /// 子类可重写此方法以实现自定义的状态转换规则
    /// </summary>
    /// <param name="target">希望转换到的目标状态对象</param>
    /// <returns>如果允许转换则返回true，否则返回false</returns>
    public virtual bool CanTransitionTo(IState target)
    {
        return true;
    }
}