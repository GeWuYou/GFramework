namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     定义UI弹窗的关闭策略枚举
/// </summary>
public enum UiPopPolicy
{
    /// <summary>
    ///     销毁实例
    /// </summary>
    Destroy,

    /// <summary>
    ///     可恢复
    /// </summary>
    Suspend
}