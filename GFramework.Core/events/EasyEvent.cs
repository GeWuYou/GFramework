namespace GFramework.Core.events;

/// <summary>
///     简单事件类，用于注册、注销和触发无参事件回调
/// </summary>
public class EasyEvent
{
    private Action _mOnEvent = () => { };

    /// <summary>
    ///     注册事件回调函数
    /// </summary>
    /// <param name="onEvent">要注册的事件回调函数</param>
    /// <returns>用于注销事件的 unregister 对象</returns>
    public IUnRegister Register(Action onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => { UnRegister(onEvent); });
    }

    /// <summary>
    ///     注销已注册的事件回调函数
    /// </summary>
    /// <param name="onEvent">要注销的事件回调函数</param>
    public void UnRegister(Action onEvent)
    {
        _mOnEvent -= onEvent;
    }

    /// <summary>
    ///     触发所有已注册的事件回调函数
    /// </summary>
    public void Trigger()
    {
        _mOnEvent?.Invoke();
    }
}