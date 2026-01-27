namespace GFramework.Game.Abstractions.ui;

/// <summary>
///     UI缓存统计信息接口
/// </summary>
public interface IUiCacheStatistics
{
    /// <summary>
    ///     缓存总数
    /// </summary>
    int CacheSize { get; }

    /// <summary>
    ///     缓存命中次数
    /// </summary>
    int HitCount { get; }

    /// <summary>
    ///     缓存未命中次数
    /// </summary>
    int MissCount { get; }

    /// <summary>
    ///     命中率
    /// </summary>
    double HitRate { get; }

    /// <summary>
    ///     最近访问时间
    /// </summary>
    DateTime? LastAccessTime { get; }
}