using GFramework.Core.architecture;
using GFramework.Core.rule;

namespace GFramework.Core.system;

/// <summary>
/// 抽象系统基类，实现系统接口的基本功能
/// 提供架构关联和初始化机制
/// </summary>
public abstract class AbstractSystem : ISystem
{
    private IArchitecture _mArchitecture;

    /// <summary>
    /// 获取当前系统所属的架构实例
    /// </summary>
    /// <returns>返回系统关联的架构对象</returns>
    IArchitecture IBelongToArchitecture.GetArchitecture() => _mArchitecture;

    /// <summary>
    /// 设置系统所属的架构实例
    /// </summary>
    /// <param name="architecture">要关联的架构对象</param>
    void ICanSetArchitecture.SetArchitecture(IArchitecture architecture) => _mArchitecture = architecture;

    /// <summary>
    /// 系统初始化方法，调用抽象初始化方法
    /// </summary>
    void ISystem.Init() => OnInit();

    /// <summary>
    /// 抽象初始化方法，由子类实现具体的初始化逻辑
    /// </summary>
    protected abstract void OnInit();
}
