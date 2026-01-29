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

using GFramework.Core.Abstractions.serializer;
using GFramework.Core.Abstractions.storage;
using GFramework.Core.extensions;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.data.events;

namespace GFramework.Game.data;

/// <summary>
/// 使用单一文件存储所有设置数据的仓库实现
/// </summary>
public class UnifiedSettingsRepository(
    IStorage? storage,
    IRuntimeTypeSerializer? serializer,
    DataRepositoryOptions? options = null,
    string fileName = "settings.json")
    : AbstractContextUtility, IDataRepository
{
    private readonly Dictionary<string, string> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly DataRepositoryOptions _options = options ?? new DataRepositoryOptions();
    private bool _loaded;
    private IRuntimeTypeSerializer? _serializer = serializer;
    private IStorage? _storage = storage;

    private IStorage Storage =>
        _storage ?? throw new InvalidOperationException("IStorage not initialized.");

    private IRuntimeTypeSerializer Serializer =>
        _serializer ?? throw new InvalidOperationException("ISerializer not initialized.");

    // =========================
    // IDataRepository
    // =========================

    /// <summary>
    /// 异步加载指定类型的数据
    /// </summary>
    /// <typeparam name="T">要加载的数据类型，必须继承自IData接口并具有无参构造函数</typeparam>
    /// <returns>加载的数据实例</returns>
    public async Task<T> LoadAsync<T>() where T : class, IData, new()
    {
        await EnsureLoadedAsync();

        var key = GetTypeKey(typeof(T));

        var result = _cache.TryGetValue(key, out var json) ? Serializer.Deserialize<T>(json) : new T();

        if (_options.EnableEvents)
            this.SendEvent(new DataLoadedEvent<T>(result));

        return result;
    }

    /// <summary>
    /// 异步加载指定类型的数据（通过Type参数）
    /// </summary>
    /// <param name="type">要加载的数据类型</param>
    /// <returns>加载的数据实例</returns>
    /// <exception cref="ArgumentException">当类型不符合要求时抛出异常</exception>
    public async Task<IData> LoadAsync(Type type)
    {
        if (!typeof(IData).IsAssignableFrom(type))
            throw new ArgumentException($"{type.Name} does not implement IData");

        if (!type.IsClass || type.GetConstructor(Type.EmptyTypes) == null)
            throw new ArgumentException($"{type.Name} must have parameterless ctor");

        await EnsureLoadedAsync();

        var key = GetTypeKey(type);

        IData result;
        if (_cache.TryGetValue(key, out var json))
        {
            result = (IData)Serializer.Deserialize(json, type);
        }
        else
        {
            result = (IData)Activator.CreateInstance(type)!;
        }

        if (_options.EnableEvents)
            this.SendEvent(new DataLoadedEvent<IData>(result));

        return result;
    }

    /// <summary>
    /// 异步保存数据到存储
    /// </summary>
    /// <typeparam name="T">要保存的数据类型</typeparam>
    /// <param name="data">要保存的数据实例</param>
    public async Task SaveAsync<T>(T data) where T : class, IData
    {
        await EnsureLoadedAsync();

        var key = GetTypeKey(typeof(T));
        _cache[key] = Serializer.Serialize(data);

        await SaveUnifiedFileAsync();

        if (_options.EnableEvents)
            this.SendEvent(new DataSavedEvent<T>(data));
    }

    /// <summary>
    /// 异步批量保存多个数据实例
    /// </summary>
    /// <param name="dataList">要保存的数据实例集合</param>
    public async Task SaveAllAsync(IEnumerable<IData> dataList)
    {
        await EnsureLoadedAsync();

        var list = dataList.ToList();
        foreach (var data in list)
        {
            var key = GetTypeKey(data.GetType());
            _cache[key] = Serializer.Serialize(data);
        }

        await SaveUnifiedFileAsync();

        if (_options.EnableEvents)
            this.SendEvent(new DataBatchSavedEvent(list));
    }

    /// <summary>
    /// 检查指定类型的数据是否存在
    /// </summary>
    /// <typeparam name="T">要检查的数据类型</typeparam>
    /// <returns>如果存在返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync<T>() where T : class, IData
    {
        await EnsureLoadedAsync();
        return _cache.ContainsKey(GetTypeKey(typeof(T)));
    }

    /// <summary>
    /// 删除指定类型的数据
    /// </summary>
    /// <typeparam name="T">要删除的数据类型</typeparam>
    public async Task DeleteAsync<T>() where T : class, IData
    {
        await EnsureLoadedAsync();

        _cache.Remove(GetTypeKey(typeof(T)));
        await SaveUnifiedFileAsync();

        if (_options.EnableEvents)
            this.SendEvent(new DataDeletedEvent(typeof(T)));
    }

    protected override void OnInit()
    {
        _storage ??= this.GetUtility<IStorage>()!;
        _serializer ??= this.GetUtility<IRuntimeTypeSerializer>()!;
    }

    // =========================
    // Internals
    // =========================

    /// <summary>
    /// 确保数据已从存储中加载到缓存
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        await _lock.WaitAsync();
        try
        {
            if (_loaded) return;

            if (await Storage.ExistsAsync(GetUnifiedKey()))
            {
                var data = await Storage.ReadAsync<Dictionary<string, string>>(GetUnifiedKey());
                _cache.Clear();
                foreach (var (k, v) in data)
                    _cache[k] = v;
            }

            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 将缓存中的所有数据保存到统一文件
    /// </summary>
    private async Task SaveUnifiedFileAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await Storage.WriteAsync(GetUnifiedKey(), _cache);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 获取统一文件的存储键名
    /// </summary>
    /// <returns>完整的存储键名</returns>
    private string GetUnifiedKey()
    {
        var name = string.IsNullOrEmpty(_options.KeyPrefix) ? fileName : $"{_options.KeyPrefix}_{fileName}";
        return string.IsNullOrEmpty(_options.BasePath) ? name : $"{_options.BasePath.TrimEnd('/')}/{name}";
    }

    /// <summary>
    /// 获取类型的唯一标识键
    /// </summary>
    /// <param name="type">要获取键的类型</param>
    /// <returns>类型的全名作为键</returns>
    private static string GetTypeKey(Type type)
        => type.FullName!; // ⚠️ 刻意不用 AssemblyQualifiedName
}