namespace GFramework.Core.architecture;

/// <summary>
///     可扩展架构接口，继承自IArchitecture接口，提供模块安装和生命周期钩子注册功能
/// </summary>
public interface IArchitectureExtensible : IArchitecture
{
    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的架构模块实例</param>
    void InstallModule(IArchitectureModule module);

    /// <summary>
    ///     注册架构生命周期钩子
    /// </summary>
    /// <param name="hook">要注册的架构生命周期钩子实例</param>
    void RegisterLifecycleHook(IArchitectureLifecycle hook);
}