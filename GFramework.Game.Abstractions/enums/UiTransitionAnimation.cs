namespace GFramework.Game.Abstractions.enums;

/// <summary>
/// UI过渡动画类型枚举
/// 定义UI切换时支持的动画效果
/// </summary>
public enum UiTransitionAnimation
{
    /// <summary>
    /// 无动画
    /// </summary>
    None,

    /// <summary>
    /// 淡入淡出动画
    /// </summary>
    Fade,

    /// <summary>
    /// 从右侧滑入
    /// </summary>
    SlideLeft,

    /// <summary>
    /// 从左侧滑入
    /// </summary>
    SlideRight,

    /// <summary>
    /// 从下方滑入
    /// </summary>
    SlideUp,

    /// <summary>
    /// 从上方滑入
    /// </summary>
    SlideDown,

    /// <summary>
    /// 缩放动画
    /// </summary>
    Scale,

    /// <summary>
    /// 自定义动画（需要提供自定义的 IUiTransition 实现）
    /// </summary>
    Custom
}
