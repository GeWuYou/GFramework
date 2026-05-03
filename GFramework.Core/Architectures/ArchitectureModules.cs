// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
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
    ///     从指定程序集显式注册 CQRS 处理器。
    ///     该入口用于把默认架构程序集之外的扩展处理器接入当前架构容器。
    /// </summary>
    /// <param name="assembly">包含 CQRS 处理器或生成注册器的程序集。</param>
    /// <exception cref="ArgumentNullException"><paramref name="assembly" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">底层容器已冻结，无法继续注册处理器。</exception>
    public void RegisterCqrsHandlersFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        logger.Debug($"Registering CQRS handlers from assembly: {assembly.FullName ?? assembly.GetName().Name}");
        services.Container.RegisterCqrsHandlersFromAssembly(assembly);
    }

    /// <summary>
    ///     从多个程序集显式注册 CQRS 处理器。
    ///     它会复用容器级去重逻辑，避免模块重复接入相同程序集时重复注册 handler。
    /// </summary>
    /// <param name="assemblies">要接入的程序集集合。</param>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">底层容器已冻结，无法继续注册处理器。</exception>
    public void RegisterCqrsHandlersFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        logger.Debug("Registering CQRS handlers from additional assemblies.");
        services.Container.RegisterCqrsHandlersFromAssemblies(assemblies);
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
