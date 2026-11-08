// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     在调度线程上等待 Task，并传播其失败或取消。
/// </summary>
/// <remarks>
///     仅观察传入任务，不拥有或取消它；停止协程不会取消外部 Task。
///     直接读取完成状态无需分配延续，也不会从后台线程调用调度器。
/// </remarks>
public sealed class WaitForTask : IYieldInstruction, ICoroutineCompletionStatusSource, ICoroutineCompletionConsumer
{
    private readonly Task _task;

    /// <summary>
    ///     创建一个观察已有 Task 的等待指令。
    /// </summary>
    /// <param name="task">需要等待的任务。</param>
    public WaitForTask(Task task)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
    }

    /// <summary>
    ///     获取任务持有的异常；该属性不重新抛出异常。
    /// </summary>
    public Exception? Exception => _task.Exception;

    /// <inheritdoc />
    public bool IsDone => _task.IsCompleted;

    /// <summary>
    ///     保留底层 Task 的终结分类，避免把 Faulted Task 中的取消异常误判为正常取消。
    /// </summary>
    CoroutineCompletionStatus ICoroutineCompletionStatusSource.CompletionStatus => _task.IsFaulted
        ? CoroutineCompletionStatus.Faulted
        : _task.IsCanceled ? CoroutineCompletionStatus.Cancelled
        : _task.IsCompleted ? CoroutineCompletionStatus.Completed
        : CoroutineCompletionStatus.Unknown;

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        // 仅在任务完成后取结果，不阻塞驱动线程；GetResult 保留原始异常和取消类型。
        if (_task.IsCompleted)
        {
            _task.GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    void ICoroutineCompletionConsumer.ConsumeCompletion()
    {
        _task.GetAwaiter().GetResult();
    }
}
