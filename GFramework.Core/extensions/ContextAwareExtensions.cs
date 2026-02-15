using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using Mediator;

namespace GFramework.Core.extensions;

/// <summary>
///     提供对 IContextAware 接口的扩展方法
/// </summary>
public static class ContextAwareExtensions
{
    /// <summary>
    ///     从上下文感知对象中获取指定类型的服务
    /// </summary>
    /// <typeparam name="TService">要获取的服务类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的上下文感知对象</param>
    /// <returns>指定类型的服务实例,如果未找到则返回 null</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 参数为 null 时抛出</exception>
    public static TService? GetService<TService>(this IContextAware contextAware) where TService : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetService<TService>();
    }

    /// <summary>
    ///     获取架构上下文中的指定系统
    /// </summary>
    /// <typeparam name="TSystem">目标系统类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的系统实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TSystem? GetSystem<TSystem>(this IContextAware contextAware) where TSystem : class, ISystem
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetSystem<TSystem>();
    }


    /// <summary>
    ///     获取架构上下文中的指定模型
    /// </summary>
    /// <typeparam name="TModel">目标模型类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的模型实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TModel? GetModel<TModel>(this IContextAware contextAware) where TModel : class, IModel
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetModel<TModel>();
    }

    /// <summary>
    ///     获取架构上下文中的指定工具
    /// </summary>
    /// <typeparam name="TUtility">目标工具类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <returns>指定类型的工具实例</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static TUtility? GetUtility<TUtility>(this IContextAware contextAware) where TUtility : class, IUtility
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        return context.GetUtility<TUtility>();
    }


    /// <summary>
    ///     发送一个事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <exception cref="ArgumentNullException">当 contextAware 为 null 时抛出</exception>
    public static void SendEvent<TEvent>(this IContextAware contextAware) where TEvent : new()
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        var context = contextAware.GetContext();
        context.SendEvent<TEvent>();
    }

    /// <summary>
    ///     发送一个具体的事件实例
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="e">事件实例</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 e 为 null 时抛出</exception>
    public static void SendEvent<TEvent>(this IContextAware contextAware, TEvent e) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(e);

        var context = contextAware.GetContext();
        context.SendEvent(e);
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    public static IUnRegister RegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(handler);

        var context = contextAware.GetContext();
        return context.RegisterEvent(handler);
    }

    /// <summary>
    ///     取消对某类型事件的监听
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="onEvent">之前绑定的事件处理器</param>
    public static void UnRegisterEvent<TEvent>(this IContextAware contextAware, Action<TEvent> onEvent)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(onEvent);

        // 获取上下文对象并取消事件注册
        var context = contextAware.GetContext();
        context.UnRegisterEvent(onEvent);
    }


    /// <summary>
    ///     获取指定类型的环境对象
    /// </summary>
    /// <typeparam name="T">要获取的环境对象类型</typeparam>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>指定类型的环境对象,如果无法转换则返回null</returns>
    public static T? GetEnvironment<T>(this IContextAware contextAware) where T : class
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        // 获取上下文对象并返回其环境
        var context = contextAware.GetContext();
        return context.GetEnvironment() as T;
    }

    /// <summary>
    ///     获取环境对象
    /// </summary>
    /// <param name="contextAware">上下文感知对象</param>
    /// <returns>环境对象</returns>
    public static IEnvironment GetEnvironment(this IContextAware contextAware)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        // 获取上下文对象并返回其环境
        var context = contextAware.GetContext();
        return context.GetEnvironment();
    }


    /// <summary>
    ///     [Mediator] 发送命令的同步版本（不推荐,仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令对象</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResponse SendCommand<TResponse>(this IContextAware contextAware,
        Mediator.ICommand<TResponse> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    ///     发送一个带返回结果的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static TResult SendCommand<TResult>(this IContextAware contextAware,
        Abstractions.command.ICommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommand(command);
    }

    /// <summary>
    ///     发送一个无返回结果的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static void SendCommand(this IContextAware contextAware, Abstractions.command.ICommand command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        context.SendCommand(command);
    }


    /// <summary>
    ///     [Mediator] 异步发送命令并返回结果
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令对象</param>
    /// <param name="cancellationToken">取消令牌,用于取消操作</param>
    /// <returns>包含命令执行结果的ValueTask</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendCommandAsync<TResponse>(this IContextAware contextAware,
        Mediator.ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendCommandAsync(command, cancellationToken);
    }

    /// <summary>
    ///     发送并异步执行一个无返回值的命令
    /// </summary>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static async Task SendCommandAsync(this IContextAware contextAware, IAsyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        await context.SendCommandAsync(command);
    }

    /// <summary>
    ///     发送并异步执行一个带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static async Task<TResult> SendCommandAsync<TResult>(this IContextAware contextAware,
        IAsyncCommand<TResult> command)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return await context.SendCommandAsync(command);
    }

    /// <summary>
    ///     [Mediator] 发送查询的同步版本（不推荐,仅用于兼容性）
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询对象</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static TResponse SendQuery<TResponse>(this IContextAware contextAware, Mediator.IQuery<TResponse> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.SendQuery(query);
    }

    /// <summary>
    ///     发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static TResult SendQuery<TResult>(this IContextAware contextAware, Abstractions.query.IQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.SendQuery(query);
    }

    /// <summary>
    ///     [Mediator] 异步发送查询并返回结果
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询对象</param>
    /// <param name="cancellationToken">取消令牌,用于取消操作</param>
    /// <returns>包含查询结果的ValueTask</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendQueryAsync<TResponse>(this IContextAware contextAware,
        Mediator.IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.SendQueryAsync(query, cancellationToken);
    }

    /// <summary>
    ///     异步发送一个查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的异步查询</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static async Task<TResult> SendQueryAsync<TResult>(this IContextAware contextAware,
        IAsyncQuery<TResult> query)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return await context.SendQueryAsync(query);
    }

    // === 统一请求处理方法 ===

    /// <summary>
    ///     发送请求（统一处理 Command/Query）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="request">要发送的请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>请求结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 request 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendRequestAsync<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(request);

        var context = contextAware.GetContext();
        return context.SendRequestAsync(request, cancellationToken);
    }

    /// <summary>
    ///     发送请求（同步版本,不推荐）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="request">要发送的请求</param>
    /// <returns>请求结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 request 为 null 时抛出</exception>
    public static TResponse SendRequest<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> request)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(request);

        var context = contextAware.GetContext();
        return context.SendRequest(request);
    }

    /// <summary>
    ///     发布通知（一对多事件）
    /// </summary>
    /// <typeparam name="TNotification">通知类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="notification">要发布的通知</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 notification 为 null 时抛出</exception>
    public static ValueTask PublishAsync<TNotification>(this IContextAware contextAware,
        TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(notification);

        var context = contextAware.GetContext();
        return context.PublishAsync(notification, cancellationToken);
    }

    /// <summary>
    ///     创建流式请求（用于大数据集）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="request">流式请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步响应流</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 request 为 null 时抛出</exception>
    public static IAsyncEnumerable<TResponse> CreateStream<TResponse>(this IContextAware contextAware,
        IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(request);

        var context = contextAware.GetContext();
        return context.CreateStream(request, cancellationToken);
    }

    // === 便捷扩展方法 ===

    /// <summary>
    ///     发送命令（无返回值）
    /// </summary>
    /// <typeparam name="TCommand">命令类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask SendAsync<TCommand>(this IContextAware contextAware, TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendAsync(command, cancellationToken);
    }

    /// <summary>
    ///     发送命令（有返回值）
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="command">要发送的命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令执行结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 command 为 null 时抛出</exception>
    public static ValueTask<TResponse> SendAsync<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(command);

        var context = contextAware.GetContext();
        return context.SendAsync(command, cancellationToken);
    }

    /// <summary>
    ///     发送查询
    /// </summary>
    /// <typeparam name="TResponse">响应类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="query">要发送的查询</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 query 为 null 时抛出</exception>
    public static ValueTask<TResponse> QueryAsync<TResponse>(this IContextAware contextAware,
        IRequest<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(query);

        var context = contextAware.GetContext();
        return context.QueryAsync(query, cancellationToken);
    }

    /// <summary>
    ///     发布事件通知
    /// </summary>
    /// <typeparam name="TNotification">通知类型</typeparam>
    /// <param name="contextAware">实现 IContextAware 接口的对象</param>
    /// <param name="notification">要发布的通知</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    /// <exception cref="ArgumentNullException">当 contextAware 或 notification 为 null 时抛出</exception>
    public static ValueTask PublishEventAsync<TNotification>(this IContextAware contextAware,
        TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(contextAware);
        ArgumentNullException.ThrowIfNull(notification);

        var context = contextAware.GetContext();
        return context.PublishEventAsync(notification, cancellationToken);
    }
}