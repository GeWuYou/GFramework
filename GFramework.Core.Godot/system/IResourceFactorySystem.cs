using GFramework.Core.system;

namespace GFramework.Core.Godot.system;

/// <summary>
/// 资源工厂系统接口，用于获取指定类型的资源创建函数
/// </summary>
public interface IResourceFactorySystem : ISystem
{
    /// <summary>
    /// 获取指定类型T的资源创建函数
    /// </summary>
    /// <typeparam name="T">要获取创建函数的资源类型</typeparam>
    /// <returns>返回一个创建T类型实例的函数委托</returns>
    Func<T> Get<T>();
}
