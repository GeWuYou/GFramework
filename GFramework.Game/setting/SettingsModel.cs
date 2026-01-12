using GFramework.Core.model;
using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting;

/// <summary>
/// 设置模型类，用于管理不同类型的应用程序设置部分
/// </summary>
public class SettingsModel : AbstractModel, ISettingsModel
{
    private readonly Dictionary<Type, ISettingsSection> _sections = new();

    /// <summary>
    /// 获取指定类型的设置部分实例，如果不存在则创建新的实例
    /// </summary>
    /// <typeparam name="T">设置部分的类型，必须实现ISettingsSection接口并具有无参构造函数</typeparam>
    /// <returns>指定类型的设置部分实例</returns>
    public T Get<T>() where T : class, ISettingsSection, new()
    {
        var type = typeof(T);

        // 尝试从字典中获取已存在的设置部分实例
        if (_sections.TryGetValue(type, out var existing))
            return (T)existing;

        // 创建新的设置部分实例并存储到字典中
        var created = new T();
        _sections[type] = created;
        return created;
    }

    /// <summary>
    /// 尝试获取指定类型的设置部分实例
    /// </summary>
    /// <param name="type">设置部分的类型</param>
    /// <param name="section">输出参数，如果找到则返回对应的设置部分实例，否则为null</param>
    /// <returns>如果找到指定类型的设置部分则返回true，否则返回false</returns>
    public bool TryGet(Type type, out ISettingsSection section)
        => _sections.TryGetValue(type, out section!);

    /// <summary>
    /// 获取所有设置部分的集合
    /// </summary>
    /// <returns>包含所有设置部分的可枚举集合</returns>
    public IEnumerable<ISettingsSection> All()
        => _sections.Values;

    /// <summary>
    /// 注册一个可应用的设置对象到管理器中
    /// </summary>
    /// <param name="applyAble">要注册的可应用设置对象</param>
    public void Register(IApplyAbleSettings applyAble)
    {
        // 获取传入对象的类型信息
        var type = applyAble.GetType();
        // 尝试将类型和对象添加到线程安全的字典中
        _sections.TryAdd(type, applyAble);
    }


    /// <summary>
    /// 初始化方法，用于执行模型的初始化逻辑
    /// </summary>
    protected override void OnInit()
    {
    }
}