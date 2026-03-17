using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构模块管理器
///     负责管理架构模块的安装和中介行为注册
/// </summary>
internal sealed class ArchitectureModules(
    IArchitecture architecture,
    IArchitectureServices services,
    ILogger logger)
{
    /// <summary>
    ///     注册中介行为管道
    ///     用于配置Mediator框架的行为拦截和处理逻辑
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        logger.Debug($"Registering mediator behavior: {typeof(TBehavior).Name}");
        services.Container.RegisterMediatorBehavior<TBehavior>();
    }

    /// <summary>
    ///     安装架构模块
    /// </summary>
    /// <param name="module">要安装的模块</param>
    /// <returns>安装的模块实例</returns>
    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        var name = module.GetType().Name;
        logger.Debug($"Installing module: {name}");
        module.Install(architecture);
        logger.Info($"Module installed: {name}");
        return module;
    }
}