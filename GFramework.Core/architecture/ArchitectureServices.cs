using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.query;
using GFramework.Core.command;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.query;
using IAsyncQueryBus = GFramework.Core.Abstractions.query.IAsyncQueryBus;

namespace GFramework.Core.architecture;

/// <summary>
///     架构服务类，提供依赖注入容器、事件系统、命令总线和查询总线等核心服务
/// </summary>
public class ArchitectureServices : IArchitectureServices
{
    /// <summary>
    ///     异步查询总线实例
    /// </summary>
    private readonly IAsyncQueryBus _asyncQueryBus;

    /// <summary>
    ///     命令总线实例
    /// </summary>
    private readonly ICommandBus _commandBus;

    private readonly IIocContainer _container;

    /// <summary>
    ///     事件总线实例
    /// </summary>
    private readonly IEventBus _eventBus;

    /// <summary>
    ///     查询总线实例
    /// </summary>
    private readonly IQueryBus _queryBus;

    private IArchitectureContext _context = null!;

    /// <summary>
    ///     构造函数，初始化架构服务
    /// </summary>
    public ArchitectureServices()
    {
        _container = new IocContainer();

        // 创建服务实例
        _eventBus = new EventBus();
        _commandBus = new CommandBus();
        _queryBus = new QueryBus();
        _asyncQueryBus = new AsyncQueryBus();

        // 将服务注册到容器
        _container.RegisterPlurality(_eventBus);
        _container.RegisterPlurality(_commandBus);
        _container.RegisterPlurality(_queryBus);
        _container.RegisterPlurality(_asyncQueryBus);
    }

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    public IIocContainer Container => _container;

    /// <summary>
    ///     获取类型事件系统
    /// </summary>
    public IEventBus EventBus => _eventBus;

    /// <summary>
    ///     获取命令总线
    /// </summary>
    public ICommandBus CommandBus => _commandBus;

    /// <summary>
    ///     获取查询总线
    /// </summary>
    public IQueryBus QueryBus => _queryBus;

    /// <summary>
    ///     获取异步查询总线
    /// </summary>
    public IAsyncQueryBus AsyncQueryBus => _asyncQueryBus;

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