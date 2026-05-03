// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Time;

namespace GFramework.Core.Time;

/// <summary>
///     系统时间提供者，返回真实的系统时间
/// </summary>
public sealed class SystemTimeProvider : ITimeProvider
{
    /// <summary>
    ///     获取当前 UTC 时间
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}