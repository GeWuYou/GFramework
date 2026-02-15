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
///     使用单一文件存储所有设置数据的仓库实现
/// </summary>
public class UnifiedSettingsDataRepository(
    IStorage? storage,
    IRuntimeTypeSerializer? serializer,
    DataRepositoryOptions? options = null,
    string fileName = "settings.json")
    : AbstractContextUtility, ISettingsDataRepository
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly DataRepositoryOptions _options = options ?? new DataRepositoryOptions();
    private readonly Dictionary<string, Type> _typeRegistry = new();
    private UnifiedSettingsFile? _file;
    private bool _loaded;
    private IRuntimeTypeSerializer? _serializer = serializer;
    private IStorage? _storage = storage;

    private IStorage Storage =>
        _storage ?? throw new InvalidOperationException("IStorage not initialized.");

    private IRuntimeTypeSerializer Serializer =>
        _serializer ?? throw new InvalidOperationException("ISerializer not initialized.");

    private UnifiedSettingsFile File =>
        _file ?? throw new InvalidOperationException("UnifiedSettingsFile not set.");

    private string UnifiedKey => GetUnifiedKey();

    // =========================
    // IDataRepository
    // =========================

    /// <summary>
    ///     异步加载指定位置的数据
    /// </summary>
    /// <typeparam name="T">数据类型，必须继承自IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <returns>加载的数据对象</returns>
    public async Task<T> LoadAsync<T>(IDataLocation location)
        where T : class, IData, new()
    {
        await EnsureLoadedAsync();
        var key = location.Key;
        var result = _file!.Sections.TryGetValue(key, out var raw) ? Serializer.Deserialize<T>(raw) : new T();
        if (_options.EnableEvents)
            this.SendEvent(new DataLoadedEvent<IData>(result));
        return result;
    }

    /// <summary>
    ///     异步保存数据到指定位置
    /// </summary>
    /// <typeparam name="T">数据类型，必须继承自IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <param name="data">要保存的数据对象</param>
    /// <returns>异步操作任务</returns>
    public async Task SaveAsync<T>(IDataLocation location, T data)
        where T : class, IData
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();

            var key = location.Key;
            var serialized = Serializer.Serialize(data);

            _file!.Sections[key] = serialized;

            await Storage.WriteAsync(UnifiedKey, _file);
            if (_options.EnableEvents)
                this.SendEvent(new DataSavedEvent<T>(data));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     检查指定位置的数据是否存在
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>如果数据存在则返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync(IDataLocation location)
    {
        await EnsureLoadedAsync();
        return File.Sections.ContainsKey(location.Key);
    }

    /// <summary>
    ///     删除指定位置的数据
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>异步操作任务</returns>
    public async Task DeleteAsync(IDataLocation location)
    {
        await EnsureLoadedAsync();

        if (File.Sections.Remove(location.Key))
        {
            await SaveUnifiedFileAsync();

            if (_options.EnableEvents)
                this.SendEvent(new DataDeletedEvent(location));
        }
    }

    /// <summary>
    ///     批量保存多个数据项到存储
    /// </summary>
    /// <param name="dataList">包含数据位置和数据对象的枚举集合</param>
    /// <returns>异步操作任务</returns>
    public async Task SaveAllAsync(
        IEnumerable<(IDataLocation location, IData data)> dataList)
    {
        await EnsureLoadedAsync();

        var valueTuples = dataList.ToList();
        foreach (var (location, data) in valueTuples)
        {
            var serialized = Serializer.Serialize(data);
            File.Sections[location.Key] = serialized;
        }

        await SaveUnifiedFileAsync();

        if (_options.EnableEvents)
            this.SendEvent(new DataBatchSavedEvent(valueTuples.ToList()));
    }

    /// <summary>
    ///     加载所有存储的数据项
    /// </summary>
    /// <returns>包含所有数据项的字典，键为数据位置键，值为数据对象</returns>
    public async Task<IDictionary<string, IData>> LoadAllAsync()
    {
        await EnsureLoadedAsync();

        var result = new Dictionary<string, IData>();

        foreach (var (key, raw) in File.Sections)
        {
            if (!_typeRegistry.TryGetValue(key, out var type))
                continue;

            var data = (IData)Serializer.Deserialize(raw, type);
            result[key] = data;
        }

        return result;
    }

    /// <summary>
    ///     注册数据类型到类型注册表中
    /// </summary>
    /// <param name="location">数据位置信息，用于获取键值</param>
    /// <param name="type">数据类型</param>
    public void RegisterDataType(IDataLocation location, Type type)
    {
        _typeRegistry[location.Key] = type;
    }

    /// <summary>
    ///     初始化
    /// </summary>
    protected override void OnInit()
    {
        _storage ??= this.GetUtility<IStorage>()!;
        _serializer ??= this.GetUtility<IRuntimeTypeSerializer>()!;
    }

    // =========================
    // Internals
    // =========================

    /// <summary>
    ///     确保数据已从存储中加载到缓存
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        await _lock.WaitAsync();
        try
        {
            if (_loaded) return;

            var key = UnifiedKey;

            if (await Storage.ExistsAsync(key))
                _file = await Storage.ReadAsync<UnifiedSettingsFile>(key);
            else
                _file = new UnifiedSettingsFile { Version = 1 };

            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }


    /// <summary>
    ///     将缓存中的所有数据保存到统一文件
    /// </summary>
    private async Task SaveUnifiedFileAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await Storage.WriteAsync(UnifiedKey, _file);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     获取统一文件的存储键名
    /// </summary>
    /// <returns>完整的存储键名</returns>
    protected virtual string GetUnifiedKey()
    {
        return string.IsNullOrEmpty(_options.BasePath) ? fileName : $"{_options.BasePath}/{fileName}";
    }
}