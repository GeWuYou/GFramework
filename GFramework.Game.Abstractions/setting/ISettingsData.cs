namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 设置数据接口 - 纯数据，可自动创建
/// </summary>
public interface ISettingsData : ISettingsSection
{
    /// <summary>
    /// 重置设置为默认值
    /// </summary>
    void Reset();
}