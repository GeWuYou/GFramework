// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     实现优先级的服务
/// </summary>
public sealed class PrioritizedService : IPrioritizedService
{
    /// <summary>
    ///     获取或设置优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     获取或设置服务名称
    /// </summary>
    public string? Name { get; set; }
}
