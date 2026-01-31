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