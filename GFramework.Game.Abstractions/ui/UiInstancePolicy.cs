namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI页面实例管理策略（控制实例的生命周期）
/// </summary>
public enum UiInstancePolicy
{
    /// <summary>
    ///     总是创建新实例
    /// </summary>
    AlwaysCreate,

    /// <summary>
    ///     复用已存在的实例（如果有）
    /// </summary>
    Reuse,

    /// <summary>
    ///     从预加载池中获取或创建
    /// </summary>
    Pooled
}