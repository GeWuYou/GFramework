using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Abstractions.architecture;

/// <summary>
///     架构服务接口，定义了框架核心架构所需的服务组件
/// </summary>
public interface IArchitectureServices : IContextAware
{
    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    /// <returns>IIocContainer类型的依赖注入容器实例</returns>
    IIocContainer Container { get; }

    /// <summary>
    ///     获取类型事件系统
    /// </summary>
    /// <returns>ITypeEventSystem类型的事件系统实例</returns>
    IEventBus EventBus { get; }

    /// <summary>
    ///     获取命令总线
    /// </summary>
    public ICommandBus CommandBus { get; }

    /// <summary>
    ///     获取查询总线
    /// </summary>
    public IQueryBus QueryBus { get; }

    /// <summary>
    ///     获取异步查询总线
    /// </summary>
    public IAsyncQueryBus AsyncQueryBus { get; }
}