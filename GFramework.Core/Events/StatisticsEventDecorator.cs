// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     带统计功能的事件装饰器
///     使用装饰器模式为任何 IEvent 实现添加统计功能
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
internal sealed class StatisticsEventDecorator<T>
{
    private readonly string _eventTypeName;
    private readonly IEvent _innerEvent;
    private readonly EventStatistics _statistics;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="innerEvent">被装饰的事件对象</param>
    /// <param name="statistics">统计对象</param>
    public StatisticsEventDecorator(IEvent innerEvent, EventStatistics statistics)
    {
        _innerEvent = innerEvent ?? throw new ArgumentNullException(nameof(innerEvent));
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        _eventTypeName = typeof(T).Name;
    }

    /// <summary>
    ///     注册事件监听器（带统计）
    /// </summary>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口</returns>
    public IUnRegister Register(Action<T> onEvent)
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

        var unregister = _innerEvent.Register(() => { }); // 占位，实际不使用

        // 直接注册到内部事件
        if (_innerEvent is Event<T> typedEvent)
        {
            unregister = typedEvent.Register(wrappedHandler);
        }
        else if (_innerEvent is PriorityEvent<T> priorityEvent)
        {
            unregister = priorityEvent.Register(wrappedHandler);
        }

        // 更新监听器统计
        UpdateListenerCount();

        return new DefaultUnRegister(() =>
        {
            unregister.UnRegister();
            UpdateListenerCount();
        });
    }

    /// <summary>
    ///     触发事件（带统计）
    /// </summary>
    /// <param name="data">事件数据</param>
    public void Trigger(T data)
    {
        _statistics.RecordPublish(_eventTypeName);

        if (_innerEvent is Event<T> typedEvent)
        {
            typedEvent.Trigger(data);
        }
        else if (_innerEvent is PriorityEvent<T> priorityEvent)
        {
            priorityEvent.Trigger(data, EventPropagation.All);
        }
    }

    /// <summary>
    ///     更新监听器数量统计
    /// </summary>
    private void UpdateListenerCount()
    {
        var count = 0;

        if (_innerEvent is Event<T> typedEvent)
        {
            count = typedEvent.GetListenerCount();
        }
        else if (_innerEvent is PriorityEvent<T> priorityEvent)
        {
            count = priorityEvent.GetListenerCount();
        }

        _statistics.UpdateListenerCount(_eventTypeName, count);
    }
}