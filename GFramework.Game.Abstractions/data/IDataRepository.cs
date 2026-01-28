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

namespace GFramework.Game.Abstractions.data;

/// <summary>
/// 定义数据仓库接口，提供异步的数据加载、保存、检查存在性和删除操作
/// </summary>
public interface IDataRepository
{
    /// <summary>
    /// 异步加载指定类型的数据对象
    /// </summary>
    /// <typeparam name="T">要加载的数据类型，必须实现IData接口并具有无参构造函数</typeparam>
    /// <returns>返回加载的数据对象的Task</returns>
    Task<T> LoadAsync<T>() where T : class, IData, new();

    /// <summary>
    /// 异步保存指定的数据对象
    /// </summary>
    /// <typeparam name="T">要保存的数据类型，必须实现IData接口</typeparam>
    /// <param name="data">要保存的数据对象</param>
    /// <returns>表示异步保存操作的Task</returns>
    Task SaveAsync<T>(T data) where T : class, IData;

    /// <summary>
    /// 异步检查指定类型的数据是否存在
    /// </summary>
    /// <typeparam name="T">要检查的数据类型，必须实现IData接口</typeparam>
    /// <returns>返回表示数据是否存在布尔值的Task</returns>
    Task<bool> ExistsAsync<T>() where T : class, IData;

    /// <summary>
    /// 异步删除指定类型的数据
    /// </summary>
    /// <typeparam name="T">要删除的数据类型，必须实现IData接口</typeparam>
    /// <returns>表示异步删除操作的Task</returns>
    Task DeleteAsync<T>() where T : class, IData;

    /// <summary>
    /// 批量保存多个数据
    /// </summary>
    Task SaveAllAsync(IEnumerable<IData> dataList);
}