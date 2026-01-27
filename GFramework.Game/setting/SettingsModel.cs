using GFramework.Core.extensions;
using GFramework.Core.model;
using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting;

/// <summary>
///     设置模型类，用于管理不同类型的应用程序设置部分
/// </summary>
public class SettingsModel : AbstractModel, ISettingsModel
{
    private readonly Dictionary<Type, IApplyAbleSettings> _applicators = new();
    private readonly Dictionary<Type, ISettingsData> _dataSettings = new();
    private ISettingsPersistence? _persistence;

    /// <summary>
    ///     获取或创建数据设置
    /// </summary>
    /// <typeparam name="T">设置数据类型，必须实现ISettingsData接口并具有无参构造函数</typeparam>
    /// <returns>指定类型的设置数据实例</returns>
    public T GetData<T>() where T : class, ISettingsData, new()
    {
        var type = typeof(T);

        // 尝试从现有字典中获取已存在的设置数据
        if (_dataSettings.TryGetValue(type, out var existing))
            return (T)existing;

        // 创建新的设置数据实例并存储到字典中
        var created = new T();
        _dataSettings[type] = created;
        return created;
    }

    /// <summary>
    ///     注册可应用设置（必须手动注册）
    /// </summary>
    /// <typeparam name="T">可应用设置的类型，必须继承自class和IApplyAbleSettings</typeparam>
    /// <param name="applicator">要注册的可应用设置实例</param>
    /// <returns>返回当前设置模型实例，支持链式调用</returns>
    public ISettingsModel RegisterApplicator<T>(T applicator) where T : class, IApplyAbleSettings
    {
        var type = typeof(T);
        _applicators[type] = applicator;

        // 如果这个应用设置同时也是数据设置，也注册到数据字典中
        if (applicator is ISettingsData data)
        {
            _dataSettings[type] = data;
        }

        return this;
    }

    /// <summary>
    ///     获取已注册的可应用设置
    /// </summary>
    /// <typeparam name="T">可应用设置类型，必须实现IApplyAbleSettings接口</typeparam>
    /// <returns>找到的可应用设置实例，如果未找到则返回null</returns>
    public T? GetApplicator<T>() where T : class, IApplyAbleSettings
    {
        var type = typeof(T);
        return _applicators.TryGetValue(type, out var applicator)
            ? (T)applicator
            : null;
    }

    /// <summary>
    ///     尝试获取指定类型的设置节
    /// </summary>
    /// <param name="type">要查找的设置类型</param>
    /// <param name="section">输出参数，找到的设置节实例</param>
    /// <returns>如果找到设置节则返回true，否则返回false</returns>
    public bool TryGet(Type type, out ISettingsSection section)
    {
        // 首先在数据设置字典中查找
        if (_dataSettings.TryGetValue(type, out var data))
        {
            section = data;
            return true;
        }

        // 然后在应用器字典中查找
        if (_applicators.TryGetValue(type, out var applicator))
        {
            section = applicator;
            return true;
        }

        section = null!;
        return false;
    }

    /// <summary>
    ///     获取所有设置节的集合
    /// </summary>
    /// <returns>包含所有设置节的可枚举集合</returns>
    public IEnumerable<ISettingsSection> All()
    {
        // 使用 HashSet 去重（避免同时实现两个接口的设置被重复返回）
        var sections = new HashSet<ISettingsSection>();

        foreach (var applicator in _applicators.Values)
            sections.Add(applicator);

        foreach (var data in _dataSettings.Values)
            sections.Add(data);

        return sections;
    }

    /// <summary>
    ///     初始化并加载指定类型的设置数据
    /// </summary>
    public async Task InitializeAsync(params Type[] settingTypes)
    {
        foreach (var type in settingTypes)
        {
            if (!typeof(ISettingsData).IsAssignableFrom(type) ||
                !type.IsClass ||
                type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            // 使用反射调用泛型方法 LoadAsync<T>
            var method = typeof(ISettingsPersistence)
                .GetMethod(nameof(ISettingsPersistence.LoadAsync))!
                .MakeGenericMethod(type);

            var task = (Task)method.Invoke(_persistence, null)!;
            await task;

            var loaded = (ISettingsData)((dynamic)task).Result;
            _dataSettings[type] = loaded;
        }
    }


    /// <summary>
    ///     初始化方法，用于执行模型的初始化逻辑
    /// </summary>
    protected override void OnInit()
    {
        _persistence = this.GetUtility<ISettingsPersistence>();
    }
}