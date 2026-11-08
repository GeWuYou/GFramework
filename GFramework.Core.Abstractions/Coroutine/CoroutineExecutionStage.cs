// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     表示协程调度器当前所处的执行阶段。
/// </summary>
/// <remarks>
///     SwitchTo 指令显式切换阶段；切换后留在目标阶段，直至再次切换。
///     宿主须声明支持的阶段，并在对应的主线程回调中推进。
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
    ///     宿主延迟调用阶段，不承诺渲染已经完成。
    /// </summary>
    Deferred
}
