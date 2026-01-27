using GFramework.Game.Abstractions.enums;

namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI缓存配置
///     用于配置UI实例的缓存行为
/// </summary>
public class UiCacheConfig
{
    /// <summary>
    ///     最大缓存数量
    /// </summary>
    public int MaxCacheSize { get; set; } = 10;

    /// <summary>
    ///     缓存淘汰策略
    /// </summary>
    public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.Lru;

    /// <summary>
    ///     访问后过期时间（可选，null 表示不启用）
    /// </summary>
    public TimeSpan? ExpireAfterAccess { get; set; }

    /// <summary>
    ///     创建默认配置（LRU 策略，最大 10 个实例）
    /// </summary>
    public static UiCacheConfig Default => new()
    {
        MaxCacheSize = 10,
        EvictionPolicy = CacheEvictionPolicy.Lru,
        ExpireAfterAccess = null
    };

    /// <summary>
    ///     创建 LRU 策略配置
    /// </summary>
    /// <param name="maxSize">最大缓存数量</param>
    /// <param name="expireAfter">访问后过期时间</param>
    public static UiCacheConfig Lru(int maxSize = 10, TimeSpan? expireAfter = null)
    {
        return new UiCacheConfig
        {
            MaxCacheSize = maxSize,
            EvictionPolicy = CacheEvictionPolicy.Lru,
            ExpireAfterAccess = expireAfter
        };
    }

    /// <summary>
    ///     创建 LFU 策略配置
    /// </summary>
    /// <param name="maxSize">最大缓存数量</param>
    /// <param name="expireAfter">访问后过期时间</param>
    public static UiCacheConfig Lfu(int maxSize = 10, TimeSpan? expireAfter = null)
    {
        return new UiCacheConfig
        {
            MaxCacheSize = maxSize,
            EvictionPolicy = CacheEvictionPolicy.Lfu,
            ExpireAfterAccess = expireAfter
        };
    }
}