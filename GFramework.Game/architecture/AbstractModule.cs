using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;

namespace GFramework.Game.architecture;

/// <summary>
///     抽象模块类，实现IArchitectureModule接口，为架构模块提供基础功能
/// </summary>
public abstract class AbstractModule : IArchitectureModule
{
    /// <summary>
    ///     在指定架构阶段执行的操作
    /// </summary>
    /// <param name="phase">架构阶段枚举值</param>
    /// <param name="architecture">架构实例</param>
    public virtual void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
    }

    /// <summary>
    ///     在架构阶段执行的操作
    /// </summary>
    /// <param name="phase">架构阶段枚举值</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    /// <summary>
    ///     安装模块到架构中
    /// </summary>
    /// <param name="architecture">要安装到的架构实例</param>
    public abstract void Install(IArchitecture architecture);
}