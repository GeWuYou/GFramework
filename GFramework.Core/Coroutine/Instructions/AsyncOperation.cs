// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Instructions;

/// <summary>
///     把外部完成通知同时暴露为协程等待与标准 Task awaiter。
/// </summary>
/// <remarks>
///     完成可以由任意线程竞争，首次终结获胜。Task 管理延续注册及异常传播；
///     异步延续不会在设置结果的线程内联执行。引擎对象仍须由宿主线程访问。
/// </remarks>
public sealed class AsyncOperation : IYieldInstruction, ICoroutineCompletionStatusSource
{
    private readonly TaskCompletionSource<object?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    ///     获取可重复等待且保留终结结果的任务。
    /// </summary>
    public Task<object?> Task => _completion.Task;

    /// <summary>
    ///     获取外部操作是否已成功、失败或取消。
    /// </summary>
    public bool IsCompleted => Task.IsCompleted;

    /// <inheritdoc />
    public bool IsDone => IsCompleted;

    /// <summary>
    ///     保留显式失败与显式取消的区别，即使失败异常本身是 OperationCanceledException。
    /// </summary>
    CoroutineCompletionStatus ICoroutineCompletionStatusSource.CompletionStatus => Task.IsFaulted
        ? CoroutineCompletionStatus.Faulted
        : Task.IsCanceled ? CoroutineCompletionStatus.Cancelled
        : Task.IsCompleted ? CoroutineCompletionStatus.Completed
        : CoroutineCompletionStatus.Unknown;

    /// <inheritdoc />
    public void Update(double deltaTime)
    {
        if (IsCompleted)
        {
            _ = Task.GetAwaiter().GetResult();
        }
    }

    /// <summary>
    ///     尝试完成操作；重复或竞争的终结请求不会覆盖已有结果。
    /// </summary>
    public void SetCompleted()
    {
        _completion.TrySetResult(null);
    }

    /// <summary>
    ///     尝试以异常终结操作。
    /// </summary>
    /// <param name="exception">要保留并传播给等待者的异常。</param>
    public void SetException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _completion.TrySetException(exception);
    }

    /// <summary>
    ///     尝试以取消状态终结操作。
    /// </summary>
    /// <param name="cancellationToken">描述取消来源的令牌。</param>
    public void SetCanceled(CancellationToken cancellationToken = default)
    {
        _completion.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    ///     返回标准 Task awaiter，避免复制延续竞争和结果传播逻辑。
    /// </summary>
    /// <returns>由 Task 实现的 awaiter。</returns>
    public TaskAwaiter<object?> GetAwaiter()
    {
        return Task.GetAwaiter();
    }
}
