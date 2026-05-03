// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.Abstractions.Cqrs;

/// <summary>
///     表示没有实际返回值的 CQRS 响应类型。
///     该类型用于统一命令与请求的泛型签名，避免引入外部库的 <c>Unit</c> 定义。
/// </summary>
public readonly record struct Unit
{
    /// <summary>
    ///     获取默认的空响应实例。
    /// </summary>
    public static Unit Value { get; } = new();
}
