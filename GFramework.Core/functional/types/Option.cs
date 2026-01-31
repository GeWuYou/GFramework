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
/// 表示一个可能存在也可能不存在的值
/// </summary>
public readonly struct Option<T>
{
    private readonly T _value;

    /// <summary>
    /// 获取当前Option是否包含值
    /// </summary>
    public bool IsSome { get; }

    /// <summary>
    /// 获取当前Option是否为空值
    /// </summary>
    public bool IsNone => !IsSome;

    /// <summary>
    /// 使用指定值创建Option实例
    /// </summary>
    /// <param name="value">要包装的值</param>
    private Option(T value)
    {
        _value = value;
        IsSome = true;
    }

    /// <summary>
    /// 创建空的Option实例
    /// </summary>
    /// <param name="_">占位参数，用于区分构造函数重载</param>
    private Option(bool _)
    {
        _value = default!;
        IsSome = false;
    }

    /// <summary>
    /// 创建包含指定值的Option实例
    /// </summary>
    /// <param name="value">要包装的值，不能为null</param>
    /// <returns>包含指定值的Option实例</returns>
    /// <exception cref="ArgumentNullException">当value为null时抛出</exception>
    public static Option<T> Some(T value)
    {
        return value is null ? throw new ArgumentNullException(nameof(value)) : new Option<T>(value);
    }

    /// <summary>
    /// 创建空的Option实例
    /// </summary>
    /// <returns>空的Option实例</returns>
    public static Option<T> None() => new(false);

    /// <summary>
    /// 获取Option中包含的值
    /// </summary>
    /// <exception cref="InvalidOperationException">当Option为空时抛出</exception>
    public T Value =>
        IsSome
            ? _value
            : throw new InvalidOperationException("Option has no value");
}