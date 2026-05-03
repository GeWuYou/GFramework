// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Time;

/// <summary>
///     时间提供者接口，用于抽象时间获取以支持测试
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    ///     获取当前 UTC 时间
    /// </summary>
    DateTime UtcNow { get; }
}