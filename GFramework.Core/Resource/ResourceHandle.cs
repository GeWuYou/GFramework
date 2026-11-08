// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Abstractions.Resource;
using GFramework.Core.Logging;

namespace GFramework.Core.Resource;

/// <summary>
///     资源句柄实现，管理资源的生命周期和引用计数
///     线程安全：所有公共方法都是线程安全的
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
/// <remarks>
///     创建资源句柄
/// </remarks>
/// <param name="logger">标准日志记录器；未提供时使用空输出。</param>
/// <param name="resource">资源实例</param>
/// <param name="path">资源路径</param>
/// <param name="onDispose">释放时的回调</param>
internal sealed class ResourceHandle<T>(T resource, string path, Action<string> onDispose, ILogger? logger = null) : IResourceHandle<T> where T : class
{
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly Action<string> _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    private bool _disposed;
    private int _referenceCount = 1;

    /// <summary>
    ///     获取资源实例
    /// </summary>
    public T? Resource { get; private set; } = resource ?? throw new ArgumentNullException(nameof(resource));

    /// <summary>
    ///     获取资源路径
    /// </summary>
    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

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
            // net8/net9 compile with C# 12/13; retain the constructed nameof until C# 14 is supported by every target.
            // ThrowIf(object/type) would change the established ObjectName from ResourceHandle to a full runtime type name.
#pragma warning disable IDE0340, CA1513
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceHandle<T>));
#pragma warning restore IDE0340, CA1513

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
            if (_logger.IsEnabled(global::Microsoft.Extensions.Logging.LogLevel.Error))
            {
                _logger.LogError("[ResourceHandle] Error disposing resource '{ResourcePath}': {ErrorMessage}", Path, ex.Message);
            }
        }
    }
}
