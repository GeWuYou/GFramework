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
/// 提供函数式编程中的管道和组合操作扩展方法
/// </summary>
public static class PipeExtensions
{
    /// <summary>
    /// Pipe：把值送进函数（value.Pipe(func)）
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="value">要传递给函数的输入值</param>
    /// <param name="func">接收输入值并返回结果的函数</param>
    /// <returns>函数执行后的结果</returns>
    public static TResult Pipe<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> func)
        => func(value);

    /// <summary>
    /// Compose：函数组合（f1.Then(f2)）
    /// </summary>
    /// <typeparam name="TSource">第一个函数的输入类型</typeparam>
    /// <typeparam name="TMiddle">第一个函数的输出类型，也是第二个函数的输入类型</typeparam>
    /// <typeparam name="TResult">第二个函数的输出类型</typeparam>
    /// <param name="first">第一个要执行的函数</param>
    /// <param name="second">第二个要执行的函数</param>
    /// <returns>组合后的新函数，先执行first再执行second</returns>
    public static Func<TSource, TResult> Then<TSource, TMiddle, TResult>(
        this Func<TSource, TMiddle> first,
        Func<TMiddle, TResult> second)
        => x => second(first(x));
    
    /// <summary>
    /// Compose：反向组合（f2.After(f1)）
    /// </summary>
    /// <typeparam name="TSource">第一个函数的输入类型</typeparam>
    /// <typeparam name="TMiddle">第一个函数的输出类型，也是第二个函数的输入类型</typeparam>
    /// <typeparam name="TResult">第二个函数的输出类型</typeparam>
    /// <param name="second">第二个要执行的函数</param>
    /// <param name="first">第一个要执行的函数</param>
    /// <returns>组合后的新函数，先执行first再执行second</returns>
    public static Func<TSource, TResult> After<TSource, TMiddle, TResult>(
        this Func<TMiddle, TResult> second,
        Func<TSource, TMiddle> first)
        => x => second(first(x));
    
    /// <summary>
    /// Tap：执行副作用操作但返回原值（用于调试、日志等）
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要执行操作的输入值</param>
    /// <param name="action">要执行的副作用操作</param>
    /// <returns>原始输入值</returns>
    public static TSource Tap<TSource>(
        this TSource value,
        Action<TSource> action)
    {
        action(value);
        return value;
    }

    /// <summary>
    /// Map：对集合中的每个元素应用函数
    /// </summary>
    /// <typeparam name="TSource">源集合元素的类型</typeparam>
    /// <typeparam name="TResult">映射后集合元素的类型</typeparam>
    /// <param name="source">要映射的源集合</param>
    /// <param name="selector">用于转换元素的函数</param>
    /// <returns>映射后的元素序列</returns>
    public static IEnumerable<TResult> Map<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
        => source.Select(selector);

    /// <summary>
    /// Filter：过滤集合
    /// </summary>
    /// <typeparam name="TSource">集合元素的类型</typeparam>
    /// <param name="source">要过滤的源集合</param>
    /// <param name="predicate">用于确定是否包含元素的条件函数</param>
    /// <returns>满足条件的元素序列</returns>
    public static IEnumerable<TSource> Filter<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
        => source.Where(predicate);

    /// <summary>
    /// Reduce：将集合归约为单个值
    /// </summary>
    /// <typeparam name="TSource">集合元素的类型</typeparam>
    /// <typeparam name="TResult">归约结果的类型</typeparam>
    /// <param name="source">要归约的源集合</param>
    /// <param name="seed">初始累加值</param>
    /// <param name="accumulator">累加器函数</param>
    /// <returns>归约后的最终值</returns>
    public static TResult Reduce<TSource, TResult>(
        this IEnumerable<TSource> source,
        TResult seed,
        Func<TResult, TSource, TResult> accumulator)
        => source.Aggregate(seed, accumulator);

    /// <summary>
    /// Apply：将函数应用于值（柯里化辅助）
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="func">要应用的函数</param>
    /// <param name="value">要传递给函数的输入值</param>
    /// <returns>函数执行后的结果</returns>
    public static TResult Apply<TSource, TResult>(
        this Func<TSource, TResult> func,
        TSource value)
        => func(value);

    /// <summary>
    /// Curry：将二参数函数转换为柯里化形式
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型</typeparam>
    /// <typeparam name="T2">第二个参数的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="func">要柯里化的二参数函数</param>
    /// <returns>柯里化后的函数，接受一个参数并返回另一个函数</returns>
    public static Func<T1, Func<T2, TResult>> Curry<T1, T2, TResult>(
        this Func<T1, T2, TResult> func)
        => x => y => func(x, y);

    /// <summary>
    /// Uncurry：将柯里化函数转换回二参数函数
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型</typeparam>
    /// <typeparam name="T2">第二个参数的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="func">要取消柯里化的函数</param>
    /// <returns>恢复为二参数的函数</returns>
    public static Func<T1, T2, TResult> Uncurry<T1, T2, TResult>(
        this Func<T1, Func<T2, TResult>> func)
        => (x, y) => func(x)(y);

    /// <summary>
    /// Partial：部分应用函数（固定第一个参数）
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型</typeparam>
    /// <typeparam name="T2">第二个参数的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="func">要部分应用的二参数函数</param>
    /// <param name="firstArg">要固定的第一个参数值</param>
    /// <returns>部分应用后的函数，只接受第二个参数</returns>
    public static Func<T2, TResult> Partial<T1, T2, TResult>(
        this Func<T1, T2, TResult> func,
        T1 firstArg)
        => x => func(firstArg, x);

    /// <summary>
    /// Match：模式匹配（类似switch表达式）
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">匹配结果的类型</typeparam>
    /// <param name="value">要进行模式匹配的输入值</param>
    /// <param name="cases">匹配案例数组，每个包含谓词和处理器</param>
    /// <returns>匹配到的处理结果</returns>
    /// <exception cref="InvalidOperationException">当没有匹配的案例时抛出</exception>
    public static TResult Match<TSource, TResult>(
        this TSource value,
        params (Func<TSource, bool> predicate, Func<TSource, TResult> handler)[] cases)
    {
        foreach (var (predicate, handler) in cases)
        {
            if (predicate(value))
                return handler(value);
        }
        throw new InvalidOperationException("No matching case found");
    }

    /// <summary>
    /// MatchOrDefault：带默认值的模式匹配
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">匹配结果的类型</typeparam>
    /// <param name="value">要进行模式匹配的输入值</param>
    /// <param name="defaultValue">当没有匹配案例时的默认返回值</param>
    /// <param name="cases">匹配案例数组，每个包含谓词和处理器</param>
    /// <returns>匹配到的处理结果或默认值</returns>
    public static TResult MatchOrDefault<TSource, TResult>(
        this TSource value,
        TResult defaultValue,
        params (Func<TSource, bool> predicate, Func<TSource, TResult> handler)[] cases)
    {
        foreach (var (predicate, handler) in cases)
        {
            if (predicate(value))
                return handler(value);
        }
        return defaultValue;
    }

    /// <summary>
    /// If：条件执行
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <param name="thenFunc">条件为真时执行的转换函数</param>
    /// <returns>条件为真时返回转换后的值，否则返回原值</returns>
    public static TSource If<TSource>(
        this TSource value,
        Func<TSource, bool> predicate,
        Func<TSource, TSource> thenFunc)
        => predicate(value) ? thenFunc(value) : value;

    /// <summary>
    /// IfElse：条件分支
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <param name="thenFunc">条件为真时执行的转换函数</param>
    /// <param name="elseFunc">条件为假时执行的转换函数</param>
    /// <returns>根据条件返回相应的转换结果</returns>
    public static TSource IfElse<TSource>(
        this TSource value,
        Func<TSource, bool> predicate,
        Func<TSource, TSource> thenFunc,
        Func<TSource, TSource> elseFunc)
        => predicate(value) ? thenFunc(value) : elseFunc(value);

    /// <summary>
    /// As：类型转换（安全转换）
    /// </summary>
    /// <typeparam name="TSource">源类型的泛型参数</typeparam>
    /// <typeparam name="TResult">目标类型的泛型参数</typeparam>
    /// <param name="value">要进行类型转换的值</param>
    /// <returns>转换后的值，如果转换失败则返回null</returns>
    public static TResult? As<TSource, TResult>(
        this TSource value)
        where TResult : class
        => value as TResult;

    /// <summary>
    /// Cast：强制类型转换
    /// </summary>
    /// <typeparam name="TResult">目标类型的泛型参数</typeparam>
    /// <param name="value">要进行类型转换的对象</param>
    /// <returns>强制转换后的值</returns>
    public static TResult Cast<TResult>(
        this object value)
        => (TResult)value;

    /// <summary>
    /// Also：执行操作并返回原值
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要执行操作的输入值</param>
    /// <param name="action">要执行的操作</param>
    /// <returns>原始输入值</returns>
    public static TSource Also<TSource>(
        this TSource value,
        Action<TSource> action)
    {
        action(value);
        return value;
    }

    /// <summary>
    /// Let：将值转换为另一个值
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">转换结果的类型</typeparam>
    /// <param name="value">要进行转换的输入值</param>
    /// <param name="transform">用于转换值的函数</param>
    /// <returns>转换后的结果</returns>
    public static TResult Let<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> transform)
        => transform(value);

    /// <summary>
    /// TakeIf：条件返回值或null
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>条件为真时返回原值，否则返回null</returns>
    public static TSource? TakeIf<TSource>(
        this TSource value,
        Func<TSource, bool> predicate)
        where TSource : class
        => predicate(value) ? value : null;

    /// <summary>
    /// TakeUnless：条件相反的TakeIf
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">要进行条件判断的输入值</param>
    /// <param name="predicate">条件判断函数</param>
    /// <returns>条件为假时返回原值，否则返回null</returns>
    public static TSource? TakeUnless<TSource>(
        this TSource value,
        Func<TSource, bool> predicate)
        where TSource : class
        => !predicate(value) ? value : null;

    /// <summary>
    /// Repeat：重复执行函数n次
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <param name="value">初始输入值</param>
    /// <param name="times">重复执行的次数</param>
    /// <param name="func">要重复执行的函数</param>
    /// <returns>经过多次变换后的最终值</returns>
    public static TSource Repeat<TSource>(
        this TSource value,
        int times,
        Func<TSource, TSource> func)
    {
        var result = value;
        for (int i = 0; i < times; i++)
        {
            result = func(result);
        }
        return result;
    }

    /// <summary>
    /// Try：安全执行，捕获异常
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="value">要传递给函数的输入值</param>
    /// <param name="func">要安全执行的函数</param>
    /// <returns>包含执行状态、结果和错误信息的元组</returns>
    public static (bool success, TResult? result, Exception? error) Try<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> func)
    {
        try
        {
            return (true, func(value), null);
        }
        catch (Exception ex)
        {
            return (false, default, ex);
        }
    }

    /// <summary>
    /// Memoize：缓存函数结果
    /// </summary>
    /// <typeparam name="TSource">函数输入参数的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="func">要缓存结果的函数</param>
    /// <returns>带有缓存功能的包装函数</returns>
    public static Func<TSource, TResult> Memoize<TSource, TResult>(
        this Func<TSource, TResult> func)
        where TSource : notnull
    {
        var cache = new Dictionary<TSource, TResult>();
        return x =>
        {
            if (cache.TryGetValue(x, out var result))
                return result;
            result = func(x);
            cache[x] = result;
            return result;
        };
    }
}
