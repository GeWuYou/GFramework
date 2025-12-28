using GFramework.Core.Abstractions.architecture;
using GFramework.Core.architecture;
using Godot;

namespace GFramework.Godot.architecture;

/// <summary>
///     抽象的Godot模块基类，用于定义Godot框架中的模块行为
/// </summary>
public abstract class AbstractGodotModule : IGodotModule
{
    /// <summary>
    ///     获取模块关联的Godot节点
    /// </summary>
    public abstract Node Node { get; }

    /// <summary>
    ///     当架构阶段发生变化时调用此方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    /// <param name="architecture">架构实例</param>
    public virtual void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
    {
    }

    /// <summary>
    ///     当架构阶段发生变化时调用此方法
    /// </summary>
    /// <param name="phase">当前的架构阶段</param>
    public virtual void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    /// <summary>
    ///     安装模块到指定架构中
    /// </summary>
    /// <param name="architecture">要安装到的架构实例</param>
    public abstract void Install(IArchitecture architecture);

    /// <summary>
    ///     当模块从架构中分离时调用此方法
    /// </summary>
    public virtual void OnDetach()
    {
    }

    /// <summary>
    ///     当模块被附加到架构时调用此方法
    /// </summary>
    /// <param name="architecture">被附加到的架构实例</param>
    public virtual void OnAttach(Architecture architecture)
    {
    }
}