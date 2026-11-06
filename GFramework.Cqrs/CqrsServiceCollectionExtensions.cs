// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;
using GFramework.Cqrs.Notification;

namespace GFramework.Cqrs;

/// <summary>
///     提供 CQRS runtime 的标准 <see cref="IServiceCollection" /> 组合根注册入口。
///     这些扩展不依赖 Architecture：任意使用标准 DI 的应用都可以独立安装并使用
///     request、notification、stream 与 pipeline 能力。
/// </summary>
/// <remarks>
///     <para>推荐接入顺序：先 <c>AddCqrsRuntime</c>，再按需
///     <c>AddCqrsPipelineBehavior</c> / <c>AddCqrsStreamPipelineBehavior</c> /
///     <c>AddCqrsHandlersFromAssembly</c>。所有注册都发生在 provider 构建之前。</para>
///     <para>Architecture 用户可直接在 <c>ConfigureServices</c> 中使用这些扩展，
///     或通过 <c>UseCqrsRuntime</c> 便捷入口安装 <see cref="CqrsRuntimeModule" />。</para>
/// </remarks>
public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    ///     安装默认 CQRS runtime，并使用全局日志工厂解析器创建诊断日志器。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    public static IServiceCollection AddCqrsRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddCqrsRuntime(LoggerFactoryResolver.Provider);
    }

    /// <summary>
    ///     安装默认 CQRS runtime，并从指定日志工厂创建诊断日志器。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="loggerFactoryProvider">用于创建 runtime 诊断日志器的日志工厂。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" /> 或 <paramref name="loggerFactoryProvider" /> 为 <see langword="null" />。
    /// </exception>
    public static IServiceCollection AddCqrsRuntime(
        this IServiceCollection services,
        ILoggerFactoryProvider loggerFactoryProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(loggerFactoryProvider);

        return services.AddCqrsRuntime(loggerFactoryProvider.CreateLogger("CqrsDispatcher"));
    }

    /// <summary>
    ///     安装默认 CQRS runtime 单例。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="dispatcherLogger">用于 runtime 诊断的日志器。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="InvalidOperationException">
    ///     当前服务集合已存在 <see cref="ICqrsRuntime" /> 注册；同一容器只允许安装一个 runtime。
    /// </exception>
    /// <remarks>
    ///     <see cref="ICqrsRuntime" /> 以标准单例工厂注册：provider 构建后首次解析时创建，
    ///     与容器内其他单例遵循同一构建与释放责任。
    /// </remarks>
    public static IServiceCollection AddCqrsRuntime(this IServiceCollection services, ILogger dispatcherLogger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dispatcherLogger);

        // 同一容器只允许一个 runtime 描述;重复安装属于组合根配置错误，显式拒绝避免工厂叠加出多个单例。
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(ICqrsRuntime)))
        {
            throw new InvalidOperationException(
                $"An {typeof(ICqrsRuntime).FullName} registration already exists. Remove the duplicate " +
                $"{nameof(AddCqrsRuntime)} call before building the provider.");
        }

        services.AddSingleton<ICqrsRuntime>(serviceProvider =>
            CqrsRuntimeFactory.CreateRuntime(serviceProvider, dispatcherLogger));
        return services;
    }

    /// <summary>
    ///     注册 CQRS 请求 pipeline 行为。
    ///     同时支持开放泛型行为类型和已闭合的具体行为类型，
    ///     以兼容通用行为和针对单一请求的专用行为两种注册方式。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型。</typeparam>
    /// <param name="services">目标服务集合。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="InvalidOperationException"><typeparamref name="TBehavior" /> 未实现 <c>IPipelineBehavior&lt;,&gt;</c>。</exception>
    public static IServiceCollection AddCqrsPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddCqrsPipelineBehavior<TBehavior>(services, typeof(IPipelineBehavior<,>), "IPipelineBehavior<,>");
    }

    /// <summary>
    ///     注册 CQRS 流式请求 pipeline 行为。
    ///     同时支持开放泛型行为类型和已闭合的具体行为类型。
    /// </summary>
    /// <typeparam name="TBehavior">行为类型，必须是引用类型。</typeparam>
    /// <param name="services">目标服务集合。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="InvalidOperationException"><typeparamref name="TBehavior" /> 未实现 <c>IStreamPipelineBehavior&lt;,&gt;</c>。</exception>
    public static IServiceCollection AddCqrsStreamPipelineBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddCqrsPipelineBehavior<TBehavior>(
            services,
            typeof(IStreamPipelineBehavior<,>),
            "IStreamPipelineBehavior<,>");
    }

    /// <summary>
    ///     从指定程序集显式注册 CQRS 处理器。
    ///     运行时会优先使用程序集级源码生成注册器；若不存在可用注册器，则自动回退到反射扫描。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="assembly">包含 CQRS 处理器或生成注册器的程序集。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> 为 <see langword="null" />。</exception>
    public static IServiceCollection AddCqrsHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return services.AddCqrsHandlersFromAssemblies([assembly]);
    }

    /// <summary>
    ///     从多个程序集显式注册 CQRS 处理器。
    ///     同一程序集只会被接入一次，避免重复配置产生重复 handler 映射。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="assemblies">要接入的程序集集合。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies" /> 为 <see langword="null" />。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies" /> 中存在 <see langword="null" /> 元素。</exception>
    public static IServiceCollection AddCqrsHandlersFromAssemblies(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        var assemblyArray = assemblies.ToArray();
        foreach (var assembly in assemblyArray)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assemblies), "Assemblies collection cannot contain null items.");
            }
        }

        ResolveRegistrationService(services).RegisterHandlers(assemblyArray);
        return services;
    }

    /// <summary>
    ///     复用 request 与 stream 两条入口的开放泛型/封闭接口校验与注册逻辑。
    /// </summary>
    private static IServiceCollection AddCqrsPipelineBehavior<TBehavior>(
        IServiceCollection services,
        Type openGenericInterfaceType,
        string interfaceTypeDisplayName)
        where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);
        if (behaviorType.IsGenericTypeDefinition)
        {
            services.AddSingleton(openGenericInterfaceType, behaviorType);
            return services;
        }

        var pipelineInterfaces = behaviorType
            .GetInterfaces()
            .Where(type => type.IsGenericType &&
                           type.GetGenericTypeDefinition() == openGenericInterfaceType)
            .ToList();

        if (pipelineInterfaces.Count == 0)
        {
            throw new InvalidOperationException(
                $"{behaviorType.Name} does not implement {interfaceTypeDisplayName}");
        }

        // 为每个已闭合的行为接口建立显式映射，支持针对特定请求/响应对的专用行为。
        foreach (var pipelineInterface in pipelineInterfaces)
        {
            services.AddSingleton(pipelineInterface, behaviorType);
        }

        return services;
    }

    /// <summary>
    ///     获取当前服务集合中的程序集注册协调器；尚未安装时按需创建并登记实例绑定。
    ///     实例绑定让后续多次 handler 注册共享同一份程序集去重状态。
    /// </summary>
    private static ICqrsRegistrationService ResolveRegistrationService(IServiceCollection services)
    {
        var existingRegistrationService = services
            .Select(static descriptor => descriptor.ImplementationInstance)
            .OfType<ICqrsRegistrationService>()
            .LastOrDefault();
        if (existingRegistrationService is not null)
            return existingRegistrationService;

        var registrarLogger = LoggerFactoryResolver.Provider.CreateLogger("DefaultCqrsHandlerRegistrar");
        var registrationLogger = LoggerFactoryResolver.Provider.CreateLogger("DefaultCqrsRegistrationService");
        var registrar = new DefaultCqrsHandlerRegistrar(services, registrarLogger);
        var registrationService = new DefaultCqrsRegistrationService(registrar, registrationLogger);
        services.AddSingleton(registrationService);
        return registrationService;
    }
}
