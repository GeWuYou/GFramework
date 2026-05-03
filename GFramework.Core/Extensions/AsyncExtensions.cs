// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Extensions;

/// <summary>
///     异步扩展方法
/// </summary>
public static class AsyncExtensions
{
    /// <summary>
    ///     为任务添加超时限制
    /// </summary>
    /// <typeparam name="T">任务结果类型</typeparam>
    /// <param name="taskFactory">接收取消令牌并返回任务的工厂方法，令牌将在超时或外部取消时触发</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">外部取消令牌</param>
    /// <returns>任务结果</returns>
    /// <exception cref="ArgumentNullException">当 taskFactory 为 null 时抛出</exception>
    /// <exception cref="TimeoutException">当任务超时时抛出</exception>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    /// <example>
    /// <code>
    /// var result = await WithTimeoutAsync(
    ///     ct => SomeAsyncOperation(ct),
    ///     TimeSpan.FromSeconds(5));
    /// </code>
    /// </example>
    public static async Task<T> WithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        // linkedCts 同时响应：超时 + 外部取消
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        Task<T> task;
        try
        {
            // 将联合令牌传入实际任务，超时时任务会收到取消信号
            task = taskFactory(linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"操作在 {timeout.TotalSeconds} 秒后超时");
        }

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested
                                                 && linkedCts.IsCancellationRequested)
        {
            // linkedCts 触发但外部未取消 → 超时
            throw new TimeoutException($"操作在 {timeout.TotalSeconds} 秒后超时");
        }
    }

    /// <summary>
    ///     为任务添加超时限制（无返回值版本）
    /// </summary>
    /// <param name="taskFactory">接收取消令牌并返回任务的工厂方法</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">外部取消令牌</param>
    /// <exception cref="ArgumentNullException">当 taskFactory 为 null 时抛出</exception>
    /// <exception cref="TimeoutException">当任务超时时抛出</exception>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    public static async Task WithTimeoutAsync(
        Func<CancellationToken, Task> taskFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout);

        Task task;
        try
        {
            task = taskFactory(linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"操作在 {timeout.TotalSeconds} 秒后超时");
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested
                                                 && linkedCts.IsCancellationRequested)
        {
            throw new TimeoutException($"操作在 {timeout.TotalSeconds} 秒后超时");
        }
    }

    /// <summary>
    ///     为任务添加失败回退机制
    /// </summary>
    /// <typeparam name="T">任务结果类型</typeparam>
    /// <param name="task">要执行的任务</param>
    /// <param name="fallback">失败时的回退函数</param>
    /// <returns>任务结果或回退值</returns>
    /// <exception cref="ArgumentNullException">当 task 或 fallback 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = await RiskyOperation()
    ///     .WithFallbackAsync(ex => DefaultValue);
    /// </code>
    /// </example>
    public static async Task<T> WithFallbackAsync<T>(this Task<T> task, Func<Exception, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(fallback);

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return fallback(ex);
        }
    }
}
