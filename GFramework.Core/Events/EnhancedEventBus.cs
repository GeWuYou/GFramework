// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     增强的事件总线，支持统计和过滤器
///     线程安全：使用 ConcurrentDictionary 存储事件
/// </summary>
public sealed class EnhancedEventBus : IEventBus
{
    private readonly EasyEvents _mEvents = new();
    private readonly ConcurrentDictionary<Type, object> _mFilterableEvents = new();
    private readonly EasyEvents _mPriorityEvents = new();
    private readonly ConcurrentDictionary<Type, object> _mWeakEvents = new();
    private readonly EventStatistics? _statistics;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="enableStatistics">是否启用统计功能</param>
    public EnhancedEventBus(bool enableStatistics = false)
    {
        _statistics = enableStatistics ? new EventStatistics() : null;
    }

    /// <summary>
    ///     获取事件统计信息（如果启用）
    /// </summary>
    public IEventStatistics? Statistics => _statistics;

    #region IEventBus Implementation

    /// <summary>
    ///     发送指定类型的事件实例（自动创建默认实例）
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public void Send<T>() where T : new()
    {
        _statistics?.RecordPublish(typeof(T).Name);

        _mEvents
            .GetOrAddEvent<Event<T>>()
            .Trigger(new T());
    }

    /// <summary>
    ///     发送指定类型的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    public void Send<T>(T e)
    {
        _statistics?.RecordPublish(typeof(T).Name);

        _mEvents
            .GetOrAddEvent<Event<T>>()
            .Trigger(e);
    }

    /// <summary>
    ///     发送具有指定传播方式的优先级事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    /// <param name="propagation">事件传播方式</param>
    public void Send<T>(T e, EventPropagation propagation)
    {
        _statistics?.RecordPublish(typeof(T).Name);

        _mPriorityEvents
            .GetOrAddEvent<PriorityEvent<T>>()
            .Trigger(e, propagation);
    }

    /// <summary>
    ///     注册事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口，用于取消订阅</returns>
    public IUnRegister Register<T>(Action<T> onEvent)
    {
        if (_statistics != null)
        {
            // 包装回调以添加统计
            Action<T> wrappedHandler = data =>
            {
                try
                {
                    onEvent(data);
                    _statistics.RecordHandle();
                }
                catch
                {
                    _statistics.RecordFailure();
                    throw;
                }
            };

            var unregister = _mEvents.GetOrAddEvent<Event<T>>().Register(wrappedHandler);
            UpdateEventListenerCount<T>();
            return new DefaultUnRegister(() =>
            {
                unregister.UnRegister();
                UpdateEventListenerCount<T>();
            });
        }

        return _mEvents.GetOrAddEvent<Event<T>>().Register(onEvent);
    }

    /// <summary>
    ///     注册具有优先级的监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调</param>
    /// <param name="priority">优先级，数值越大优先级越高</param>
    /// <returns>反注册接口，用于取消订阅</returns>
    public IUnRegister Register<T>(Action<T> onEvent, int priority)
    {
        if (_statistics != null)
        {
            // 包装回调以添加统计
            Action<T> wrappedHandler = data =>
            {
                try
                {
                    onEvent(data);
                    _statistics.RecordHandle();
                }
                catch
                {
                    _statistics.RecordFailure();
                    throw;
                }
            };

            var unregister = _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().Register(wrappedHandler, priority);
            UpdatePriorityEventListenerCount<T>();
            return new DefaultUnRegister(() =>
            {
                unregister.UnRegister();
                UpdatePriorityEventListenerCount<T>();
            });
        }

        return _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().Register(onEvent, priority);
    }

    /// <summary>
    ///     注销事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调</param>
    public void UnRegister<T>(Action<T> onEvent)
    {
        _mEvents.GetEvent<Event<T>>().UnRegister(onEvent);
        UpdateEventListenerCount<T>();
    }

    /// <summary>
    ///     注册带有上下文信息的优先级事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调，接收事件上下文</param>
    /// <returns>反注册接口，用于取消订阅</returns>
    public IUnRegister RegisterWithContext<T>(Action<EventContext<T>> onEvent)
    {
        if (_statistics != null)
        {
            // 包装回调以添加统计
            Action<EventContext<T>> wrappedHandler = context =>
            {
                try
                {
                    onEvent(context);
                    _statistics.RecordHandle();
                }
                catch
                {
                    _statistics.RecordFailure();
                    throw;
                }
            };

            var unregister = _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().RegisterWithContext(wrappedHandler);
            UpdatePriorityEventListenerCount<T>();
            return new DefaultUnRegister(() =>
            {
                unregister.UnRegister();
                UpdatePriorityEventListenerCount<T>();
            });
        }

        return _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().RegisterWithContext(onEvent);
    }

    /// <summary>
    ///     注册带有上下文信息和优先级的监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调，接收事件上下文</param>
    /// <param name="priority">优先级，数值越大优先级越高</param>
    /// <returns>反注册接口，用于取消订阅</returns>
    public IUnRegister RegisterWithContext<T>(Action<EventContext<T>> onEvent, int priority)
    {
        if (_statistics != null)
        {
            // 包装回调以添加统计
            Action<EventContext<T>> wrappedHandler = context =>
            {
                try
                {
                    onEvent(context);
                    _statistics.RecordHandle();
                }
                catch
                {
                    _statistics.RecordFailure();
                    throw;
                }
            };

            var unregister = _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>()
                .RegisterWithContext(wrappedHandler, priority);
            UpdatePriorityEventListenerCount<T>();
            return new DefaultUnRegister(() =>
            {
                unregister.UnRegister();
                UpdatePriorityEventListenerCount<T>();
            });
        }

        return _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().RegisterWithContext(onEvent, priority);
    }

    #endregion

    #region Filterable Events

    /// <summary>
    ///     发送支持过滤的事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    public void SendFilterable<T>(T e)
    {
        var evt = (FilterableEvent<T>)_mFilterableEvents.GetOrAdd(
            typeof(T),
            _ => new FilterableEvent<T>(_statistics));
        evt.Trigger(e);
    }

    /// <summary>
    ///     注册支持过滤的事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口</returns>
    public IUnRegister RegisterFilterable<T>(Action<T> onEvent)
    {
        var evt = (FilterableEvent<T>)_mFilterableEvents.GetOrAdd(
            typeof(T),
            _ => new FilterableEvent<T>(_statistics));
        return evt.Register(onEvent);
    }

    /// <summary>
    ///     为指定事件类型添加过滤器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="filter">过滤器</param>
    public void AddFilter<T>(IEventFilter<T> filter)
    {
        var evt = (FilterableEvent<T>)_mFilterableEvents.GetOrAdd(
            typeof(T),
            _ => new FilterableEvent<T>(_statistics));
        evt.AddFilter(filter);
    }

    /// <summary>
    ///     移除指定事件类型的过滤器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="filter">过滤器</param>
    public void RemoveFilter<T>(IEventFilter<T> filter)
    {
        if (_mFilterableEvents.TryGetValue(typeof(T), out var obj))
        {
            var evt = (FilterableEvent<T>)obj;
            evt.RemoveFilter(filter);
        }
    }

    /// <summary>
    ///     清除指定事件类型的所有过滤器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public void ClearFilters<T>()
    {
        if (_mFilterableEvents.TryGetValue(typeof(T), out var obj))
        {
            var evt = (FilterableEvent<T>)obj;
            evt.ClearFilters();
        }
    }

    #endregion

    #region Weak Events

    /// <summary>
    ///     发送弱引用事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    public void SendWeak<T>(T e)
    {
        var evt = (WeakEvent<T>)_mWeakEvents.GetOrAdd(
            typeof(T),
            _ => new WeakEvent<T>(_statistics));
        evt.Trigger(e);
    }

    /// <summary>
    ///     注册弱引用事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口</returns>
    public IUnRegister RegisterWeak<T>(Action<T> onEvent)
    {
        var evt = (WeakEvent<T>)_mWeakEvents.GetOrAdd(
            typeof(T),
            _ => new WeakEvent<T>(_statistics));
        return evt.Register(onEvent);
    }

    /// <summary>
    ///     清理指定事件类型的已回收弱引用
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public void CleanupWeak<T>()
    {
        if (_mWeakEvents.TryGetValue(typeof(T), out var obj))
        {
            var evt = (WeakEvent<T>)obj;
            evt.Cleanup();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     更新普通事件的监听器数量统计
    /// </summary>
    private void UpdateEventListenerCount<T>()
    {
        if (_statistics == null)
            return;

        var evt = _mEvents.GetEvent<Event<T>>();
        if (evt != null)
        {
            var count = evt.GetListenerCount();
            _statistics.UpdateListenerCount(typeof(T).Name, count);
        }
    }

    /// <summary>
    ///     更新优先级事件的监听器数量统计
    /// </summary>
    private void UpdatePriorityEventListenerCount<T>()
    {
        if (_statistics == null)
            return;

        var evt = _mPriorityEvents.GetEvent<PriorityEvent<T>>();
        if (evt != null)
        {
            var count = evt.GetListenerCount();
            _statistics.UpdateListenerCount(typeof(T).Name, count);
        }
    }

    #endregion
}