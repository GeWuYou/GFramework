// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程优先级枚举
///     定义协程的执行优先级，高优先级的协程会优先执行
/// </summary>
public enum CoroutinePriority
{
    /// <summary>
    ///     最低优先级
    /// </summary>
    Lowest = 0,

    /// <summary>
    ///     低优先级
    /// </summary>
    Low = 1,

    /// <summary>
    ///     普通优先级（默认）
    /// </summary>
    Normal = 2,

    /// <summary>
    ///     高优先级
    /// </summary>
    High = 3,

    /// <summary>
    ///     最高优先级
    /// </summary>
    Highest = 4
}