// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     表示协程的最终完成结果。
/// </summary>
public enum CoroutineCompletionStatus
{
    /// <summary>
    ///     调度器无法确认该句柄的最终结果。
    ///     这通常意味着句柄无效，或者句柄对应的历史结果已经不可用。
    /// </summary>
    Unknown,

    /// <summary>
    ///     协程自然执行结束。
    /// </summary>
    Completed,

    /// <summary>
    ///     协程被外部终止、清空或取消令牌中断。
    /// </summary>
    Cancelled,

    /// <summary>
    ///     协程在推进过程中抛出了异常。
    /// </summary>
    Faulted
}