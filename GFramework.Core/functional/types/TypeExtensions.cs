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
namespace GFramework.Core.functional.types;

/// <summary>
/// 提供类型转换相关的扩展方法
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// 安全类型转换方法，将源类型转换为目标类型
    /// 如果转换失败或值为null，则返回null而不抛出异常
    /// </summary>
    /// <typeparam name="TSource">源类型参数</typeparam>
    /// <typeparam name="TResult">目标类型参数，必须为引用类型</typeparam>
    /// <param name="value">需要进行类型转换的源值</param>
    /// <returns>转换成功时返回目标类型实例，失败时返回null</returns>
    public static TResult? As<TSource, TResult>(
        this TSource value)
        where TResult : class
        => value as TResult;

    /// <summary>
    /// 强制类型转换方法，将对象转换为指定的目标类型
    /// 转换失败时会抛出InvalidCastException异常
    /// </summary>
    /// <typeparam name="TResult">目标类型参数</typeparam>
    /// <param name="value">需要进行强制类型转换的对象</param>
    /// <returns>转换后的目标类型实例</returns>
    /// <exception cref="InvalidCastException">当转换失败时抛出此异常</exception>
    public static TResult Cast<TResult>(
        this object value)
        => (TResult)value;
}
