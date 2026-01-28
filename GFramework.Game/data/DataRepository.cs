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

namespace GFramework.Game.data;

/// <summary>
/// 数据仓库类，用于管理游戏数据的存储和读取
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
    /// 异步加载指定类型的数据
    /// </summary>
    /// <typeparam name="T">要加载的数据类型，必须实现IData接口</typeparam>
    /// <returns>加载的数据对象</returns>
    public async Task<T> LoadAsync<T>() where T : class, IData, new()
    {
        var key = GetKey<T>();

        T result;
        // 检查存储中是否存在指定键的数据
        if (await Storage.ExistsAsync(key))
        {
            result = await Storage.ReadAsync<T>(key);
        }
        else
        {
            result = new T();
        }

        // 如果启用事件功能，则发送数据加载完成事件
        if (_options.EnableEvents)
            this.SendEvent(new DataLoadedEvent<T>(result));

        return result;
    }

    /// <summary>
    /// 异步加载指定类型的数据（通过Type参数）
    /// </summary>
    /// <param name="type">要加载的数据类型</param>
    /// <returns>加载的数据对象</returns>
    public async Task<IData> LoadAsync(Type type)
    {
        if (!typeof(IData).IsAssignableFrom(type))
            throw new ArgumentException($"{type.Name} does not implement IData");

        if (!type.IsClass || type.GetConstructor(Type.EmptyTypes) == null)
            throw new ArgumentException($"{type.Name} must be a class with parameterless constructor");

        var key = GetKey(type);

        IData result;
        // 检查存储中是否存在指定键的数据
        if (await Storage.ExistsAsync(key))
        {
            result = await Storage.ReadAsync<IData>(key);
        }
        else
        {
            result = (IData)Activator.CreateInstance(type)!;
        }

        // 如果启用事件功能，则发送数据加载完成事件
        if (_options.EnableEvents)
            this.SendEvent(new DataLoadedEvent<IData>(result));

        return result;
    }


    /// <summary>
    /// 异步保存指定类型的数据
    /// </summary>
    /// <typeparam name="T">要保存的数据类型</typeparam>
    /// <param name="data">要保存的数据对象</param>
    public async Task SaveAsync<T>(T data) where T : class, IData
    {
        var key = GetKey<T>();

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
    /// 检查指定类型的数据是否存在
    /// </summary>
    /// <typeparam name="T">要检查的数据类型</typeparam>
    /// <returns>如果数据存在返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync<T>() where T : class, IData
    {
        var key = GetKey<T>();
        return await Storage.ExistsAsync(key);
    }

    /// <summary>
    /// 异步删除指定类型的数据
    /// </summary>
    /// <typeparam name="T">要删除的数据类型</typeparam>
    public async Task DeleteAsync<T>() where T : class, IData
    {
        var key = GetKey<T>();
        await Storage.DeleteAsync(key);

        if (_options.EnableEvents)
            this.SendEvent(new DataDeletedEvent(typeof(T)));
    }

    /// <summary>
    /// 批量异步保存多个数据对象
    /// </summary>
    /// <param name="dataList">要保存的数据对象集合</param>
    public async Task SaveAllAsync(IEnumerable<IData> dataList)
    {
        var list = dataList.ToList();
        foreach (var data in list)
        {
            var type = data.GetType();
            var key = GetKey(type);
            await Storage.WriteAsync(key, data);
        }

        if (_options.EnableEvents)
            this.SendEvent(new DataBatchSavedEvent(list));
    }

    protected override void OnInit()
    {
        _storage ??= this.GetUtility<IStorage>()!;
    }

    /// <summary>
    /// 根据类型生成存储键
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <returns>生成的存储键</returns>
    private string GetKey<T>() where T : IData => GetKey(typeof(T));

    /// <summary>
    /// 根据类型生成存储键
    /// </summary>
    /// <param name="type">数据类型</param>
    /// <returns>生成的存储键</returns>
    private string GetKey(Type type)
    {
        var fileName = $"{_options.KeyPrefix}_{type.Name}";

        if (string.IsNullOrEmpty(_options.BasePath))
            return fileName;
        var basePath = _options.BasePath.TrimEnd('/');
        return $"{basePath}/{fileName}";
    }
}