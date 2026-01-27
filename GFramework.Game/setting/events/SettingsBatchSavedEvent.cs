using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
///     表示设置批量保存事件
/// </summary>
/// <param name="settings">要保存的设置数据集合</param>
public class SettingsBatchSavedEvent(IEnumerable<ISettingsData> settings) : ISettingsChangedEvent
{
    /// <summary>
    ///     获取已保存的设置数据只读集合
    /// </summary>
    public IReadOnlyCollection<ISettingsData> SavedSettings { get; } = settings.ToList();

    /// <summary>
    ///     获取设置类型（始终返回ISettingsSection类型）
    /// </summary>
    public Type SettingsType => typeof(ISettingsSection);

    /// <summary>
    ///     获取设置节（在此事件中始终为null）
    /// </summary>
    public ISettingsSection Settings => null!;

    /// <summary>
    ///     获取更改发生的时间（UTC时间）
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}