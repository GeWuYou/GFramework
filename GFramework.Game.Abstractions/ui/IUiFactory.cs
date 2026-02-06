using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI工厂接口，用于创建UI页面实例
/// </summary>
public interface IUiFactory : IContextUtility
{
    /// <summary>
    ///     创建或获取UI页面实例
    /// </summary>
    /// <param name="uiKey">UI标识键</param>
    /// <returns>UI页面实例</returns>
    IUiPageBehavior Create(string uiKey);
}