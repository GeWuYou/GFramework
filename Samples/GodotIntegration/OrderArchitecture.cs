// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;

namespace GodotIntegration;

/// <summary>
///     示例架构：普通服务经由标准 DI 注册，不要求 Model/System/Utility 角色。
/// </summary>
/// <remarks>使用给定架构配置构造实例。</remarks>
/// <param name="configuration">架构配置；包含宿主日志工厂。</param>
public sealed class OrderArchitecture(IArchitectureConfiguration? configuration = null) : Architecture(configuration)
{

    /// <summary>描述服务注册；此阶段只记录描述，不解析实例。</summary>
    /// <param name="services">当前架构将构建和拥有的服务集合。</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<OrderService>();
    }
}
