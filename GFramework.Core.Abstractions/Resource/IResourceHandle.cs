// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Resource;

/// <summary>One independently disposable lease on a resource. Keep it alive for the entire use of the resource.</summary>
/// <typeparam name="T">The resource type.</typeparam>
/// <remarks>
///     Disposal releases exactly one manager reference; the loader releases the underlying resource. Consumers
///     must coordinate concurrent use and disposal of the same lease. Retain creates a separately owned lease.
/// </remarks>
public interface IResourceHandle<out T> : IDisposable where T : class
{
    /// <summary>Gets the leased resource; access after this lease is disposed throws.</summary>
    T Resource
    {
        get;
    }

    /// <summary>Gets the loader-specific path.</summary>
    string Path
    {
        get;
    }

    /// <summary>Gets whether this lease has not been disposed.</summary>
    bool IsValid
    {
        get;
    }

    /// <summary>Creates an independent lease on the same resource.</summary>
    /// <returns>A new lease whose disposal does not invalidate this lease.</returns>
    /// <exception cref="ObjectDisposedException">This lease or its manager is closed.</exception>
    IResourceHandle<T> Retain();
}
