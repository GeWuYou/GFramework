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
/// 提供Nullable类型转换为Option类型的扩展方法
/// </summary>
public static class NullableExtensions
{
    /// <summary>
    /// 将可空引用类型转换为Option类型
    /// </summary>
    /// <typeparam name="T">引用类型</typeparam>
    /// <param name="value">可空的引用类型值</param>
    /// <returns>如果值为null则返回None，否则返回包含该值的Some</returns>
    public static Option<T> ToOption<T>(this T? value)
        where T : class
        => value is null
            ? Option<T>.None()
            : Option<T>.Some(value);

    /// <summary>
    /// 将可空值类型转换为Option类型
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">可空的值类型值</param>
    /// <returns>如果值有值则返回包含该值的Some，否则返回None</returns>
    public static Option<T> ToOption<T>(this T? value)
        where T : struct
        => value.HasValue
            ? Option<T>.Some(value.Value)
            : Option<T>.None();
}