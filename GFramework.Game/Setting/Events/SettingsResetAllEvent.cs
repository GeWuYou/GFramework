// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Setting;

namespace GFramework.Game.Setting.Events;

/// <summary>
///     表示所有设置重置完成事件
/// </summary>
/// <param name="newSettings">重置后的所有设置</param>
public class SettingsResetAllEvent(IEnumerable<ISettingsSection> newSettings) : ISettingsChangedEvent
{
    /// <summary>
    ///     获取重置后的所有设置
    /// </summary>
    public IReadOnlyCollection<ISettingsSection> NewSettings { get; } = newSettings.ToList();

    /// <summary>
    ///     获取设置类型，固定返回 ISettingsSection
    /// </summary>
    public Type SettingsType => typeof(ISettingsSection);

    /// <summary>
    ///     获取设置实例，批量事件中返回 null
    /// </summary>
    public ISettingsSection Settings => null!;

    /// <summary>
    ///     获取重置时间
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}