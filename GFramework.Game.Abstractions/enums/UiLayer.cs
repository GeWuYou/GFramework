namespace GFramework.Game.Abstractions.enums;

/// <summary>
///     UI层级枚举，定义UI界面的显示层级
///     用于管理不同类型的UI在屏幕上的显示顺序
/// </summary>
public enum UiLayer
{
    /// <summary>
    ///     页面层，使用栈管理UI的切换
    /// </summary>
    Page,

    /// <summary>
    ///     浮层，用于覆盖层、对话框等
    /// </summary>
    Overlay,

    /// <summary>
    ///     模态层，会阻挡下层交互，带有遮罩效果
    /// </summary>
    Modal,

    /// <summary>
    ///     提示层，用于轻量提示如toast消息、loading指示器等
    /// </summary>
    Toast,

    /// <summary>
    ///     顶层，用于系统级弹窗、全屏加载等
    /// </summary>
    Topmost
}