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
    /// Also：
    /// 对值执行副作用操作并返回原值
    ///
    /// 适用于日志、调试、状态同步等场景
    /// </summary>
    public static T Also<T>(
        this T value,
        Action<T> action)
    {
        action(value);
        return value;
    }
}