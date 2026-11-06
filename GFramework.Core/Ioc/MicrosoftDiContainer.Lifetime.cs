// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GFramework.Core.Ioc;

/// <summary>
///     将标准 DI provider 的资源清理与注册/解析路径隔离，确保异步资源和并发释放共享同一结果。
/// </summary>
public partial class MicrosoftDiContainer
{
    /// <summary>自有标准容器共享给包装上下文的访问窗口，不作为可解析业务服务暴露。</summary>
    internal global::GFramework.Core.Architectures.ArchitectureContextAccess? ContextAccess { get; set; }

    /// <summary>
    ///     只保护清理操作的发布；资源回调不在该锁或容器读写锁内执行。
    /// </summary>
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _disposalSync = new();
#else
    private readonly object _disposalSync = new();
#endif

    /// <summary>
    ///     同步和异步调用都复用首次发布的清理任务，包括清理失败结果。
    /// </summary>
    private Task? _disposalTask;

    /// <summary>标准 DI 式同链重入为 no-op；其他并发调用仍等待共享释放任务。</summary>
    private readonly System.Threading.AsyncLocal<bool> _insideDisposal = new();

    /// <summary>
    ///     同步释放根 provider 创建的同步资源；含异步资源的容器应使用 DisposeAsync。
    /// </summary>
    /// <remarks>
    ///     不在容器读写锁内调用资源回调。多个释放调用会等待同一个完成任务，
    ///     不会在首次清理仍运行时提前返回。
    /// </remarks>
    public void Dispose()
    {
        GetDisposalTask(asyncMode: false).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     异步释放根 provider 及其拥有的同步和异步资源。
    /// </summary>
    /// <returns>所有并发释放调用共享的清理操作。</returns>
    public ValueTask DisposeAsync()
    {
        return new ValueTask(GetDisposalTask(asyncMode: true));
    }

    /// <summary>
    ///     先发布完成任务再开始清理，使后续调用能够稳定等待同一结果。
    /// </summary>
    /// <param name="asyncMode">首次调用是否允许异步释放资源。</param>
    /// <returns>共享清理任务。</returns>
    private Task GetDisposalTask(bool asyncMode)
    {
        if (_insideDisposal.Value && _disposalTask is { IsCompleted: false })
            return Task.CompletedTask;
        TaskCompletionSource<object?> completion;
        lock (_disposalSync)
        {
            if (_disposalTask != null)
            {
                return _disposalTask;
            }

            completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposalTask = completion.Task;
        }

        // CompleteDisposalAsync catches and publishes every exception through the shared completion.
        _ = CompleteDisposalAsync(completion, asyncMode);
        return completion.Task;
    }

    /// <summary>
    ///     截断新访问后释放拥有的 provider，并向所有等待者发布清理结果。
    /// </summary>
    /// <param name="completion">已向调用方发布的共享完成源。</param>
    /// <param name="asyncMode">是否优先调用标准 provider 的异步释放入口。</param>
    /// <returns>内部清理过程；异常会传递给共享完成源。</returns>
    private async Task CompleteDisposalAsync(TaskCompletionSource<object?> completion, bool asyncMode)
    {
        _insideDisposal.Value = true;
        try
        {
            try
            {
                var provider = DetachOwnedProvider();
                if (asyncMode && provider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            finally
            {
                DisposeLockWhenQuiescent();
            }

            _logger.LogInformation("IOC Container disposed");
            completion.TrySetResult(null);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            _insideDisposal.Value = false;
        }
    }

    /// <summary>
    ///     等待现有解析退出，标记终态并移交本容器拥有的 provider；外部实例不会被逐项释放。
    /// </summary>
    /// <returns>需要由本次清理释放的根 provider；尚未构建时为空。</returns>
    private IServiceProvider? DetachOwnedProvider()
    {
        _lock.EnterWriteLock();
        try
        {
            _disposed = true;
            var provider = _provider;
            _provider = null;
            _frozenServiceTypeIndex = null;
            GetServicesUnsafe.Clear();
            _registeredInstances.Clear();
            _frozen = false;
            return provider;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
