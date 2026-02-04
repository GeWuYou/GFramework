using GFramework.Core.Abstractions.logging;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.model;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.setting;
using GFramework.Game.setting.events;

namespace GFramework.Game.setting;

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
        return (T)_data.GetOrAdd(typeof(T), _ => new T());
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
    public ISettingsModel RegisterMigration(ISettingsMigration migration)
    {
        _migrations[(migration.SettingsType, migration.FromVersion)] = migration;
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
            allData = await DataRepository.LoadAllAsync();
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

                var migrated = MigrateIfNeeded(loaded);

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
                await DataRepository.SaveAsync(location, data);
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
                await applicator.Value.Apply();
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
    // Init
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
            var location = _locationProvider.GetLocation(type);
            DataRepository.RegisterDataType(location, type);
        }
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
}