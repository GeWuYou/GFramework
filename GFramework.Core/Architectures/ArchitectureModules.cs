using System.ComponentModel;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构模块管理器
///     负责管理架构模块的安装和 CQRS 行为注册
/// </summary>
internal sealed class ArchitectureModules(
    IArchitecture architecture,
    IArchitectureServices services,
    ILogger logger)
{
    /// <summary>
    ///     注册 CQRS 请求管道行为。
    ///     支持开放泛型行为类型和针对单一请求的封闭行为类型。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    public void RegisterCqrsPipelineBehavior<TBehavior>() where TBehavior : class
    {
        logger.Debug($"Registering CQRS pipeline behavior: {typeof(TBehavior).Name}");
        services.Container.RegisterCqrsPipelineBehavior<TBehavior>();
    }

    /// <summary>
    ///     注册 CQRS 请求管道行为。
    ///     该成员保留旧名称以兼容历史调用点，内部行为与 <see cref="RegisterCqrsPipelineBehavior{TBehavior}" /> 一致。
    ///     新代码不应继续依赖该别名；兼容层计划在未来的 major 版本中移除。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete(
        "Use RegisterCqrsPipelineBehavior<TBehavior>() instead. This compatibility alias will be removed in a future major version.")]
    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        RegisterCqrsPipelineBehavior<TBehavior>();
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
