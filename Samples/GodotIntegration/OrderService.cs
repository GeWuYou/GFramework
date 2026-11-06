// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace GodotIntegration;

/// <summary>示例状态服务；只依赖标准 DI。</summary>
public sealed class OrderService
{
    private readonly HashSet<string> _orders = new(StringComparer.Ordinal);

    /// <summary>登记一条订单编号并输出日志。</summary>
    /// <param name="orderId">订单编号。</param>
    /// <param name="logger">由 DI 注入的标准日志记录器。</param>
    public void Register(string orderId, ILogger<OrderService> logger)
    {
        _orders.Add(orderId);
        logger.LogInformation("Order {OrderId} registered via Godot host", orderId);
    }
}
