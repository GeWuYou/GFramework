using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
///     异步操作包装器，用于桥接协程系统和async/await异步编程模型
/// </summary>
public class AsyncOperation : IYieldInstruction, INotifyCompletion
{
    private readonly TaskCompletionSource<object?> _tcs = new();
    private volatile bool _completed;
    private volatile Action? _continuation;

    /// <summary>
    ///     获取异步操作的任务
    /// </summary>
    public Task Task => _tcs.Task;

    /// <summary>
    ///     检查是否已完成
    /// </summary>
    public bool IsCompleted => _completed;

    /// <summary>
    ///     设置延续操作
    /// </summary>
    /// <param name="continuation">要执行的延续操作</param>
    public void OnCompleted(Action continuation)
    {
        // 尝试添加延续
        var current = _continuation;
        var newContinuation = current == null ? continuation : current + continuation;

        if (Interlocked.CompareExchange(ref _continuation, newContinuation, current) != current)
        {
            // 如果CAS失败，说明可能已经完成，直接执行
            if (_completed)
                continuation();
            else
                // 重试
                OnCompleted(continuation);

            return;
        }

        // 双重检查：如果在设置延续后发现已完成，需要执行延续
        if (_completed)
        {
            var cont = Interlocked.Exchange(ref _continuation, null);
            if (cont != null) cont();
        }
    }

    /// <summary>
    ///     获取异步操作是否已完成
    /// </summary>
    public bool IsDone => _completed;

    /// <summary>
    ///     更新方法，用于处理时间更新逻辑
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        // 由外部调用SetCompleted来更新状态
    }

    /// <summary>
    ///     标记异步操作已完成
    /// </summary>
    public void SetCompleted()
    {
        if (_completed) return;

        _completed = true;
        _tcs.SetResult(null);

        var continuation = Interlocked.Exchange(ref _continuation, null);
        if (continuation != null)
            try
            {
                continuation.Invoke();
            }
            catch
            {
                // 忽略延续中的异常
            }
    }

    /// <summary>
    ///     标记异步操作因异常而失败
    /// </summary>
    /// <param name="exception">导致失败的异常</param>
    public void SetException(Exception exception)
    {
        if (_completed) return;

        _completed = true;
        _tcs.SetException(exception);

        var continuation = Interlocked.Exchange(ref _continuation, null);
        if (continuation != null)
            try
            {
                continuation.Invoke();
            }
            catch
            {
                // 忽略延续中的异常
            }
    }

    /// <summary>
    ///     获取异步操作结果
    /// </summary>
    /// <returns>操作结果</returns>
    public object? GetResult()
    {
        return _tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    ///     获取awaiter对象
    /// </summary>
    public AsyncOperation GetAwaiter()
    {
        return this;
    }
}