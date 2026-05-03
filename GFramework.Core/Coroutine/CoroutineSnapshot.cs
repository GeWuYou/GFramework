// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>
///     表示某个活跃协程在调度器中的只读运行快照。
/// </summary>
/// <param name="Handle">协程句柄。</param>
/// <param name="State">当前协程状态。</param>
/// <param name="Priority">当前协程优先级。</param>
/// <param name="Tag">可选标签。</param>
/// <param name="Group">可选分组。</param>
/// <param name="StartTimeMs">协程启动时间，单位为毫秒。</param>
/// <param name="IsWaiting">当前是否正被等待指令阻塞。</param>
/// <param name="WaitingInstructionType">
///     当前等待指令的具体类型。
///     若协程当前未处于等待状态，则该值为 <see langword="null" />。
/// </param>
/// <param name="ExecutionStage">所属调度器的执行阶段。</param>
public readonly record struct CoroutineSnapshot(
    CoroutineHandle Handle,
    CoroutineState State,
    CoroutinePriority Priority,
    string? Tag,
    string? Group,
    double StartTimeMs,
    bool IsWaiting,
    Type? WaitingInstructionType,
    CoroutineExecutionStage ExecutionStage);