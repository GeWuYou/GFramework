// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Logging;

namespace GFramework.Core.Resource;

/// <summary>
///     资源句柄实现，管理资源的生命周期和引用计数
///     线程安全：所有公共方法都是线程安全的
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
internal sealed class ResourceHandle<T> : IResourceHandle<T> where T : class
{
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif
    private readonly ILogger _logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(ResourceHandle<T>));
    private readonly Action<string> _onDispose;
    private bool _disposed;
    private int _referenceCount;

    /// <summary>
    ///     创建资源句柄
    /// </summary>
    /// <param name="resource">资源实例</param>
    /// <param name="path">资源路径</param>
    /// <param name="onDispose">释放时的回调</param>
    public ResourceHandle(T resource, string path, Action<string> onDispose)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        _referenceCount = 1;
    }

    /// <summary>
    ///     获取资源实例
    /// </summary>
    public T? Resource { get; private set; }

    /// <summary>
    ///     获取资源路径
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     获取资源是否有效
    /// </summary>
    public bool IsValid
    {
        get
        {
            lock (_lock)
            {
                return !_disposed && Resource != null;
            }
        }
    }

    /// <summary>
    ///     获取当前引用计数
    /// </summary>
    public int ReferenceCount
    {
        get
        {
            lock (_lock)
            {
                return _referenceCount;
            }
        }
    }

    /// <summary>
    ///     增加引用计数
    /// </summary>
    public void AddReference()
    {
        lock (_lock)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceHandle<T>));

            _referenceCount++;
        }
    }

    /// <summary>
    ///     减少引用计数
    /// </summary>
    public void RemoveReference()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _referenceCount--;

            if (_referenceCount <= 0)
            {
                DisposeInternal();
            }
        }
    }

    /// <summary>
    ///     释放资源句柄
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _referenceCount--;

            if (_referenceCount <= 0)
            {
                DisposeInternal();
            }
        }
    }

    /// <summary>
    ///     内部释放方法（必须在锁内调用）
    /// </summary>
    private void DisposeInternal()
    {
        if (_disposed)
            return;

        _disposed = true;
        Resource = null;

        try
        {
            _onDispose(Path);
        }
        catch (Exception ex)
        {
            _logger.Error($"[ResourceHandle] Error disposing resource '{Path}': {ex.Message}");
        }
    }
}
