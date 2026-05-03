// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;

namespace GFramework.Core.Events;

/// <summary>
///     泛型事件类，支持一个泛型参数 <typeparamref name="T" /> 的事件注册、注销与触发。
///     实现了 <see cref="IEvent" /> 接口以提供统一的事件操作接口。
/// </summary>
/// <typeparam name="T">事件回调函数的第一个参数类型。</typeparam>
public class Event<T> : IEvent
{
    /// <summary>
    ///     存储已注册的事件处理委托。
    ///     未注册监听器时保持 <see langword="null" />，从而让监听器计数与真实订阅数量保持一致。
    /// </summary>
    private Action<T>? _mOnEvent;

    /// <summary>
    ///     显式实现 <see cref="IEvent" /> 接口中的 <c>Register</c> 方法。
    ///     允许使用无参 <see cref="Action" /> 来订阅当前带参事件。
    /// </summary>
    /// <param name="onEvent">无参事件处理方法。</param>
    /// <returns><see cref="IUnRegister" /> 对象，用于稍后注销该事件监听器。</returns>
    IUnRegister IEvent.Register(Action onEvent)
    {
        return Register(Action);

        void Action(T _)
        {
            onEvent();
        }
    }

    /// <summary>
    ///     注册一个事件监听器，并返回可用于取消注册的对象。
    /// </summary>
    /// <param name="onEvent">要注册的事件处理方法。</param>
    /// <returns><see cref="IUnRegister" /> 对象，用于稍后注销该事件监听器。</returns>
    public IUnRegister Register(Action<T> onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => UnRegister(onEvent));
    }

    /// <summary>
    ///     取消指定的事件监听器。
    /// </summary>
    /// <param name="onEvent">需要被注销的事件处理方法。</param>
    public void UnRegister(Action<T> onEvent)
    {
        _mOnEvent -= onEvent;
    }

    /// <summary>
    ///     触发所有已注册的事件处理程序，并传递参数 <paramref name="t" />。
    /// </summary>
    /// <param name="t">传递给事件处理程序的参数。</param>
    public void Trigger(T t)
    {
        _mOnEvent?.Invoke(t);
    }

    /// <summary>
    ///     获取当前已注册的监听器数量。
    /// </summary>
    /// <returns>监听器数量。</returns>
    public int GetListenerCount()
    {
        return _mOnEvent?.GetInvocationList().Length ?? 0;
    }
}

/// <summary>
///     支持两个泛型参数 <typeparamref name="T" /> 和 <typeparamref name="TK" /> 的事件类。
///     提供事件注册、注销和触发功能。
/// </summary>
/// <typeparam name="T">第一个参数类型。</typeparam>
/// <typeparam name="TK">第二个参数类型。</typeparam>
public class Event<T, TK> : IEvent
{
    /// <summary>
    ///     存储已注册的双参数事件处理委托。
    ///     未注册监听器时保持 <see langword="null" />，从而让监听器计数与真实订阅数量保持一致。
    /// </summary>
    private Action<T, TK>? _mOnEvent;

    /// <summary>
    ///     显式实现 <see cref="IEvent" /> 接口中的 <c>Register</c> 方法。
    ///     允许使用无参 <see cref="Action" /> 来订阅当前带参事件。
    /// </summary>
    /// <param name="onEvent">无参事件处理方法。</param>
    /// <returns><see cref="IUnRegister" /> 对象，用于稍后注销该事件监听器。</returns>
    IUnRegister IEvent.Register(Action onEvent)
    {
        return Register(Action);

        void Action(T _, TK __)
        {
            onEvent();
        }
    }

    /// <summary>
    ///     注册一个接受两个参数的事件监听器，并返回可用于取消注册的对象。
    /// </summary>
    /// <param name="onEvent">要注册的事件处理方法。</param>
    /// <returns><see cref="IUnRegister" /> 对象，用于稍后注销该事件监听器。</returns>
    public IUnRegister Register(Action<T, TK> onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => UnRegister(onEvent));
    }

    /// <summary>
    ///     取消指定的双参数事件监听器。
    /// </summary>
    /// <param name="onEvent">需要被注销的事件处理方法。</param>
    public void UnRegister(Action<T, TK> onEvent)
    {
        _mOnEvent -= onEvent;
    }

    /// <summary>
    ///     触发所有已注册的事件处理程序，并传递参数 <paramref name="t" /> 和 <paramref name="k" />。
    /// </summary>
    /// <param name="t">第一个参数。</param>
    /// <param name="k">第二个参数。</param>
    public void Trigger(T t, TK k)
    {
        _mOnEvent?.Invoke(t, k);
    }

    /// <summary>
    ///     获取当前已注册的监听器数量。
    /// </summary>
    /// <returns>监听器数量。</returns>
    public int GetListenerCount()
    {
        return _mOnEvent?.GetInvocationList().Length ?? 0;
    }
}
