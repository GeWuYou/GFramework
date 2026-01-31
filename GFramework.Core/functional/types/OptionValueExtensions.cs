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
/// 提供Option类型值的操作扩展方法
/// </summary>
public static class OptionValueExtensions
{
    /// <summary>
    /// 获取Option中的值，如果Option为空则返回默认值
    /// </summary>
    /// <typeparam name="T">Option中存储的值的类型</typeparam>
    /// <param name="option">要获取值的Option对象</param>
    /// <param name="defaultValue">当Option为空时返回的默认值</param>
    /// <returns>Option中的值或默认值</returns>
    public static T GetOrElse<T>(
        this Option<T> option,
        T defaultValue)
        => option.IsSome ? option.Value : defaultValue;

    /// <summary>
    /// 获取Option中的值，如果Option为空则通过工厂函数生成默认值
    /// </summary>
    /// <typeparam name="T">Option中存储的值的类型</typeparam>
    /// <param name="option">要获取值的Option对象</param>
    /// <param name="defaultFactory">当Option为空时用于生成默认值的工厂函数</param>
    /// <returns>Option中的值或通过工厂函数生成的值</returns>
    public static T GetOrElse<T>(
        this Option<T> option,
        Func<T> defaultFactory)
        => option.IsSome ? option.Value : defaultFactory();

    /// <summary>
    /// 获取当前Option，如果当前Option为空则返回备用Option
    /// </summary>
    /// <typeparam name="T">Option中存储的值的类型</typeparam>
    /// <param name="option">当前Option对象</param>
    /// <param name="fallback">当当前Option为空时返回的备用Option</param>
    /// <returns>当前Option或备用Option</returns>
    public static Option<T> OrElse<T>(
        this Option<T> option,
        Option<T> fallback)
        => option.IsSome ? option : fallback;
}