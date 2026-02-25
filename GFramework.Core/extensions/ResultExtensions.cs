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

using GFramework.Core.Functional;

namespace GFramework.Core.Extensions;

/// <summary>
///     Result 类型的扩展方法，提供函数式编程支持
/// </summary>
public static class ResultExtensions
{
    #region 聚合

    /// <summary>
    ///     将多个结果合并，全部成功则返回值列表，遇到第一个失败即短路返回
    /// </summary>
    /// <example>
    /// <code>
    /// var combined = new[]
    /// {
    ///     Result&lt;int&gt;.Succeed(1),
    ///     Result&lt;int&gt;.Succeed(2),
    ///     Result&lt;int&gt;.Succeed(3)
    /// }.Combine(); // Result&lt;List&lt;int&gt;&gt; [1, 2, 3]
    /// </code>
    /// </example>
    public static Result<List<T>> Combine<T>(
        this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var values = new List<T>();
        foreach (var result in results)
        {
            if (result.IsFaulted)
                return Result<List<T>>.Fail(result.Exception);

            if (result.IsBottom)
                return Result<List<T>>.Fail(new InvalidOperationException("Cannot combine Bottom results"));

            if (result.IsSuccess)
                result.IfSucc(values.Add);
        }

        return Result<List<T>>.Succeed(values);
    }

    #endregion

    #region 变换

    /// <summary>
    ///     映射成功值到新类型
    /// </summary>
    /// <example>
    /// <code>
    /// var result = Result&lt;int&gt;.Succeed(42);
    /// var mapped = result.Map(x => x.ToString()); // Result&lt;string&gt; "42"
    /// </code>
    /// </example>
    public static Result<TResult> Map<T, TResult>(
        this Result<T> result,
        Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return result.Map(mapper);
    }

    /// <summary>
    ///     绑定操作，将结果链式转换
    /// </summary>
    /// <example>
    /// <code>
    /// var result = Result&lt;int&gt;.Succeed(42);
    /// var bound = result.Bind(x => x > 0
    ///     ? Result&lt;string&gt;.Succeed(x.ToString())
    ///     : Result&lt;string&gt;.Fail(new ArgumentException("Value must be positive")));
    /// </code>
    /// </example>
    public static Result<TResult> Bind<T, TResult>(
        this Result<T> result,
        Func<T, Result<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return result.Bind(binder);
    }

    /// <summary>
    ///     异步绑定操作，将结果链式转换为异步结果
    /// </summary>
    /// <example>
    /// <code>
    /// var result = Result&lt;int&gt;.Succeed(42);
    /// var bound = await result.BindAsync(async x =>
    ///     await GetUserAsync(x) is User user
    ///         ? Result&lt;User&gt;.Succeed(user)
    ///         : Result&lt;User&gt;.Fail(new Exception("User not found")));
    /// </code>
    /// </example>
    public static async Task<Result<TResult>> BindAsync<T, TResult>(
        this Result<T> result,
        Func<T, Task<Result<TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return result.IsSuccess
            ? await binder(result.Match(succ: v => v, fail: _ => throw new InvalidOperationException()))
            : Result<TResult>.Fail(result.Exception);
    }

    #endregion

    #region 副作用

    /// <summary>
    ///     成功时执行副作用，返回原始结果（可链式调用）
    /// </summary>
    /// <example>
    /// <code>
    /// Result&lt;int&gt;.Succeed(42)
    ///     .OnSuccess(x => Console.WriteLine($"Value: {x}"))
    ///     .OnFailure(ex => Console.WriteLine($"Error: {ex.Message}"));
    /// </code>
    /// </example>
    public static Result<T> OnSuccess<T>(
        this Result<T> result,
        Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        result.IfSucc(action);
        return result;
    }

    /// <summary>
    ///     失败时执行副作用，返回原始结果（可链式调用）
    /// </summary>
    public static Result<T> OnFailure<T>(
        this Result<T> result,
        Action<Exception> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        result.IfFail(action);
        return result;
    }

    #endregion

    #region 验证

    /// <summary>
    ///     确保成功值满足条件，否则转换为失败
    /// </summary>
    /// <example>
    /// <code>
    /// var result = Result&lt;int&gt;.Succeed(42)
    ///     .Ensure(x => x > 0, "Value must be positive")
    ///     .Ensure(x => x &lt; 100, "Value must be less than 100");
    /// </code>
    /// </example>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (result.IsFaulted) return result;

        return result.Match(
            succ: value => predicate(value)
                ? result
                : Result<T>.Fail(new ArgumentException(errorMessage)),
            fail: _ => result
        );
    }

    /// <summary>
    ///     Ensure 的异常重载，可以传入更具体的异常类型
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Func<T, Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(exceptionFactory);

        if (result.IsFaulted) return result;

        return result.Match(
            succ: value => predicate(value)
                ? result
                : Result<T>.Fail(exceptionFactory(value)),
            fail: _ => result
        );
    }

    #endregion

    #region 安全执行

    /// <summary>
    ///     安全执行同步委托，自动捕获异常为 Result
    /// </summary>
    /// <example>
    /// <code>
    /// var result = ResultExtensions.Try(() => int.Parse("42"));
    /// </code>
    /// </example>
    public static Result<T> Try<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return Result<T>.Try(func);
    }

    /// <summary>
    ///     安全执行异步委托，自动捕获异常为 Result
    /// </summary>
    /// <example>
    /// <code>
    /// var result = await ResultExtensions.TryAsync(async () => await GetDataAsync());
    /// </code>
    /// </example>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        try
        {
            return Result<T>.Succeed(await func());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex);
        }
    }

    #endregion

    #region 类型转换

    /// <summary>
    ///     成功时返回值，失败时返回 null（值类型）
    /// </summary>
    public static T? ToNullable<T>(this Result<T> result) where T : struct =>
        result.IsSuccess
            ? result.Match(succ: v => (T?)v, fail: _ => null)
            : null;

    /// <summary>
    ///     将可空引用类型转换为 Result
    /// </summary>
    public static Result<T> ToResult<T>(
        this T? value,
        string errorMessage = "Value is null") where T : class =>
        value is not null
            ? Result<T>.Succeed(value)
            : Result<T>.Fail(new ArgumentNullException(nameof(value), errorMessage));

    /// <summary>
    ///     将可空值类型转换为 Result
    /// </summary>
    public static Result<T> ToResult<T>(
        this T? value,
        string errorMessage = "Value is null") where T : struct =>
        value.HasValue
            ? Result<T>.Succeed(value.Value)
            : Result<T>.Fail(new ArgumentNullException(nameof(value), errorMessage));

    #endregion
}