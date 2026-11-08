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


namespace GFramework.Game.Abstractions.Data;

/// <summary>
///     存档仓库接口，管理基于槽位的存档系统
/// </summary>
/// <typeparam name="TSaveData">存档数据类型，必须实现IData接口并具有无参构造函数</typeparam>
public interface ISaveRepository<TSaveData>
    where TSaveData : class, IData, new()
{
    /// <summary>
    ///     检查指定槽位是否存在存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>如果存档存在返回true，否则返回false</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task<bool> ExistsAsync(int slot, CancellationToken cancellationToken = default);

    /// <summary>
    ///     加载指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>存档数据对象，如果不存在则返回新实例</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task<TSaveData> LoadAsync(int slot, CancellationToken cancellationToken = default);

    /// <summary>
    ///     保存存档到指定槽位
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="data">要保存的存档数据</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task SaveAsync(int slot, TSaveData data, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task DeleteAsync(int slot, CancellationToken cancellationToken = default);

    /// <summary>
    ///     列出所有有效的存档槽位
    /// </summary>
    /// <returns>包含所有有效存档槽位编号的只读列表，按升序排列</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    Task<IReadOnlyList<int>> ListSlotsAsync(CancellationToken cancellationToken = default);
}
