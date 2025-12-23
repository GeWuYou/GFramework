using GFramework.Core.architecture;
using GFramework.Core.logging;
using GFramework.Core.rule;

namespace GFramework.Core.system;

/// <summary>
///     抽象系统基类，实现系统接口的基本功能
///     提供架构关联、初始化和销毁机制
/// </summary>
public abstract class AbstractSystem : ISystem
{
    private IArchitecture _mArchitecture;

    /// <summary>
    ///     获取当前系统所属的架构实例
    /// </summary>
    /// <returns>返回系统关联的架构对象</returns>
    IArchitecture IBelongToArchitecture.GetArchitecture()
    {
        return _mArchitecture;
    }

    /// <summary>
    ///     设置系统所属的架构实例
    /// </summary>
    /// <param name="architecture">要关联的架构对象</param>
    void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
    {
        _mArchitecture = architecture;
    }

    /// <summary>
    ///     系统初始化方法，调用抽象初始化方法
    /// </summary>
    void ISystem.Init()
    {
        var logger = Log.CreateLogger("System");
        logger.Debug($"Initializing system: {GetType().Name}");
        
        OnInit();
        
        logger.Info($"System initialized: {GetType().Name}");
    }

    /// <summary>
    ///     系统销毁方法，调用抽象销毁方法
    /// </summary>
    void ISystem.Destroy()
    {
        var logger = Log.CreateLogger("System");
        logger.Debug($"Destroying system: {GetType().Name}");
        
        OnDestroy();
        
        logger.Info($"System destroyed: {GetType().Name}");
    }

    /// <summary>
    ///     抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();
    
    /// <summary>
    ///     抽象销毁方法，由子类实现具体的资源清理逻辑
    /// </summary>
    protected virtual void OnDestroy() { }
}