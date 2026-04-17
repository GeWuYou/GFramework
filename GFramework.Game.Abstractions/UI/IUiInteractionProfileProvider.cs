using GFramework.Game.Abstractions.Enums;

namespace GFramework.Game.Abstractions.UI;

/// <summary>
///     由页面视图实现，用于按运行时状态动态提供交互语义配置。
/// </summary>
public interface IUiInteractionProfileProvider
{
    /// <summary>
    ///     获取页面当前应使用的交互配置。
    /// </summary>
    /// <param name="layer">页面绑定的默认 UI 层级。</param>
    /// <returns>当前页面的交互配置。</returns>
    UiInteractionProfile GetUiInteractionProfile(UiLayer layer);
}
