using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Ioc;
using GFramework.Core.Services;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构服务类，提供依赖注入容器、事件系统、命令总线和查询总线等核心服务。
///     该类负责管理架构运行所需的核心组件，并提供统一的服务访问接口。
/// </summary>
public class ArchitectureServices : IArchitectureServices
{
    private readonly IServiceModuleManager _moduleManager;
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     构造函数，初始化架构服务。
    ///     初始化依赖注入容器，并创建事件总线、命令执行器、查询执行器和异步查询执行器的实例，
    ///     然后将这些服务注册到容器中。
    /// </summary>
    public ArchitectureServices()
    {
        Container = new MicrosoftDiContainer();
        _moduleManager = new ServiceModuleManager();
    }

    /// <summary>
    ///     获取服务模块管理器实例。
    ///     服务模块管理器用于管理架构中的服务模块，支持模块的动态加载和卸载。
    /// </summary>
    public IServiceModuleManager ModuleManager => _moduleManager;

    /// <summary>
    ///     获取依赖注入容器。
    ///     该容器用于管理架构中所有服务的生命周期和依赖关系。
    /// </summary>
    public IIocContainer Container { get; }

    /// <summary>
    ///     获取事件总线实例。
    ///     事件总线用于在架构中发布和订阅事件，实现组件间的松耦合通信。
    /// </summary>
    public IEventBus EventBus => Container.Get<IEventBus>()!;

    /// <summary>
    ///     获取命令执行器实例。
    ///     命令执行器用于处理命令请求，执行业务逻辑。
    /// </summary>
    public ICommandExecutor CommandExecutor => Container.Get<ICommandExecutor>()!;

    /// <summary>
    ///     获取查询执行器实例。
    ///     查询执行器用于处理同步查询请求，获取数据或状态信息。
    /// </summary>
    public IQueryExecutor QueryExecutor => Container.Get<IQueryExecutor>()!;

    /// <summary>
    ///     获取异步查询执行器实例。
    ///     异步查询执行器用于处理异步查询请求，支持非阻塞的数据获取操作。
    /// </summary>
    public IAsyncQueryExecutor AsyncQueryExecutor => Container.Get<IAsyncQueryExecutor>()!;

    /// <summary>
    ///     设置架构上下文。
    /// </summary>
    /// <param name="context">要设置的架构上下文实例。</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
        Container.SetContext(context);
    }

    /// <summary>
    ///     获取当前架构上下文。
    /// </summary>
    /// <returns>当前的架构上下文实例。</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }
}