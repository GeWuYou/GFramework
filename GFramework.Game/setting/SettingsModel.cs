using System.Collections.Concurrent;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.model;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting;

/// <summary>
///     设置模型：
///     - 管理 Settings Data 的生命周期（Load / Save / Reset / Migration）
///     - 编排 Settings Applicator 的 Apply 行为
/// </summary>
public class SettingsModel<TRepository> : AbstractModel, ISettingsModel
    where TRepository : class, IDataRepository
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(SettingsModel<TRepository>));

    // =========================
    // Fields
    // =========================

    private readonly ConcurrentDictionary<Type, ISettingsData> _data = new();
    private readonly ConcurrentBag<IResetApplyAbleSettings> _applicators = new();
    private readonly ConcurrentDictionary<(Type type, int from), ISettingsMigration> _migrations = new();
    private readonly ConcurrentDictionary<Type, Dictionary<int, ISettingsMigration>> _migrationCache = new();

    private IDataRepository? _repository;
    private IDataLocationProvider? _locationProvider;

    private IDataRepository Repository =>
        _repository ?? throw new InvalidOperationException("IDataRepository not initialized.");

    private IDataLocationProvider LocationProvider =>
        _locationProvider ?? throw new InvalidOperationException("IDataLocationProvider not initialized.");
    // =========================
    // Init
    // =========================

    protected override void OnInit()
    {
        _repository ??= this.GetUtility<TRepository>()!;
        _locationProvider ??= this.GetUtility<IDataLocationProvider>()!;
    }
    // =========================
    // Data access
    // =========================

    /// <summary>
    ///     获取指定类型的设置数据实例（唯一实例）
    /// </summary>
    public T GetData<T>() where T : class, ISettingsData, new()
    {
        return (T)_data.GetOrAdd(typeof(T), _ => new T());
    }

   
    public IEnumerable<ISettingsData> AllData()
    {
        return _data.Values;
    }

    // =========================
    // Applicator
    // =========================

    /// <summary>
    ///     注册设置应用器
    /// </summary>
    public ISettingsModel RegisterApplicator(IResetApplyAbleSettings applicator)
    {
        _applicators.Add(applicator);
        return this;
    }

    /// <summary>
    ///     获取所有设置应用器
    /// </summary>
    public IEnumerable<IResetApplyAbleSettings> AllApplicators()
    {
        return _applicators;
    }

    // =========================
    // Migration
    // =========================

    public ISettingsModel RegisterMigration(ISettingsMigration migration)
    {
        _migrations[(migration.SettingsType, migration.FromVersion)] = migration;
        return this;
    }

    private ISettingsData MigrateIfNeeded(ISettingsData data)
    {
        if (data is not IVersionedData versioned)
            return data;

        var type = data.GetType();
        var current = data;

        if (!_migrationCache.TryGetValue(type, out var versionMap))
        {
            versionMap = _migrations
                .Where(kv => kv.Key.type == type)
                .ToDictionary(kv => kv.Key.from, kv => kv.Value);

            _migrationCache[type] = versionMap;
        }

        while (versionMap.TryGetValue(versioned.Version, out var migration))
        {
            current = (ISettingsData)migration.Migrate(current);
            versioned = current;
        }

        return current;
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
        foreach (var data in _data.Values)
        {
            try
            {
                var type = data.GetType();
                var location = LocationProvider.GetLocation(type);

                if (!await Repository.ExistsAsync(location))
                    continue;

                var loaded = await Repository.LoadAsync<ISettingsData>(location);
                var migrated = MigrateIfNeeded(loaded);

                // 回填数据（不替换实例）
                data.LoadFrom(migrated);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize settings data: {data.GetType().Name}", ex);
            }
        }
    }

    /// <summary>
    ///     将所有 Settings Data 持久化
    /// </summary>
    public async Task SaveAllAsync()
    {
        foreach (var data in _data.Values)
        {
            try
            {
                var location = LocationProvider.GetLocation(data.GetType());
                await Repository.SaveAsync(location, data);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save settings data: {data.GetType().Name}", ex);
            }
        }
    }

    /// <summary>
    ///     应用所有设置
    /// </summary>
    public async Task ApplyAllAsync()
    {
        foreach (var applicator in _applicators)
        {
            try
            {
                await applicator.Apply();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply settings: {applicator.GetType().Name}", ex);
            }
        }
    }

    /// <summary>
    ///     重置所有设置
    /// </summary>
    public void ResetAll()
    {
        foreach (var data in _data.Values)
            data.Reset();

        foreach (var applicator in _applicators)
            applicator.Reset();
    }

}