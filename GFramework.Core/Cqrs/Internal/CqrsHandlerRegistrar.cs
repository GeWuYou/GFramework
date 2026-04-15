using System.Reflection;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Cqrs.Internal;

/// <summary>
///     在架构初始化期间扫描并注册 CQRS 处理器。
///     首批实现采用运行时反射扫描，优先满足“无需额外注册步骤即可工作”的迁移目标。
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

        foreach (var assembly in assemblies
                     .Where(static assembly => assembly is not null)
                     .Distinct()
                     .OrderBy(GetAssemblySortKey, StringComparer.Ordinal))
        {
            RegisterAssemblyHandlers(container.GetServicesUnsafe, assembly, logger);
        }
    }

    /// <summary>
    ///     注册单个程序集里的所有 CQRS 处理器映射。
    /// </summary>
    private static void RegisterAssemblyHandlers(IServiceCollection services, Assembly assembly, ILogger logger)
    {
        foreach (var implementationType in GetLoadableTypes(assembly, logger).Where(IsConcreteHandlerType))
        {
            var handlerInterfaces = implementationType
                .GetInterfaces()
                .Where(IsSupportedHandlerInterface)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToList();

            if (handlerInterfaces.Count == 0)
                continue;

            foreach (var handlerInterface in handlerInterfaces)
            {
                // Request/notification handlers receive context injection before every dispatch.
                // Transient registration avoids sharing mutable Context across concurrent requests.
                services.AddTransient(handlerInterface, implementationType);
                logger.Debug(
                    $"Registered CQRS handler {implementationType.FullName} as {handlerInterface.FullName}.");
            }
        }
    }

    /// <summary>
    ///     安全获取程序集中的可加载类型，并在部分类型加载失败时保留其余处理器注册能力。
    /// </summary>
    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly, ILogger logger)
    {
        try
        {
            return assembly.GetTypes()
                .Where(static type => type is not null)
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToList();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return RecoverLoadableTypes(assembly, exception, logger);
        }
    }

    /// <summary>
    ///     记录部分类型加载失败，并返回仍然可用的类型集合。
    /// </summary>
    private static IReadOnlyList<Type> RecoverLoadableTypes(
        Assembly assembly,
        ReflectionTypeLoadException exception,
        ILogger logger)
    {
        var assemblyName = GetAssemblySortKey(assembly);
        logger.Warn(
            $"CQRS handler scan partially failed for assembly {assemblyName}. Continuing with loadable types.");

        foreach (var loaderException in exception.LoaderExceptions.Where(static ex => ex is not null))
        {
            logger.Warn(
                $"Failed to load one or more types while scanning {assemblyName}: {loaderException!.Message}");
        }

        return exception.Types
            .Where(static type => type is not null)
            .Cast<Type>()
            .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
            .ToList();
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

    /// <summary>
    ///     生成程序集排序键，保证跨运行环境的处理器注册顺序稳定。
    /// </summary>
    private static string GetAssemblySortKey(Assembly assembly)
    {
        return assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
    }

    /// <summary>
    ///     生成类型排序键，保证同一程序集内的处理器与接口映射顺序稳定。
    /// </summary>
    private static string GetTypeSortKey(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
