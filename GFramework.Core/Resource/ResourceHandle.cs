// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Resource;

namespace GFramework.Core.Resource;

/// <summary>Owns one cache reference and creates independent references through Retain.</summary>
/// <typeparam name="T">The resource type.</typeparam>
internal sealed class ResourceHandle<T>(T resource, string path, Func<IResourceHandle<T>> retain, Action release)
    : IResourceHandle<T> where T : class
{
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private bool _disposed;

    /// <inheritdoc />
    public T Resource
    {
        get
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return resource;
            }
        }
    }

    /// <inheritdoc />
    public string Path { get; } = path;

    /// <inheritdoc />
    public bool IsValid
    {
        get
        {
            lock (_gate)
                return !_disposed;
        }
    }

    /// <inheritdoc />
    public IResourceHandle<T> Retain()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return retain();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
        }
        // External release runs outside the lease lock so reentrant disposal cannot deadlock.
        release();
    }
}
