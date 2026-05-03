// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     定义UI弹窗的关闭策略枚举
/// </summary>
public enum UiPopPolicy
{
    /// <summary>
    ///     销毁实例
    /// </summary>
    Destroy,

    /// <summary>
    ///     可恢复
    /// </summary>
    Suspend
}