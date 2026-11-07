// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Logging;

namespace GFramework.Core.Resource;

/// <summary>
///     资源管理器实现，提供资源加载、缓存和卸载功能
///     句柄获取、卸载和释放策略在同一操作锁内串行化。加载器拥有它创建的资源及释放责任；
///     管理器保留原始加载器的释放操作，不额外调用资源的 Dispose。
/// </summary>
public class ResourceManager : IResourceManager
{
    /// <summary>
    ///     Path 参数验证错误消息常量
    /// </summary>
    private const string PathCannotBeNullOrEmptyMessage = "Path cannot be null or whitespace.";

    private readonly ResourceCache _cache = new();
    private readonly ConcurrentDictionary<Type, object> _loaders = new();
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _loadLock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _loadLock = new();
#endif
    private readonly ILogger _logger;
    private IResourceReleaseStrategy _releaseStrategy;

    /// <summary>
    ///     创建资源管理器
    ///     默认使用手动释放策略
    /// </summary>
    public ResourceManager(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _releaseStrategy = new ManualReleaseStrategy();
    }

    /// <summary>
    ///     获取已加载资源的数量
    /// </summary>
    public int LoadedResourceCount => _cache.Count;

    /// <summary>
    ///     同步加载资源
    /// </summary>
    public T? Load<T>(string path) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        lock (_loadLock)
        {
            var cached = GetCachedResource<T>(path);
            if (cached != null)
            {
                return cached;
            }

            var loader = GetLoader<T>();
            if (loader == null)
            {
                throw new InvalidOperationException($"No loader registered for type {typeof(T).Name}");
            }

            try
            {
                var resource = loader.Load(path);
                _cache.Add(path, resource, value => loader.Unload((T)value));
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load resource '{path}'");
                return null;
            }
        }
    }

    /// <summary>
    ///     异步加载指定路径的资源，并在缓存中进行并发去重。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径，不能为空或空白。</param>
    /// <returns>加载成功返回资源实例；加载失败返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="path"/> 为空或空白时抛出。</exception>
    /// <exception cref="InvalidOperationException">当未注册对应资源加载器时抛出。</exception>
    /// <remarks>内部使用 <c>ConfigureAwait(false)</c>，后续延续不保证回到调用线程。</remarks>
    public async Task<T?> LoadAsync<T>(string path) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        lock (_loadLock)
        {
            var cached = GetCachedResource<T>(path);
            if (cached != null)
                return cached;
        }

        var loader = GetLoader<T>();
        if (loader == null)
        {
            throw new InvalidOperationException($"No loader registered for type {typeof(T).Name}");
        }

        try
        {
            var resource = await loader.LoadAsync(path).ConfigureAwait(false);
            lock (_loadLock)
            {
                // 双重检查
                var cached = _cache.Get<T>(path);
                if (cached != null)
                {
                    // 有些加载器内部也会缓存实例；同一引用仍被缓存拥有时不能把它当作失败竞争者释放。
                    if (!ReferenceEquals(cached, resource))
                        loader.Unload(resource);
                    return cached;
                }

                if (_cache.Contains(path))
                {
                    // 另一个类型已经占用同一路径时不覆盖其所有权，也不能泄漏本次创建的实例。
                    loader.Unload(resource);
                    throw new InvalidOperationException($"Resource '{path}' is already loaded with a different type.");
                }

                _cache.Add(path, resource, value => loader.Unload((T)value));
            }

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load resource '{path}'");
            return null;
        }
    }

    /// <summary>
    ///     获取资源句柄
    /// </summary>
    public IResourceHandle<T>? GetHandle<T>(string path) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        lock (_loadLock)
        {
            var entry = _cache.GetEntry(path);
            if (entry?.Resource is not T resource)
                return null;

            // 检查实例和增加引用必须与卸载互斥；回调绑定条目身份，不能作用到同一路径的后续实例。
            _cache.AddReference(path);
            return new ResourceHandle<T>(resource, path, releasedPath => HandleDispose(releasedPath, entry));
        }
    }

    /// <summary>
    ///     卸载指定路径的资源
    /// </summary>
    public bool Unload(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        lock (_loadLock)
        {
            var entry = _cache.GetEntry(path);
            if (entry == null)
                return false;

            // 检查引用计数
            var refCount = _cache.GetReferenceCount(path);
            if (refCount > 0)
            {
                _logger.LogError($"Cannot unload resource '{path}' with {refCount} active references");
                return false;
            }

            // 先移除条目，再调用拥有者，避免重入查询取得正在释放的实例。
            _cache.Remove(path);
            UnloadResource(entry);
            return true;
        }
    }

    /// <summary>
    ///     卸载所有没有活跃句柄的资源。仍被句柄引用的条目保持有效，调用方释放句柄后可再次卸载。
    /// </summary>
    public void UnloadAll()
    {
        lock (_loadLock)
        {
            var paths = _cache.GetAllPaths().ToList();

            foreach (var path in paths)
            {
                Unload(path);
            }
        }
    }

    /// <summary>
    ///     检查资源是否已加载
    /// </summary>
    public bool IsLoaded(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        return _cache.Contains(path);
    }

    /// <summary>
    ///     注册资源加载器
    /// </summary>
    public void RegisterLoader<T>(IResourceLoader<T> loader) where T : class
    {
        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        _loaders[typeof(T)] = loader;
    }

    /// <summary>
    ///     取消注册资源加载器
    /// </summary>
    public void UnregisterLoader<T>() where T : class
    {
        _loaders.TryRemove(typeof(T), out _);
    }

    /// <summary>
    ///     预加载资源到缓存中。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径，不能为空或空白。</param>
    /// <returns>表示预加载流程完成的任务。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="path"/> 为空或空白时抛出。</exception>
    /// <exception cref="InvalidOperationException">当未注册对应资源加载器时抛出。</exception>
    /// <remarks>内部委托给 <see cref="LoadAsync{T}(string)"/>，同样不捕获同步上下文。</remarks>
    public async Task PreloadAsync<T>(string path) where T : class
    {
        await LoadAsync<T>(path).ConfigureAwait(false);
    }

    /// <summary>
    ///     获取所有已加载资源的路径
    /// </summary>
    public IEnumerable<string> GetLoadedResourcePaths()
    {
        return _cache.GetAllPaths();
    }

    /// <summary>
    ///     设置资源释放策略
    /// </summary>
    /// <param name="strategy">资源释放策略</param>
    public void SetReleaseStrategy(IResourceReleaseStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        lock (_loadLock)
        {
            _releaseStrategy = strategy;
        }
    }

    /// <summary>
    ///     获取指定类型的资源加载器
    /// </summary>
    private IResourceLoader<T>? GetLoader<T>() where T : class
    {
        if (_loaders.TryGetValue(typeof(T), out var loader))
        {
            return loader as IResourceLoader<T>;
        }

        return null;
    }

    /// <summary>
    ///     从当前条目取得类型兼容的缓存实例；调用方必须持有操作锁。
    /// </summary>
    /// <typeparam name="T">请求的资源类型。</typeparam>
    /// <param name="path">缓存路径。</param>
    /// <returns>缓存实例；路径尚未加载时返回 null。</returns>
    /// <exception cref="InvalidOperationException">路径已经绑定到不兼容的资源类型。</exception>
    private T? GetCachedResource<T>(string path) where T : class
    {
        var entry = _cache.GetEntry(path);
        if (entry == null)
            return null;
        if (entry.Resource is T resource)
            return resource;
        throw new InvalidOperationException($"Resource '{path}' is already loaded with a different type.");
    }

    /// <summary>
    ///     交由创建条目的原始加载器释放资源，管理器不推断运行时类型或再次 Dispose。
    /// </summary>
    /// <param name="entry">已经从缓存移除的条目。</param>
    private void UnloadResource(ResourceCacheEntry entry)
    {
        try
        {
            entry.Release(entry.Resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading resource");
        }
    }

    /// <summary>
    ///     句柄释放时的回调
    /// </summary>
    /// <param name="path">句柄持有的路径。</param>
    /// <param name="entry">句柄获取时所绑定的条目。</param>
    private void HandleDispose(string path, ResourceCacheEntry entry)
    {
        lock (_loadLock)
        {
            if (!ReferenceEquals(_cache.GetEntry(path), entry))
                return;

            var refCount = _cache.RemoveReference(path);
            if (refCount > 0 || !_releaseStrategy.ShouldRelease(path, refCount))
                return;

            _cache.Remove(path);
            UnloadResource(entry);
        }
    }
}
