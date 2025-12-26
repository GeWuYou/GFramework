namespace GFramework.Core.architecture;

/// <summary>
///     架构模块接口，继承自架构生命周期接口。
///     定义了模块安装到架构中的标准方法。
/// </summary>
public interface IArchitectureModule : IArchitectureLifecycle, IArchitecturePhaseAware
{
    /// <summary>
    ///     将当前模块安装到指定的架构中。
    /// </summary>
    /// <param name="architecture">要安装模块的目标架构实例。</param>
    void Install(IArchitecture architecture);
}