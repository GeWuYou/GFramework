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

using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.data;

/// <summary>
/// 数据位置接口，定义了数据存储的位置信息和相关属性
/// </summary>
public interface IDataLocation
{
    /// <summary>
    /// 存储键（文件路径 / redis key / db key）
    /// </summary>
    string Key { get; }

    /// <summary>
    /// 存储类型（Local / Remote / Database / Memory）
    /// </summary>
    StorageKind Kind { get; }

    /// <summary>
    /// 命名空间/分区
    /// </summary>
    string? Namespace { get; }

    /// <summary>
    /// 扩展元数据（用于存储额外信息，如压缩、加密等）
    /// </summary>
    IReadOnlyDictionary<string, string>? Metadata { get; }
}