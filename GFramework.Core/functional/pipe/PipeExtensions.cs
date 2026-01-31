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
    /// On：将值应用于函数（与Apply功能相同，但参数顺序相反）
    /// </summary>
    /// <typeparam name="TSource">输入值的类型</typeparam>
    /// <typeparam name="TResult">函数返回结果的类型</typeparam>
    /// <param name="value">要传递给函数的输入值</param>
    /// <param name="func">要应用的函数</param>
    /// <returns>函数执行后的结果</returns>
    public static TResult On<TSource, TResult>(
        this TSource value,
        Func<TSource, TResult> func)
        => func(value);

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
}