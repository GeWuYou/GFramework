// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     类型事件系统，提供基于类型的事件发送、注册和注销功能
/// </summary>
public class EventBus : IEventBus
{
    private readonly EasyEvents _mEvents = new();
    private readonly EasyEvents _mPriorityEvents = new();

    /// <summary>
    ///     发送事件，自动创建事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须具有无参构造函数</typeparam>
    public void Send<T>() where T : new()
    {
        _mEvents
            .GetOrAddEvent<Event<T>>()
            .Trigger(new T());
    }

    /// <summary>
    ///     发送指定的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    public void Send<T>(T e)
    {
        _mEvents
            .GetOrAddEvent<Event<T>>()
            .Trigger(e);
    }

    /// <summary>
    ///     发送指定的事件实例，并指定传播模式
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    /// <param name="propagation">事件传播模式</param>
    public void Send<T>(T e, EventPropagation propagation)
    {
        _mPriorityEvents
            .GetOrAddEvent<PriorityEvent<T>>()
            .Trigger(e, propagation);
    }

    /// <summary>
    ///     注册事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调函数</param>
    /// <returns>反注册接口，用于注销事件监听</returns>
    public IUnRegister Register<T>(Action<T> onEvent)
    {
        return _mEvents.GetOrAddEvent<Event<T>>().Register(onEvent);
    }

    /// <summary>
    ///     注册事件监听器，并指定优先级
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调函数</param>
    /// <param name="priority">优先级，数值越大优先级越高</param>
    /// <returns>反注册接口，用于注销事件监听</returns>
    public IUnRegister Register<T>(Action<T> onEvent, int priority)
    {
        return _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().Register(onEvent, priority);
    }

    /// <summary>
    ///     注销事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">要注销的事件处理回调函数</param>
    public void UnRegister<T>(Action<T> onEvent)
    {
        _mEvents.GetEvent<Event<T>>().UnRegister(onEvent);
    }

    /// <summary>
    ///     注册上下文事件监听器，默认优先级为 0
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调函数，接收 EventContext 参数</param>
    /// <returns>反注册接口，用于注销事件监听</returns>
    public IUnRegister RegisterWithContext<T>(Action<EventContext<T>> onEvent)
    {
        return _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().RegisterWithContext(onEvent);
    }

    /// <summary>
    ///     注册上下文事件监听器，并指定优先级
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调函数，接收 EventContext 参数</param>
    /// <param name="priority">优先级，数值越大优先级越高</param>
    /// <returns>反注册接口，用于注销事件监听</returns>
    public IUnRegister RegisterWithContext<T>(Action<EventContext<T>> onEvent, int priority)
    {
        return _mPriorityEvents.GetOrAddEvent<PriorityEvent<T>>().RegisterWithContext(onEvent, priority);
    }
}