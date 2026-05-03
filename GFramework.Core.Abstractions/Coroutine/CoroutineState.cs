// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     表示协程的执行状态枚举
/// </summary>
public enum CoroutineState
{
    /// <summary>
    ///     协程正在运行中
    /// </summary>
    Running,

    /// <summary>
    ///     协程已暂停
    /// </summary>
    Paused,

    /// <summary>
    ///     协程已完成执行
    /// </summary>
    Completed,

    /// <summary>
    ///     协程已被取消
    /// </summary>
    Cancelled
}