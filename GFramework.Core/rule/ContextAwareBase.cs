using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.architecture;

namespace GFramework.Core.rule;

/// <summary>
///     上下文感知基类，实现了IContextAware接口，为需要感知架构上下文的类提供基础实现
/// </summary>
public abstract class ContextAwareBase : IContextAware
{
    /// <summary>
    ///     获取当前实例的架构上下文
    /// </summary>
    protected IArchitectureContext? Context { get; set; }

    /// <summary>
    ///     设置架构上下文的实现方法，由框架调用
    /// </summary>
    /// <param name="context">要设置的架构上下文实例</param>
    void IContextAware.SetContext(IArchitectureContext context)
    {
        Context = context;
        OnContextReady();
    }

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    /// <returns>当前架构上下文对象</returns>
    IArchitectureContext IContextAware.GetContext()
    {
        Context ??= GameContext.GetFirstArchitectureContext();
        return Context;
    }

    /// <summary>
    ///     当上下文准备就绪时调用的虚方法，子类可以重写此方法来执行上下文相关的初始化逻辑
    /// </summary>
    protected virtual void OnContextReady()
    {
    }
}