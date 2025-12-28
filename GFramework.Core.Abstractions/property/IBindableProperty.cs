namespace GFramework.Core.Abstractions.property;

/// <summary>
///     可绑定属性接口，继承自只读可绑定属性接口，提供可读写的属性绑定功能
/// </summary>
/// <typeparam name="T">属性值的类型</typeparam>
public interface IBindableProperty<T> : IReadonlyBindableProperty<T>
{
    /// <summary>
    ///     获取或设置属性的值
    /// </summary>
    new T Value { get; set; }

    /// <summary>
    ///     设置属性值但不触发事件通知
    /// </summary>
    /// <param name="newValue">要设置的新值</param>
    void SetValueWithoutEvent(T newValue);
}