// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     定义框架级 UI 语义动作。
///     这些动作由输入层映射后交给 UI 路由统一仲裁，避免页面直接依赖具体按键或设备事件。
/// </summary>
public enum UiInputAction
{
    /// <summary>
    ///     未指定动作。
    /// </summary>
    None = 0,

    /// <summary>
    ///     取消、返回或关闭当前 UI。
    /// </summary>
    Cancel = 1,

    /// <summary>
    ///     确认当前 UI 操作。
    /// </summary>
    Confirm = 2
}
