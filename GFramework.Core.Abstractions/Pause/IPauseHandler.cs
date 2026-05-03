// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Pause;

/// <summary>
/// 暂停处理器接口，由引擎层实现具体的暂停/恢复逻辑
/// </summary>
public interface IPauseHandler
{
    /// <summary>
    /// 处理器优先级（数值越小优先级越高）
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// 当某个组的暂停状态变化时调用
    /// </summary>
    /// <param name="group">暂停组</param>
    /// <param name="isPaused">是否暂停</param>
    void OnPauseStateChanged(PauseGroup group, bool isPaused);
}