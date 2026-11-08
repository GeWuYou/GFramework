// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     带统计功能的事件装饰器
///     使用装饰器模式为任何 IEvent 实现添加统计功能
/// </summary>
/// <typeparam name="T">事件数据类型</typeparam>
/// <remarks>
///     构造函数
/// </remarks>
/// <param name="innerEvent">被装饰的事件对象</param>
/// <param name="statistics">统计对象</param>
[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
internal sealed class StatisticsEventDecorator<T>(IEvent innerEvent, EventStatistics statistics)
{
    private readonly string _eventTypeName = typeof(T).Name;
    private readonly IEvent _innerEvent = innerEvent ?? throw new ArgumentNullException(nameof(innerEvent));
    private readonly EventStatistics _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));

    /// <summary>
    ///     注册事件监听器（带统计）
    /// </summary>
    /// <param name="onEvent">事件处理回调</param>
    /// <returns>反注册接口</returns>
    public IDisposable Register(Action<T> onEvent)
    {
        // 包装回调以添加统计
        void wrappedHandler(T data)
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
        }

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

        return new Subscription(() =>
        {
            unregister.Dispose();
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
