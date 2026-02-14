using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.query;
using GFramework.Core.command;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.query;

namespace GFramework.Core.architecture;

/// <summary>
///     架构服务类，提供依赖注入容器、事件系统、命令总线和查询总线等核心服务
/// </summary>
public class ArchitectureServices : IArchitectureServices
{
    /// <summary>
    ///     异步查询执行器实例
    /// </summary>
    private readonly IAsyncQueryExecutor _asyncQueryExecutor;

    /// <summary>
    ///     命令执行器实例
    /// </summary>
    private readonly ICommandExecutor _commandExecutor;

    /// <summary>
    ///     事件总线实例
    /// </summary>
    private readonly IEventBus _eventBus;

    /// <summary>
    ///     同步查询执行器实例
    /// </summary>
    private readonly IQueryExecutor _queryExecutor;

    /// <summary>
    ///     架构上下文对象
    /// </summary>
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     构造函数，初始化架构服务
    ///     初始化依赖注入容器，并创建事件总线、命令执行器、查询执行器和异步查询执行器的实例，
    ///     然后将这些服务注册到容器中。
    /// </summary>
    public ArchitectureServices()
    {
        Container = new MicrosoftDiContainer();

        // 创建服务实例
        _eventBus = new EventBus();
        _commandExecutor = new CommandExecutor();
        _queryExecutor = new QueryExecutor();
        _asyncQueryExecutor = new AsyncQueryExecutor();

        // 将服务注册到容器
        Container.RegisterPlurality(_eventBus);
        Container.RegisterPlurality(_commandExecutor);
        Container.RegisterPlurality(_queryExecutor);
        Container.RegisterPlurality(_asyncQueryExecutor);
    }

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    public IIocContainer Container { get; }

    /// <summary>
    ///     获取事件总线实例
    /// </summary>
    public IEventBus EventBus => _eventBus;

    /// <summary>
    ///     获取命令执行器实例
    /// </summary>
    public ICommandExecutor CommandExecutor => _commandExecutor;

    /// <summary>
    ///     获取同步查询执行器实例
    /// </summary>
    public IQueryExecutor QueryExecutor => _queryExecutor;

    /// <summary>
    ///     获取异步查询执行器实例
    /// </summary>
    public IAsyncQueryExecutor AsyncQueryExecutor => _asyncQueryExecutor;

    /// <summary>
    ///     设置架构上下文
    /// </summary>
    /// <param name="context">架构上下文对象</param>
    public void SetContext(IArchitectureContext context)
    {
        _context = context;
        Container.SetContext(context);
    }

    /// <summary>
    ///     获取架构上下文
    /// </summary>
    /// <returns>架构上下文对象</returns>
    public IArchitectureContext GetContext()
    {
        return _context;
    }
}