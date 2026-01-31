// Copyright (c) 2026 GeWuYou
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

namespace GFramework.Core.functional.types;

/// <summary>
/// 提供Option类型的功能扩展方法，支持映射、绑定、过滤和匹配操作
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// 将Option中的值通过指定的映射函数转换为另一种类型的Option
    /// </summary>
    /// <typeparam name="TSource">源Option中值的类型</typeparam>
    /// <typeparam name="TResult">映射后结果的类型</typeparam>
    /// <param name="option">要进行映射操作的Option实例</param>
    /// <param name="mapper">用于将源值转换为目标值的映射函数</param>
    /// <returns>包含映射后值的新Option实例，如果原Option为空则返回None</returns>
    public static Option<TResult> Map<TSource, TResult>(
        this Option<TSource> option,
        Func<TSource, TResult> mapper)
    {
        return option.IsSome
            ? Option<TResult>.Some(mapper(option.Value))
            : Option<TResult>.None();
    }

    /// <summary>
    /// 将Option中的值通过指定的绑定函数转换为另一个Option，实现Option的扁平化映射
    /// </summary>
    /// <typeparam name="TSource">源Option中值的类型</typeparam>
    /// <typeparam name="TResult">绑定后Option中值的类型</typeparam>
    /// <param name="option">要进行绑定操作的Option实例</param>
    /// <param name="binder">用于将源值转换为新Option的绑定函数</param>
    /// <returns>绑定函数返回的Option实例，如果原Option为空则返回None</returns>
    public static Option<TResult> Bind<TSource, TResult>(
        this Option<TSource> option,
        Func<TSource, Option<TResult>> binder)
    {
        return option.IsSome
            ? binder(option.Value)
            : Option<TResult>.None();
    }

    /// <summary>
    /// 根据指定的谓词函数过滤Option中的值
    /// </summary>
    /// <typeparam name="TSource">Option中值的类型</typeparam>
    /// <param name="option">要进行过滤操作的Option实例</param>
    /// <param name="predicate">用于判断值是否满足条件的谓词函数</param>
    /// <returns>如果Option有值且满足谓词条件则返回原Option，否则返回None</returns>
    public static Option<TSource> Filter<TSource>(
        this Option<TSource> option,
        Func<TSource, bool> predicate)
    {
        return option.IsSome && predicate(option.Value)
            ? option
            : Option<TSource>.None();
    }

    /// <summary>
    /// 对Option进行模式匹配，根据Option的状态执行不同的函数
    /// </summary>
    /// <typeparam name="TSource">Option中值的类型</typeparam>
    /// <typeparam name="TResult">匹配结果的类型</typeparam>
    /// <param name="option">要进行匹配操作的Option实例</param>
    /// <param name="some">当Option包含值时执行的函数</param>
    /// <param name="none">当Option为空时执行的函数</param>
    /// <returns>根据Option状态执行相应函数后的结果</returns>
    public static TResult Match<TSource, TResult>(
        this Option<TSource> option,
        Func<TSource, TResult> some,
        Func<TResult> none)
    {
        return option.IsSome
            ? some(option.Value)
            : none();
    }
}