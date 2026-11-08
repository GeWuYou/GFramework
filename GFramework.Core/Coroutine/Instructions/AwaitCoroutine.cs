// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待已有协程，并将目标的失败或取消传播给等待者。
/// </summary>
/// <param name="scheduler">目标所属调度器。</param>
/// <param name="handle">需要等待的任务句柄。</param>
/// <remarks>与 Join 相同，该指令不拥有或取消目标任务。</remarks>
public sealed class AwaitCoroutine(CoroutineScheduler scheduler, CoroutineHandle handle)
    : JoinCoroutine(scheduler, handle, propagateFailure: true);
