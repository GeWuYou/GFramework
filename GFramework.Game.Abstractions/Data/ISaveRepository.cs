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
using System.Collections.Generic;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Utility;

namespace GFramework.Game.Abstractions.Data;

/// <summary>
///     存档仓库接口，管理基于槽位的存档系统
/// </summary>
/// <typeparam name="TSaveData">存档数据类型，必须实现IData接口并具有无参构造函数</typeparam>
public interface ISaveRepository<TSaveData> : IUtility
    where TSaveData : class, IData, new()
{
    /// <summary>
    ///     注册存档迁移器。
    /// </summary>
    /// <param name="migration">
    ///     负责将某个旧版本存档升级到新版本的迁移器。
    ///     仅当 <typeparamref name="TSaveData" /> 实现 <see cref="IVersionedData" /> 时该功能才有效。
    /// </param>
    /// <returns>当前存档仓库实例，便于链式注册多个迁移器。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="migration" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     <typeparamref name="TSaveData" /> 未实现 <see cref="IVersionedData" />，因此无法建立版本迁移管线。
    /// </exception>
    ISaveRepository<TSaveData> RegisterMigration(ISaveMigration<TSaveData> migration);

    /// <summary>
    ///     检查指定槽位是否存在存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>如果存档存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(int slot);

    /// <summary>
    ///     加载指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>存档数据对象，如果不存在则返回新实例</returns>
    Task<TSaveData> LoadAsync(int slot);

    /// <summary>
    ///     保存存档到指定槽位
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="data">要保存的存档数据</param>
    Task SaveAsync(int slot, TSaveData data);

    /// <summary>
    ///     删除指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    Task DeleteAsync(int slot);

    /// <summary>
    ///     列出所有有效的存档槽位
    /// </summary>
    /// <returns>包含所有有效存档槽位编号的只读列表，按升序排列</returns>
    Task<IReadOnlyList<int>> ListSlotsAsync();
}
