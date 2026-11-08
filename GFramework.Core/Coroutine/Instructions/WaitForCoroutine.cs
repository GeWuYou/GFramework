// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     启动并等待一个归属于当前流程的子协程。
/// </summary>
/// <param name="coroutine">转交给子协程管理的枚举器。</param>
/// <remarks>
///     该指令只能使用一次。父取消会取消子任务，子失败通过当前流程的失败路径传播。
///     等待已经启动且不属于本流程的句柄时使用 JoinCoroutine 或 AwaitCoroutine。
/// </remarks>
public sealed class WaitForCoroutine(IEnumerator<IYieldInstruction> coroutine) : IYieldInstruction
{
    private readonly IEnumerator<IYieldInstruction> _coroutine =
        coroutine ?? throw new ArgumentNullException(nameof(coroutine));

    /// <summary>
    /// 获取内部协程枚举器
    /// </summary>
    internal IEnumerator<IYieldInstruction> Coroutine => _coroutine;

    /// <summary>
    /// 获取当前等待的协程是否已完成
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    ///     获取子协程的终结结果；结束前为 Unknown。
    /// </summary>
    public CoroutineResult Result { get; private set; }

    /// <summary>
    /// 更新方法，用于处理协程等待逻辑
    /// </summary>
    /// <param name="delta">时间增量</param>
    public void Update(double delta)
    {
    }

    /// <summary>
    ///     在唤醒前交付子协程结果，显式等待不受 Supervisor 故障隔离策略影响。
    /// </summary>
    /// <param name="result">子协程的终结结果。</param>
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
