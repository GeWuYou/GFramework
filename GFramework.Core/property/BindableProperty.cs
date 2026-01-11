using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.property;

namespace GFramework.Core.property;

/// <summary>
///     可绑定属性类，用于实现数据绑定功能
/// </summary>
/// <typeparam name="T">属性值的类型</typeparam>
/// <param name="defaultValue">属性的默认值</param>
public class BindableProperty<T>(T defaultValue = default!) : IBindableProperty<T>
{
    private Action<T>? _mOnValueChanged;
    protected T MValue = defaultValue;

    /// <summary>
    ///     获取或设置属性值比较器，默认使用Equals方法进行比较
    /// </summary>
    public static Func<T, T, bool> Comparer { get; set; } = (a, b) => a!.Equals(b);

    /// <summary>
    ///     获取或设置属性值，当值发生变化时会触发注册的回调事件
    /// </summary>
    public T Value
    {
        get => GetValue();
        set
        {
            // 使用 default(T) 替代 null 比较，避免 SonarQube 警告
            if (EqualityComparer<T>.Default.Equals(value, default!) &&
                EqualityComparer<T>.Default.Equals(MValue, default!))
                return;

            // 若新值与旧值相等则不执行后续操作
            if (!EqualityComparer<T>.Default.Equals(value, default!) && Comparer(value, MValue))
                return;

            SetValue(value);
            _mOnValueChanged?.Invoke(value);
        }
    }

    /// <summary>
    ///     直接设置属性值而不触发事件
    /// </summary>
    /// <param name="newValue">新的属性值</param>
    public void SetValueWithoutEvent(T newValue)
    {
        MValue = newValue;
    }

    /// <summary>
    ///     实现IEasyEvent接口的注册方法，将无参事件转换为有参事件处理
    /// </summary>
    /// <param name="onEvent">无参事件回调</param>
    /// <returns>可用于取消注册的接口</returns>
    IUnRegister IEvent.Register(Action onEvent)
    {
        return Register(Action);

        void Action(T _)
        {
            onEvent();
        }
    }

    /// <summary>
    ///     注册属性值变化事件回调
    /// </summary>
    /// <param name="onValueChanged">属性值变化时的回调函数</param>
    /// <returns>可用于取消注册的接口</returns>
    public IUnRegister Register(Action<T> onValueChanged)
    {
        _mOnValueChanged += onValueChanged;
        return new BindablePropertyUnRegister<T>(this, onValueChanged);
    }

    /// <summary>
    ///     注册属性值变化事件回调，并立即调用回调函数传递当前值
    /// </summary>
    /// <param name="action">属性值变化时的回调函数</param>
    /// <returns>可用于取消注册的接口</returns>
    public IUnRegister RegisterWithInitValue(Action<T> action)
    {
        action(MValue);
        return Register(action);
    }

    /// <summary>
    ///     取消注册属性值变化事件回调
    /// </summary>
    /// <param name="onValueChanged">要取消注册的回调函数</param>
    public void UnRegister(Action<T> onValueChanged)
    {
        _mOnValueChanged -= onValueChanged;
    }

    /// <summary>
    ///     设置自定义比较器
    /// </summary>
    /// <param name="comparer">用于比较两个值是否相等的函数</param>
    /// <returns>当前可绑定属性实例</returns>
    public BindableProperty<T> WithComparer(Func<T, T, bool> comparer)
    {
        Comparer = comparer;
        return this;
    }

    /// <summary>
    ///     设置属性值的虚方法，可在子类中重写
    /// </summary>
    /// <param name="newValue">新的属性值</param>
    protected virtual void SetValue(T newValue)
    {
        MValue = newValue;
    }

    /// <summary>
    ///     获取属性值的虚方法，可在子类中重写
    /// </summary>
    /// <returns>当前属性值</returns>
    protected virtual T GetValue()
    {
        return MValue;
    }

    /// <summary>
    ///     返回属性值的字符串表示形式
    /// </summary>
    /// <returns>属性值的字符串表示</returns>
    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }
}