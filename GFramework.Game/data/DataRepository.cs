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

using GFramework.Core.Abstractions.storage;
using GFramework.Core.extensions;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.data.events;
using GFramework.Game.extensions;

namespace GFramework.Game.data;

/// <summary>
///     数据仓库类，用于管理游戏数据的存储和读取
/// </summary>
/// <param name="storage">存储接口实例</param>
/// <param name="options">数据仓库配置选项</param>
public class DataRepository(IStorage? storage, DataRepositoryOptions? options = null)
    : AbstractContextUtility, IDataRepository
{
    private readonly DataRepositoryOptions _options = options ?? new DataRepositoryOptions();
    private IStorage? _storage = storage;

    private IStorage Storage => _storage ??
                                throw new InvalidOperationException(
                                    "Failed to initialize storage. No IStorage utility found in context.");

    /// <summary>
    ///     异步加载指定位置的数据
    /// </summary>
    /// <typeparam name="T">数据类型，必须实现IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <returns>加载的数据对象</returns>
    public async Task<T> LoadAsync<T>(IDataLocation location)
        where T : class, IData, new()
    {
        var key = location.ToStorageKey();

        T result;
        // 检查存储中是否存在指定键的数据
        if (await Storage.ExistsAsync(key))
            result = await Storage.ReadAsync<T>(key);
        else
            result = new T();

        // 如果启用事件功能，则发送数据加载完成事件
        if (_options.EnableEvents)
            this.SendEvent(new DataLoadedEvent<T>(result));

        return result;
    }

    /// <summary>
    ///     异步保存数据到指定位置
    /// </summary>
    /// <typeparam name="T">数据类型，必须实现IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <param name="data">要保存的数据对象</param>
    public async Task SaveAsync<T>(IDataLocation location, T data)
        where T : class, IData
    {
        var key = location.ToStorageKey();

        // 自动备份
        if (_options.AutoBackup && await Storage.ExistsAsync(key))
        {
            var backupKey = $"{key}.backup";
            var existing = await Storage.ReadAsync<T>(key);
            await Storage.WriteAsync(backupKey, existing);
        }

        await Storage.WriteAsync(key, data);

        if (_options.EnableEvents)
            this.SendEvent(new DataSavedEvent<T>(data));
    }

    /// <summary>
    ///     检查指定位置的数据是否存在
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>如果数据存在返回true，否则返回false</returns>
    public Task<bool> ExistsAsync(IDataLocation location)
    {
        return Storage.ExistsAsync(location.ToStorageKey());
    }

    /// <summary>
    ///     异步删除指定位置的数据
    /// </summary>
    /// <param name="location">数据位置信息</param>
    public async Task DeleteAsync(IDataLocation location)
    {
        var key = location.ToStorageKey();
        await Storage.DeleteAsync(key);
        if (_options.EnableEvents)
            this.SendEvent(new DataDeletedEvent(location));
    }

    /// <summary>
    ///     异步批量保存多个数据项
    /// </summary>
    /// <param name="dataList">包含数据位置和数据对象的枚举集合</param>
    public async Task SaveAllAsync(IEnumerable<(IDataLocation location, IData data)> dataList)
    {
        var valueTuples = dataList.ToList();
        foreach (var (location, data) in valueTuples) await SaveAsync(location, data);

        if (_options.EnableEvents)
            this.SendEvent(new DataBatchSavedEvent(valueTuples));
    }

    protected override void OnInit()
    {
        _storage ??= this.GetUtility<IStorage>()!;
    }
}