using System.Threading.Tasks;

namespace GFramework.Game.Abstractions.setting;

/// <summary>
/// 定义可应用设置的接口，继承自ISettingsSection
/// </summary>
public interface IApplyAbleSettings : ISettingsSection
{
    /// <summary>
    /// 应用当前设置到系统中
    /// </summary>
    Task Apply();
}