using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
/// 表示所有设置已加载完成的事件
/// </summary>
/// <param name="all">包含所有设置节的可枚举集合</param>
public class SettingsAllLoadedEvent(IEnumerable<ISettingsSection> all) : ISettingsChangedEvent
{
    /// <summary>
    /// 获取所有设置节的只读集合
    /// </summary>
    public IReadOnlyCollection<ISettingsSection> AllSettings { get; } = all.ToList();

    /// <summary>
    /// 获取设置类型，始终返回 ISettingsSection 类型
    /// </summary>
    public Type SettingsType => typeof(ISettingsSection);

    /// <summary>
    /// 获取具体的设置节，此事件中始终为 null
    /// </summary>
    public ISettingsSection Settings => null!;

    /// <summary>
    /// 获取事件发生的时间（UTC时间）
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}