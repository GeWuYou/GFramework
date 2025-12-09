using GFramework.framework.events;

namespace GFramework.framework.property;


/// <summary>
/// 只读可绑定属性接口，提供属性值的读取和变更监听功能
/// </summary>
/// <typeparam name="T">属性值的类型</typeparam>
public interface IReadonlyBindableProperty<out T> : IEasyEvent
{
    /// <summary>
    /// 获取属性的当前值
    /// </summary>
    T Value { get; }

    /// <summary>
    /// 注册属性值变更回调，并立即执行一次初始值的回调
    /// </summary>
    /// <param name="action">属性值变更时执行的回调函数，参数为新的属性值</param>
    /// <returns>用于取消注册的句柄对象</returns>
    IUnRegister RegisterWithInitValue(Action<T> action);
    
    /// <summary>
    /// 取消注册属性值变更回调
    /// </summary>
    /// <param name="onValueChanged">要取消注册的回调函数</param>
    void UnRegister(Action<T> onValueChanged);
    
    /// <summary>
    /// 注册属性值变更回调
    /// </summary>
    /// <param name="onValueChanged">属性值变更时执行的回调函数，参数为新的属性值</param>
    /// <returns>用于取消注册的句柄对象</returns>
    IUnRegister Register(Action<T> onValueChanged);
}
