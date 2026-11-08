// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     等待带返回值的 Task，并在调度线程上传播任务失败或取消。
/// </summary>
/// <typeparam name="T">任务结果类型。</typeparam>
/// <remarks>该等待不拥有外部任务的取消权。</remarks>
public sealed class WaitForTask<T> : IYieldInstruction, ICoroutineCompletionStatusSource
{
    private readonly Task<T> _task;

    /// <summary>
    ///     创建一个观察已有 Task 的等待指令。
    /// </summary>
    /// <param name="task">需要等待的任务。</param>
    public WaitForTask(Task<T> task)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
    }

    /// <summary>
    ///     获取已完成任务的结果。未完成时明确失败，避免阻塞游戏线程。
    /// </summary>
    /// <exception cref="InvalidOperationException">任务尚未完成。</exception>
    public T Result => _task.IsCompleted
        ? _task.GetAwaiter().GetResult()
        : throw new InvalidOperationException("The task has not completed.");

    /// <summary>
    ///     获取任务持有的异常；该属性不重新抛出异常。
    /// </summary>
    public Exception? Exception => _task.Exception;

    /// <inheritdoc />
    public bool IsDone => _task.IsCompleted;

    /// <summary>
    ///     向调度器交付底层 Task 的真实分类，不根据异常类型推断取消。
    /// </summary>
    CoroutineCompletionStatus ICoroutineCompletionStatusSource.CompletionStatus => _task.IsFaulted
        ? CoroutineCompletionStatus.Faulted
        : _task.IsCanceled ? CoroutineCompletionStatus.Cancelled
        : _task.IsCompleted ? CoroutineCompletionStatus.Completed
        : CoroutineCompletionStatus.Unknown;

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        if (_task.IsCompleted)
        {
            _ = _task.GetAwaiter().GetResult();
        }
    }
}
