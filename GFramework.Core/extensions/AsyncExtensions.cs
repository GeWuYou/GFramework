namespace GFramework.Core.extensions;

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
    /// var result = await WithTimeout(
    ///     ct => SomeAsyncOperation(ct),
    ///     TimeSpan.FromSeconds(5));
    /// </code>
    /// </example>
    public static async Task<T> WithTimeout<T>(
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
    public static async Task WithTimeout(
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
    ///     为任务工厂添加重试机制
    /// </summary>
    /// <typeparam name="T">任务结果类型</typeparam>
    /// <param name="taskFactory">任务工厂函数</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="delay">重试间隔</param>
    /// <param name="shouldRetry">判断是否应该重试的函数，默认对所有异常重试</param>
    /// <param name="throwOriginal">当为 true 时直接抛出原始异常，否则包装为 AggregateException</param>
    /// <returns>任务结果</returns>
    /// <exception cref="ArgumentNullException">当 taskFactory 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 maxRetries 小于 0 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = await (() => UnreliableOperation())
    ///     .WithRetry(maxRetries: 3, delay: TimeSpan.FromSeconds(1));
    /// </code>
    /// </example>
    public static async Task<T> WithRetry<T>(
        this Func<Task<T>> taskFactory,
        int maxRetries,
        TimeSpan delay,
        Func<Exception, bool>? shouldRetry = null,
        bool throwOriginal = false)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "最大重试次数不能为负数");

        shouldRetry ??= _ => true;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await taskFactory();
            }
            catch (Exception ex)
            {
                // 若还有重试机会且允许重试，则等待后继续；否则统一包装为 AggregateException 抛出
                if (attempt < maxRetries && shouldRetry(ex))
                {
                    await Task.Delay(delay);
                }
                else
                {
                    if (throwOriginal)
                        throw;

                    throw new AggregateException($"操作在 {attempt} 次重试后仍然失败", ex);
                }
            }
        }

        // 理论上不可达，仅满足编译器要求
        throw new AggregateException($"操作在 {maxRetries} 次重试后仍然失败");
    }

    /// <summary>
    ///     安全执行异步操作，将异常包装为 Result 类型
    /// </summary>
    /// <typeparam name="T">任务结果类型</typeparam>
    /// <param name="func">要执行的异步函数</param>
    /// <returns>包含结果或异常的 Result 对象</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = await (() => RiskyOperation()).TryAsync();
    /// result.Match(
    ///     value => Console.WriteLine($"成功: {value}"),
    ///     error => Console.WriteLine($"失败: {error.Message}")
    /// );
    /// </code>
    /// </example>
    public static async Task<Result<T>> TryAsync<T>(this Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            var result = await func();
            return new Result<T>(result);
        }
        catch (Exception ex)
        {
            return new Result<T>(ex);
        }
    }

    /// <summary>
    ///     等待所有任务完成
    /// </summary>
    /// <param name="tasks">任务集合</param>
    /// <exception cref="ArgumentNullException">当 tasks 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var tasks = new[] { Task1(), Task2(), Task3() };
    /// await tasks.WhenAll();
    /// </code>
    /// </example>
    public static Task WhenAll(this IEnumerable<Task> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        return Task.WhenAll(tasks);
    }

    /// <summary>
    ///     等待所有任务完成并返回结果数组
    /// </summary>
    /// <typeparam name="T">任务结果类型</typeparam>
    /// <param name="tasks">任务集合</param>
    /// <returns>所有任务的结果数组</returns>
    /// <exception cref="ArgumentNullException">当 tasks 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var tasks = new[] { GetValue1(), GetValue2(), GetValue3() };
    /// var results = await tasks.WhenAll();
    /// </code>
    /// </example>
    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        return Task.WhenAll(tasks);
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
    ///     .WithFallback(ex => DefaultValue);
    /// </code>
    /// </example>
    public static async Task<T> WithFallback<T>(this Task<T> task, Func<Exception, T> fallback)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(fallback);

        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            return fallback(ex);
        }
    }
}