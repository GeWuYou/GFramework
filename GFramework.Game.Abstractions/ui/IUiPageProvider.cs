namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI页面提供者接口，用于创建UI页面实例
/// </summary>
public interface IUiPageProvider
{
    /// <summary>
    /// 获取UI页面实例
    /// </summary>
    /// <returns>UI页面实例</returns>
    IPageBehavior GetPage();
}