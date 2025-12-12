using GFramework.Core.events;

namespace GFramework.Core.property;

/// <summary>
///     可绑定属性注销器类，用于取消注册可绑定属性的值变化监听
/// </summary>
/// <typeparam name="T">可绑定属性的值类型</typeparam>
/// <param name="bindableProperty">需要注销的可绑定属性实例</param>
/// <param name="onValueChanged">需要注销的值变化回调函数</param>
public class BindablePropertyUnRegister<T>(BindableProperty<T> bindableProperty, Action<T> onValueChanged)
    : IUnRegister
{
    /// <summary>
    ///     获取或设置可绑定属性实例
    /// </summary>
    public BindableProperty<T> BindableProperty { get; set; } = bindableProperty;

    /// <summary>
    ///     获取或设置值变化时的回调函数
    /// </summary>
    public Action<T> OnValueChanged { get; set; } = onValueChanged;

    /// <summary>
    ///     执行注销操作，取消注册值变化监听并清理引用
    /// </summary>
    public void UnRegister()
    {
        // 调用可绑定属性的注销方法，传入值变化回调函数
        BindableProperty.UnRegister(OnValueChanged);
        // 清理属性引用
        BindableProperty = null;
        // 清理回调函数引用
        OnValueChanged = null;
    }
}