// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace MinimalApp;

/// <summary>示例领域服务；通过构造注入获取标准日志与协作服务。</summary>
/// <param name="orders">订单状态服务。</param>
/// <param name="logger">标准日志记录器。</param>
internal sealed class OrderProcessor(OrderService orders, ILogger<OrderProcessor> logger)
{
    /// <summary>处理一条订单并输出日志。</summary>
    /// <param name="orderId">订单编号。</param>
    public void Process(string orderId)
    {
        orders.Register(orderId);
        logger.LogInformation("Order {OrderId} processed", orderId);
    }
}
