using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.system;
using GFramework.Core.rule;

namespace GFramework.Core.system;

/// <summary>
///     抽象系统基类，实现系统接口的基本功能
///     提供架构关联、初始化和销毁机制
/// </summary>
public abstract class AbstractSystem : ContextAwareBase, ISystem
{
    private ILogger _logger = null!;

    /// <summary>
    ///     系统初始化方法，调用抽象初始化方法
    /// </summary>
    void ISystem.Init()
    {
        _logger = Context.LoggerFactory.GetLogger(nameof(AbstractSystem));
        _logger.Debug($"Initializing system: {GetType().Name}");

        OnInit();

        _logger.Info($"System initialized: {GetType().Name}");
    }

    /// <summary>
    ///     系统销毁方法，调用抽象销毁方法
    /// </summary>
    void ISystem.Destroy()
    {
        _logger.Debug($"Destroying system: {GetType().Name}");

        OnDestroy();

        _logger.Info($"System destroyed: {GetType().Name}");
    }

    /// <summary>
    ///     处理架构阶段事件的虚拟方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();

    /// <summary>
    ///     抽象销毁方法，由子类实现具体的资源清理逻辑
    /// </summary>
    protected virtual void OnDestroy()
    {
    }
}