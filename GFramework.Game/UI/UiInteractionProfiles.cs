using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;

namespace GFramework.Game.UI;

/// <summary>
///     为 <see cref="UiInteractionProfile" /> 提供运行时默认值与语义判定。
/// </summary>
/// <remarks>
///     该 helper 保留在运行时程序集内，避免把默认策略和输入判定逻辑放回 Abstractions。
///     UI 页面和路由器都应通过这里共享同一套默认语义，避免层级默认值漂移。
/// </remarks>
public static class UiInteractionProfiles
{
    /// <summary>
    ///     获取不捕获动作、也不阻断 World 输入的默认配置。
    /// </summary>
    public static UiInteractionProfile Default { get; } = new();

    /// <summary>
    ///     获取会捕获取消动作并阻断 World 输入的阻塞型默认配置。
    /// </summary>
    public static UiInteractionProfile BlockingCancel { get; } = new()
    {
        CapturedActions = UiInputActionMask.Cancel,
        BlocksWorldPointerInput = true,
        BlocksWorldActionInput = true
    };

    /// <summary>
    ///     为指定层级生成默认交互配置。
    /// </summary>
    /// <param name="layer">UI 层级。</param>
    /// <returns>该层级的默认交互语义。</returns>
    public static UiInteractionProfile CreateDefault(UiLayer layer)
    {
        return layer switch
        {
            UiLayer.Modal or UiLayer.Topmost => BlockingCancel,
            _ => Default
        };
    }

    /// <summary>
    ///     判断指定配置是否捕获了目标 UI 语义动作。
    /// </summary>
    /// <param name="profile">目标配置。</param>
    /// <param name="action">要查询的动作。</param>
    /// <returns>如果配置声明捕获了该动作则返回 <see langword="true" />。</returns>
    public static bool Captures(UiInteractionProfile profile, UiInputAction action)
    {
        return action switch
        {
            UiInputAction.Cancel => (profile.CapturedActions & UiInputActionMask.Cancel) != UiInputActionMask.None,
            UiInputAction.Confirm => (profile.CapturedActions & UiInputActionMask.Confirm) != UiInputActionMask.None,
            _ => false
        };
    }
}
