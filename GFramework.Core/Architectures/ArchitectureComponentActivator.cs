// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Architectures;

/// <summary>
///     为架构组件的类型注册路径提供实例创建能力。
///     该类型在容器冻结前基于当前服务集合和已注册实例进行激活，
///     使 <see cref="ArchitectureComponentRegistry" /> 可以在注册阶段就物化 System / Model，
///     避免它们在 Ready 之后首次解析时才参与生命周期而导致状态不一致。
/// </summary>
internal sealed class ArchitectureComponentActivator(
    IIocContainer container,
    ILogger logger)
{
    /// <summary>
    ///     预冻结阶段的单例实例缓存。
    ///     该缓存跨越整个架构组件激活周期共享，确保多个组件在同一轮初始化中解析到同一个单例描述时不会重复创建实例。
    /// </summary>
    private readonly Dictionary<Type, object?> _singletonCache = [];

    /// <summary>
    ///     根据当前容器状态创建组件实例。
    ///     激活过程优先复用已经注册到容器中的实例，再按服务描述解析实现类型或工厂方法，
    ///     以兼容构造函数依赖于框架服务、用户实例服务和先前注册组件的场景。
    /// </summary>
    /// <typeparam name="TComponent">要创建的组件类型。</typeparam>
    /// <returns>创建完成的组件实例。</returns>
    public TComponent CreateInstance<TComponent>() where TComponent : class
    {
        var activationProvider = new RegistrationServiceProvider(container, logger, _singletonCache);
        return ActivatorUtilities.CreateInstance<TComponent>(activationProvider);
    }

    /// <summary>
    ///     面向组件注册的轻量级服务提供者。
    ///     该实现只覆盖预冻结阶段需要的解析能力，避免引入完整容器冻结过程。
    /// </summary>
    private sealed class RegistrationServiceProvider(
        IIocContainer container,
        ILogger logger,
        Dictionary<Type, object?> singletonCache) : IServiceProvider
    {
        /// <summary>
        ///     共享的单例缓存。
        ///     该缓存由外层 activator 统一持有，从而把单例复用范围提升到整个组件注册批次，而不是单次实例创建调用。
        /// </summary>
        private readonly Dictionary<Type, object?> _singletonCache = singletonCache;

        /// <summary>
        ///     从当前服务集合中解析指定类型的服务。
        ///     解析顺序为：已注册实例 → 服务描述实例/工厂/实现类型 → 可直接实例化的具体类型。
        /// </summary>
        /// <param name="serviceType">请求解析的服务类型。</param>
        /// <returns>解析到的服务实例；若无法解析则返回 null。</returns>
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
                return this;

            var existingInstance = container.Get(serviceType);
            if (existingInstance != null)
                return existingInstance;

            var descriptor =
                container.GetServicesUnsafe.LastOrDefault(candidate => candidate.ServiceType == serviceType);
            if (descriptor != null)
                return ResolveDescriptor(serviceType, descriptor);

            if (!serviceType.IsAbstract && !serviceType.IsInterface)
                return ActivatorUtilities.CreateInstance(this, serviceType);

            logger.Trace($"Activation provider could not resolve {serviceType.Name}");
            return null;
        }

        /// <summary>
        ///     根据服务描述解析实例，并对单例描述进行缓存。
        ///     这样可以保证同一类型在一次组件注册流程中只创建一次依赖实例。
        /// </summary>
        /// <param name="requestedType">请求的服务类型。</param>
        /// <param name="descriptor">命中的服务描述。</param>
        /// <returns>解析到的实例。</returns>
        private object? ResolveDescriptor(Type requestedType, ServiceDescriptor descriptor)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton &&
                _singletonCache.TryGetValue(requestedType, out var cached))
                return cached;

            object? resolved = descriptor switch
            {
                { ImplementationInstance: not null } => descriptor.ImplementationInstance,
                { ImplementationFactory: not null } => descriptor.ImplementationFactory(this),
                { ImplementationType: not null } => ActivatorUtilities.CreateInstance(this,
                    descriptor.ImplementationType),
                _ => null
            };

            if (descriptor.Lifetime == ServiceLifetime.Singleton && resolved != null)
                _singletonCache[requestedType] = resolved;

            return resolved;
        }
    }
}