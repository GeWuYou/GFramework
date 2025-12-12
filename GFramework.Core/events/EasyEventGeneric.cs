namespace GFramework.Core.events;

/// <summary>
///     泛型事件类，支持一个泛型参数 T 的事件注册、注销与触发。
///     实现了 IEasyEvent 接口以提供统一的事件操作接口。
/// </summary>
/// <typeparam name="T">事件回调函数的第一个参数类型。</typeparam>
public class EasyEvent<T> : IEasyEvent
{
    /// <summary>
    ///     存储已注册的事件处理委托。
    ///     默认为空操作（no-op）委托，避免 null 检查。
    /// </summary>
    private Action<T> _mOnEvent = e => { };

    /// <summary>
    ///     显式实现 IEasyEvent 接口中的 Register 方法。
    ///     允许使用无参 Action 来订阅当前带参事件。
    /// </summary>
    /// <param name="onEvent">无参事件处理方法。</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器。</returns>
    IUnRegister IEasyEvent.Register(Action onEvent)
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
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器。</returns>
    public IUnRegister Register(Action<T> onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => { UnRegister(onEvent); });
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
    ///     触发所有已注册的事件处理程序，并传递参数 t。
    /// </summary>
    /// <param name="t">传递给事件处理程序的参数。</param>
    public void Trigger(T t)
    {
        _mOnEvent?.Invoke(t);
    }
}

/// <summary>
///     支持两个泛型参数 T 和 TK 的事件类。
///     提供事件注册、注销和触发功能。
/// </summary>
/// <typeparam name="T">第一个参数类型。</typeparam>
/// <typeparam name="TK">第二个参数类型。</typeparam>
public class EasyEvent<T, TK> : IEasyEvent
{
    /// <summary>
    ///     存储已注册的双参数事件处理委托。
    ///     默认为空操作（no-op）委托。
    /// </summary>
    private Action<T, TK> _mOnEvent = (_, _) => { };

    /// <summary>
    ///     显式实现 IEasyEvent 接口中的 Register 方法。
    ///     允许使用无参 Action 来订阅当前带参事件。
    /// </summary>
    /// <param name="onEvent">无参事件处理方法。</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器。</returns>
    IUnRegister IEasyEvent.Register(Action onEvent)
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
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器。</returns>
    public IUnRegister Register(Action<T, TK> onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => { UnRegister(onEvent); });
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
    ///     触发所有已注册的事件处理程序，并传递参数 t 和 k。
    /// </summary>
    /// <param name="t">第一个参数。</param>
    /// <param name="k">第二个参数。</param>
    public void Trigger(T t, TK k)
    {
        _mOnEvent?.Invoke(t, k);
    }
}

/// <summary>
///     支持三个泛型参数 T、TK 和 TS 的事件类。
///     提供事件注册、注销和触发功能。
/// </summary>
/// <typeparam name="T">第一个参数类型。</typeparam>
/// <typeparam name="TK">第二个参数类型。</typeparam>
/// <typeparam name="TS">第三个参数类型。</typeparam>
public class EasyEvent<T, TK, TS> : IEasyEvent
{
    /// <summary>
    ///     存储已注册的三参数事件处理委托。
    ///     默认为空操作（no-op）委托。
    /// </summary>
    private Action<T, TK, TS> _mOnEvent = (_, _, _) => { };

    /// <summary>
    ///     显式实现 IEasyEvent 接口中的 Register 方法。
    ///     允许使用无参 Action 来订阅当前带参事件。
    /// </summary>
    /// <param name="onEvent">无参事件处理方法。</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器。</returns>
    IUnRegister IEasyEvent.Register(Action onEvent)
    {
        return Register(Action);

        void Action(T _, TK __, TS ___)
        {
            onEvent();
        }
    }

    /// <summary>
    ///     注册一个接受三个参数的事件监听器，并返回可用于取消注册的对象。
    /// </summary>
    /// <param name="onEvent">要注册的事件处理方法。</param>
    /// <returns>IUnRegister 对象，用于稍后注销该事件监听器。</returns>
    public IUnRegister Register(Action<T, TK, TS> onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => { UnRegister(onEvent); });
    }

    /// <summary>
    ///     取消指定的三参数事件监听器。
    /// </summary>
    /// <param name="onEvent">需要被注销的事件处理方法。</param>
    public void UnRegister(Action<T, TK, TS> onEvent)
    {
        _mOnEvent -= onEvent;
    }

    /// <summary>
    ///     触发所有已注册的事件处理程序，并传递参数 t、k 和 s。
    /// </summary>
    /// <param name="t">第一个参数。</param>
    /// <param name="k">第二个参数。</param>
    /// <param name="s">第三个参数。</param>
    public void Trigger(T t, TK k, TS s)
    {
        _mOnEvent?.Invoke(t, k, s);
    }
}