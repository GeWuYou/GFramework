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

using System.Collections.Concurrent;

namespace GFramework.Core.functional.functions;

/// <summary>
///     函数式编程扩展方法集合，提供柯里化、偏函数应用、重复执行、安全执行和缓存等功能
/// </summary>
public static class FunctionExtensions
{
    #region Repeat

    /// <summary>
    ///     Repeat：对值重复应用函数 n 次
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     当 times 小于 0 时抛出
    /// </exception>
    public static T Repeat<T>(
        this T value,
        int times,
        Func<T, T> func)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(times);

        var result = value;
        for (var i = 0; i < times; i++) result = func(result);

        return result;
    }

    #endregion

    #region Try → Result

    /// <summary>
    ///     Try：安全执行并返回 language-ext 的 Result
    /// </summary>
    public static Result<TResult> Try<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> func)
    {
        try
        {
            return new Result<TResult>(func(value));
        }
        catch (Exception ex)
        {
            return new Result<TResult>(ex);
        }
    }

    #endregion

    #region Memoize (Unbounded / Unsafe)

    /// <summary>
    ///     MemoizeUnbounded：
    ///     对函数结果进行无界缓存（线程安全）
    ///     ⚠ 注意：
    ///     - 缓存永不释放
    ///     - TSource 必须具有稳定的 Equals / GetHashCode
    ///     - 仅适用于纯函数
    /// </summary>
    public static Func<TSource, TResult> MemoizeUnbounded<TSource, TResult>(
        this Func<TSource, TResult> func)
        where TSource : notnull
    {
        var cache = new ConcurrentDictionary<TSource, TResult>();
        return key => cache.GetOrAdd(key, func);
    }

    #endregion

    #region Partial (Advanced)

    /// <summary>
    ///     Partial：部分应用（二参数函数固定第一个参数）
    ///     ⚠ 偏函数应用属于高级用法，不建议在业务代码滥用
    /// </summary>
    public static Func<T2, TResult> Partial<T1, T2, TResult>(
        this Func<T1, T2, TResult> func,
        T1 firstArg)
    {
        return second => func(firstArg, second);
    }

    #endregion
}