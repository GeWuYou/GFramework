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

using GFramework.Game.Abstractions.data;

namespace GFramework.Game.extensions;

/// <summary>
///     提供数据位置相关的扩展方法
/// </summary>
public static class DataLocationExtensions
{
    /// <summary>
    ///     将数据位置转换为存储键
    /// </summary>
    /// <param name="location">数据位置对象</param>
    /// <returns>格式化的存储键字符串，如果命名空间为空则返回键值，否则返回"命名空间/键值"格式</returns>
    public static string ToStorageKey(this IDataLocation location)
    {
        return string.IsNullOrEmpty(location.Namespace) ? location.Key : $"{location.Namespace}/{location.Key}";
    }
}