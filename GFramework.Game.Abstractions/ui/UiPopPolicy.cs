namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// 定义UI弹窗的关闭策略枚举
/// </summary>
public enum UiPopPolicy
{
    /// <summary>
    /// 销毁实例
    /// </summary>
    Destroy,
    
    /// <summary>
    /// 隐藏但保留实例（下次Push可复用）
    /// </summary>
    Cache
}