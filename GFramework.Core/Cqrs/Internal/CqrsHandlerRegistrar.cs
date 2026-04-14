using System.Reflection;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Cqrs.Internal;

/// <summary>
///     在架构初始化期间扫描并注册 CQRS 处理器。
///     首批实现采用运行时反射扫描，优先满足“无需 AddMediator 即可工作”的迁移目标。
/// </summary>
internal static class CqrsHandlerRegistrar
{
    /// <summary>
    ///     扫描指定程序集并注册所有 CQRS 请求/通知/流式处理器。
    /// </summary>
    /// <param name="container">目标容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    /// <param name="logger">日志记录器。</param>
    public static void RegisterHandlers(
        IIocContainer container,
        IEnumerable<Assembly> assemblies,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(logger);

        foreach (var assembly in assemblies.Distinct())
        {
            RegisterAssemblyHandlers(container.GetServicesUnsafe, assembly, logger);
        }
    }

    /// <summary>
    ///     注册单个程序集里的所有 CQRS 处理器映射。
    /// </summary>
    private static void RegisterAssemblyHandlers(IServiceCollection services, Assembly assembly, ILogger logger)
    {
        foreach (var implementationType in assembly.GetTypes().Where(IsConcreteHandlerType))
        {
            var handlerInterfaces = implementationType
                .GetInterfaces()
                .Where(IsSupportedHandlerInterface)
                .ToList();

            if (handlerInterfaces.Count == 0)
                continue;

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddSingleton(handlerInterface, implementationType);
                logger.Debug(
                    $"Registered CQRS handler {implementationType.FullName} as {handlerInterface.FullName}.");
            }
        }
    }

    /// <summary>
    ///     判断指定类型是否可作为可实例化处理器。
    /// </summary>
    private static bool IsConcreteHandlerType(Type type)
    {
        return type is { IsAbstract: false, IsInterface: false } && !type.ContainsGenericParameters;
    }

    /// <summary>
    ///     判断接口是否为当前运行时支持的 CQRS 处理器接口。
    /// </summary>
    private static bool IsSupportedHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(IRequestHandler<,>) ||
               definition == typeof(INotificationHandler<>) ||
               definition == typeof(IStreamRequestHandler<,>);
    }
}
