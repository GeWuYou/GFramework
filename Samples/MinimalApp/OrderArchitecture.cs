// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalApp;

/// <summary>
///     示例架构：只使用标准 DI 注册普通服务，不引入 Model/System/Utility 角色。
///     Architecture 在这里只提供实例级的初始化/停止编排与日志工厂所有权。
/// </summary>
internal sealed class OrderArchitecture : Architecture
{
    /// <summary>描述服务注册；此阶段只记录描述，不解析实例。</summary>
    /// <param name="services">当前架构将构建和拥有的服务集合。</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        // 普通服务：无角色标记、无 ContextAware、无基类要求。
        services.AddSingleton<OrderService>();
        services.AddSingleton<OrderProcessor>();
    }

    /// <summary>宿主准备完成后通过标准解析使用普通服务。</summary>
    protected override void OnStarted()
    {
        var processor = Services.GetRequiredService<OrderProcessor>();
        processor.Process("boot");
    }

    /// <summary>提交一条示例订单并输出处理日志。</summary>
    /// <param name="orderId">订单编号。</param>
    public void PlaceDemoOrder(string orderId)
    {
        var processor = Services.GetRequiredService<OrderProcessor>();
        processor.Process(orderId);
    }
}
