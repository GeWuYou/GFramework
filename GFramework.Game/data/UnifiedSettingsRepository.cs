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
public class UnifiedSettingsRepository(
    IStorage? storage,
    IRuntimeTypeSerializer? serializer,
    DataRepositoryOptions? options = null,
    string fileName = "settings.json")
    : AbstractContextUtility, IDataRepository
{
    private UnifiedSettingsFile? _file;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly DataRepositoryOptions _options = options ?? new DataRepositoryOptions();
    private bool _loaded;
    private IRuntimeTypeSerializer? _serializer = serializer;
    private IStorage? _storage = storage;

    private IStorage Storage =>
        _storage ?? throw new InvalidOperationException("IStorage not initialized.");

    private IRuntimeTypeSerializer Serializer =>
        _serializer ?? throw new InvalidOperationException("ISerializer not initialized.");

    private UnifiedSettingsFile File =>
        _file ?? throw new InvalidOperationException("UnifiedSettingsFile not set.");

    protected override void OnInit()
    {
        _storage ??= this.GetUtility<IStorage>()!;
        _serializer ??= this.GetUtility<IRuntimeTypeSerializer>()!;
    }
    // =========================
    // IDataRepository
    // =========================

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

    public async Task SaveAsync<T>(IDataLocation location, T data)
        where T : class, IData
    {
        await EnsureLoadedAsync();

        var key = location.Key;
        var serialized = Serializer.Serialize(data);

        _file!.Sections[key] = serialized;

        await Storage.WriteAsync(fileName, _file);
        if (_options.EnableEvents)
            this.SendEvent(new DataSavedEvent<T>(data));
    }

    public async Task<bool> ExistsAsync(IDataLocation location)
    {
        await EnsureLoadedAsync();
        return File.Sections.ContainsKey(location.Key);
    }


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

            if (await Storage.ExistsAsync(fileName))
            {
                _file = await Storage.ReadAsync<UnifiedSettingsFile>(fileName);
            }
            else
            {
                _file = new UnifiedSettingsFile { Version = 1 };
            }

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
            await Storage.WriteAsync(GetUnifiedKey(), File);
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