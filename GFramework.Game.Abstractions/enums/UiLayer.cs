namespace GFramework.Game.Abstractions.enums;

/// <summary>
///     UI层级枚举，定义UI界面的显示层级
///     用于管理不同类型的UI在屏幕上的显示顺序
/// </summary>
public enum UiLayer
{
    /// <summary>
    ///     页面层，使用栈管理UI的切换
    ///     是否可重入：❌ 不可重入，基于栈管理，不支持重入
    /// </summary>
    Page,

    /// <summary>
    ///     浮层，用于覆盖层、对话框等
    ///     是否可重入：✅ 支持重入，可叠加显示，适合多层浮层
    /// </summary>
    Overlay,

    /// <summary>
    ///     模态层，会阻挡下层交互，带有遮罩效果
    ///     是否可重入：✅ 支持重入（需谨慎），可重入但需注意层级和焦点管理
    /// </summary>
    Modal,

    /// <summary>
    ///     提示层，用于轻量提示如toast消息、loading指示器等
    ///     是否可重入：✅ 支持重入，轻量提示，允许多个同时存在
    /// </summary>
    Toast,

    /// <summary>
    ///     顶层，用于系统级弹窗、全屏加载等
    ///     是否可重入：❌ 不可重入，最高优先级，通常不允许重入
    /// </summary>
    Topmost
}