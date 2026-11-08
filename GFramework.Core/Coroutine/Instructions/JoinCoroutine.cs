// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待已启动的协程并保存其结果，不取得目标任务的生命周期所有权。
/// </summary>
/// <remarks>
///     取消等待者不会取消目标。结果由调度器主动交付，因此完成历史淘汰不会使等待挂起。
///     若需要失败或取消向等待者传播，请使用 <see cref="AwaitCoroutine" />。
/// </remarks>
/// <remarks>
///     创建一个等待已有句柄的指令。
/// </remarks>
/// <param name="scheduler">拥有目标任务的调度器。</param>
/// <param name="handle">需要等待的任务。</param>
/// <param name="propagateFailure">是否将目标失败或取消传播给等待者。</param>
public class JoinCoroutine(CoroutineScheduler scheduler, CoroutineHandle handle, bool propagateFailure = false) : IYieldInstruction
{

    /// <summary>
    ///     获取目标所属调度器，供安装等待依赖时验证边界。
    /// </summary>
    internal CoroutineScheduler Scheduler { get; } = scheduler ?? throw new ArgumentNullException(nameof(scheduler));

    /// <summary>
    ///     获取等待的句柄。
    /// </summary>
    internal CoroutineHandle Handle { get; } = handle;

    /// <summary>
    ///     获取结果交付后是否需要传播失败和取消。
    /// </summary>
    internal bool PropagateFailure { get; } = propagateFailure;

    /// <summary>
    ///     获取最终结果；完成前返回 Unknown。
    /// </summary>
    public CoroutineResult Result
    {
        get; private set;
    }

    /// <inheritdoc />
    public bool IsDone
    {
        get; private set;
    }

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        // 调度器维护依赖图并交付结果，避免逐帧查询或额外 Task 分配。
    }

    /// <summary>
    ///     由调度器交付一次最终结果。
    /// </summary>
    /// <param name="result">目标任务的终结结果。</param>
    internal void Complete(CoroutineResult result)
    {
        if (IsDone)
        {
            return;
        }

        Result = result;
        IsDone = true;
    }
}
