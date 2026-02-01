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

namespace GFramework.Game.Abstractions.enums;

/// <summary>
///     存储类型枚举，用于标识不同的存储方式
///     此枚举使用 Flags 特性，支持位运算组合多个存储类型
/// </summary>
[Flags]
public enum StorageKinds
{
    /// <summary>
    ///     无存储类型
    /// </summary>
    None = 0,

    /// <summary>
    ///     本地文件系统存储
    /// </summary>
    Local = 1 << 0,

    /// <summary>
    ///     内存存储
    /// </summary>
    Memory = 1 << 1,

    /// <summary>
    ///     远程存储
    /// </summary>
    Remote = 1 << 2,

    /// <summary>
    ///     数据库存储
    /// </summary>
    Database = 1 << 3
}