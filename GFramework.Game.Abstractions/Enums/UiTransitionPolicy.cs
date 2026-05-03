// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Enums;

/// <summary>
///     UI页面过渡策略枚举
///     定义了UI页面在出栈时的不同处理方式
/// </summary>
public enum UiTransitionPolicy
{
    /// <summary>
    ///     独占显示（下层页面 Pause + Suspend）
    /// </summary>
    Exclusive,

    /// <summary>
    ///     覆盖显示（下层页面仅 Pause，不隐藏）
    /// </summary>
    Overlay
}