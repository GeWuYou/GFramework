// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     UI页面行为提供者接口，用于获取页面行为实例
/// </summary>
public interface IUiPageBehaviorProvider
{
    /// <summary>
    ///     获取页面行为实例
    /// </summary>
    /// <returns>页面行为接口实例</returns>
    IUiPageBehavior GetPage();
}