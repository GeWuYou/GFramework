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

namespace GFramework.Core.functional.control;

/// <summary>
///     控制流扩展方法类，提供函数式编程风格的控制结构
/// </summary>
public static class ControlExtensions
{
    /// <summary>
    ///     TakeIf：条件返回值或null
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>条件为真时返回原值，否则返回null</returns>
    public static TSource? TakeIf<TSource>(
        this TSource value,
        Func<TSource, bool> predicate)
        where TSource : class
    {
        return predicate(value) ? value : null;
    }

    /// <summary>
    ///     TakeUnless：条件相反的TakeIf
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>条件为假时返回原值，否则返回null</returns>
    public static TSource? TakeUnless<TSource>(
        this TSource value,
        Func<TSource, bool> predicate)
        where TSource : class
    {
        return !predicate(value) ? value : null;
    }

    /// <summary>
    ///     TakeIfValue：值类型版本的 TakeIf，返回 Nullable
    /// </summary>
    /// <typeparam name="TSource">值类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>条件为真时返回原值，否则返回 null</returns>
    /// <exception cref="ArgumentNullException">当 predicate 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = 42.TakeIfValue(x => x > 0); // 42
    /// var result2 = -5.TakeIfValue(x => x > 0); // null
    /// </code>
    /// </example>
    public static TSource? TakeIfValue<TSource>(
        this TSource value,
        Func<TSource, bool> predicate)
        where TSource : struct
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return predicate(value) ? value : null;
    }

    /// <summary>
    ///     TakeUnlessValue：值类型版本的 TakeUnless，返回 Nullable
    /// </summary>
    /// <typeparam name="TSource">值类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>条件为假时返回原值，否则返回 null</returns>
    /// <exception cref="ArgumentNullException">当 predicate 为 null 时抛出</exception>
    /// <example>
    /// <code><![CDATA[
    /// var result = 42.TakeUnlessValue(x => x < 0); // 42
    /// var result2 = -5.TakeUnlessValue(x => x < 0); // null
    /// ]]></code>
    /// </example>
    public static TSource? TakeUnlessValue<TSource>(
        this TSource value,
        Func<TSource, bool> predicate)
        where TSource : struct
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return !predicate(value) ? value : null;
    }

    /// <summary>
    ///     When：条件执行，满足条件时执行操作并返回原值
    /// </summary>
    /// <typeparam name="TSource">值的类型</typeparam>
    /// <param name="value">输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <param name="action">满足条件时执行的操作</param>
    /// <returns>原始值</returns>
    /// <exception cref="ArgumentNullException">当 predicate 或 action 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = 42
    ///     .When(x => x > 0, x => Console.WriteLine($"Positive: {x}"))
    ///     .When(x => x % 2 == 0, x => Console.WriteLine("Even"));
    /// </code>
    /// </example>
    public static TSource When<TSource>(
        this TSource value,
        Func<TSource, bool> predicate,
        Action<TSource> action)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(action);

        if (predicate(value))
            action(value);

        return value;
    }

    /// <summary>
    ///     RepeatUntil：重复执行函数直到条件满足
    /// </summary>
    /// <typeparam name="TSource">值的类型</typeparam>
    /// <param name="value">初始值</param>
    /// <param name="func">每次迭代执行的函数</param>
    /// <param name="predicate">停止条件</param>
    /// <param name="maxIterations">最大迭代次数（防止无限循环）</param>
    /// <returns>满足条件时的值</returns>
    /// <exception cref="ArgumentNullException">当 func 或 predicate 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 maxIterations 小于 1 时抛出</exception>
    /// <exception cref="InvalidOperationException">当达到最大迭代次数仍未满足条件时抛出</exception>
    /// <example>
    /// <code>
    /// var result = 1.RepeatUntil(
    ///     x => x * 2,
    ///     x => x >= 100,
    ///     maxIterations: 10
    /// ); // 128
    /// </code>
    /// </example>
    public static TSource RepeatUntil<TSource>(
        this TSource value,
        Func<TSource, TSource> func,
        Func<TSource, bool> predicate,
        int maxIterations = 1000)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1);

        var current = value;
        var iterations = 0;

        while (!predicate(current))
        {
            if (iterations >= maxIterations)
                throw new InvalidOperationException(
                    $"RepeatUntil 达到最大迭代次数 {maxIterations} 但条件仍未满足");

            current = func(current);
            iterations++;
        }

        return current;
    }

    /// <summary>
    ///     Retry：同步重试机制，失败时重试指定次数
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="delayMilliseconds">每次重试之间的延迟（毫秒）</param>
    /// <returns>函数执行结果</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 maxRetries 小于 0 或 delayMilliseconds 小于 0 时抛出</exception>
    /// <exception cref="AggregateException">当所有重试都失败时抛出，包含所有异常</exception>
    /// <example>
    /// <code>
    /// var result = ControlExtensions.Retry(
    ///     () => UnstableOperation(),
    ///     maxRetries: 3,
    ///     delayMilliseconds: 100
    /// );
    /// </code>
    /// </example>
    public static TResult Retry<TResult>(
        Func<TResult> func,
        int maxRetries = 3,
        int delayMilliseconds = 0)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfNegative(delayMilliseconds);

        var exceptions = new List<Exception>();
        var attempts = maxRetries + 1;

        for (var i = 0; i < attempts; i++)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                if (i < maxRetries && delayMilliseconds > 0)
                    Thread.Sleep(delayMilliseconds);
            }
        }

        throw new AggregateException(
            $"操作在 {attempts} 次尝试后仍然失败",
            exceptions);
    }
}