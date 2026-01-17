using GFramework.Game.Abstractions.setting;

namespace GFramework.Game.setting.events;

/// <summary>
/// 表示设置应用完成事件
/// </summary>
/// <typeparam name="T">设置节类型，必须实现ISettingsSection接口</typeparam>
public class SettingsAppliedEvent<T>(T settings, bool success, Exception? error = null) : ISettingsChangedEvent
    where T : ISettingsSection
{
    /// <summary>
    /// 获取类型化的设置节实例
    /// </summary>
    public T TypedSettings => (T)Settings;

    /// <summary>
    /// 获取设置应用是否成功的状态
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// 获取设置应用过程中发生的错误异常（如果有的话）
    /// </summary>
    public Exception? Error { get; } = error;

    /// <summary>
    /// 获取设置类型的Type信息
    /// </summary>
    public Type SettingsType => typeof(T);

    /// <summary>
    /// 获取应用的设置节实例
    /// </summary>
    public ISettingsSection Settings { get; } = settings;

    /// <summary>
    /// 获取设置变更的时间戳（UTC时间）
    /// </summary>
    public DateTime ChangedAt { get; } = DateTime.UtcNow;
}