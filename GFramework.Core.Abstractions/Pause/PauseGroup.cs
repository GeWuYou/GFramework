// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Pause;

/// <summary>
/// 暂停组枚举，定义不同的暂停作用域
/// </summary>
public enum PauseGroup
{
    /// <summary>
    /// 全局暂停（影响所有系统）
    /// </summary>
    Global = 0,

    /// <summary>
    /// 游戏逻辑暂停（不影响 UI）
    /// </summary>
    Gameplay = 1,

    /// <summary>
    /// 动画暂停
    /// </summary>
    Animation = 2,

    /// <summary>
    /// 音频暂停
    /// </summary>
    Audio = 3,

    /// <summary>
    /// 自定义组 1
    /// </summary>
    Custom1 = 10,

    /// <summary>
    /// 自定义组 2
    /// </summary>
    Custom2 = 11,

    /// <summary>
    /// 自定义组 3
    /// </summary>
    Custom3 = 12
}