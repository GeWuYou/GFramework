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

using GFramework.Core.Abstractions.Storage;
using GFramework.Core.Events;
using GFramework.Game.Abstractions.Data.Events;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Extensions;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Data;

/// <summary>
///     数据仓库类，用于管理游戏数据的存储和读取。
/// </summary>
/// <param name="storage">存储接口实例</param>
/// <param name="options">数据仓库配置选项</param>
/// <param name="publishEvent">Optional observation callback; its failures do not reverse completed operations.</param>
/// <param name="observerLogger">Notification diagnostics; defaults to Trace.</param>
public class DataRepository(IStorage storage, DataRepositoryOptions? options = null, Action<object>? publishEvent = null, ILogger? observerLogger = null)
    : IDataRepository
{
    private readonly ConcurrentDictionary<Type, Func<IDataLocation, IData, CancellationToken, Task>> _batchWriters = new();

    /// <summary>Registers an exact runtime data type for heterogeneous batches using a compiled generic writer.</summary>
    /// <typeparam name="T">A concrete persisted data type.</typeparam>
    /// <returns>This repository for composition-time chaining.</returns>
    /// <remarks>Register every batch type explicitly. Generic single-item SaveAsync does not require registration.</remarks>
    public DataRepository RegisterDataType<T>() where T : class, IData
    {
        _batchWriters.TryAdd(typeof(T), (location, data, token) =>
            SaveCoreAsync(location, (T)data, emitSavedEvent: false, token));
        return this;
    }

    private readonly DataRepositoryOptions _options = options ?? new DataRepositoryOptions();
    private readonly IStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    private IStorage Storage => _storage;

    /// <summary>
    ///     异步加载指定位置的数据
    /// </summary>
    /// <typeparam name="T">数据类型，必须实现IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <returns>加载的数据对象</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<T> LoadAsync<T>(IDataLocation location, CancellationToken cancellationToken = default)
        where T : class, IData, new()
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = location.ToStorageKey();

        // 检查存储中是否存在指定键的数据
        T result = await Storage.ExistsAsync(key).ConfigureAwait(false)
            ? await Storage.ReadAsync<T>(key).ConfigureAwait(false)
            : new T();

        cancellationToken.ThrowIfCancellationRequested();
        // 如果启用事件功能，则发送数据加载完成事件
        if (_options.EnableEvents)
            ObserverNotification.Notify(publishEvent, (object)new DataLoadedEvent<T>(result), observerLogger);

        return result;
    }

    /// <summary>
    ///     异步保存数据到指定位置
    /// </summary>
    /// <typeparam name="T">数据类型，必须实现IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <param name="data">要保存的数据对象</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task SaveAsync<T>(IDataLocation location, T data, CancellationToken cancellationToken = default)
        where T : class, IData
    {
        cancellationToken.ThrowIfCancellationRequested();
        await SaveCoreAsync(location, data, emitSavedEvent: true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     检查指定位置的数据是否存在
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>如果数据存在返回true，否则返回false</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public Task<bool> ExistsAsync(IDataLocation location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Storage.ExistsAsync(location.ToStorageKey());
    }

    /// <summary>
    ///     异步删除指定位置的数据
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task DeleteAsync(IDataLocation location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = location.ToStorageKey();

        if (!await Storage.ExistsAsync(key).ConfigureAwait(false))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Storage.DeleteAsync(key).ConfigureAwait(false);
        if (_options.EnableEvents)
            ObserverNotification.Notify(publishEvent, (object)new DataDeletedEvent(location), observerLogger);
    }

    /// <summary>
    ///     异步批量保存多个数据项
    /// </summary>
    /// <param name="dataList">包含数据位置和数据对象的枚举集合</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    /// <remarks>Register exact batch runtime types with RegisterDataType before use. Admission validates every
    /// item before the first write; subsequent real storage failures are not a cross-file transaction.</remarks>
    /// <exception cref="InvalidOperationException">A runtime data type has no registered typed writer.</exception>
    public async Task SaveAllAsync(IEnumerable<(IDataLocation location, IData data)> dataList, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(dataList);
        var valueTuples = dataList.ToList();
        // Validate the whole batch before the first write: an unknown later type cannot cause a partial commit.
        var writers = valueTuples.Select(item =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(item.location, nameof(dataList));
            ArgumentNullException.ThrowIfNull(item.data, nameof(dataList));
            return _batchWriters.TryGetValue(item.data.GetType(), out var writer)
                ? writer
                : throw new InvalidOperationException(
                    $"Batch data type '{item.data.GetType()}' must be registered through RegisterDataType<T>().");
        }).ToArray();

        // 批量保存对订阅者而言应视为一次显式提交，因此这里复用底层保存逻辑，
        // 但抑制逐项 DataSavedEvent，避免监听器对同一批次收到重复语义的事件。
        for (var index = 0; index < valueTuples.Count; index++)
        {
            var (location, data) = valueTuples[index];
            await writers[index](location, data, cancellationToken).ConfigureAwait(false);
        }

        if (_options.EnableEvents)
            ObserverNotification.Notify(publishEvent, (object)new DataBatchSavedEvent(valueTuples), observerLogger);
    }


    /// <summary>
    ///     执行单项保存的共享流程，并根据调用入口决定是否发送单项保存事件。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    /// <param name="location">目标数据位置。</param>
    /// <param name="data">要保存的数据对象。</param>
    /// <param name="emitSavedEvent">是否在成功写入后发送单项保存事件。</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    private async Task SaveCoreAsync<T>(IDataLocation location, T data, bool emitSavedEvent, CancellationToken cancellationToken)
        where T : class, IData
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = location.ToStorageKey();

        await BackupIfNeededAsync<T>(key).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await Storage.WriteAsync(key, data).ConfigureAwait(false);

        if (emitSavedEvent && _options.EnableEvents)
        {
            ObserverNotification.Notify(publishEvent, (object)new DataSavedEvent<T>(data), observerLogger);
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

}
