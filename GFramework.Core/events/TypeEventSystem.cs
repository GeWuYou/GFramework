namespace GFramework.Core.events;

/// <summary>
///     类型事件系统，提供基于类型的事件发送、注册和注销功能
/// </summary>
public class TypeEventSystem : ITypeEventSystem
{
    private readonly EasyEvents _mEvents = new();

    /// <summary>
    ///     发送事件，自动创建事件实例
    /// </summary>
    /// <typeparam name="T">事件类型，必须具有无参构造函数</typeparam>
    public void Send<T>() where T : new()
    {
        _mEvents.GetEvent<EasyEvent<T>>().Trigger(new T());
    }

    /// <summary>
    ///     发送指定的事件实例
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    public void Send<T>(T e)
    {
        _mEvents.GetEvent<EasyEvent<T>>().Trigger(e);
    }

    /// <summary>
    ///     注册事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">事件处理回调函数</param>
    /// <returns>反注册接口，用于注销事件监听</returns>
    public IUnRegister Register<T>(Action<T> onEvent)
    {
        return _mEvents.GetOrAddEvent<EasyEvent<T>>().Register(onEvent);
    }

    /// <summary>
    ///     注销事件监听器
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="onEvent">要注销的事件处理回调函数</param>
    public void UnRegister<T>(Action<T> onEvent)
    {
        _mEvents.GetEvent<EasyEvent<T>>().UnRegister(onEvent);
    }
}