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

    #region Compose & AndThen

    /// <summary>
    ///     Compose：函数组合，返回 f(g(x))
    ///     数学表示：(f ∘ g)(x) = f(g(x))
    /// </summary>
    /// <typeparam name="T">输入类型</typeparam>
    /// <typeparam name="TIntermediate">中间类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    /// <param name="f">外层函数</param>
    /// <param name="g">内层函数</param>
    /// <returns>组合后的函数</returns>
    /// <exception cref="ArgumentNullException">当 f 或 g 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// Func&lt;int, int&gt; addOne = x => x + 1;
    /// Func&lt;int, int&gt; multiplyTwo = x => x * 2;
    /// var composed = multiplyTwo.Compose(addOne); // (x + 1) * 2
    /// var result = composed(5); // (5 + 1) * 2 = 12
    /// </code>
    /// </example>
    public static Func<T, TResult> Compose<T, TIntermediate, TResult>(
        this Func<TIntermediate, TResult> f,
        Func<T, TIntermediate> g)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(g);
        return x => f(g(x));
    }

    /// <summary>
    ///     AndThen：函数链式调用，返回 g(f(x))
    ///     数学表示：(f >> g)(x) = g(f(x))
    /// </summary>
    /// <typeparam name="T">输入类型</typeparam>
    /// <typeparam name="TIntermediate">中间类型</typeparam>
    /// <typeparam name="TResult">输出类型</typeparam>
    /// <param name="f">第一个函数</param>
    /// <param name="g">第二个函数</param>
    /// <returns>链式调用后的函数</returns>
    /// <exception cref="ArgumentNullException">当 f 或 g 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// Func&lt;int, int&gt; addOne = x => x + 1;
    /// Func&lt;int, int&gt; multiplyTwo = x => x * 2;
    /// var chained = addOne.AndThen(multiplyTwo); // (x + 1) * 2
    /// var result = chained(5); // (5 + 1) * 2 = 12
    /// </code>
    /// </example>
    public static Func<T, TResult> AndThen<T, TIntermediate, TResult>(
        this Func<T, TIntermediate> f,
        Func<TIntermediate, TResult> g)
    {
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(g);
        return x => g(f(x));
    }

    #endregion

    #region Curry & Uncurry

    /// <summary>
    ///     Curry：将二参数函数柯里化为嵌套的单参数函数
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要柯里化的函数</param>
    /// <returns>柯里化后的函数</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// Func&lt;int, int, int&gt; add = (x, y) => x + y;
    /// var curriedAdd = add.Curry();
    /// var add5 = curriedAdd(5);
    /// var result = add5(3); // 8
    /// </code>
    /// </example>
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(
        this Func<T1, T2, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return arg1 => arg2 => func(arg1, arg2);
    }

    /// <summary>
    ///     Curry：将三参数函数柯里化为嵌套的单参数函数
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要柯里化的函数</param>
    /// <returns>柯里化后的函数</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// Func&lt;int, int, int, int&gt; add3 = (x, y, z) => x + y + z;
    /// var curriedAdd = add3.Curry();
    /// var result = curriedAdd(1)(2)(3); // 6
    /// </code>
    /// </example>
    public static Func<T1, Func<T2, Func<T3, TResult>>> Curry<T1, T2, T3, TResult>(
        this Func<T1, T2, T3, TResult> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return arg1 => arg2 => arg3 => func(arg1, arg2, arg3);
    }

    /// <summary>
    ///     Uncurry：将柯里化的函数还原为多参数函数
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">柯里化的函数</param>
    /// <returns>还原后的多参数函数</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// Func&lt;int, Func&lt;int, int&gt;&gt; curriedAdd = x => y => x + y;
    /// var add = curriedAdd.Uncurry();
    /// var result = add(5, 3); // 8
    /// </code>
    /// </example>
    public static Func<T1, T2, TResult> Uncurry<T1, T2, TResult>(
        this Func<T1, Func<T2, TResult>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return (arg1, arg2) => func(arg1)(arg2);
    }

    #endregion

    #region Defer & Once

    /// <summary>
    ///     Defer：延迟执行函数，返回 Lazy&lt;T&gt;
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要延迟执行的函数</param>
    /// <returns>包装了延迟执行的 Lazy 对象</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var lazy = (() => ExpensiveComputation()).Defer();
    /// // 此时尚未执行
    /// var result = lazy.Value; // 首次访问时才执行
    /// </code>
    /// </example>
    public static Lazy<TResult> Defer<TResult>(this Func<TResult> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return new Lazy<TResult>(func);
    }

    /// <summary>
    ///     Once：确保函数只执行一次，后续调用返回缓存的结果（线程安全）
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="func">要执行的函数</param>
    /// <returns>包装后的函数，确保只执行一次</returns>
    /// <exception cref="ArgumentNullException">当 func 为 null 时抛出</exception>
    /// <example>
    /// <code>
    /// var counter = 0;
    /// var once = (() => ++counter).Once();
    /// var result1 = once(); // 1
    /// var result2 = once(); // 1 (不会再次执行)
    /// </code>
    /// </example>
    public static Func<TResult> Once<TResult>(this Func<TResult> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        var lazy = new Lazy<TResult>(func, LazyThreadSafetyMode.ExecutionAndPublication);
        return () => lazy.Value;
    }

    #endregion
}