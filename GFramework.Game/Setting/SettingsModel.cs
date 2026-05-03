// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Extensions;
using GFramework.Core.Logging;
using GFramework.Core.Model;
using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Internal;
using GFramework.Game.Setting.Events;

namespace GFramework.Game.Setting;

/// <summary>
///     设置模型：
///     - 管理 Settings Data 的生命周期（Load / Save / Reset / Migration）
///     - 编排 Settings Applicator 的 Apply 行为
/// </summary>
public class SettingsModel<TRepository>(IDataLocationProvider? locationProvider, TRepository? repository)
    : AbstractModel, ISettingsModel
    where TRepository : class, ISettingsDataRepository
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(SettingsModel<TRepository>));

    private readonly ConcurrentDictionary<Type, IResetApplyAbleSettings> _applicators = new();

    // =========================
    // Fields
    // =========================

    private readonly ConcurrentDictionary<Type, ISettingsData> _data = new();
    private readonly ConcurrentDictionary<Type, Dictionary<int, ISettingsMigration>> _migrationCache = new();
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _migrationMapLock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _migrationMapLock = new();
#endif
    private readonly ConcurrentDictionary<(Type type, int from), ISettingsMigration> _migrations = new();
    private volatile bool _initialized;

    private IDataLocationProvider? _locationProvider = locationProvider;

    private ISettingsDataRepository? _repository = repository;

    private ISettingsDataRepository DataRepository =>
        _repository ?? throw new InvalidOperationException("ISettingsDataRepository not initialized.");

    private IDataLocationProvider LocationProvider =>
        _locationProvider ?? throw new InvalidOperationException("IDataLocationProvider not initialized.");

    /// <summary>
    /// 获取一个布尔值，指示当前对象是否已初始化。
    /// </summary>
    /// <returns>如果对象已初始化则返回 true，否则返回 false。</returns>
    public bool IsInitialized => _initialized;

    // =========================
    // Data access
    // =========================

    /// <summary>
    ///     获取指定类型的设置数据实例（唯一实例）
    /// </summary>
    /// <typeparam name="T">实现ISettingsData接口且具有无参构造函数的类型</typeparam>
    /// <returns>指定类型的设置数据实例</returns>
    public T GetData<T>() where T : class, ISettingsData, new()
    {
        // 使用_data字典获取或添加指定类型的实例，确保唯一性
        var data = (T)_data.GetOrAdd(typeof(T), _ => new T());
        TryRegisterDataType(typeof(T));
        return data;
    }

    /// <summary>
    ///     获取所有设置数据实例的集合
    /// </summary>
    /// <returns>包含所有ISettingsData实例的可枚举集合</returns>
    public IEnumerable<ISettingsData> AllData()
    {
        // 返回_data字典中所有值的集合
        return _data.Values;
    }

    // =========================
    // Applicator
    // =========================

    /// <summary>
    ///     注册设置应用器
    /// </summary>
    public ISettingsModel RegisterApplicator<T>(T applicator)
        where T : class, IResetApplyAbleSettings
    {
        _applicators[typeof(T)] = applicator;
        _data[applicator.DataType] = applicator.Data;
        TryRegisterDataType(applicator.DataType);
        return this;
    }

    /// <summary>
    ///     获取所有设置应用器的集合。
    /// </summary>
    /// <returns>
    ///     返回一个包含所有设置应用器的可枚举集合。
    /// </returns>
    public IEnumerable<IResetApplyAbleSettings> AllApplicators()
    {
        return _applicators.Values;
    }

    // =========================
    // Migration
    // =========================

    /// <summary>
    ///     注册一个设置迁移对象，并将其与指定的设置类型和版本关联。
    /// </summary>
    /// <param name="migration">
    ///     要注册的设置迁移对象，需实现 ISettingsMigration 接口。
    /// </param>
    /// <returns>
    ///     返回当前 ISettingsModel 实例，支持链式调用。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="migration" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     迁移声明的目标版本不大于源版本时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     同一设置类型与源版本已经注册过迁移器时抛出。
    /// </exception>
    /// <remarks>
    ///     迁移注册表与按类型缓存的版本映射需要保持一致；因此注册与 cache miss 时的缓存重建
    ///     统一通过 <see cref="_migrationMapLock" /> 串行化，避免并发加载把旧快照重新写回缓存。
    /// </remarks>
    public ISettingsModel RegisterMigration(ISettingsMigration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);

        VersionedMigrationRunner.ValidateForwardOnlyRegistration(
            migration.SettingsType.Name,
            "Settings migration",
            migration.FromVersion,
            migration.ToVersion,
            nameof(migration));

        lock (_migrationMapLock)
        {
            if (!_migrations.TryAdd((migration.SettingsType, migration.FromVersion), migration))
            {
                throw new InvalidOperationException(
                    $"Duplicate settings migration registration for {migration.SettingsType.Name} from version {migration.FromVersion}.");
            }

            _migrationCache.TryRemove(migration.SettingsType, out _);
        }

        return this;
    }

    // =========================
    // Lifecycle
    // =========================

    /// <summary>
    ///     初始化设置模型：
    ///     - 加载所有已存在的 Settings Data
    ///     - 执行必要的迁移
    /// </summary>
    public async Task InitializeAsync()
    {
        IDictionary<string, IData> allData;

        try
        {
            allData = await DataRepository.LoadAllAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load unified settings file.", ex);
            return;
        }

        foreach (var data in _data.Values)
            try
            {
                var type = data.GetType();
                var location = LocationProvider.GetLocation(type);

                if (!allData.TryGetValue(location.Key, out var raw))
                    continue;

                if (raw is not ISettingsData loaded)
                    continue;

                var migrated = MigrateIfNeeded(loaded, data);

                // 回填（不替换实例）
                data.LoadFrom(migrated);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize settings data: {data.GetType().Name}", ex);
            }

        _initialized = true;
        this.SendEvent(new SettingsInitializedEvent());
    }


    /// <summary>
    ///     将所有 Settings Data 持久化
    /// </summary>
    public async Task SaveAllAsync()
    {
        foreach (var data in _data.Values)
            try
            {
                var location = LocationProvider.GetLocation(data.GetType());
                await DataRepository.SaveAsync(location, data).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save settings data: {data.GetType().Name}", ex);
            }

        this.SendEvent(new SettingsSavedAllEvent());
    }

    /// <summary>
    ///     应用所有设置
    /// </summary>
    public async Task ApplyAllAsync()
    {
        foreach (var applicator in _applicators)
            try
            {
                await applicator.Value.ApplyAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply settings: {applicator.GetType().Name}", ex);
            }

        this.SendEvent(new SettingsAppliedAllEvent());
    }

    /// <summary>
    ///     重置指定类型的可重置对象
    /// </summary>
    /// <typeparam name="T">要重置的对象类型，必须是class类型，实现IResettable接口，并具有无参构造函数</typeparam>
    public void Reset<T>() where T : class, ISettingsData, new()
    {
        var data = GetData<T>();
        data.Reset();
        this.SendEvent(new SettingsResetEvent<T>(data));
    }

    /// <summary>
    ///     重置所有设置
    /// </summary>
    public void ResetAll()
    {
        foreach (var data in _data.Values)
            data.Reset();

        foreach (var applicator in _applicators)
            applicator.Value.Reset();
        this.SendEvent(new SettingsResetAllEvent(_data.Values));
    }

    /// <summary>
    ///     获取指定类型的设置应用器
    /// </summary>
    /// <typeparam name="T">要获取的设置应用器类型，必须继承自IResetApplyAbleSettings</typeparam>
    /// <returns>设置应用器实例，如果不存在则返回null</returns>
    public T? GetApplicator<T>() where T : class, IResetApplyAbleSettings
    {
        return _applicators.TryGetValue(typeof(T), out var app)
            ? (T)app
            : null;
    }
    // =========================
    // OnInitialize
    // =========================

    /// <summary>
    /// 初始化函数，在对象创建时调用。该函数负责初始化数据仓库和位置提供者，
    /// 并注册所有已知数据类型到数据仓库中。
    /// </summary>
    protected override void OnInit()
    {
        // 初始化数据仓库实例，如果尚未赋值则通过依赖注入获取
        _repository ??= this.GetUtility<TRepository>()!;

        // 初始化位置提供者实例，如果尚未赋值则通过依赖注入获取
        _locationProvider ??= this.GetUtility<IDataLocationProvider>()!;

        // 遍历所有已知的数据类型，为其分配位置并注册到数据仓库中
        foreach (var type in _data.Keys)
        {
            TryRegisterDataType(type);
        }
    }

    private void TryRegisterDataType(Type type)
    {
        if (_repository == null || _locationProvider == null)
        {
            return;
        }

        var location = _locationProvider.GetLocation(type);
        _repository.RegisterDataType(location, type);
    }

    /// <summary>
    ///     将已加载的设置数据迁移到当前运行时实例声明的目标版本。
    /// </summary>
    /// <param name="data">从仓库读取的设置数据。</param>
    /// <param name="latestData">当前内存中的设置实例，其 <c>Version</c> 值代表目标版本。</param>
    /// <returns>迁移后的设置数据；如果无需迁移则返回原对象。</returns>
    /// <remarks>
    ///     该方法按设置类型缓存迁移表，并始终以 <paramref name="latestData" /> 的版本作为目标运行时版本，
    ///     避免把旧文件中的版本号误当成当前版本。具体的缺链、版本一致性与前进性校验都委托给
    ///     <see cref="VersionedMigrationRunner" /> 统一处理。缓存重建与迁移注册共用
    ///     <see cref="_migrationMapLock" />，确保运行中的初始化不会把过期迁移快照写回缓存。
    /// </remarks>
    private ISettingsData MigrateIfNeeded(ISettingsData data, ISettingsData latestData)
    {
        var type = data.GetType();
        Dictionary<int, ISettingsMigration> versionMap;
        lock (_migrationMapLock)
        {
            if (!_migrationCache.TryGetValue(type, out var cachedVersionMap))
            {
                // cache miss 与 RegisterMigration 共用同一把锁，避免注册新迁移后又被旧快照覆盖回缓存。
                versionMap = _migrations
                    .Where(kv => kv.Key.type == type)
                    .ToDictionary(kv => kv.Key.from, kv => kv.Value);

                _migrationCache[type] = versionMap;
            }
            else
            {
                versionMap = cachedVersionMap;
            }
        }

        return VersionedMigrationRunner.MigrateToTargetVersion(
            data,
            latestData.Version,
            static settings => settings.Version,
            fromVersion => versionMap.TryGetValue(fromVersion, out var migration) ? migration : null,
            static migration => migration.ToVersion,
            static (migration, current) => ApplySettingsMigration(migration, current),
            $"{type.Name} settings",
            "settings migration");
    }

    /// <summary>
    ///     执行单步设置迁移，并验证迁移结果仍然属于已注册的设置类型。
    /// </summary>
    /// <param name="migration">要执行的迁移器。</param>
    /// <param name="currentData">当前版本的数据。</param>
    /// <returns>迁移后的设置数据。</returns>
    /// <exception cref="InvalidOperationException">
    ///     迁移结果不实现 <see cref="ISettingsData" />，或返回了与声明设置类型不兼容的数据时抛出。
    /// </exception>
    private static ISettingsData ApplySettingsMigration(ISettingsMigration migration, ISettingsData currentData)
    {
        var fromVersion = currentData.Version;
        var migrated = migration.Migrate(currentData);
        if (migrated is not ISettingsData migratedData)
        {
            throw new InvalidOperationException(
                $"Settings migration for {migration.SettingsType.Name} from version {fromVersion} must return {nameof(ISettingsData)}.");
        }

        if (!migration.SettingsType.IsInstanceOfType(migratedData))
        {
            throw new InvalidOperationException(
                $"Settings migration for {migration.SettingsType.Name} from version {fromVersion} returned incompatible data type {migratedData.GetType().Name}.");
        }

        return migratedData;
    }
}
