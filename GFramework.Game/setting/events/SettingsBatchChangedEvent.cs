using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
///     批量设置变更事件
///     表示多个设置项同时发生变更的事件
/// </summary>
/// <param name="settings">发生变更的设置数据集合</param>
public class SettingsBatchChangedEvent(IEnumerable<IResettable> settings) : ISettingsChangedEvent
{
    /// <summary>
    ///     获取发生变更的具体设置数据列表
    /// </summary>
    public IEnumerable<IResettable> ChangedSettings { get; } = settings.ToList();

    /// <summary>
    ///     获取设置类型，对于批量变更事件，固定返回ISettingsSection类型
    /// </summary>
    public Type SettingsType => typeof(ISettingsSection);

    /// <summary>
    ///     获取设置实例，批量变更事件中此属性返回null
    /// </summary>
    public ISettingsSection Settings => null!;

    /// <summary>
    ///     获取变更发生的时间戳（UTC时间）
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}