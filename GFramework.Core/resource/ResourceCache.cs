using System.Collections.Concurrent;

namespace GFramework.Core.resource;

/// <summary>
///     资源缓存条目
/// </summary>
internal sealed class ResourceCacheEntry(object resource, Type resourceType)
{
    public object Resource { get; } = resource ?? throw new ArgumentNullException(nameof(resource));
    public Type ResourceType { get; } = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
    public int ReferenceCount { get; set; }
    public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     资源缓存系统，管理已加载资源的缓存和引用计数
///     线程安全：所有公共方法都是线程安全的
/// </summary>
internal sealed class ResourceCache
{
    /// <summary>
    ///     Path 参数验证错误消息常量
    /// </summary>
    private const string PathCannotBeNullOrEmptyMessage = "Path cannot be null or whitespace.";

    private readonly ConcurrentDictionary<string, ResourceCacheEntry> _cache = new();
    private readonly object _lock = new();

    /// <summary>
    ///     获取已缓存资源的数量
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    ///     添加资源到缓存
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="resource">资源实例</param>
    /// <returns>如果成功添加返回 true，如果已存在返回 false</returns>
    public bool Add<T>(string path, T resource) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        ArgumentNullException.ThrowIfNull(resource);

        var entry = new ResourceCacheEntry(resource, typeof(T));
        return _cache.TryAdd(path, entry);
    }

    /// <summary>
    ///     从缓存中获取资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>资源实例，如果不存在或类型不匹配返回 null</returns>
    public T? Get<T>(string path) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        if (_cache.TryGetValue(path, out var entry))
        {
            lock (_lock)
            {
                entry.LastAccessTime = DateTime.UtcNow;
            }

            if (entry.Resource is T typedResource)
            {
                return typedResource;
            }
        }

        return null;
    }

    /// <summary>
    ///     检查资源是否在缓存中
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>如果存在返回 true，否则返回 false</returns>
    public bool Contains(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        return _cache.ContainsKey(path);
    }

    /// <summary>
    ///     从缓存中移除资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>如果成功移除返回 true，否则返回 false</returns>
    public bool Remove(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        return _cache.TryRemove(path, out _);
    }

    /// <summary>
    ///     清空所有缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    ///     增加资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>如果成功增加返回 true，如果资源不存在返回 false</returns>
    public bool AddReference(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        if (_cache.TryGetValue(path, out var entry))
        {
            lock (_lock)
            {
                entry.ReferenceCount++;
                entry.LastAccessTime = DateTime.UtcNow;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     减少资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>减少后的引用计数，如果资源不存在返回 -1</returns>
    public int RemoveReference(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        if (_cache.TryGetValue(path, out var entry))
        {
            lock (_lock)
            {
                entry.ReferenceCount--;
                return entry.ReferenceCount;
            }
        }

        return -1;
    }

    /// <summary>
    ///     获取资源的引用计数
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>引用计数，如果资源不存在返回 -1</returns>
    public int GetReferenceCount(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        if (_cache.TryGetValue(path, out var entry))
        {
            lock (_lock)
            {
                return entry.ReferenceCount;
            }
        }

        return -1;
    }

    /// <summary>
    ///     获取所有已缓存资源的路径
    /// </summary>
    /// <returns>资源路径集合</returns>
    public IEnumerable<string> GetAllPaths()
    {
        return _cache.Keys.ToList();
    }

    /// <summary>
    ///     获取所有引用计数为 0 的资源路径
    /// </summary>
    /// <returns>资源路径集合</returns>
    public IEnumerable<string> GetUnreferencedPaths()
    {
        var unreferencedPaths = new List<string>();

        foreach (var kvp in _cache)
        {
            lock (_lock)
            {
                if (kvp.Value.ReferenceCount <= 0)
                {
                    unreferencedPaths.Add(kvp.Key);
                }
            }
        }

        return unreferencedPaths;
    }
}