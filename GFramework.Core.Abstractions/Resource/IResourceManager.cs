// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Resource;

/// <summary>Coordinates typed resource loading, caching and leases independently of Architecture.</summary>
/// <remarks>
///     Cache identity is the requested type and ordinal path. Requests share one in-flight load for each identity.
///     Closing rejects new work, waits for in-flight loads and releases, and releases unleased resources. Existing leases remain
///     usable until disposed. Externally registered loaders and policies remain owned by the caller; their release
///     capabilities must outlive every issued lease. Return leases before disposing a DI provider that owns loaders.
///     Callbacks are not automatically dispatched to an engine thread; loaders must arrange their required thread affinity.
/// </remarks>
public interface IResourceManager : IDisposable, IAsyncDisposable
{
    /// <summary>Gets the number of loaded entries, including those held by active leases.</summary>
    int LoadedResourceCount
    {
        get;
    }

    /// <summary>Loads or retrieves a resource and atomically acquires one lease.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="path">A nonblank loader-specific path.</param>
    /// <returns>An independently disposable lease.</returns>
    /// <exception cref="InvalidOperationException">No loader exists, or a shared load is in flight.</exception>
    IResourceHandle<T> Acquire<T>(string path) where T : class;

    /// <summary>Shares a load and acquires a lease; cancellation affects only this caller's wait.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="path">A nonblank loader-specific path.</param>
    /// <param name="cancellationToken">Cancels this wait without canceling other waiters.</param>
    /// <returns>An independently disposable lease.</returns>
    Task<IResourceHandle<T>> AcquireAsync<T>(string path, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Pins a loaded entry until explicit unloading or manager closure without issuing a lease.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="path">A nonblank loader-specific path.</param>
    /// <param name="cancellationToken">Cancels this caller's wait.</param>
    /// <returns>The preload operation.</returns>
    Task PreloadAsync<T>(string path, CancellationToken cancellationToken = default) where T : class;

    /// <summary>Unloads an identity only when no lease or pending acquisition holds it.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="path">The resource path.</param>
    /// <returns>Whether a loaded entry was removed.</returns>
    bool Unload<T>(string path) where T : class;

    /// <summary>Unloads all entries without leases or pending acquisitions, attempting every eligible release.</summary>
    void UnloadAll();

    /// <summary>Checks whether a typed cache entry has completed loading.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="path">The resource path.</param>
    /// <returns>Whether the entry is loaded.</returns>
    bool IsLoaded<T>(string path) where T : class;

    /// <summary>Registers the loader used for future loads of a type.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="loader">The externally owned loader.</param>
    void RegisterLoader<T>(IResourceLoader<T> loader) where T : class;

    /// <summary>Removes a loader for future requests; existing entries keep their original release operation.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    void UnregisterLoader<T>() where T : class;

    /// <summary>Sets the policy for releasing the last lease; explicit preloads remain pinned.</summary>
    /// <param name="strategy">The externally owned policy.</param>
    void SetReleaseStrategy(IResourceReleaseStrategy strategy);
}
