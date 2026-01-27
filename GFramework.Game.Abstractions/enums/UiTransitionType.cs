namespace GFramework.Game.Abstractions.enums;

/// <summary>
///     UI切换类型枚举，定义不同的UI切换操作类型
/// </summary>
public enum UiTransitionType
{
    /// <summary>
    ///     压入新页面到栈顶
    /// </summary>
    Push,

    /// <summary>
    ///     弹出栈顶页面
    /// </summary>
    Pop,

    /// <summary>
    ///     替换当前页面
    /// </summary>
    Replace,

    /// <summary>
    ///     清空所有页面
    /// </summary>
    Clear
}