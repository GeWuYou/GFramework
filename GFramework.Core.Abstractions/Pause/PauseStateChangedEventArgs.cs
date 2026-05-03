// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Pause;

/// <summary>
///     表示暂停状态变化事件的数据。
///     该类型用于向事件订阅者传递暂停组以及该组变化后的暂停状态。
/// </summary>
public sealed class PauseStateChangedEventArgs : EventArgs
{
    /// <summary>
    ///     初始化 <see cref="PauseStateChangedEventArgs"/> 的新实例。
    /// </summary>
    /// <param name="group">发生状态变化的暂停组。</param>
    /// <param name="isPaused">暂停组变化后的新状态。</param>
    public PauseStateChangedEventArgs(PauseGroup group, bool isPaused)
    {
        Group = group;
        IsPaused = isPaused;
    }

    /// <summary>
    ///     获取发生状态变化的暂停组。
    /// </summary>
    public PauseGroup Group { get; }

    /// <summary>
    ///     获取暂停组变化后的新状态。
    ///     为 <see langword="true"/> 表示进入暂停，为 <see langword="false"/> 表示恢复运行。
    /// </summary>
    public bool IsPaused { get; }
}