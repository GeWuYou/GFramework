// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     由页面视图实现，用于接收路由仲裁后的 UI 语义动作。
/// </summary>
public interface IUiActionHandler
{
    /// <summary>
    ///     处理一个 UI 语义动作。
    /// </summary>
    /// <param name="action">当前要处理的动作。</param>
    /// <returns>
    ///     如果页面已经完成处理则返回 <see langword="true" />；
    ///     返回 <see langword="false" /> 时，路由器仍会把声明捕获该动作视为已消费。
    /// </returns>
    bool TryHandleUiAction(UiInputAction action);
}
