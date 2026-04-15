using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Cqrs.Internal;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Services.Modules;

/// <summary>
///     CQRS runtime 模块，用于把默认请求分发器与处理器注册器接入架构容器。
///     该模块在架构初始化早期完成注册，保证用户初始化阶段即可使用 CQRS 入口与 handler 自动接入能力。
/// </summary>
public sealed class CqrsRuntimeModule : IServiceModule
{
    /// <summary>
    ///     获取模块名称。
    /// </summary>
    public string ModuleName => nameof(CqrsRuntimeModule);

    /// <summary>
    ///     获取模块优先级。
    ///     CQRS runtime 需要先于架构默认 handler 扫描路径可用，因此放在基础总线模块之后、用户初始化之前注册。
    /// </summary>
    public int Priority => 15;

    /// <summary>
    ///     获取模块启用状态，默认启用。
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    ///     注册默认 CQRS runtime seam 实现。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    public void Register(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var dispatcherLogger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsDispatcher));
        var registrarLogger = LoggerFactoryResolver.Provider.CreateLogger(nameof(DefaultCqrsHandlerRegistrar));

        container.Register<ICqrsRuntime>(new CqrsDispatcher(container, dispatcherLogger));
        container.Register<ICqrsHandlerRegistrar>(new DefaultCqrsHandlerRegistrar(container, registrarLogger));
    }

    /// <summary>
    ///     初始化模块。
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    ///     异步销毁模块。
    /// </summary>
    /// <returns>已完成的值任务。</returns>
    public ValueTask DestroyAsync()
    {
        return ValueTask.CompletedTask;
    }
}
