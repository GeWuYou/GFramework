// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine;

/// <summary>
///     协程槽位类，用于管理单个协程的执行状态和调度信息
/// </summary>
internal sealed class CoroutineSlot
{
    /// <summary>
    ///     由外部取消令牌创建的注册。
    ///     调度器在协程结束时必须释放该注册，避免泄漏取消回调。
    /// </summary>
    public CancellationTokenRegistration CancellationRegistration;

    /// <summary>
    ///     创建该协程时传入的取消令牌。
    ///     当协程启动子协程时，会把同一个取消令牌继续传递下去，以保持父子协程的取消语义一致。
    /// </summary>
    public CancellationToken CancellationToken;

    /// <summary>
    ///     协程枚举器，包含协程的执行逻辑
    /// </summary>
    public required IEnumerator<IYieldInstruction> Enumerator;

    /// <summary>
    ///     协程句柄，用于标识和管理协程实例
    /// </summary>
    public CoroutineHandle Handle;

    /// <summary>
    ///     协程的优先级
    /// </summary>
    public CoroutinePriority Priority;

    /// <summary>
    ///     协程当前状态
    /// </summary>
    public CoroutineState State;

    /// <summary>
    ///     当前等待的指令，用于控制协程的暂停和恢复
    /// </summary>
    public IYieldInstruction? Waiting;
}