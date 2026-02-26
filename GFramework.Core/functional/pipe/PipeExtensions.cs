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

namespace GFramework.Core.functional.pipe;

/// <summary>
///     提供函数式编程中的管道和组合操作扩展方法
/// </summary>
public static class PipeExtensions
{
    /// <summary>
    ///     Also：
    ///     对值执行副作用操作并返回原值
    ///     适用于日志、调试、状态同步等场景
    /// </summary>
    public static T Also<T>(
        this T value,
        Action<T> action)
    {
        action(value);
        return value;
    }

    /// <summary>
    ///     Tap：
    ///     Also 的别名，对值执行副作用操作并返回原值
    ///     提供更符合某些编程风格的命名
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="value">要处理的值</param>
    /// <param name="action">要执行的副作用操作</param>
    /// <returns>原始值</returns>
    /// <exception cref="ArgumentNullException">当 action 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = GetUser()
    ///     .Tap(user => Console.WriteLine($"User: {user.Name}"))
    ///     .Tap(user => _logger.LogInfo($"Processing user {user.Id}"));
    /// </code>
    /// </example>
    public static T Tap<T>(this T value, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action(value);
        return value;
    }

    /// <summary>
    ///     Pipe：
    ///     管道操作符，将值传递给函数并返回结果
    ///     用于构建流式的函数调用链
    /// </summary>
    /// <typeparam name="TSource">输入类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    /// <param name="value">输入值</param>
    /// <param name="func">转换函数</param>
    /// <returns>转换后的值</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = 42
    ///     .Pipe(x => x * 2)
    ///     .Pipe(x => x.ToString())
    ///     .Pipe(s => $"Result: {s}");
    /// </code>
    /// </example>
    public static TResult Pipe<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return func(value);
    }

    /// <summary>
    ///     Let：
    ///     作用域函数，将值传递给函数并返回结果
    ///     与 Pipe 功能相同，但提供不同的语义（Kotlin 风格）
    /// </summary>
    /// <typeparam name="TSource">输入类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    /// <param name="value">输入值</param>
    /// <param name="transform">转换函数</param>
    /// <returns>转换后的值</returns>
    /// <exception cref="ArgumentNullException">当 transform 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = GetUser().Let(user => new UserDto
    /// {
    ///     Id = user.Id,
    ///     Name = user.Name
    /// });
    /// </code>
    /// </example>
    public static TResult Let<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return transform(value);
    }

    /// <summary>
    ///     PipeIf：
    ///     条件管道，根据条件选择不同的转换函数
    /// </summary>
    /// <typeparam name="TSource">输入类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    /// <param name="value">输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <param name="ifTrue">条件为真时的转换函数</param>
    /// <param name="ifFalse">条件为假时的转换函数</param>
    /// <returns>转换后的值</returns>
    /// <exception cref="ArgumentNullException">当任何参数为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var result = 42.PipeIf(
    ///     x => x > 0,
    ///     x => $"Positive: {x}",
    ///     x => $"Non-positive: {x}"
    /// );
    /// </code>
    /// </example>
    public static TResult PipeIf<TSource, TResult>(
        this TSource value,
        Func<TSource, bool> predicate,
        Func<TSource, TResult> ifTrue,
        Func<TSource, TResult> ifFalse)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(ifTrue);
        ArgumentNullException.ThrowIfNull(ifFalse);
        return predicate(value) ? ifTrue(value) : ifFalse(value);
    }
}