// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;

namespace GFramework.Core.Logging.Appenders;

/// <summary>
///     异步日志输出器，使用 <see cref="Channel" /> 将调用线程与慢速日志目标解耦。
/// </summary>
/// <remarks>
///     <para>
///         该输出器在后台线程中顺序消费日志条目，因此调用方不会因为文件 IO 或其他慢速输出目标而阻塞。
///     </para>
///     <para>
///         内部输出器抛出的异常不会重新抛回调用线程；如需观察后台处理失败，请在构造函数中提供
///         <c>processingErrorHandler</c> 回调。
///     </para>
/// </remarks>
public sealed class AsyncLogAppender : ILogAppender
{
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly SemaphoreSlim _flushSemaphore = new(0, 1);
    private readonly ILogAppender _innerAppender;
    private readonly Action<Exception>? _processingErrorHandler;
    private readonly Task _processingTask;
    private bool _disposed;
    private int _isProcessingEntry;
    private volatile bool _flushRequested;

    /// <summary>
    ///     创建异步日志输出器
    /// </summary>
    /// <param name="innerAppender">内部日志输出器</param>
    /// <param name="bufferSize">缓冲区大小（默认 10000）</param>
    /// <param name="processingErrorHandler">
    ///     后台处理日志时的错误回调。
    ///     默认值为 <see langword="null" />，表示吞掉内部异常以避免污染宿主标准错误输出。
    /// </param>
    public AsyncLogAppender(
        ILogAppender innerAppender,
        int bufferSize = 10000,
        Action<Exception>? processingErrorHandler = null)
    {
        _innerAppender = innerAppender ?? throw new ArgumentNullException(nameof(innerAppender));
        _processingErrorHandler = processingErrorHandler;

        if (bufferSize <= 0)
            throw new ArgumentException("Buffer size must be greater than 0.", nameof(bufferSize));

        // 创建有界 Channel
        var options = new BoundedChannelOptions(bufferSize)
        {
            FullMode = BoundedChannelFullMode.Wait, // 缓冲区满时等待
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<LogEntry>(options);
        _cts = new CancellationTokenSource();

        // 启动后台处理任务
        _processingTask = Task.Run(() => ProcessLogsAsync(_cts.Token));
    }

    /// <summary>
    ///     获取当前缓冲区中的日志数量
    /// </summary>
    public int PendingCount => _channel.Reader.Count;

    /// <summary>
    ///     获取是否已完成处理
    /// </summary>
    public bool IsCompleted => _channel.Reader.Completion.IsCompleted;

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // 标记 Channel 为完成状态
        _channel.Writer.Complete();

        // 等待处理任务完成（最多等待 5 秒）
        if (!_processingTask.Wait(TimeSpan.FromSeconds(5)))
        {
            _cts.Cancel();
        }

        // 释放内部 Appender
        if (_innerAppender is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _cts.Dispose();
        _flushSemaphore.Dispose();
        _disposed = true;
    }

    /// <summary>
    ///     追加日志条目（非阻塞）
    /// </summary>
    /// <param name="entry">日志条目</param>
    public void Append(LogEntry entry)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AsyncLogAppender));

        // 尝试非阻塞写入，如果失败则丢弃（避免阻塞调用线程）
        _channel.Writer.TryWrite(entry);
    }

    /// <summary>
    ///     刷新缓冲区（ILogAppender 接口实现）
    ///     注意：此方法会阻塞直到所有待处理日志写入完成，或超时（默认30秒）
    ///     超时结果可通过 OnFlushCompleted 事件观察
    /// </summary>
    void ILogAppender.Flush()
    {
        Flush();
    }

    /// <summary>
    ///     Flush 操作完成事件。
    ///     事件数据通过 <see cref="AsyncLogFlushCompletedEventArgs" /> 提供。
    /// </summary>
    public event EventHandler<AsyncLogFlushCompletedEventArgs>? OnFlushCompleted;

    /// <summary>
    ///     刷新缓冲区，等待所有日志写入完成
    ///     使用信号量机制确保可靠的完成通知，避免竞态条件
    /// </summary>
    /// <param name="timeout">超时时间（默认30秒）</param>
    /// <returns>是否成功刷新所有日志（true=成功，false=超时）</returns>
    public bool Flush(TimeSpan? timeout = null)
    {
        if (_disposed) return false;

        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);

        // 请求刷新
        _flushRequested = true;
        TrySignalFlushCompletion();

        try
        {
            // 等待处理任务发出完成信号
            var success = _flushSemaphore.Wait(actualTimeout);
            OnFlushCompleted?.Invoke(this, new AsyncLogFlushCompletedEventArgs(success));
            return success;
        }
        finally
        {
            _flushRequested = false;
        }
    }

    /// <summary>
    ///     后台处理日志的异步方法。
    ///     该循环必须始终保持存活，因此所有内部异常都通过回调上报并被吞掉。
    /// </summary>
    private async Task ProcessLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    Volatile.Write(ref _isProcessingEntry, 1);
                    _innerAppender.Append(entry);
                }
                catch (Exception ex)
                {
                    // 后台消费失败只通过显式回调暴露，避免测试宿主将 stderr 误判为测试告警。
                    ReportProcessingError(ex);
                }
                finally
                {
                    Volatile.Write(ref _isProcessingEntry, 0);
                }

                TrySignalFlushCompletion();
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
        catch (Exception ex)
        {
            ReportProcessingError(ex);
        }
        finally
        {
            // 确保最后刷新
            try
            {
                _innerAppender.Flush();
            }
            catch (Exception ex)
            {
                ReportProcessingError(ex);
            }
        }
    }

    /// <summary>
    ///     在后台消费者已经处理完当前条目且队列为空时完成挂起的 Flush 请求。
    /// </summary>
    private void TrySignalFlushCompletion()
    {
        if (!_flushRequested)
        {
            return;
        }

        if (Volatile.Read(ref _isProcessingEntry) != 0 || _channel.Reader.Count != 0)
        {
            return;
        }

        _innerAppender.Flush();

        if (_flushSemaphore.CurrentCount == 0)
        {
            _flushSemaphore.Release();
        }
    }

    /// <summary>
    ///     上报后台处理异常，同时隔离观察者自身抛出的错误，避免终止处理循环。
    ///     取消相关异常表示关闭流程中的预期控制流，不应被视为后台处理失败。
    /// </summary>
    /// <param name="exception">后台处理中捕获到的异常。</param>
    private void ReportProcessingError(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        if (_processingErrorHandler is null)
        {
            return;
        }

        try
        {
            _processingErrorHandler(exception);
        }
        catch
        {
            // 错误观察者只用于诊断，绝不能反向影响日志处理线程的生命周期。
        }
    }
}
