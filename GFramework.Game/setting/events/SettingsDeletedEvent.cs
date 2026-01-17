using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
/// 表示设置删除事件
/// </summary>
public class SettingsDeletedEvent(Type settingsType) : ISettingsChangedEvent
{
    /// <summary>
    /// 获取被删除的设置类型
    /// </summary>
    public Type SettingsType { get; } = settingsType;

    /// <summary>
    /// 获取设置实例，删除事件中返回 null
    /// </summary>
    public ISettingsSection Settings => null!;

    /// <summary>
    /// 获取删除时间
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}