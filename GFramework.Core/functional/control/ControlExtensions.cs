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
/// 控制流扩展方法类，提供函数式编程风格的控制结构
/// </summary>
public static class ControlExtensions
{
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
}
