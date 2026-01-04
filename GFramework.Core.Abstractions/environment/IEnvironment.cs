namespace GFramework.Core.Abstractions.environment;

/// <summary>
///     定义环境接口，提供应用程序运行环境的相关信息
/// </summary>
public interface IEnvironment
{
    /// <summary>
    ///     获取环境名称
    /// </summary>
    public string Name { get; }


    /// <summary>
    ///     根据键值获取指定类型的环境配置值
    /// </summary>
    /// <typeparam name="T">要获取的值的类型，必须为引用类型</typeparam>
    /// <param name="key">用于查找配置值的键</param>
    /// <returns>与指定键关联的配置值，如果未找到则返回null</returns>
    T? Get<T>(string key) where T : class;

    /// <summary>
    ///     尝试获取环境值（显式判断）
    /// </summary>
    /// <typeparam name="T">要获取的值的类型，必须为引用类型</typeparam>
    /// <param name="key">用于查找配置值的键</param>
    /// <param name="value">输出参数，如果找到配置值则返回该值，否则返回默认值</param>
    /// <returns>如果找到指定键的配置值则返回true，否则返回false</returns>
    bool TryGet<T>(string key, out T value) where T : class;

    /// <summary>
    ///     获取必须存在的环境值（强依赖）
    /// </summary>
    /// <typeparam name="T">要获取的值的类型，必须为引用类型</typeparam>
    /// <param name="key">用于查找配置值的键</param>
    /// <returns>与指定键关联的配置值，如果未找到则抛出异常</returns>
    T GetRequired<T>(string key) where T : class;

    /// <summary>
    ///     注册键值对到环境值字典中
    /// </summary>
    /// <param name="key">要注册的键</param>
    /// <param name="value">要注册的值</param>
    void Register(string key, object value);

    /// <summary>
    ///     初始化环境值字典
    /// </summary>
    void Initialize();
}