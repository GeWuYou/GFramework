using GFramework.Core.Abstractions.environment;
using GFramework.Core.rule;

namespace GFramework.Core.environment;

/// <summary>
///     环境基础抽象类，实现了IEnvironment接口，提供环境值的存储和获取功能
/// </summary>
public abstract class EnvironmentBase : ContextAwareBase, IEnvironment
{
    /// <summary>
    ///     存储环境值的字典，键为字符串，值为对象类型
    /// </summary>
    protected readonly Dictionary<string, object> Values = new();

    /// <summary>
    ///     获取环境名称的抽象属性
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    ///     根据键获取指定类型的值
    /// </summary>
    /// <typeparam name="T">要获取的值的类型，必须为引用类型</typeparam>
    /// <param name="key">用于查找值的键</param>
    /// <returns>如果找到则返回对应类型的值，否则返回null</returns>
    public virtual T? Get<T>(string key) where T : class
    {
        return TryGet<T>(key, out var value) ? value : null;
    }

    /// <summary>
    ///     尝试根据键获取指定类型的值
    /// </summary>
    /// <typeparam name="T">要获取的值的类型，必须为引用类型</typeparam>
    /// <param name="key">用于查找值的键</param>
    /// <param name="value">输出参数，如果成功则包含找到的值，否则为null</param>
    /// <returns>如果找到指定键且类型匹配则返回true，否则返回false</returns>
    public virtual bool TryGet<T>(string key, out T value) where T : class
    {
        if (Values.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }

    /// <summary>
    ///     根据键获取必需的指定类型值，如果找不到则抛出异常
    /// </summary>
    /// <typeparam name="T">要获取的值的类型，必须为引用类型</typeparam>
    /// <param name="key">用于查找值的键</param>
    /// <returns>找到的对应类型的值</returns>
    /// <exception cref="InvalidOperationException">当指定键的值不存在时抛出</exception>
    public virtual T GetRequired<T>(string key) where T : class
    {
        if (TryGet<T>(key, out var value))
            return value;

        throw new InvalidOperationException(
            $"Environment [{Name}] missing required value: key='{key}', type='{typeof(T).Name}'");
    }

    void IEnvironment.Register(string key, object value)
    {
        Register(key, value);
    }

    public abstract void Initialize();

    /// <summary>
    ///     注册键值对到环境值字典中
    /// </summary>
    /// <param name="key">要注册的键</param>
    /// <param name="value">要注册的值</param>
    protected void Register(string key, object value)
    {
        Values[key] = value;
    }
}