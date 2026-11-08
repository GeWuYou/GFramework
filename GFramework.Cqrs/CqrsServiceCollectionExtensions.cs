// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;
using GFramework.Cqrs.Notification;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

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
///     或通过显式配置模块安装 <see cref="CqrsRuntimeModule" />。</para>
/// </remarks>
public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    ///     安装默认 CQRS runtime。
    ///     日志由宿主决定：CQRS 不创建也不拥有进程级日志工厂，
    ///     runtime 会在首次解析时从容器解析 <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}" />，
    ///     未注册日志基础设施时回退到空实现。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    public static IServiceCollection AddCqrsRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddCqrsRuntime(loggerFactory: null);
    }

    /// <summary>
    ///     安装默认 CQRS runtime，并可选地把调用方提供的标准日志工厂登记到容器。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="loggerFactory">
    ///     可选的标准 <see cref="Microsoft.Extensions.Logging.ILoggerFactory" />；
    ///     提供时会以 <c>TryAddSingleton</c> 登记该实例并补充开放泛型
    ///     <c>ILogger&lt;&gt;</c> → <c>Logger&lt;&gt;</c> 映射（已注册时保持现状），
    ///     使 DI 激活的服务与行为可以通过构造函数注入 <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}" />；
    ///     为 <see langword="null" /> 时不登记任何日志基础设施，由宿主或 Architecture 负责注册。
    ///     CQRS 本身不创建、不拥有进程级日志工厂。
    /// </param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" /> 为 <see langword="null" />。
    /// </exception>
    public static IServiceCollection AddCqrsRuntime(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(services);

        // 仅登记调用方显式提供的工厂；TryAdd 语义保证不覆盖宿主已注册的日志配置。
        if (loggerFactory is not null)
        {
            services.TryAddSingleton(loggerFactory);
            services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        return RegisterRuntime(services, ServiceLifetime.Singleton);
    }

    /// <summary>
    ///     安装默认 CQRS runtime 单例，并显式指定 runtime 诊断日志器。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="dispatcherLogger">用于 runtime 诊断的日志器。</param>
    /// <returns>同一个 <paramref name="services" />，便于链式配置。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" /> 或 <paramref name="dispatcherLogger" /> 为 <see langword="null" />。
    /// </exception>
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

        return RegisterRuntime(services, ServiceLifetime.Singleton, dispatcherLogger);
    }

    /// <summary>
    ///     安装绑定到调用方 DI 作用域的 CQRS runtime，适用于具有 Scoped handler 或依赖的请求宿主。
    /// </summary>
    /// <param name="services">目标服务集合；handler 与依赖的生命周期仍由调用方明确登记。</param>
    /// <param name="loggerFactory">可选的宿主日志工厂；不覆盖已存在的日志配置。</param>
    /// <returns>同一服务集合。</returns>
    /// <exception cref="ArgumentNullException">服务集合为空。</exception>
    /// <exception cref="InvalidOperationException">已经登记单例或作用域 CQRS runtime。</exception>
    /// <remarks>
    ///     从每个作用域的 ServiceProvider 解析 ICqrsRuntime；不能从根 provider 解析 Scoped runtime。
    ///     同一作用域内复用 runtime，跨作用域隔离 handler 解析，依赖由各自作用域释放。
    ///     Architecture 的根单例默认仍使用 AddCqrsRuntime，不与此注册混用。
    /// </remarks>
    public static IServiceCollection AddScopedCqrsRuntime(this IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (loggerFactory != null)
        {
            services.TryAddSingleton(loggerFactory);
            services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }
        return RegisterRuntime(services, ServiceLifetime.Scoped);
    }

    private static IServiceCollection RegisterRuntime(IServiceCollection services, ServiceLifetime lifetime,
        ILogger? dispatcherLogger = null)
    {
        // 单例与作用域模式共享重复检查，避免同一容器出现两个相互覆盖的 runtime 配置。
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(ICqrsRuntime)))
        {
            throw new InvalidOperationException(
                $"An {typeof(ICqrsRuntime).FullName} registration already exists. Remove the duplicate " +
                $"{nameof(AddCqrsRuntime)} call before building the provider.");
        }

        Func<IServiceProvider, object> create = dispatcherLogger == null
            ? static provider => CqrsRuntimeFactory.CreateRuntime(provider, notificationPublisher: null)
            : provider => CqrsRuntimeFactory.CreateRuntime(provider, dispatcherLogger);
        services.Add(ServiceDescriptor.Describe(typeof(ICqrsRuntime), create, lifetime));
        return services;
    }

    /// <summary>Registers a closed request behavior with an explicit DI lifetime.</summary>
    /// <typeparam name="TBehavior">A concrete implementation of IPipelineBehavior.</typeparam>
    /// <param name="services">The composition root.</param>
    /// <param name="lifetime">Transient by default; scoped dependencies require a scoped runtime.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddCqrsPipelineBehavior<TBehavior>(
        this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TBehavior : class => services.AddCqrsPipelineBehavior(typeof(TBehavior), lifetime);

    /// <summary>Registers a closed or open generic request behavior before building the provider.</summary>
    /// <param name="services">The composition root.</param>
    /// <param name="behaviorType">A concrete behavior; open generic arguments must map in declaration order.</param>
    /// <param name="lifetime">The DI lifetime; no scope is implicitly created.</param>
    /// <returns>The original collection.</returns>
    /// <exception cref="ArgumentException">The type, mapping or lifetime is invalid.</exception>
    public static IServiceCollection AddCqrsPipelineBehavior(this IServiceCollection services,
        Type behaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient) =>
        RegisterPipelineBehavior(services, behaviorType, typeof(IPipelineBehavior<,>), lifetime);

    /// <summary>Registers a closed stream behavior with an explicit DI lifetime.</summary>
    /// <typeparam name="TBehavior">A concrete implementation of IStreamPipelineBehavior.</typeparam>
    /// <param name="services">The composition root.</param>
    /// <param name="lifetime">Transient by default; the caller scope must cover the complete enumeration.</param>
    /// <returns>The original collection.</returns>
    public static IServiceCollection AddCqrsStreamPipelineBehavior<TBehavior>(
        this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TBehavior : class => services.AddCqrsStreamPipelineBehavior(typeof(TBehavior), lifetime);

    /// <summary>Registers a closed or open generic stream behavior before building the provider.</summary>
    /// <param name="services">The composition root.</param>
    /// <param name="behaviorType">A concrete behavior; open generic arguments must map in declaration order.</param>
    /// <param name="lifetime">The explicit DI lifetime.</param>
    /// <returns>The original collection.</returns>
    /// <exception cref="ArgumentException">The type, mapping or lifetime is invalid.</exception>
    public static IServiceCollection AddCqrsStreamPipelineBehavior(this IServiceCollection services,
        Type behaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient) =>
        RegisterPipelineBehavior(services, behaviorType, typeof(IStreamPipelineBehavior<,>), lifetime);

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

    /// <summary>Validates the complete mapping before adding any descriptors to the collection.</summary>
    private static IServiceCollection RegisterPipelineBehavior(IServiceCollection services, Type behaviorType,
        Type contract, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(behaviorType);
        if (!Enum.IsDefined(lifetime))
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        if (!behaviorType.IsClass || behaviorType.IsAbstract ||
            (behaviorType.ContainsGenericParameters && !behaviorType.IsGenericTypeDefinition))
            throw new ArgumentException("A concrete closed type or generic type definition is required.", nameof(behaviorType));

        var mappings = behaviorType.GetInterfaces()
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == contract).ToArray();
        if (mappings.Length == 0)
            throw new ArgumentException($"The behavior must implement {contract}.", nameof(behaviorType));

        if (behaviorType.IsGenericTypeDefinition)
        {
            // Standard DI closes implementation parameters in service argument order; reordered or fixed
            // arguments would produce a different closed interface and must not be silently registered.
            var arguments = behaviorType.GetGenericArguments();
            if (arguments.Length != 2 || mappings.Length != 1 ||
                !mappings[0].GetGenericArguments().SequenceEqual(arguments))
                throw new ArgumentException("Open behaviors must map both generic parameters in declaration order.", nameof(behaviorType));
            services.Add(ServiceDescriptor.Describe(contract, behaviorType, lifetime));
        }
        else
        {
            foreach (var mapping in mappings)
                services.Add(ServiceDescriptor.Describe(mapping, behaviorType, lifetime));
        }
        return services;
    }

    /// <summary>
    ///     获取当前服务集合中的程序集注册协调器；尚未安装时按需创建并登记实例绑定。
    ///     实例绑定让后续多次 handler 注册共享同一份程序集去重状态。
    ///     注册阶段日志优先复用服务集合中已登记的标准 <see cref="Microsoft.Extensions.Logging.ILoggerFactory" />
    ///     实例；不存在时回退到空实现，注册期诊断默认静默。
    /// </summary>
    private static ICqrsRegistrationService ResolveRegistrationService(IServiceCollection services)
    {
        var existingRegistrationService = services
            .Select(static descriptor => descriptor.ImplementationInstance)
            .OfType<ICqrsRegistrationService>()
            .LastOrDefault();
        if (existingRegistrationService is not null)
            return existingRegistrationService;

        var loggerFactory = services
            .Select(static descriptor => descriptor.ImplementationInstance)
            .OfType<ILoggerFactory>()
            .LastOrDefault();
        var registrarLogger = loggerFactory?.CreateLogger(nameof(DefaultCqrsHandlerRegistrar))
                              ?? NullLogger<DefaultCqrsHandlerRegistrar>.Instance;
        var registrationLogger = loggerFactory?.CreateLogger(nameof(DefaultCqrsRegistrationService))
                                 ?? NullLogger<DefaultCqrsRegistrationService>.Instance;
        var registrar = new DefaultCqrsHandlerRegistrar(services, registrarLogger);
        var registrationService = new DefaultCqrsRegistrationService(registrar, registrationLogger);
        services.AddSingleton(registrationService);
        return registrationService;
    }
}
