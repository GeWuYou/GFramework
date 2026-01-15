namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// 定义UI弹窗的关闭策略枚举
/// </summary>
public enum UiPopPolicy
{
    /// <summary>
    /// 销毁模式：关闭时完全销毁UI对象
    /// </summary>
    Destroy,

    /// <summary>
    /// 隐藏模式：关闭时仅隐藏UI对象，保留实例
    /// </summary>
    Hide
}