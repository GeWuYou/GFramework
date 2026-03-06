using GFramework.Core.Abstractions.events;

namespace GFramework.Core.events;

/// <summary>
///     支持弱引用订阅的泛型事件类
///     使用弱引用存储监听器，避免事件订阅导致的内存泄漏
///     线程安全：使用锁保护监听器列表的修改
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
public sealed class WeakEvent<T>
{
    private readonly object _lock = new();
    private readonly EventStatistics? _statistics;
    private readonly List<WeakReference<Action<T>>> _weakHandlers = new();

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="statistics">事件统计对象（可选）</param>
    public WeakEvent(EventStatistics? statistics = null)
    {
        _statistics = statistics;
    }

    /// <summary>
    ///     注册事件监听器（弱引用）
    /// </summary>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口</returns>
    public IUnRegister Register(Action<T> onEvent)
    {
        lock (_lock)
        {
            _weakHandlers.Add(new WeakReference<Action<T>>(onEvent));
            UpdateListenerCount();
        }

        return new DefaultUnRegister(() => UnRegister(onEvent));
    }

    /// <summary>
    ///     注销事件监听器
    /// </summary>
    /// <param name="onEvent">事件处理回调</param>
    public void UnRegister(Action<T> onEvent)
    {
        lock (_lock)
        {
            _weakHandlers.RemoveAll(wr =>
            {
                if (!wr.TryGetTarget(out var target))
                    return true; // 目标已被回收，移除
                return ReferenceEquals(target, onEvent);
            });
            UpdateListenerCount();
        }
    }

    /// <summary>
    ///     触发事件
    /// </summary>
    /// <param name="data">事件数据</param>
    public void Trigger(T data)
    {
        // 记录发布统计
        _statistics?.RecordPublish(typeof(T).Name);

        // 收集存活的监听器
        var aliveHandlers = new List<Action<T>>();
        var needsUpdate = false;

        lock (_lock)
        {
            var beforeCount = _weakHandlers.Count;

            // 清理已回收的弱引用并收集存活的监听器
            _weakHandlers.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out var target))
                {
                    aliveHandlers.Add(target);
                    return false;
                }

                return true; // 目标已被回收，移除
            });

            // 检查是否有监听器被清理
            needsUpdate = _weakHandlers.Count != beforeCount;
        }

        // 在锁外调用监听器，避免死锁
        foreach (var handler in aliveHandlers)
        {
            try
            {
                handler(data);
                _statistics?.RecordHandle();
            }
            catch
            {
                _statistics?.RecordFailure();
                throw;
            }
        }

        // 如果有监听器被清理，更新统计
        if (needsUpdate)
        {
            lock (_lock)
            {
                UpdateListenerCount();
            }
        }
    }

    /// <summary>
    ///     清理已回收的弱引用
    /// </summary>
    public void Cleanup()
    {
        lock (_lock)
        {
            var beforeCount = _weakHandlers.Count;
            _weakHandlers.RemoveAll(wr => !wr.TryGetTarget(out _));
            if (_weakHandlers.Count != beforeCount)
                UpdateListenerCount();
        }
    }

    /// <summary>
    ///     获取当前存活的监听器数量
    /// </summary>
    public int GetListenerCount()
    {
        lock (_lock)
        {
            return _weakHandlers.Count(wr => wr.TryGetTarget(out _));
        }
    }

    /// <summary>
    ///     更新监听器数量统计
    /// </summary>
    private void UpdateListenerCount()
    {
        if (_statistics == null)
            return;

        var count = _weakHandlers.Count(wr => wr.TryGetTarget(out _));
        _statistics.UpdateListenerCount(typeof(T).Name, count);
    }
}