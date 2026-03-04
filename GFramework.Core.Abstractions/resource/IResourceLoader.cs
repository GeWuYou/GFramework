namespace GFramework.Core.Abstractions.resource;

/// <summary>
///     资源加载器接口，用于加载特定类型的资源
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public interface IResourceLoader<T> where T : class
{
    /// <summary>
    ///     同步加载资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>加载的资源实例</returns>
    T Load(string path);

    /// <summary>
    ///     异步加载资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>加载的资源实例</returns>
    Task<T> LoadAsync(string path);

    /// <summary>
    ///     卸载资源
    /// </summary>
    /// <param name="resource">要卸载的资源</param>
    void Unload(T resource);

    /// <summary>
    ///     检查是否支持加载指定路径的资源
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <returns>如果支持返回 true，否则返回 false</returns>
    bool CanLoad(string path);
}