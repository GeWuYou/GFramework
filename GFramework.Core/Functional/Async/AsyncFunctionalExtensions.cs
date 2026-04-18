// Copyright (c) 2025 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace GFramework.Core.Functional.Async;

/// <summary>
///     异步函数式编程扩展方法
/// </summary>
public static class AsyncFunctionalExtensions
{
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
    ///     .WithRetryAsync(maxRetries: 3, delay: TimeSpan.FromSeconds(1));
    /// </code>
    /// </example>
    public static async Task<T> WithRetryAsync<T>(
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
                return await taskFactory().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 若还有重试机会且允许重试，则等待后继续；否则统一包装为 AggregateException 抛出
                if (attempt < maxRetries && shouldRetry(ex))
                {
                    await Task.Delay(delay).ConfigureAwait(false);
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
            var result = await func().ConfigureAwait(false);
            return new Result<T>(result);
        }
        catch (Exception ex)
        {
            return new Result<T>(ex);
        }
    }
}
