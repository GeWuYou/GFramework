// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Resource;

/// <summary>Loads resources and owns their matching release, including native or disposable state.</summary>
/// <typeparam name="T">The resource type.</typeparam>
/// <remarks>
///     The manager does not dispose registered loaders. The owner must keep each loader and its release capability
///     alive until the last lease has been returned, even after manager shutdown completes. Callbacks obey their
///     host's thread requirements. A loader
///     sharing instances across different identities must retain/release its ownership for each successful load.
/// </remarks>
public interface IResourceLoader<T> where T : class
{
    /// <summary>Loads one resource ownership and propagates failures.</summary>
    /// <param name="path">The resource path.</param>
    /// <returns>A nonnull resource.</returns>
    T Load(string path);

    /// <summary>Loads one resource ownership asynchronously.</summary>
    /// <param name="path">The resource path.</param>
    /// <param name="cancellationToken">Cancels loading when the owner requests cancellation.</param>
    /// <returns>A nonnull resource.</returns>
    Task<T> LoadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Releases ownership returned by a successful load, exactly once per cache entry.</summary>
    /// <param name="resource">The loaded resource.</param>
    void Unload(T resource);

    /// <summary>Reports whether the loader accepts a path.</summary>
    /// <param name="path">The resource path.</param>
    /// <returns>Whether loading is supported.</returns>
    bool CanLoad(string path);
}
