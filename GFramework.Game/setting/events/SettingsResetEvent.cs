using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
/// 表示设置重置事件
/// </summary>
/// <typeparam name="T">设置节类型</typeparam>
public class SettingsResetEvent<T> : ISettingsChangedEvent
    where T : ISettingsSection
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="oldSettings">重置前的设置</param>
    /// <param name="newSettings">重置后的新设置</param>
    public SettingsResetEvent(T oldSettings, T newSettings)
    {
        OldSettings = oldSettings;
        NewSettings = newSettings;
    }

    /// <summary>
    /// 获取重置前的设置
    /// </summary>
    public T OldSettings { get; }

    /// <summary>
    /// 获取重置后的新设置
    /// </summary>
    public T NewSettings { get; }

    /// <summary>
    /// 获取类型化的设置实例（返回新设置）
    /// </summary>
    public T TypedSettings => NewSettings;

    /// <summary>
    /// 获取设置类型
    /// </summary>
    public Type SettingsType => typeof(T);

    /// <summary>
    /// 获取设置实例
    /// </summary>
    public ISettingsSection Settings => NewSettings;

    /// <summary>
    /// 获取重置时间
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}