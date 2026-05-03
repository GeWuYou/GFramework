// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Utility;

namespace GFramework.Core.Abstractions.Resource;

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
    ///     异步加载指定路径的资源，并在缓存中对并发加载进行去重。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径，不能为空或空白。</param>
    /// <returns>加载成功返回资源实例；加载失败返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="path"/> 为空或空白时抛出。</exception>
    /// <exception cref="InvalidOperationException">当未注册对应资源加载器时抛出。</exception>
    /// <remarks>实现内部可能使用 <c>ConfigureAwait(false)</c>，异步延续不保证回到调用线程。</remarks>
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
    ///     预加载资源到缓存中。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径，不能为空或空白。</param>
    /// <returns>表示预加载流程完成的任务。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="path"/> 为空或空白时抛出。</exception>
    /// <exception cref="InvalidOperationException">当未注册对应资源加载器时抛出。</exception>
    /// <remarks>内部委托给 <see cref="LoadAsync{T}(string)"/>，同样不捕获同步上下文。</remarks>
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
