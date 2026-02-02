using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;

namespace GFramework.Core.architecture;

/// <summary>
///     架构上下文类，提供对系统、模型、工具等组件的访问以及命令、查询、事件的执行管理
/// </summary>
public class ArchitectureContext(IIocContainer container) : IArchitectureContext
{
    private readonly IIocContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly Dictionary<Type, object> _serviceCache = new();

    /// <summary>
    /// 获取指定类型的服务实例，如果缓存中存在则直接返回，否则从容器中获取并缓存
    /// </summary>
    /// <typeparam name="TService">服务类型，必须为引用类型</typeparam>
    /// <returns>服务实例，如果未找到则返回null</returns>
    public TService? GetService<TService>() where TService : class
    {
        return GetOrCache<TService>();
    }

    /// <summary>
    /// 从缓存中获取或创建指定类型的服务实例
    /// 首先尝试从缓存中获取服务实例，如果缓存中不存在则从容器中获取并存入缓存
    /// </summary>
    /// <typeparam name="TService">服务类型，必须为引用类型</typeparam>
    /// <returns>服务实例，如果未找到则返回null</returns>
    private TService? GetOrCache<TService>() where TService : class
    {
        // 尝试从服务缓存中获取已存在的服务实例
        if (_serviceCache.TryGetValue(typeof(TService), out var cached))
            return (TService)cached;

        // 从依赖注入容器中获取服务实例
        var service = _container.Get<TService>();
        // 如果服务实例存在，则将其存入缓存以供后续使用
        if (service != null)
            _serviceCache[typeof(TService)] = service;

        return service;
    }


    #region Query Execution

    /// <summary>
    ///     发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    public TResult SendQuery<TResult>(IQuery<TResult> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        var queryBus = GetOrCache<IQueryBus>();
        if (queryBus == null) throw new InvalidOperationException("IQueryBus not registered");
        return queryBus.Send(query);
    }

    /// <summary>
    ///     异步发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要发送的异步查询</param>
    /// <returns>查询结果</returns>
    public async Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        var asyncQueryBus = GetOrCache<IAsyncQueryBus>();
        if (asyncQueryBus == null) throw new InvalidOperationException("IAsyncQueryBus not registered");
        return await asyncQueryBus.SendAsync(query);
    }

    #endregion

    #region Component Retrieval

    /// <summary>
    ///     从IOC容器中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <returns>对应的系统实例</returns>
    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return GetService<TSystem>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <returns>对应的模型实例</returns>
    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return GetService<TModel>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <returns>对应的工具实例</returns>
    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return GetService<TUtility>();
    }

    #endregion

    #region Command Execution

    /// <summary>
    ///     发送一个命令请求
    /// </summary>
    /// <param name="command">要发送的命令</param>
    public void SendCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandBus>();
        commandBus?.Send(command);
    }

    /// <summary>
    ///     发送一个带返回值的命令请求
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    public TResult SendCommand<TResult>(ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandBus>();
        if (commandBus == null) throw new InvalidOperationException("ICommandBus not registered");
        return commandBus.Send(command);
    }

    /// <summary>
    ///     发送并异步执行一个命令请求
    /// </summary>
    /// <param name="command">要发送的命令</param>
    public async Task SendCommandAsync(IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandBus>();
        if (commandBus == null) throw new InvalidOperationException("ICommandBus not registered");
        await commandBus.SendAsync(command);
    }

    /// <summary>
    ///     发送并异步执行一个带返回值的命令请求
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    public async Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandBus>();
        if (commandBus == null) throw new InvalidOperationException("ICommandBus not registered");
        return await commandBus.SendAsync(command);
    }

    #endregion

    #region Event Management

    /// <summary>
    ///     发送一个默认构造的新事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
        var eventBus = GetOrCache<IEventBus>();
        eventBus?.Send<TEvent>();
    }

    /// <summary>
    ///     发送一个具体的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">事件参数</param>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(e);
        var eventBus = GetOrCache<IEventBus>();
        eventBus?.Send(e);
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var eventBus = GetOrCache<IEventBus>();
        if (eventBus == null) throw new InvalidOperationException("IEventBus not registered");
        return eventBus.Register(handler);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
        ArgumentNullException.ThrowIfNull(onEvent);
        var eventBus = GetOrCache<IEventBus>();
        eventBus?.UnRegister(onEvent);
    }

    /// <summary>
    ///     获取当前环境对象
    /// </summary>
    /// <returns>环境对象实例</returns>
    public IEnvironment GetEnvironment()
    {
        var environment = GetOrCache<IEnvironment>();
        return environment ?? throw new InvalidOperationException("IEnvironment not registered");
    }

    #endregion
}