using System.Threading.Channels;
using GFramework.Core.Abstractions.logging;

namespace GFramework.Core.logging.appenders;

/// <summary>
///     异步日志输出器，使用 Channel 实现非阻塞日志写入
/// </summary>
public sealed class AsyncLogAppender : ILogAppender, IDisposable
{
    private readonly Channel<LogEntry> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly SemaphoreSlim _flushSemaphore = new(0, 1);
    private readonly ILogAppender _innerAppender;
    private readonly Task _processingTask;
    private bool _disposed;
    private volatile bool _flushRequested;

    /// <summary>
    ///     创建异步日志输出器
    /// </summary>
    /// <param name="innerAppender">内部日志输出器</param>
    /// <param name="bufferSize">缓冲区大小（默认 10000）</param>
    public AsyncLogAppender(ILogAppender innerAppender, int bufferSize = 10000)
    {
        _innerAppender = innerAppender ?? throw new ArgumentNullException(nameof(innerAppender));

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
        var success = Flush();
        OnFlushCompleted?.Invoke(success);
    }

    /// <summary>
    ///     Flush 操作完成事件，参数指示是否成功（true）或超时（false）
    /// </summary>
    public event Action<bool>? OnFlushCompleted;

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

        try
        {
            // 等待处理任务发出完成信号
            var success = _flushSemaphore.Wait(actualTimeout);
            OnFlushCompleted?.Invoke(success);
            return success;
        }
        finally
        {
            _flushRequested = false;
        }
    }

    /// <summary>
    ///     后台处理日志的异步方法
    /// </summary>
    private async Task ProcessLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    _innerAppender.Append(entry);
                }
                catch (Exception ex)
                {
                    // 记录内部错误到控制台（避免递归）
                    await Console.Error.WriteLineAsync($"[AsyncLogAppender] Error processing log entry: {ex.Message}");
                }

                // 检查是否有刷新请求且通道已空
                if (_flushRequested && _channel.Reader.Count == 0)
                {
                    _innerAppender.Flush();

                    // 发出完成信号
                    if (_flushSemaphore.CurrentCount == 0)
                    {
                        _flushSemaphore.Release();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[AsyncLogAppender] Fatal error in processing task: {ex}");
        }
        finally
        {
            // 确保最后刷新
            try
            {
                _innerAppender.Flush();
            }
            catch
            {
                // 忽略刷新错误
            }
        }
    }
}