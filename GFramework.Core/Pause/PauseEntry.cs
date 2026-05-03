// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;

namespace GFramework.Core.Pause;

/// <summary>
/// 暂停条目（内部数据结构）
/// </summary>
internal class PauseEntry
{
    /// <summary>
    /// 令牌 ID
    /// </summary>
    public required Guid TokenId { get; init; }

    /// <summary>
    /// 暂停原因
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// 暂停组
    /// </summary>
    public required PauseGroup Group { get; init; }

    /// <summary>
    /// 创建时间戳
    /// </summary>
    public required DateTime Timestamp { get; init; }
}