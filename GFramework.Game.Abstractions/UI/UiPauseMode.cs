// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     定义页面显示时与暂停系统的协作模式。
/// </summary>
public enum UiPauseMode
{
    /// <summary>
    ///     页面显示不会触发暂停请求。
    /// </summary>
    None = 0,

    /// <summary>
    ///     页面在可见期间持有一个暂停请求，隐藏或销毁时释放。
    /// </summary>
    WhileVisible = 1
}
