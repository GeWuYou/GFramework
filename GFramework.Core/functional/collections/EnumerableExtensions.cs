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
namespace GFramework.Core.functional.collections;

/// <summary>
/// 提供集合的函数式编程扩展方法
/// </summary>
public static class EnumerableExtensions
{
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
}
