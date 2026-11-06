// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Reflection;
using GFramework.Core.Ioc;
using GFramework.Cqrs;

namespace GFramework.Tests.Common;

/// <summary>
///     为测试项目提供与生产一致的 CQRS 接线入口。
/// </summary>
/// <remarks>
///     该测试基础设施直接复用生产组合根扩展，避免测试复制注册实现细节。
///     runtime 以标准单例工厂注册：容器冻结并构建根 provider 后首次解析时创建；
///     handler 注册发生在冻结前的服务集合阶段。
/// </remarks>
public static class CqrsTestRuntime
{
    /// <summary>
    ///     为测试容器补齐默认 CQRS runtime 注册。
    /// </summary>
    /// <param name="container">目标测试容器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="container" /> 为 <see langword="null" />。</exception>
    /// <remarks>
    ///     该入口只写入服务描述，不在容器冻结前解析任何服务；
    ///     冻结后通过容器公开的 <see cref="IServiceProvider.GetService" /> 解析
    ///     <see cref="GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime" /> 单例。
    /// </remarks>
    public static void RegisterInfrastructure(MicrosoftDiContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        // 幂等包装：测试 SetUp 与 RegisterHandlers 可能各自触发一次安装。
        var services = container.GetServicesUnsafe;
        if (services.Any(static descriptor => descriptor.ServiceType == typeof(GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime)))
        {
            return;
        }

        // CQRS 运行时不要求进程级日志；未提供工厂时使用空输出。
        services.AddCqrsRuntime();
    }

    /// <summary>
    ///     通过与生产一致的注册入口扫描并注册指定程序集中的 CQRS 处理器。
    /// </summary>
    /// <param name="container">承载处理器映射的测试容器。</param>
    /// <param name="assemblies">要扫描的程序集集合。</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="container" /> 或 <paramref name="assemblies" /> 为 <see langword="null" />。
    /// </exception>
    /// <remarks>
    ///     该入口会自动调用 <see cref="RegisterInfrastructure" />，测试通常无需预先手动接线。
    ///     程序集去重与空元素过滤由生产注册入口统一处理。
    /// </remarks>
    public static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);

        RegisterInfrastructure(container);
        container.GetServicesUnsafe.AddCqrsHandlersFromAssemblies(assemblies);
    }
}
