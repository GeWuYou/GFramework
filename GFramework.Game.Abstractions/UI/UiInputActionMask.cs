// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     以位标记形式声明 UI 页面要捕获的语义动作集合。
/// </summary>
[Flags]
public enum UiInputActionMask
{
    /// <summary>
    ///     不捕获任何动作。
    /// </summary>
    None = 0,

    /// <summary>
    ///     捕获取消动作。
    /// </summary>
    Cancel = 1 << 0,

    /// <summary>
    ///     捕获确认动作。
    /// </summary>
    Confirm = 1 << 1
}
