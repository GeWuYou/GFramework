using GFramework.Core.events;
using GFramework.Core.ioc;

namespace GFramework.Core.architecture;

/// <summary>
/// 架构服务接口，定义了框架核心架构所需的服务组件
/// </summary>
public interface IArchitectureServices
{
    /// <summary>
    /// 获取依赖注入容器
    /// </summary>
    /// <returns>IIocContainer类型的依赖注入容器实例</returns>
    IIocContainer Container { get; }
    
    /// <summary>
    /// 获取类型事件系统
    /// </summary>
    /// <returns>ITypeEventSystem类型的事件系统实例</returns>
    ITypeEventSystem TypeEventSystem { get; }
}
