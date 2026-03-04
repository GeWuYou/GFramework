using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.Abstractions.resource;

/// <summary>
///     资源管理器接口，提供资源加载、缓存和卸载功能
///     线程安全：所有方法都是线程安全的
/// </summary>
public interface IResourceManager : IUtility
{
    /// <summary>
    ///     获取已加载资源的数量
    /// </summary>
    int LoadedResourceCount { get; }

    /// <summary>
    ///     同步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>资源实例，如果加载失败返回 null</returns>
    T? Load<T>(string path) where T : class;

    /// <summary>
    ///     异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>资源实例，如果加载失败返回 null</returns>
    Task<T?> LoadAsync<T>(string path) where T : class;

    /// <summary>
    ///     获取资源句柄（增加引用计数）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns>资源句柄</returns>
    IResourceHandle<T>? GetHandle<T>(string path) where T : class;

    /// <summary>
    ///     卸载指定路径的资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>如果成功卸载返回 true，否则返回 false</returns>
    bool Unload(string path);

    /// <summary>
    ///     卸载所有资源
    /// </summary>
    void UnloadAll();

    /// <summary>
    ///     检查资源是否已加载
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>如果已加载返回 true，否则返回 false</returns>
    bool IsLoaded(string path);

    /// <summary>
    ///     注册资源加载器
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="loader">资源加载器</param>
    void RegisterLoader<T>(IResourceLoader<T> loader) where T : class;

    /// <summary>
    ///     取消注册资源加载器
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    void UnregisterLoader<T>() where T : class;

    /// <summary>
    ///     预加载资源（加载但不返回）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    Task PreloadAsync<T>(string path) where T : class;

    /// <summary>
    ///     获取所有已加载资源的路径
    /// </summary>
    IEnumerable<string> GetLoadedResourcePaths();

    /// <summary>
    ///     设置资源释放策略
    /// </summary>
    /// <param name="strategy">资源释放策略</param>
    void SetReleaseStrategy(IResourceReleaseStrategy strategy);
}