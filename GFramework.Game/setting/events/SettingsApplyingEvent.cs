using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
/// 表示设置应用事件的泛型类
/// </summary>
/// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
public class SettingsApplyingEvent<T>(T settings) : ISettingsChangedEvent
    where T : ISettingsSection
{
    /// <summary>
    /// 获取类型化的设置对象
    /// </summary>
    public T TypedSettings { get; } = settings;

    /// <summary>
    /// 获取设置类型的Type信息
    /// </summary>
    public Type SettingsType => typeof(T);

    /// <summary>
    /// 获取设置节基接口实例
    /// </summary>
    public ISettingsSection Settings => TypedSettings;

    /// <summary>
    /// 获取设置变更的时间戳
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}