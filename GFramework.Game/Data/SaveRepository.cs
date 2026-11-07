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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Storage;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Storage;
using GFramework.Game.Internal;
using GFramework.Game.Storage;

namespace GFramework.Game.Data;

/// <summary>
///     基于槽位的存档仓库实现
/// </summary>
/// <typeparam name="TSaveData">存档数据类型</typeparam>
public partial class SaveRepository<TSaveData> : ISaveRepository<TSaveData>
    where TSaveData : class, IData, new()
{
    private readonly SaveConfiguration _config;
    private readonly Dictionary<int, ISaveMigration<TSaveData>> _migrations = new();
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _migrationsLock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _migrationsLock = new();
#endif
    private readonly IStorage _rootStorage;
    // Slot-wide operations must stay serialized through inspection, backup, repair and atomic commit.
    // The lifetime is one repository instance; this does not claim inter-process synchronization.
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _slotGates = new();
    private readonly ContentSaveOptions<TSaveData>? _contentOptions;
    private readonly Guid _reportOwner = Guid.NewGuid();

    /// <summary>
    ///     初始化存档仓库
    /// </summary>
    /// <param name="storage">存储实例</param>
    /// <param name="config">存档配置</param>
    public SaveRepository(IStorage storage, SaveConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
        _rootStorage = new ScopedStorage(storage, config.SaveRoot);
    }

    /// <summary>
    /// Creates an opt-in content-aware repository. Lossless snapshot storage is required so incompatible
    /// files can be inspected without migrating them and backed up before any approved repair.
    /// </summary>
    /// <param name="storage">Storage implementing the optional <see cref="IStorageBackup"/> capability.</param>
    /// <param name="config">Slot layout.</param>
    /// <param name="contentOptions">Game-owned reference, validation and recovery policies.</param>
    public SaveRepository(IStorage storage, SaveConfiguration config, ContentSaveOptions<TSaveData> contentOptions)
        : this(storage, config)
    {
        ArgumentNullException.ThrowIfNull(contentOptions);
        ArgumentNullException.ThrowIfNull(contentOptions.CurrentCatalog, nameof(contentOptions));
        ArgumentNullException.ThrowIfNull(contentOptions.EnumerateReferences, nameof(contentOptions));
        if (!typeof(IContentAwareSaveData).IsAssignableFrom(typeof(TSaveData)))
        {
            throw new ArgumentException("Content-aware saves must implement IContentAwareSaveData.", nameof(contentOptions));
        }
        if (storage is not IStorageBackup)
        {
            throw new ArgumentException("Content-aware saves require lossless IStorageBackup support.", nameof(storage));
        }
        _contentOptions = contentOptions;
    }

    /// <summary>
    ///     注册存档迁移器，使仓库在加载旧版本存档时自动执行升级。
    /// </summary>
    /// <param name="migration">要注册的存档迁移器。</param>
    /// <returns>当前存档仓库实例，支持链式调用。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="migration" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     <typeparamref name="TSaveData" /> 未实现 <see cref="IVersionedData" />，无法使用版本化迁移。
    ///     或者同一个源版本已经注册过迁移器，导致迁移链配置存在歧义。
    /// </exception>
    /// <exception cref="ArgumentException">迁移器的目标版本不大于源版本。</exception>
    /// <remarks>
    ///     迁移注册表是可变共享状态。注册路径通过 <see cref="_migrationsLock" /> 串行化；
    ///     加载路径会在同一把锁下复制一次快照，保证单次加载始终使用同一个迁移链视图。
    /// </remarks>
    public ISaveRepository<TSaveData> RegisterMigration(ISaveMigration<TSaveData> migration)
    {
        ArgumentNullException.ThrowIfNull(migration);
        EnsureVersionedSaveType();

        VersionedMigrationRunner.ValidateForwardOnlyRegistration(
            typeof(TSaveData).Name,
            "Save migration",
            migration.FromVersion,
            migration.ToVersion,
            nameof(migration));

        lock (_migrationsLock)
        {
            if (_migrations.ContainsKey(migration.FromVersion))
            {
                throw new InvalidOperationException(
                    $"Duplicate save migration registration for {typeof(TSaveData).Name} from version {migration.FromVersion}.");
            }

            _migrations.Add(migration.FromVersion, migration);
        }

        return this;
    }

    /// <summary>
    ///     检查指定槽位是否存在存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>如果存档存在返回true，否则返回false</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<bool> ExistsAsync(int slot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var storage = GetSlotStorage(slot);
        return await storage.ExistsAsync(_config.SaveFileName).ConfigureAwait(false);
    }

    /// <summary>
    ///     加载指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>存档数据对象，如果不存在则返回新实例</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<TSaveData> LoadAsync(int slot, CancellationToken cancellationToken = default)
    {
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var storage = GetSlotStorage(slot);
            if (!await storage.ExistsAsync(_config.SaveFileName).ConfigureAwait(false))
            {
                var fresh = new TSaveData();
                if (_contentOptions is not null)
                {
                    ((IContentAwareSaveData)fresh).Content = GetCurrentCatalog().Snapshot;
                }
                return fresh;
            }

            var loaded = await ReadForLoadAsync(slot, storage, cancellationToken).ConfigureAwait(false);
            return await MigrateIfNeededAsync(slot, storage, loaded, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    ///     保存存档到指定槽位
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="data">要保存的存档数据</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task SaveAsync(int slot, TSaveData data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var slotPath = $"{_config.SaveSlotPrefix}{slot}";
            if (!await _rootStorage.DirectoryExistsAsync(slotPath).ConfigureAwait(false))
            {
                await _rootStorage.CreateDirectoryAsync(slotPath).ConfigureAwait(false);
            }
            var storage = GetSlotStorage(slot);
            if (_contentOptions is not null)
            {
                var catalog = GetCurrentCatalog();
                ValidatePreparedSave(data, catalog);
                await WriteWithContentAsync(storage, data, catalog.Snapshot, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                await storage.WriteAsync(_config.SaveFileName, data).ConfigureAwait(false);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    ///     删除指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task DeleteAsync(int slot, CancellationToken cancellationToken = default)
    {
        var gate = GetSlotGate(slot);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await GetSlotStorage(slot).DeleteAsync(_config.SaveFileName).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    ///     列出所有有效的存档槽位
    /// </summary>
    /// <returns>包含所有有效存档槽位编号的只读列表，按升序排列</returns>
    /// <param name="cancellationToken">Cancels admission and cancellable waits; accepted writes and cleanup are awaited to completion.</param>
    public async Task<IReadOnlyList<int>> ListSlotsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 列举所有槽位目录
        var directories = await _rootStorage.ListDirectoriesAsync().ConfigureAwait(false);

        var slots = new List<int>();

        foreach (var dirName in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // 检查目录名是否符合槽位前缀
            if (!dirName.StartsWith(_config.SaveSlotPrefix, StringComparison.Ordinal))
                continue;

            // 提取槽位编号
            var slotStr = dirName[_config.SaveSlotPrefix.Length..];
            if (!int.TryParse(slotStr, CultureInfo.InvariantCulture, out var slot))
                continue;

            // 直接检查存档文件是否存在，避免重复创建 ScopedStorage
            var saveFilePath = $"{dirName}/{_config.SaveFileName}";
            if (await _rootStorage.ExistsAsync(saveFilePath).ConfigureAwait(false))
                slots.Add(slot);
        }

        return slots.OrderBy(x => x).ToList();
    }

    /// <summary>
    ///     获取指定槽位的存储对象
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>对应槽位的存储对象</returns>
    private IStorage GetSlotStorage(int slot)
    {
        return new ScopedStorage(_rootStorage, $"{_config.SaveSlotPrefix}{slot}");
    }

    /// <summary>
    ///     在加载旧版本存档时按注册顺序执行迁移，并在成功后自动回写升级结果。
    /// </summary>
    /// <param name="slot">当前加载的存档槽位。</param>
    /// <param name="storage">对应槽位的存储对象。</param>
    /// <param name="data">原始加载出来的存档数据。</param>
    /// <param name="cancellationToken">Cancels before the migrated snapshot is committed.</param>
    /// <returns>迁移后的最新存档；如果无需迁移则返回原始对象。</returns>
    /// <exception cref="InvalidOperationException">
    ///     当前运行时缺少必要的迁移链、读取到更高版本的存档，或迁移器返回了非法版本。
    /// </exception>
    private async Task<TSaveData> MigrateIfNeededAsync(int slot, IStorage storage, TSaveData data,
        CancellationToken cancellationToken)
    {
        var beforeVersion = data is IVersionedData versioned ? versioned.Version : (int?)null;
        var migrated = MigrateInMemory(slot, data);
        if (_contentOptions is not null)
        {
            ValidatePreparedSave(migrated, GetCurrentCatalog());
        }
        var afterVersion = migrated is IVersionedData latest ? latest.Version : (int?)null;
        if (beforeVersion != afterVersion)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_contentOptions is not null)
            {
                await WriteWithContentAsync(storage, migrated, GetCurrentCatalog().Snapshot, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await storage.WriteAsync(_config.SaveFileName, migrated).ConfigureAwait(false);
            }
        }
        return migrated;
    }

    /// <summary>Prepares a migration without writes, allowing content recovery to commit only after full validation.</summary>
    private TSaveData MigrateInMemory(int slot, TSaveData data)
    {
        if (data is not IVersionedData || new TSaveData() is not IVersionedData template)
        {
            return data;
        }
        Dictionary<int, ISaveMigration<TSaveData>> migrations;
        lock (_migrationsLock)
        {
            migrations = new Dictionary<int, ISaveMigration<TSaveData>>(_migrations);
        }
        return VersionedMigrationRunner.MigrateToTargetVersion(
            data, template.Version,
            static value => ((IVersionedData)value).Version,
            version => migrations.TryGetValue(version, out var migration) ? migration : null,
            static migration => migration.ToVersion,
            static (migration, current) => migration.Migrate(current),
            $"{typeof(TSaveData).Name} in slot {slot}", "save migration");
    }

    /// <summary>Gets a repository-lifetime slot gate; unrelated slots may perform independent operations.</summary>
    private SemaphoreSlim GetSlotGate(int slot) => _slotGates.GetOrAdd(slot, static _ => new SemaphoreSlim(1, 1));

    /// <summary>
    ///     验证当前存档类型支持基于版本号的迁移流程。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     <typeparamref name="TSaveData" /> 未实现 <see cref="IVersionedData" />。
    /// </exception>
    private static void EnsureVersionedSaveType()
    {
        if (!typeof(IVersionedData).IsAssignableFrom(typeof(TSaveData)))
        {
            throw new InvalidOperationException(
                $"{typeof(TSaveData).Name} must implement {nameof(IVersionedData)} to use save migrations.");
        }
    }

}
