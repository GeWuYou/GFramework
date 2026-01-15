namespace GFramework.Game.Abstractions.enums;

/// <summary>
/// UI页面过渡策略枚举
/// 定义了UI页面在出栈时的不同处理方式
/// </summary>
public enum UiTransitionPolicy
{
    /// <summary>
    /// 出栈即销毁（一次性页面）
    /// 页面从栈中移除时会完全销毁实例
    /// </summary>
    Destroy,

    /// <summary>
    /// 出栈隐藏，保留实例
    /// 页面从栈中移除时仅隐藏显示，保留实例以便后续重用
    /// </summary>
    Hide,

    /// <summary>
    /// 覆盖显示（不影响下层页面）
    /// 当前页面覆盖在其他页面之上显示，不影响下层页面的状态
    /// </summary>
    Overlay,

    /// <summary>
    /// 独占显示（下层页面 Pause + Hide）
    /// 当前页面独占显示区域，下层页面会被暂停并隐藏
    /// </summary>
    Exclusive
}