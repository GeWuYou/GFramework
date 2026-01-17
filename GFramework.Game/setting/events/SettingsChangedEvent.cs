using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
/// 泛型设置变更事件
/// </summary>
/// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
/// <param name="settings">设置实例</param>
public class SettingsChangedEvent<T>(T settings) : ISettingsChangedEvent
    where T : ISettingsSection
{
    /// <summary>
    /// 获取类型化的设置实例
    /// </summary>
    public T TypedSettings => (T)Settings;

    /// <summary>
    /// 获取设置类型
    /// </summary>
    public Type SettingsType => typeof(T);

    /// <summary>
    /// 获取设置实例
    /// </summary>
    public ISettingsSection Settings { get; } = settings;

    /// <summary>
    /// 获取变更时间
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}