// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     支持过滤器和统计的泛型事件类
///     线程安全：使用锁保护监听器列表和过滤器列表的修改
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
public sealed class FilterableEvent<T>
{
    private readonly List<IEventFilter<T>> _filters = new();
#if NET9_0_OR_GREATER
    // net9.0 及以上目标使用专用 Lock，以满足分析器对专用同步原语的建议。
    private readonly System.Threading.Lock _lock = new();
#else
    // net8.0 目标仍回退到 object 锁，以保持多目标编译兼容性。
    private readonly object _lock = new();
#endif
    private readonly EventStatistics? _statistics;
    private Action<T>? _onEvent;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="statistics">事件统计对象（可选）</param>
    public FilterableEvent(EventStatistics? statistics = null)
    {
        _statistics = statistics;
    }

    /// <summary>
    ///     注册事件监听器
    /// </summary>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口</returns>
    public IUnRegister Register(Action<T> onEvent)
    {
        lock (_lock)
        {
            _onEvent += onEvent;
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
            _onEvent -= onEvent;
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

        // 在单个锁中快照过滤器和监听器
        Action<T>? handlers;
        IEventFilter<T>[] filtersSnapshot;

        lock (_lock)
        {
            filtersSnapshot = _filters.Count > 0 ? _filters.ToArray() : Array.Empty<IEventFilter<T>>();
            handlers = _onEvent;
        }

        // 在锁外执行过滤逻辑
        // 事件被过滤，不触发监听器
        if (filtersSnapshot.Any(filter => filter.ShouldFilter(data)))
            return;

        if (handlers == null)
            return;

        // 在锁外调用监听器，避免死锁
        foreach (var handler in handlers.GetInvocationList().Cast<Action<T>>())
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
    }

    /// <summary>
    ///     添加事件过滤器
    /// </summary>
    /// <param name="filter">过滤器</param>
    public void AddFilter(IEventFilter<T> filter)
    {
        lock (_lock)
        {
            _filters.Add(filter);
        }
    }

    /// <summary>
    ///     移除事件过滤器
    /// </summary>
    /// <param name="filter">过滤器</param>
    public void RemoveFilter(IEventFilter<T> filter)
    {
        lock (_lock)
        {
            _filters.Remove(filter);
        }
    }

    /// <summary>
    ///     清除所有过滤器
    /// </summary>
    public void ClearFilters()
    {
        lock (_lock)
        {
            _filters.Clear();
        }
    }

    /// <summary>
    ///     获取当前监听器数量
    /// </summary>
    public int GetListenerCount()
    {
        lock (_lock)
        {
            return _onEvent?.GetInvocationList().Length ?? 0;
        }
    }

    /// <summary>
    ///     更新监听器数量统计
    /// </summary>
    private void UpdateListenerCount()
    {
        if (_statistics == null)
            return;

        var count = _onEvent?.GetInvocationList().Length ?? 0;
        _statistics.UpdateListenerCount(typeof(T).Name, count);
    }
}
