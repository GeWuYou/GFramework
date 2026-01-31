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
namespace GFramework.Core.functional.functions;

/// <summary>
/// 函数式编程扩展方法集合，提供柯里化、偏函数应用、重复执行、安全执行和缓存等功能
/// </summary>
public static class FunctionExtensions
{
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
        // 循环执行指定次数的函数调用
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
            // 尝试从缓存中获取结果
            if (cache.TryGetValue(x, out var result))
                return result;
            // 缓存未命中时计算结果并存储
            result = func(x);
            cache[x] = result;
            return result;
        };
    }
}
