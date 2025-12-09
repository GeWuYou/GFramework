using System;
using System.Collections.Generic;

namespace GFramework.framework.ioc;

/// <summary>
/// IOC容器类，用于管理对象的注册和获取
/// </summary>
public class IocContainer
{
    private readonly Dictionary<Type, object> _mInstances = new();

    /// <summary>
    /// 注册一个实例到IOC容器中
    /// </summary>
    /// <typeparam name="T">实例的类型</typeparam>
    /// <param name="instance">要注册的实例对象</param>
    public void Register<T>(T instance)
    {
        var key = typeof(T);

        _mInstances[key] = instance;
    }

    /// <summary>
    /// 从IOC容器中获取指定类型的实例
    /// </summary>
    /// <typeparam name="T">要获取的实例类型</typeparam>
    /// <returns>返回指定类型的实例，如果未找到则返回null</returns>
    public T Get<T>() where T : class
    {
        var key = typeof(T);

        // 尝试从字典中获取实例
        if (_mInstances.TryGetValue(key, out var retInstance))
        {
            return retInstance as T;
        }

        return null;
    }
}
