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

using System;

namespace GFramework.Game.Abstractions.Data;

/// <summary>
///     定义存档数据迁移接口，用于将旧版本存档升级到较新的版本。
/// </summary>
/// <typeparam name="TSaveData">
///     存档数据类型。该类型通常需要实现 <see cref="IVersionedData" />，
///     以便仓库在加载时判断当前版本并串联迁移链。
/// </typeparam>
public interface ISaveMigration<TSaveData>
    where TSaveData : class, IData
{
    /// <summary>
    ///     获取迁移前的版本号。
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    ///     获取迁移后的目标版本号。
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    ///     将旧版本存档转换为新版本存档。
    /// </summary>
    /// <param name="oldData">待升级的旧版本存档数据。</param>
    /// <returns>迁移完成后的存档数据。</returns>
    TSaveData Migrate(TSaveData oldData);
}
