using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// UI动画策略配置
/// 用于配置UI过渡动画的行为
/// </summary>
public class UiAnimationPolicy
{
    /// <summary>
    /// 动画类型
    /// </summary>
    public UiTransitionAnimation Animation { get; set; } = UiTransitionAnimation.None;

    /// <summary>
    /// 动画持续时间（秒）
    /// </summary>
    public float Duration { get; set; } = 0.3f;

    /// <summary>
    /// 是否阻塞UI切换（等待动画完成）
    /// </summary>
    public bool BlockTransition { get; set; } = false;

    /// <summary>
    /// 自定义动画实现（仅当 Animation 为 Custom 时使用）
    /// </summary>
    public IUiTransition? CustomTransition { get; set; }

    /// <summary>
    /// 缓动函数（可选，用于调整动画曲线）
    /// </summary>
    public EasingFunction Easing { get; set; } = EasingFunction.EaseInOut;

    /// <summary>
    /// 创建默认策略（无动画）
    /// </summary>
    public static UiAnimationPolicy None => new UiAnimationPolicy { Animation = UiTransitionAnimation.None };

    /// <summary>
    /// 创建淡入淡出策略
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="block">是否阻塞</param>
    public static UiAnimationPolicy Fade(float duration = 0.3f, bool block = false)
        => new UiAnimationPolicy { Animation = UiTransitionAnimation.Fade, Duration = duration, BlockTransition = block };

    /// <summary>
    /// 创建滑入策略
    /// </summary>
    /// <param name="direction">滑动方向</param>
    /// <param name="duration">持续时间</param>
    /// <param name="block">是否阻塞</param>
    public static UiAnimationPolicy Slide(UiTransitionAnimation direction, float duration = 0.3f, bool block = false)
        => new UiAnimationPolicy { Animation = direction, Duration = duration, BlockTransition = block };

    /// <summary>
    /// 创建缩放策略
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="block">是否阻塞</param>
    public static UiAnimationPolicy Scale(float duration = 0.3f, bool block = false)
        => new UiAnimationPolicy { Animation = UiTransitionAnimation.Scale, Duration = duration, BlockTransition = block };
}

/// <summary>
/// 缓动函数枚举
/// </summary>
public enum EasingFunction
{
    /// <summary>
    /// 线性
    /// </summary>
    Linear,

    /// <summary>
    /// 缓入
    /// </summary>
    EaseIn,

    /// <summary>
    /// 缓出
    /// </summary>
    EaseOut,

    /// <summary>
    /// 缓入缓出
    /// </summary>
    EaseInOut
}
