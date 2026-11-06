// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace MinimalApp;

/// <summary>示例状态服务；不感知框架类型。</summary>
internal sealed class OrderService
{
    private readonly HashSet<string> _orders = new(StringComparer.Ordinal);

    /// <summary>登记一条订单编号。</summary>
    /// <param name="orderId">订单编号。</param>
    public void Register(string orderId) => _orders.Add(orderId);
}
