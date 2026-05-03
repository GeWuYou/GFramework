// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     表示协程调度器当前所处的执行阶段。
/// </summary>
/// <remarks>
///     某些等待指令具有阶段语义，例如 <c>WaitForFixedUpdate</c> 和 <c>WaitForEndOfFrame</c>。
///     宿主应为这些语义提供匹配的调度器阶段，否则这类等待不会自然完成。
/// </remarks>
public enum CoroutineExecutionStage
{
    /// <summary>
    ///     默认更新阶段。
    ///     普通时间等待、下一帧等待以及大多数条件等待都会在该阶段推进。
    /// </summary>
    Update,

    /// <summary>
    ///     固定更新阶段。
    ///     仅与固定步相关的等待指令会在该阶段完成。
    /// </summary>
    FixedUpdate,

    /// <summary>
    ///     帧结束阶段。
    ///     仅与帧尾或延迟执行相关的等待指令会在该阶段完成。
    /// </summary>
    EndOfFrame
}