// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Core.Architectures;

/// <summary>
///     架构上下文类，提供对系统、模型、工具等组件的访问以及命令、查询、事件的执行管理
/// </summary>
public class ArchitectureContext : IArchitectureContext
{
    private readonly IIocContainer _container;
    private readonly Lazy<ICqrsRuntime> _cqrsRuntime;
    private readonly ConcurrentDictionary<Type, object> _serviceCache = new();

    /// <summary>
    ///     初始化新的架构上下文，并绑定其依赖容器。
    /// </summary>
    /// <param name="container">
    ///     当前架构使用的 IOC 容器。
    ///     CQRS runtime 与其他框架服务会通过该容器延迟解析，以避免在上下文构造阶段强制拉起整条运行时链路。
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="container" /> 为 <see langword="null" />。</exception>
    public ArchitectureContext(IIocContainer container)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _cqrsRuntime = new Lazy<ICqrsRuntime>(
            ResolveCqrsRuntime,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    #region CQRS Integration

    /// <summary>
    ///     获取 CQRS runtime seam。
    /// </summary>
    /// <remarks>
    ///     该实例会在首次访问时从容器解析，并通过 <see cref="Lazy{T}" /> 保证并发场景下只执行一次初始化，
    ///     避免多个请求线程重复触发同一个 runtime 的容器解析。
    /// </remarks>
    private ICqrsRuntime CqrsRuntime => _cqrsRuntime.Value;

    /// <summary>
    ///     从容器解析当前架构上下文依赖的 CQRS runtime。
    /// </summary>
    /// <returns>已注册的 CQRS runtime 实例。</returns>
    /// <exception cref="InvalidOperationException">容器中未注册 <see cref="ICqrsRuntime" />。</exception>
    private ICqrsRuntime ResolveCqrsRuntime()
    {
        return _container.Get<ICqrsRuntime>() ?? throw new InvalidOperationException("ICqrsRuntime not registered");
    }

    /// <summary>
    /// 获取指定类型的服务实例，如果缓存中存在则直接返回，否则从容器中获取并缓存
    /// </summary>
    /// <typeparam name="TService">服务类型，必须为引用类型</typeparam>
    /// <returns>服务实例，如果不存在则抛出异常</returns>
    public TService GetService<TService>() where TService : class
    {
        return GetOrCache<TService>();
    }

    /// <summary>
    /// 从缓存中获取或创建指定类型的服务实例
    /// 首先尝试从缓存中获取服务实例，如果缓存中不存在则从容器中获取并存入缓存
    /// </summary>
    /// <typeparam name="TService">服务类型，必须为引用类型</typeparam>
    /// <returns>服务实例，如果不存在则抛出异常</returns>
    private TService GetOrCache<TService>() where TService : class
    {
        return (TService)_serviceCache.GetOrAdd(
            typeof(TService),
            _ => _container.Get<TService>()
                 ?? throw new InvalidOperationException(
                     $"Service {typeof(TService)} not registered"));
    }

    /// <summary>
    /// 发送请求（Command/Query）
    /// 使用 GFramework 自有 CQRS runtime 统一处理命令和查询。
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="request">请求对象（Command 或 Query）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应结果</returns>
    public async ValueTask<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await CqrsRuntime.SendAsync(this, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 发送请求的同步版本（不推荐，仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="request">请求对象</param>
    /// <returns>响应结果</returns>
    public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
    {
        return SendRequestAsync(request).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 发布通知（一对多）
    /// 使用 GFramework 自有 CQRS runtime 分发到所有已注册通知处理器。
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
        await CqrsRuntime.PublishAsync(this, notification, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 发送请求并返回流（用于大数据集）
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
        return CqrsRuntime.CreateStream(this, request, cancellationToken);
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
        await SendRequestAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// [扩展] 发送命令（有返回值）
    /// 语法糖，等同于 SendRequestAsync&lt;TResponse&gt;
    /// </summary>
    public async ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(command, cancellationToken).ConfigureAwait(false);
    }

    #endregion

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
        var queryBus = GetOrCache<IQueryExecutor>();
        if (queryBus == null) throw new InvalidOperationException("IQueryExecutor not registered");
        return queryBus.Send(query);
    }

    /// <summary>
    /// 发送 CQRS 查询的同步版本（不推荐，仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="query">要发送的查询对象</param>
    /// <returns>查询结果</returns>
    public TResponse SendQuery<TResponse>(Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query)
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
        return await asyncQueryBus.SendAsync(query).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步发送 CQRS 查询并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="query">要发送的查询对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>包含查询结果的ValueTask</returns>
    public async ValueTask<TResponse> SendQueryAsync<TResponse>(Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await SendRequestAsync(query, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Component Retrieval

    /// <summary>
    ///     获取指定类型的所有服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>所有符合条件的服务实例列表</returns>
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _container.GetAll<TService>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <returns>对应的系统实例</returns>
    public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return GetService<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <returns>所有符合条件的系统实例列表</returns>
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAll<TSystem>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <returns>对应的模型实例</returns>
    public TModel GetModel<TModel>() where TModel : class, IModel
    {
        return GetService<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <returns>所有符合条件的模型实例列表</returns>
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _container.GetAll<TModel>();
    }

    /// <summary>
    ///     从IOC容器中获取指定类型的工具实例
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <returns>对应的工具实例</returns>
    public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return GetService<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有工具实例
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <returns>所有符合条件的工具实例列表</returns>
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAll<TUtility>();
    }

    /// <summary>
    /// 获取指定类型的所有服务实例，并按优先级排序
    /// 实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>按优先级排序后的服务实例列表</returns>
    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _container.GetAllByPriority<TService>();
    }

    /// <summary>
    /// 获取指定类型的所有系统实例，并按优先级排序
    /// 实现 IPrioritized 接口的系统将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <returns>按优先级排序后的系统实例列表</returns>
    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAllByPriority<TSystem>();
    }

    /// <summary>
    /// 获取指定类型的所有模型实例，并按优先级排序
    /// 实现 IPrioritized 接口的模型将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <returns>按优先级排序后的模型实例列表</returns>
    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _container.GetAllByPriority<TModel>();
    }

    /// <summary>
    /// 获取指定类型的所有工具实例，并按优先级排序
    /// 实现 IPrioritized 接口的工具将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <returns>按优先级排序后的工具实例列表</returns>
    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAllByPriority<TUtility>();
    }

    #endregion

    #region Command Execution

    /// <summary>
    /// 异步发送 CQRS 命令并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>包含命令执行结果的ValueTask</returns>
    public async ValueTask<TResponse> SendCommandAsync<TResponse>(
        Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return await SendRequestAsync(command, cancellationToken).ConfigureAwait(false);
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
        await commandBus.SendAsync(command).ConfigureAwait(false);
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
        return await commandBus.SendAsync(command).ConfigureAwait(false);
    }

    /// <summary>
    /// 发送 CQRS 命令的同步版本（不推荐，仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行结果</returns>
    public TResponse SendCommand<TResponse>(Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command)
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
        commandBus.Send(command);
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
        eventBus.Send<TEvent>();
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
        eventBus.Send(e);
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
        eventBus.UnRegister(onEvent);
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
