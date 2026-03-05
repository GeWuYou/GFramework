using System.Collections.Concurrent;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.resource;
using GFramework.Core.logging;

namespace GFramework.Core.resource;

/// <summary>
///     资源管理器实现，提供资源加载、缓存和卸载功能
///     线程安全：所有公共方法都是线程安全的
/// </summary>
public class ResourceManager : IResourceManager
{
    /// <summary>
    ///     Path 参数验证错误消息常量
    /// </summary>
    private const string PathCannotBeNullOrEmptyMessage = "Path cannot be null or whitespace.";

    private readonly ResourceCache _cache = new();
    private readonly ConcurrentDictionary<Type, object> _loaders = new();
    private readonly object _loadLock = new();
    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(ResourceManager));
    private IResourceReleaseStrategy _releaseStrategy;

    /// <summary>
    ///     创建资源管理器
    ///     默认使用手动释放策略
    /// </summary>
    public ResourceManager()
    {
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

        // 检查缓存
        var cached = _cache.Get<T>(path);
        if (cached != null)
        {
            return cached;
        }

        // 加载资源
        lock (_loadLock)
        {
            // 双重检查
            cached = _cache.Get<T>(path);
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
                _cache.Add(path, resource);
                return resource;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load resource '{path}'", ex);
                return null;
            }
        }
    }

    /// <summary>
    ///     异步加载资源
    /// </summary>
    public async Task<T?> LoadAsync<T>(string path) where T : class
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(PathCannotBeNullOrEmptyMessage, nameof(path));

        // 检查缓存
        var cached = _cache.Get<T>(path);
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
            var resource = await loader.LoadAsync(path);
            lock (_loadLock)
            {
                // 双重检查
                cached = _cache.Get<T>(path);
                if (cached != null)
                {
                    // 已经被其他线程加载了，卸载当前加载的资源
                    loader.Unload(resource);
                    return cached;
                }

                _cache.Add(path, resource);
            }

            return resource;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load resource '{path}'", ex);
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

        var resource = _cache.Get<T>(path);
        if (resource == null)
            return null;

        _cache.AddReference(path);
        return new ResourceHandle<T>(resource, path, HandleDispose);
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
            var resource = _cache.Get<object>(path);
            if (resource == null)
                return false;

            // 检查引用计数
            var refCount = _cache.GetReferenceCount(path);
            if (refCount > 0)
            {
                _logger.Error($"Cannot unload resource '{path}' with {refCount} active references");
                return false;
            }

            // 卸载资源
            UnloadResource(resource);

            // 从缓存中移除
            return _cache.Remove(path);
        }
    }

    /// <summary>
    ///     卸载所有资源
    /// </summary>
    public void UnloadAll()
    {
        lock (_loadLock)
        {
            var paths = _cache.GetAllPaths().ToList();

            foreach (var path in paths)
            {
                var resource = _cache.Get<object>(path);
                if (resource != null)
                {
                    UnloadResource(resource);
                }
            }

            _cache.Clear();
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
    ///     预加载资源
    /// </summary>
    public async Task PreloadAsync<T>(string path) where T : class
    {
        await LoadAsync<T>(path);
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
        _releaseStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
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
    ///     卸载资源实例
    /// </summary>
    private void UnloadResource(object resource)
    {
        var resourceType = resource.GetType();

        if (_loaders.TryGetValue(resourceType, out var loaderObj))
        {
            try
            {
                var unloadMethod = loaderObj.GetType().GetMethod("Unload");
                unloadMethod?.Invoke(loaderObj, new[] { resource });
            }
            catch (Exception ex)
            {
                _logger.Error("Error unloading resource", ex);
            }
        }

        // 如果资源实现了 IDisposable，调用 Dispose
        if (resource is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error("Error disposing resource", ex);
            }
        }
    }

    /// <summary>
    ///     句柄释放时的回调
    /// </summary>
    private void HandleDispose(string path)
    {
        var refCount = _cache.RemoveReference(path);

        // 使用策略模式判断是否应该释放资源
        if (_releaseStrategy.ShouldRelease(path, refCount))
        {
            lock (_loadLock)
            {
                // 双重检查引用计数，避免竞态条件
                var currentRefCount = _cache.GetReferenceCount(path);
                if (currentRefCount <= 0)
                {
                    var resource = _cache.Get<object>(path);
                    if (resource != null)
                    {
                        UnloadResource(resource);
                        _cache.Remove(path);
                    }
                }
            }
        }
    }
}