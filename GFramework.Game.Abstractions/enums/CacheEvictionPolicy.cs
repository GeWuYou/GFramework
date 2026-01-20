namespace GFramework.Game.Abstractions.enums;

/// <summary>
/// 缓存淘汰策略枚举
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>
    /// 最近最少使用
    /// </summary>
    Lru,

    /// <summary>
    /// 最少使用频率
    /// </summary>
    Lfu,
}
