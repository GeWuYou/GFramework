// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Setting;

/// <summary>
///     可重置设置接口，继承自ISettingsSection接口
///     提供将设置重置为默认值的功能
/// </summary>
public interface IResettable : ISettingsSection
{
    /// <summary>
    ///     重置设置为默认值
    /// </summary>
    void Reset();
}