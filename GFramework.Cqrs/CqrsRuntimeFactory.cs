using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;

namespace GFramework.Cqrs;

/// <summary>
///     提供 CQRS runtime 默认实现的跨程序集创建入口。
/// </summary>
/// <remarks>
///     <see cref="GFramework.Core" /> 需要在不暴露内部实现细节的前提下接入默认 CQRS runtime，
///     因此通过该工厂返回抽象接口，而不是直接公开内部 dispatcher / registrar 类型。
/// </remarks>
public static class CqrsRuntimeFactory
{
    /// <summary>
    ///     创建默认 CQRS runtime 分发器。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    /// <param name="logger">用于 runtime 诊断的日志器。</param>
    /// <returns>默认 CQRS runtime。</returns>
    public static ICqrsRuntime CreateRuntime(IIocContainer container, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(logger);

        return new CqrsDispatcher(container, logger);
    }

    /// <summary>
    ///     创建默认 CQRS 处理器注册器。
    /// </summary>
    /// <param name="container">目标依赖注入容器。</param>
    /// <param name="logger">用于注册阶段诊断的日志器。</param>
    /// <returns>默认 CQRS handler registrar。</returns>
    public static ICqrsHandlerRegistrar CreateHandlerRegistrar(IIocContainer container, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(logger);

        return new DefaultCqrsHandlerRegistrar(container, logger);
    }
}
