using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using Mediator;
using ICommand = GFramework.Core.Abstractions.command.ICommand;

namespace GFramework.Core.architecture;

/// <summary>
///     架构上下文类，提供对系统、模型、工具等组件的访问以及命令、查询、事件的执行管理
/// </summary>
public class ArchitectureContext(IIocContainer container) : IArchitectureContext
{
    private readonly IIocContainer _container = container ?? throw new ArgumentNullException(nameof(container));
    private readonly Dictionary<Type, object> _serviceCache = new();

    #region Mediator Integration (新实现)

    /// <summary>
    /// 获取 Mediator 实例（延迟加载）
    /// </summary>
    private IMediator? Mediator => GetOrCache<IMediator>();

    /// <summary>
    /// 获取 ISender 实例（更轻量的发送器）
    /// </summary>
    private ISender? Sender => GetOrCache<ISender>();

    /// <summary>
    /// 获取 IPublisher 实例（用于发布通知）
    /// </summary>
    private IPublisher? Publisher => GetOrCache<IPublisher>();

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

    /// <summary>
    /// [Mediator] 发送请求（Command/Query）
    /// 这是推荐的新方式，统一处理命令和查询
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="request">请求对象（Command 或 Query）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应结果</returns>
    /// <exception cref="InvalidOperationException">当 Mediator 未注册时抛出</exception>
    public async ValueTask<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mediator = Mediator;
        if (mediator == null)
            throw new InvalidOperationException(
                "Mediator not registered. Call EnableMediator() in your Architecture.Init() method.");

        return await mediator.Send(request, cancellationToken);
    }

    /// <summary>
    /// [Mediator] 发送请求的同步版本（不推荐，仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="request">请求对象</param>
    /// <returns>响应结果</returns>
    public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
    {
        return SendRequestAsync(request).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// [Mediator] 发布通知（一对多）
    /// 用于事件驱动场景，多个处理器可以同时处理同一个通知
    /// </summary>
    /// <typeparam name="TNotification">通知类型</typeparam>
    /// <param name="notification">通知对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var publisher = Publisher;
        if (publisher == null)
            throw new InvalidOperationException("Publisher not registered.");

        await publisher.Publish(notification, cancellationToken);
    }

    /// <summary>
    /// [Mediator] 发送请求并返回流（用于大数据集）
    /// </summary>
    /// <typeparam name="TResponse">响应项类型</typeparam>
    /// <param name="request">流式请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步流</returns>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mediator = Mediator;
        if (mediator == null)
            throw new InvalidOperationException("Mediator not registered.");

        return mediator.CreateStream(request, cancellationToken);
    }

    /// <summary>
    /// [扩展] 发送命令（无返回值）
    /// 语法糖，等同于 SendRequestAsync&lt;Unit&gt;
    /// </summary>
    public async ValueTask SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        await SendRequestAsync(command, cancellationToken);
    }

    /// <summary>
    /// [扩展] 发送命令（有返回值）
    /// 语法糖，等同于 SendRequestAsync&lt;TResponse&gt;
    /// </summary>
    public async ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(command, cancellationToken);
    }

    #endregion

    #region Query Execution

    /// <summary>
    ///     发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    public TResult SendQuery<TResult>(Abstractions.query.IQuery<TResult> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        var queryBus = GetOrCache<IQueryExecutor>();
        if (queryBus == null) throw new InvalidOperationException("IQueryExecutor not registered");
        return queryBus.Send(query);
    }

    /// <summary>
    /// [Mediator] 发送查询的同步版本（不推荐，仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="query">要发送的查询对象</param>
    /// <returns>查询结果</returns>
    public TResponse SendQuery<TResponse>(Mediator.IQuery<TResponse> query)
    {
        return SendQueryAsync(query).AsTask().GetAwaiter().GetResult();
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
        var asyncQueryBus = GetOrCache<IAsyncQueryExecutor>();
        if (asyncQueryBus == null) throw new InvalidOperationException("IAsyncQueryExecutor not registered");
        return await asyncQueryBus.SendAsync(query);
    }

    /// <summary>
    /// [Mediator] 异步发送查询并返回结果
    /// 通过Mediator模式发送查询请求，支持取消操作
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="query">要发送的查询对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>包含查询结果的ValueTask</returns>
    public async ValueTask<TResponse> SendQueryAsync<TResponse>(Mediator.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sender = Sender;
        if (sender == null)
            throw new InvalidOperationException("Sender not registered.");

        return await sender.Send(query, cancellationToken);
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
    /// [Mediator] 异步发送命令并返回结果
    /// 通过Mediator模式发送命令请求，支持取消操作
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>包含命令执行结果的ValueTask</returns>
    public async ValueTask<TResponse> SendCommandAsync<TResponse>(Mediator.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sender = Sender;
        if (sender == null)
            throw new InvalidOperationException("Sender not registered.");

        return await sender.Send(command, cancellationToken);
    }

    /// <summary>
    ///     发送并异步执行一个命令请求
    /// </summary>
    /// <param name="command">要发送的命令</param>
    public async Task SendCommandAsync(IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandExecutor>();
        if (commandBus == null) throw new InvalidOperationException("ICommandExecutor not registered");
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
        var commandBus = GetOrCache<ICommandExecutor>();
        if (commandBus == null) throw new InvalidOperationException("ICommandExecutor not registered");
        return await commandBus.SendAsync(command);
    }

    /// <summary>
    /// [Mediator] 发送命令的同步版本（不推荐，仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行结果</returns>
    public TResponse SendCommand<TResponse>(Mediator.ICommand<TResponse> command)
    {
        return SendCommandAsync(command).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    ///     发送一个命令请求
    /// </summary>
    /// <param name="command">要发送的命令</param>
    public void SendCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandExecutor>();
        commandBus?.Send(command);
    }

    /// <summary>
    ///     发送一个带返回值的命令请求
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    public TResult SendCommand<TResult>(Abstractions.command.ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandBus = GetOrCache<ICommandExecutor>();
        if (commandBus == null) throw new InvalidOperationException("ICommandExecutor not registered");
        return commandBus.Send(command);
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