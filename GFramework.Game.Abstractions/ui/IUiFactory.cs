using GFramework.Core.Abstractions.utility;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI工厂接口，用于创建UI页面实例
/// </summary>
public interface IUiFactory : IContextUtility
{
    /// <summary>
    /// 根据UI键值创建对应的UI页面实例
    /// </summary>
    /// <param name="uiKey">UI标识键，用于确定要创建的具体UI页面类型</param>
    /// <returns>创建的UI页面实例，实现IUiPage接口</returns>
    IPageBehavior Create(string uiKey);
}