using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Abstractions.Architecture;

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
    ///     获取命令执行器
    /// </summary>
    /// <returns>ICommandExecutor类型的命令执行器实例</returns>
    ICommandExecutor CommandExecutor { get; }

    /// <summary>
    ///     获取查询执行器
    /// </summary>
    /// <returns>IQueryExecutor类型的查询执行器实例</returns>
    IQueryExecutor QueryExecutor { get; }

    /// <summary>
    ///     获取异步查询执行器
    /// </summary>
    /// <returns>IAsyncQueryExecutor类型的异步查询执行器实例</returns>
    IAsyncQueryExecutor AsyncQueryExecutor { get; }

    /// <summary>
    ///     获取服务模块管理器
    /// </summary>
    /// <returns>IServiceModuleManager类型的服务模块管理器实例</returns>
    IServiceModuleManager ModuleManager { get; }
}