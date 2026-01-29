using System.Collections.Concurrent;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.versioning;
using GFramework.Core.extensions;
using GFramework.Core.logging;
using GFramework.Core.model;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting;

/// <summary>
///     设置模型类，用于管理不同类型的应用程序设置部分
/// </summary>
public class SettingsModel<TRepository>(IDataRepository? repository)
    : AbstractModel, ISettingsModel where TRepository : class, IDataRepository
{
    private static readonly ILogger Log =
        LoggerFactoryResolver.Provider.CreateLogger(nameof(SettingsModel<TRepository>));

    private readonly ConcurrentDictionary<Type, IApplyAbleSettings> _applicators = new();
    private readonly ConcurrentDictionary<Type, IResettable> _dataSettings = new();
    private readonly ConcurrentDictionary<Type, Dictionary<int, ISettingsMigration>> _migrationCache = new();
    private readonly ConcurrentDictionary<(Type type, int from), ISettingsMigration> _migrations = new();
    private IDataRepository? _repository = repository;
    private IDataRepository Repository => _repository ?? throw new InvalidOperationException("Repository is not set");

    // -----------------------------
    // Data
    // -----------------------------

    /// <summary>
    /// 获取指定类型的设置数据实例，如果不存在则创建新的实例
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现ISettingsData接口并提供无参构造函数</typeparam>
    /// <returns>指定类型的设置数据实例</returns>
    public T GetData<T>() where T : class, IResettable, new()
    {
        return (T)_dataSettings.GetOrAdd(typeof(T), _ => new T());
    }

    /// <summary>
    /// 获取所有设置数据的枚举集合
    /// </summary>
    /// <returns>所有设置数据的枚举集合</returns>
    public IEnumerable<IResettable> AllData()
        => _dataSettings.Values;

    // -----------------------------
    // Applicator
    // -----------------------------

    /// <summary>
    /// 获取所有设置应用器的枚举集合
    /// </summary>
    /// <returns>所有设置应用器的枚举集合</returns>
    public IEnumerable<IApplyAbleSettings> AllApplicators()
        => _applicators.Values;

    /// <summary>
    /// 注册设置应用器到模型中
    /// </summary>
    /// <typeparam name="T">设置应用器类型，必须实现IApplyAbleSettings接口</typeparam>
    /// <param name="applicator">要注册的设置应用器实例</param>
    /// <returns>当前设置模型实例，支持链式调用</returns>
    public ISettingsModel RegisterApplicator<T>(T applicator)
        where T : class, IApplyAbleSettings
    {
        _applicators[typeof(T)] = applicator;
        return this;
    }

    /// <summary>
    /// 获取指定类型的设置应用器实例
    /// </summary>
    /// <typeparam name="T">设置应用器类型，必须实现IApplyAbleSettings接口</typeparam>
    /// <returns>指定类型的设置应用器实例，如果不存在则返回null</returns>
    public T? GetApplicator<T>() where T : class, IApplyAbleSettings
    {
        return _applicators.TryGetValue(typeof(T), out var app)
            ? (T)app
            : null;
    }

    // -----------------------------
    // Section lookup
    // -----------------------------

    /// <summary>
    /// 尝试获取指定类型的设置节
    /// </summary>
    /// <param name="type">要查找的设置类型</param>
    /// <param name="section">输出参数，找到的设置节实例</param>
    /// <returns>如果找到对应类型的设置节则返回true，否则返回false</returns>
    public bool TryGet(Type type, out ISettingsSection section)
    {
        if (_dataSettings.TryGetValue(type, out var data))
        {
            section = data;
            return true;
        }

        if (_applicators.TryGetValue(type, out var applicator))
        {
            section = applicator;
            return true;
        }

        section = null!;
        return false;
    }

    // -----------------------------
    // Migration
    // -----------------------------

    /// <summary>
    /// 注册设置迁移器到模型中
    /// </summary>
    /// <param name="migration">要注册的设置迁移器实例</param>
    /// <returns>当前设置模型实例，支持链式调用</returns>
    public ISettingsModel RegisterMigration(ISettingsMigration migration)
    {
        _migrations[(migration.SettingsType, migration.FromVersion)] = migration;
        return this;
    }


    // -----------------------------
    // Load / Init
    // -----------------------------

    /// <summary>
    /// 异步初始化设置模型，加载指定类型的设置数据
    /// </summary>
    /// <param name="settingTypes">要初始化的设置类型数组</param>
    public async Task InitializeAsync(params Type[] settingTypes)
    {
        foreach (var type in settingTypes)
        {
            if (!typeof(IResettable).IsAssignableFrom(type) ||
                !typeof(IData).IsAssignableFrom(type))
                continue;

            try
            {
                var loaded = (ISettingsSection)await Repository.LoadAsync(type);
                var migrated = MigrateIfNeeded(loaded);
                _dataSettings[type] = (IResettable)migrated;
                _migrationCache.TryRemove(type, out _);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load settings for {type.Name}", ex);
            }
        }
    }

    /// <summary>
    /// 重置指定类型的可重置对象
    /// </summary>
    /// <typeparam name="T">要重置的对象类型，必须是class类型，实现IResettable接口，并具有无参构造函数</typeparam>
    public void Reset<T>() where T : class, IResettable, new()
    {
        var data = GetData<T>();
        data.Reset();
    }

    /// <summary>
    /// 重置所有存储的数据设置对象
    /// </summary>
    public void ResetAll()
    {
        foreach (var data in _dataSettings.Values)
        {
            data.Reset();
        }
    }

    /// <summary>
    /// 如果需要的话，对设置节进行版本迁移
    /// </summary>
    /// <param name="section">待检查和迁移的设置节</param>
    /// <returns>迁移后的设置节</returns>
    private ISettingsSection MigrateIfNeeded(ISettingsSection section)
    {
        if (section is not IVersioned versioned)
            return section;

        var type = section.GetType();
        var current = section;

        if (!_migrationCache.TryGetValue(type, out var versionMap))
        {
            versionMap = _migrations
                .Where(kv => kv.Key.type == type)
                .ToDictionary(kv => kv.Key.from, kv => kv.Value);

            _migrationCache[type] = versionMap;
        }

        while (versionMap.TryGetValue(versioned.Version, out var migration))
        {
            current = migration.Migrate(current);
            versioned = (IVersioned)current;
        }

        return current;
    }


    /// <summary>
    /// 初始化方法，用于获取设置持久化服务
    /// </summary>
    protected override void OnInit()
    {
        _repository ??= this.GetUtility<TRepository>()!;
    }
}