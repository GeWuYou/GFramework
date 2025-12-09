using GWFramework.framework.extensions;

namespace GWFramework.framework.events;

/// <summary>
/// OrEvent类用于实现事件的或逻辑组合，当任意一个注册的事件触发时，都会触发OrEvent本身
/// </summary>
public class OrEvent : IUnRegisterList
{
    /// <summary>
    /// 将指定的事件与当前OrEvent进行或逻辑组合
    /// </summary>
    /// <param name="easyEvent">要组合的事件对象</param>
    /// <returns>返回当前OrEvent实例，支持链式调用</returns>
    public OrEvent Or(IEasyEvent easyEvent)
    {
        easyEvent.Register(Trigger).AddToUnregisterList(this);
        return this;
    }

    private Action _mOnEvent = () => { };

    /// <summary>
    /// 注册事件处理函数
    /// </summary>
    /// <param name="onEvent">要注册的事件处理函数</param>
    /// <returns>返回一个可取消注册的对象</returns>
    public IUnRegister Register(Action onEvent)
    {
        _mOnEvent += onEvent;
        return new DefaultUnRegister(() => { UnRegister(onEvent); });
    }

    /// <summary>
    /// 取消注册指定的事件处理函数
    /// </summary>
    /// <param name="onEvent">要取消注册的事件处理函数</param>
    public void UnRegister(Action onEvent)
    {
        _mOnEvent -= onEvent;
        this.UnRegisterAll();
    }

    /// <summary>
    /// 触发所有已注册的事件处理函数
    /// </summary>
    private void Trigger() => _mOnEvent?.Invoke();

    /// <summary>
    /// 获取取消注册列表
    /// </summary>
    public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();
}
