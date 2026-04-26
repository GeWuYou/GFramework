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

using System.Reflection;
using GFramework.Core.Abstractions.Storage;
using GFramework.Core.Extensions;
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Data.Events;
using GFramework.Game.Extensions;

namespace GFramework.Game.Data;

/// <summary>
///     数据仓库类，用于管理游戏数据的存储和读取。
/// </summary>
/// <param name="storage">存储接口实例</param>
/// <param name="options">数据仓库配置选项</param>
public class DataRepository(IStorage? storage, DataRepositoryOptions? options = null)
    : AbstractContextUtility, IDataRepository
{
    private static readonly MethodInfo SaveCoreGenericMethod =
        typeof(DataRepository).GetMethod(nameof(SaveCoreAsync), BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Method {nameof(SaveCoreAsync)} not found.");

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

        // 检查存储中是否存在指定键的数据
        T result = await Storage.ExistsAsync(key).ConfigureAwait(false)
            ? await Storage.ReadAsync<T>(key).ConfigureAwait(false)
            : new T();

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
        await SaveCoreAsync(location, data, emitSavedEvent: true).ConfigureAwait(false);
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

        if (!await Storage.ExistsAsync(key).ConfigureAwait(false))
        {
            return;
        }

        await Storage.DeleteAsync(key).ConfigureAwait(false);
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

        // 批量保存对订阅者而言应视为一次显式提交，因此这里复用底层保存逻辑，
        // 但抑制逐项 DataSavedEvent，避免监听器对同一批次收到重复语义的事件。
        foreach (var (location, data) in valueTuples)
        {
            await SaveCoreUntypedAsync(location, data, emitSavedEvent: false).ConfigureAwait(false);
        }

        if (_options.EnableEvents)
            this.SendEvent(new DataBatchSavedEvent(valueTuples));
    }

    /// <summary>
    ///     初始化
    /// </summary>
    protected override void OnInit()
    {
        _storage ??= this.GetUtility<IStorage>()!;
    }

    /// <summary>
    ///     执行单项保存的共享流程，并根据调用入口决定是否发送单项保存事件。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <param name="location">目标数据位置。</param>
    /// <param name="data">要保存的数据对象。</param>
    /// <param name="emitSavedEvent">是否在成功写入后发送单项保存事件。</param>
    private async Task SaveCoreAsync<T>(IDataLocation location, T data, bool emitSavedEvent)
        where T : class, IData
    {
        var key = location.ToStorageKey();

        await BackupIfNeededAsync<T>(key).ConfigureAwait(false);
        await Storage.WriteAsync(key, data).ConfigureAwait(false);

        if (emitSavedEvent && _options.EnableEvents)
        {
            this.SendEvent(new DataSavedEvent<T>(data));
        }
    }

    /// <summary>
    ///     在覆盖旧值前为当前存储键创建备份。
    /// </summary>
    /// <param name="key">即将被覆盖的存储键。</param>
    private async Task BackupIfNeededAsync<T>(string key)
        where T : class, IData
    {
        if (!_options.AutoBackup || !await Storage.ExistsAsync(key).ConfigureAwait(false))
        {
            return;
        }

        var backupKey = $"{key}.backup";
        var existing = await Storage.ReadAsync<T>(key).ConfigureAwait(false);
        await Storage.WriteAsync(backupKey, existing).ConfigureAwait(false);
    }

    /// <summary>
    ///     使用数据对象的运行时类型执行保存流程，避免批量保存时因为编译期类型退化为 <see cref="IData" /> 而破坏备份反序列化。
    /// </summary>
    /// <param name="location">目标数据位置。</param>
    /// <param name="data">要保存的数据对象。</param>
    /// <param name="emitSavedEvent">是否发送单项保存事件。</param>
    private Task SaveCoreUntypedAsync(IDataLocation location, IData data, bool emitSavedEvent)
    {
        ArgumentNullException.ThrowIfNull(data);

        var closedMethod = SaveCoreGenericMethod.MakeGenericMethod(data.GetType());
        return (Task)closedMethod.Invoke(this, [location, data, emitSavedEvent])!;
    }
}
