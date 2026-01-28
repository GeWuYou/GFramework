namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 可重置设置接口，继承自ISettingsSection接口
/// 提供将设置重置为默认值的功能
/// </summary>
public interface IResettable : ISettingsSection
{
    /// <summary>
    ///     重置设置为默认值
    /// </summary>
    void Reset();
}