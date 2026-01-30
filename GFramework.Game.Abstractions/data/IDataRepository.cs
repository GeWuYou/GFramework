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

using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.data;

/// <summary>
///     定义数据仓库接口，提供异步的数据加载、保存、检查存在性和删除操作
/// </summary>
public interface IDataRepository : IUtility
{
    /// <summary>
    /// 异步加载指定位置的数据
    /// </summary>
    /// <typeparam name="T">要加载的数据类型，必须实现IData接口并具有无参构造函数</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <returns>返回加载的数据对象</returns>
    Task<T> LoadAsync<T>(IDataLocation location)
        where T : class, IData, new();

    /// <summary>
    /// 异步保存数据到指定位置
    /// </summary>
    /// <typeparam name="T">要保存的数据类型，必须实现IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <param name="data">要保存的数据对象</param>
    /// <returns>返回异步操作任务</returns>
    Task SaveAsync<T>(IDataLocation location, T data)
        where T : class, IData;

    /// <summary>
    /// 异步检查指定位置是否存在数据
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>返回布尔值，表示数据是否存在</returns>
    Task<bool> ExistsAsync(IDataLocation location);

    /// <summary>
    /// 异步删除指定位置的数据
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>返回异步操作任务</returns>
    Task DeleteAsync(IDataLocation location);

    /// <summary>
    /// 异步批量保存多个数据项到各自的位置
    /// </summary>
    /// <param name="dataList">包含数据位置和对应数据对象的可枚举集合</param>
    /// <returns>返回异步操作任务</returns>
    Task SaveAllAsync(IEnumerable<(IDataLocation location, IData data)> dataList);
}