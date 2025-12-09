namespace GFramework.framework.events;

/// <summary>
/// 事件接口，定义了事件注册的基本功能
/// </summary>
public interface IEasyEvent
{
    /// <summary>
    /// 注册事件处理函数
    /// </summary>
    /// <param name="onEvent">事件触发时要执行的回调函数</param>
    /// <returns>用于取消注册的句柄对象</returns>
    IUnRegister Register(Action onEvent);
}