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
using GFramework.Core.Utility;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Internal;
using GFramework.Game.Storage;

namespace GFramework.Game.Data;

/// <summary>
///     基于槽位的存档仓库实现
/// </summary>
/// <typeparam name="TSaveData">存档数据类型</typeparam>
public class SaveRepository<TSaveData> : AbstractContextUtility, ISaveRepository<TSaveData>
    where TSaveData : class, IData, new()
{
    private readonly SaveConfiguration _config;
    private readonly Dictionary<int, ISaveMigration<TSaveData>> _migrations = new();
    private readonly object _migrationsLock = new();
    private readonly IStorage _rootStorage;

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
    ///     迁移注册表是可变共享状态。注册与加载可以并发发生，因此所有访问都通过 <see cref="_migrationsLock" />
    ///     串行化，避免读写竞争和“部分可见”的迁移链。
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
    public async Task<bool> ExistsAsync(int slot)
    {
        var storage = GetSlotStorage(slot);
        return await storage.ExistsAsync(_config.SaveFileName);
    }

    /// <summary>
    ///     加载指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>存档数据对象，如果不存在则返回新实例</returns>
    public async Task<TSaveData> LoadAsync(int slot)
    {
        var storage = GetSlotStorage(slot);

        if (await storage.ExistsAsync(_config.SaveFileName))
        {
            var loaded = await storage.ReadAsync<TSaveData>(_config.SaveFileName);
            return await MigrateIfNeededAsync(slot, storage, loaded);
        }

        return new TSaveData();
    }

    /// <summary>
    ///     保存存档到指定槽位
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="data">要保存的存档数据</param>
    public async Task SaveAsync(int slot, TSaveData data)
    {
        var slotPath = $"{_config.SaveSlotPrefix}{slot}";

        // 确保槽位目录存在
        if (!await _rootStorage.DirectoryExistsAsync(slotPath))
            await _rootStorage.CreateDirectoryAsync(slotPath);

        var storage = GetSlotStorage(slot);
        await storage.WriteAsync(_config.SaveFileName, data);
    }

    /// <summary>
    ///     删除指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    public async Task DeleteAsync(int slot)
    {
        var storage = GetSlotStorage(slot);
        await storage.DeleteAsync(_config.SaveFileName);
    }

    /// <summary>
    ///     列出所有有效的存档槽位
    /// </summary>
    /// <returns>包含所有有效存档槽位编号的只读列表，按升序排列</returns>
    public async Task<IReadOnlyList<int>> ListSlotsAsync()
    {
        // 列举所有槽位目录
        var directories = await _rootStorage.ListDirectoriesAsync();

        var slots = new List<int>();

        foreach (var dirName in directories)
        {
            // 检查目录名是否符合槽位前缀
            if (!dirName.StartsWith(_config.SaveSlotPrefix, StringComparison.Ordinal))
                continue;

            // 提取槽位编号
            var slotStr = dirName[_config.SaveSlotPrefix.Length..];
            if (!int.TryParse(slotStr, CultureInfo.InvariantCulture, out var slot))
                continue;

            // 直接检查存档文件是否存在，避免重复创建 ScopedStorage
            var saveFilePath = $"{dirName}/{_config.SaveFileName}";
            if (await _rootStorage.ExistsAsync(saveFilePath))
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
    /// <returns>迁移后的最新存档；如果无需迁移则返回原始对象。</returns>
    /// <exception cref="InvalidOperationException">
    ///     当前运行时缺少必要的迁移链、读取到更高版本的存档，或迁移器返回了非法版本。
    /// </exception>
    private async Task<TSaveData> MigrateIfNeededAsync(int slot, IStorage storage, TSaveData data)
    {
        if (data is not IVersionedData versionedData)
        {
            return data;
        }

        var latestTemplate = new TSaveData();
        if (latestTemplate is not IVersionedData latestVersionedData)
        {
            return data;
        }

        var currentVersion = versionedData.Version;
        var targetVersion = latestVersionedData.Version;

        if (currentVersion > targetVersion)
        {
            throw new InvalidOperationException(
                $"Save slot {slot} for {typeof(TSaveData).Name} is version {currentVersion}, " +
                $"which is newer than the current runtime version {targetVersion}.");
        }

        if (currentVersion == targetVersion)
        {
            return data;
        }

        EnsureVersionedSaveType();

        // 迁移链按“当前版本 -> 下一个已注册迁移器”推进；任何缺口都表示运行时无法安全解释旧存档。
        // 读取迁移表时使用同一把锁，保证并发注册不会让加载线程看到不一致的链路状态。
        var migrated = VersionedMigrationRunner.MigrateToTargetVersion(
            data,
            targetVersion,
            static saveData => ((IVersionedData)saveData).Version,
            fromVersion =>
            {
                lock (_migrationsLock)
                {
                    _migrations.TryGetValue(fromVersion, out var migration);
                    return migration;
                }
            },
            static migration => migration.ToVersion,
            static (migration, currentData) => migration.Migrate(currentData),
            $"{typeof(TSaveData).Name} in slot {slot}",
            "save migration");

        await storage.WriteAsync(_config.SaveFileName, migrated);
        return migrated;
    }

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

    /// <summary>
    ///     初始化逻辑
    /// </summary>
    protected override void OnInit()
    {
    }
}
