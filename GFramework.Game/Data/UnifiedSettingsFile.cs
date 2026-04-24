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

using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Versioning;

namespace GFramework.Game.Data;

/// <summary>
///     统一设置文件类，用于管理应用程序的配置设置
///     实现了版本控制接口，支持配置文件的版本管理
/// </summary>
internal sealed class UnifiedSettingsFile : IVersioned
{
    /// <summary>
    ///     配置节映射，存储不同类型的配置数据。
    /// </summary>
    /// <remarks>
    ///     这里公开为 <see cref="IDictionary{TKey,TValue}" /> 而不是具体的 <see cref="Dictionary{TKey,TValue}" />，
    ///     以避免暴露可替换的具体集合实现，同时继续兼容 Newtonsoft.Json 对字典对象的序列化与反序列化。
    ///     默认实例使用 <see cref="StringComparer.Ordinal" />；若调用方提供其他实现，仓库在可以识别底层
    ///     <see cref="Dictionary{TKey,TValue}" /> comparer 时会保留原语义，否则克隆快照时会显式回退到
    ///     <see cref="StringComparer.Ordinal" />。
    /// </remarks>
    public IDictionary<string, string> Sections { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    ///     配置文件版本号，用于版本控制和兼容性检查
    /// </summary>
    public int Version { get; set; }
}
