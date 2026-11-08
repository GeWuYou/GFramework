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

using GFramework.Core.Abstractions.Serializer;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Data.Events;

namespace GFramework.Game.Data;

/// <summary>
///     使用单一文件存储所有设置数据的仓库实现。
/// </summary>
/// <remarks>
///     该仓库通过内存缓存聚合所有设置 section，并在公开的保存或删除操作发生时整文件回写。
///     虽然底层不是“一项一个文件”，但它仍遵循 <see cref="DataRepositoryOptions" /> 定义的统一契约：
///     启用自动备份时，覆盖写入前会为整个统一文件创建单份备份；批量保存只发出批量事件，不重复发出单项保存事件。
/// </remarks>
public class UnifiedSettingsDataRepository(
    IStorage storage,
    IRuntimeTypeSerializer serializer,
    DataRepositoryOptions? options = null,
    string fileName = "settings.json",
    Action<object>? publishEvent = null)
    : ISettingsDataRepository
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly DataRepositoryOptions _options = options ?? new DataRepositoryOptions();
    private readonly Dictionary<string, Type> _typeRegistry = new(StringComparer.Ordinal);
    private UnifiedSettingsFile? _file;
    private bool _loaded;
    private readonly IRuntimeTypeSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly IStorage _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    private IStorage Storage => _storage;

    private IRuntimeTypeSerializer Serializer => _serializer;

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
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<T> LoadAsync<T>(IDataLocation location, CancellationToken cancellationToken = default)
        where T : class, IData, new()
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var key = location.Key;
        var result = _file!.Sections.TryGetValue(key, out var raw) ? Serializer.Deserialize<T>(raw) : new T();
        if (_options.EnableEvents)
            publishEvent?.Invoke(new DataLoadedEvent<T>(result));
        return result;
    }

    /// <summary>
    ///     异步保存数据到指定位置
    /// </summary>
    /// <typeparam name="T">数据类型，必须继承自IData接口</typeparam>
    /// <param name="location">数据位置信息</param>
    /// <param name="data">要保存的数据对象</param>
    /// <returns>异步操作任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task SaveAsync<T>(IDataLocation location, T data, CancellationToken cancellationToken = default)
        where T : class, IData
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        await MutateAndPersistAsync(file => file.Sections[location.Key] = Serializer.Serialize(data), cancellationToken)
            .ConfigureAwait(false);

        if (_options.EnableEvents)
        {
            publishEvent?.Invoke(new DataSavedEvent<T>(data));
        }
    }

    /// <summary>
    ///     检查指定位置的数据是否存在
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>如果数据存在则返回true，否则返回false</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<bool> ExistsAsync(IDataLocation location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return File.Sections.ContainsKey(location.Key);
    }

    /// <summary>
    ///     删除指定位置的数据
    /// </summary>
    /// <param name="location">数据位置信息</param>
    /// <returns>异步操作任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task DeleteAsync(IDataLocation location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        var removed = false;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentFile = File;
            var nextFile = CloneFile(currentFile);
            removed = nextFile.Sections.Remove(location.Key);
            if (!removed)
            {
                return;
            }

            await WriteUnifiedFileCoreAsync(currentFile, nextFile).ConfigureAwait(false);
            _file = nextFile;
        }
        finally
        {
            _lock.Release();
        }

        if (removed && _options.EnableEvents)
        {
            publishEvent?.Invoke(new DataDeletedEvent(location));
        }
    }

    /// <summary>
    ///     批量保存多个数据项到存储
    /// </summary>
    /// <param name="dataList">包含数据位置和数据对象的枚举集合</param>
    /// <returns>异步操作任务</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task SaveAllAsync(
        IEnumerable<(IDataLocation location, IData data)> dataList, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        var valueTuples = dataList.ToList();

        await MutateAndPersistAsync(file =>
            {
                foreach (var (location, data) in valueTuples)
                {
                    file.Sections[location.Key] = Serializer.Serialize(data);
                }
            }, cancellationToken)
            .ConfigureAwait(false);

        if (_options.EnableEvents)
            publishEvent?.Invoke(new DataBatchSavedEvent(valueTuples));
    }

    /// <summary>
    ///     加载所有存储的数据项
    /// </summary>
    /// <returns>包含所有数据项的字典，键为数据位置键，值为数据对象</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<IDictionary<string, IData>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        var result = new Dictionary<string, IData>(StringComparer.Ordinal);

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


    // =========================
    // Internals
    // =========================

    /// <summary>
    ///     确保数据已从存储中加载到缓存
    /// </summary>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
            return;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_loaded)
                return;

            cancellationToken.ThrowIfCancellationRequested();
            var key = UnifiedKey;

            _file = await Storage.ExistsAsync(key).ConfigureAwait(false)
                ? await Storage.ReadAsync<UnifiedSettingsFile>(key).ConfigureAwait(false)
                : new UnifiedSettingsFile { Version = 1 };

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
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    /// <param name="mutation">Builds the next uncommitted settings snapshot while holding the commit gate.</param>
    private async Task MutateAndPersistAsync(Action<UnifiedSettingsFile> mutation, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentFile = File;
            var nextFile = CloneFile(currentFile);

            // 先在副本上计算“下一份已提交状态”，只有底层持久化成功后才交换缓存，
            // 这样即使备份或写入失败，也不会把未提交修改留在内存快照里。
            mutation(nextFile);
            await WriteUnifiedFileCoreAsync(currentFile, nextFile).ConfigureAwait(false);
            _file = nextFile;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     将当前缓存快照写回底层存储，并在需要时创建整个文件的备份。
    /// </summary>
    /// <remarks>
    ///     该方法要求调用方已经持有 <see cref="_lock" />，以保证“读取当前快照 -> 写入备份 -> 提交新快照”的原子提交顺序。
    ///     只有在该方法成功返回后，调用方才应交换内存中的 <see cref="_file" /> 引用。
    /// </remarks>
    /// <param name="currentFile">当前已提交的统一文件快照。</param>
    /// <param name="nextFile">即将提交的新统一文件快照。</param>
    private async Task WriteUnifiedFileCoreAsync(UnifiedSettingsFile currentFile, UnifiedSettingsFile nextFile)
    {
        if (_options.AutoBackup && await Storage.ExistsAsync(UnifiedKey).ConfigureAwait(false))
        {
            var backupKey = $"{UnifiedKey}.backup";
            await Storage.WriteAsync(backupKey, currentFile).ConfigureAwait(false);
        }

        await Storage.WriteAsync(UnifiedKey, nextFile).ConfigureAwait(false);
    }

    /// <summary>
    ///     复制当前统一文件快照，确保未提交修改不会污染内存中的已提交状态。
    /// </summary>
    /// <param name="source">要复制的统一文件快照。</param>
    /// <returns>包含独立 section 映射副本的新快照。</returns>
    private static UnifiedSettingsFile CloneFile(UnifiedSettingsFile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // 反序列化后的运行时类型可能只是 IDictionary 实现；若底层仍是 Dictionary，则保留其 comparer。
        // 若 comparer 已因接口抽象而不可恢复，则显式回退到 Ordinal，避免让默认 comparer 语义继续隐式存在。
        var sections = source.Sections is Dictionary<string, string> dictionary
            ? new Dictionary<string, string>(dictionary, dictionary.Comparer)
            : new Dictionary<string, string>(source.Sections, StringComparer.Ordinal);

        return new UnifiedSettingsFile
        {
            Version = source.Version,
            Sections = sections,
        };
    }

    /// <summary>
    ///     获取统一文件的存储键名。
    /// </summary>
    /// <returns>完整的存储键名。</returns>
    protected virtual string GetUnifiedKey()
    {
        return string.IsNullOrEmpty(_options.BasePath) ? fileName : $"{_options.BasePath}/{fileName}";
    }
}
