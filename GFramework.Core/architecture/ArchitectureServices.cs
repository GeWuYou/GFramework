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
    private IArchitectureContext _context = null!;

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    public IIocContainer Container { get; } = new IocContainer();

    /// <summary>
    ///     获取类型事件系统
    /// </summary>
    public IEventBus EventBus { get; } = new EventBus();

    /// <summary>
    ///     获取命令总线
    /// </summary>
    public ICommandBus CommandBus { get; } = new CommandBus();

    /// <summary>
    ///     获取查询总线
    /// </summary>
    public IQueryBus QueryBus { get; } = new QueryBus();

    /// <summary>
    ///     获取异步查询总线
    /// </summary>
    public IAsyncQueryBus AsyncQueryBus { get; } = new AsyncQueryBus();

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