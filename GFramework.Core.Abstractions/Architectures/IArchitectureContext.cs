using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Core.Abstractions.Architectures;

/// <summary>
///     架构上下文接口，统一暴露框架组件访问、兼容旧命令/查询总线，以及当前推荐的 CQRS 运行时入口。
/// </summary>
/// <remarks>
///     <para>旧的 <c>GFramework.Core.Abstractions.Command</c> 与 <c>GFramework.Core.Abstractions.Query</c> 契约会继续通过原有 Command/Query Executor 路径执行，以保证存量代码兼容。</para>
///     <para>新的 <c>GFramework.Core.Abstractions.Cqrs</c> 契约由内置 CQRS dispatcher 统一处理，支持 request pipeline、notification publish 与 stream request。</para>
///     <para>新功能优先使用 <see cref="SendRequestAsync{TResponse}(IRequest{TResponse},CancellationToken)" />、<see cref="SendAsync{TCommand}(TCommand,CancellationToken)" /> 与对应的 CQRS Command/Query 重载；迁移旧代码时可先保留旧入口，再逐步替换为 CQRS 请求模型。</para>
/// </remarks>
public interface IArchitectureContext
{
    /// <summary>
    ///     获取指定类型的服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>服务实例，如果不存在则抛出异常</returns>
    TService GetService<TService>() where TService : class;

    /// <summary>
    ///     获取指定类型的所有服务实例
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>所有符合条件的服务实例列表</returns>
    IReadOnlyList<TService> GetServices<TService>() where TService : class;

    /// <summary>
    ///     获取指定类型的系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>系统实例，如果不存在则抛出异常</returns>
    TSystem GetSystem<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的所有系统实例
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>所有符合条件的系统实例列表</returns>
    IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>模型实例，如果不存在则抛出异常</returns>
    TModel GetModel<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的所有模型实例
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>所有符合条件的模型实例列表</returns>
    IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的工具类实例
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>工具类实例，如果不存在则抛出异常</returns>
    TUtility GetUtility<TUtility>() where TUtility : class, IUtility;

    /// <summary>
    ///     获取指定类型的所有工具类实例
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>所有符合条件的工具类实例列表</returns>
    IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility;

    /// <summary>
    ///     获取指定类型的所有服务实例，并按优先级排序
    ///     实现 IPrioritized 接口的服务将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>按优先级排序后的服务实例列表</returns>
    IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class;

    /// <summary>
    ///     获取指定类型的所有系统实例，并按优先级排序
    ///     实现 IPrioritized 接口的系统将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TSystem">系统类型，必须继承自ISystem接口</typeparam>
    /// <returns>按优先级排序后的系统实例列表</returns>
    IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem;

    /// <summary>
    ///     获取指定类型的所有模型实例，并按优先级排序
    ///     实现 IPrioritized 接口的模型将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TModel">模型类型，必须继承自IModel接口</typeparam>
    /// <returns>按优先级排序后的模型实例列表</returns>
    IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel;

    /// <summary>
    ///     获取指定类型的所有工具类实例，并按优先级排序
    ///     实现 IPrioritized 接口的工具将按优先级排序（数值越小优先级越高）
    /// </summary>
    /// <typeparam name="TUtility">工具类类型，必须继承自IUtility接口</typeparam>
    /// <returns>按优先级排序后的工具类实例列表</returns>
    IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility;

    /// <summary>
    ///     发送一个旧版命令。
    /// </summary>
    /// <param name="command">要发送的旧版命令。</param>
    void SendCommand(ICommand command);

    /// <summary>
    ///     发送一个旧版带返回值命令。
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型。</typeparam>
    /// <param name="command">要发送的旧版命令。</param>
    /// <returns>命令执行结果。</returns>
    TResult SendCommand<TResult>(ICommand<TResult> command);

    /// <summary>
    ///     发送一个新版 CQRS 命令并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="command">要发送的 CQRS 命令。</param>
    /// <returns>命令执行结果。</returns>
    /// <remarks>
    ///     这是迁移后的推荐命令入口。无返回值命令应实现 <c>IRequest&lt;Unit&gt;</c>，并优先通过 <see cref="SendAsync{TCommand}(TCommand,CancellationToken)" /> 调用。
    /// </remarks>
    TResponse SendCommand<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command);


    /// <summary>
    ///     异步发送一个旧版命令。
    /// </summary>
    /// <param name="command">要发送的旧版命令。</param>
    Task SendCommandAsync(IAsyncCommand command);

    /// <summary>
    ///     异步发送一个新版 CQRS 命令并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">命令响应类型。</typeparam>
    /// <param name="command">要发送的 CQRS 命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含命令执行结果的值任务。</returns>
    ValueTask<TResponse> SendCommandAsync<TResponse>(
        GFramework.Cqrs.Abstractions.Cqrs.Command.ICommand<TResponse> command,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     异步发送一个旧版带返回值命令。
    /// </summary>
    /// <typeparam name="TResult">命令执行结果类型。</typeparam>
    /// <param name="command">要发送的旧版命令。</param>
    /// <returns>命令执行结果。</returns>
    Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command);

    /// <summary>
    ///     发送一个旧版查询请求。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">要发送的旧版查询。</param>
    /// <returns>查询结果。</returns>
    TResult SendQuery<TResult>(IQuery<TResult> query);

    /// <summary>
    ///     发送一个新版 CQRS 查询并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型。</typeparam>
    /// <param name="query">要发送的 CQRS 查询。</param>
    /// <returns>查询结果。</returns>
    /// <remarks>
    ///     这是迁移后的推荐查询入口。新查询应优先实现 <c>GFramework.Core.Abstractions.Cqrs.Query.IQuery&lt;TResponse&gt;</c>。
    /// </remarks>
    TResponse SendQuery<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query);

    /// <summary>
    ///     异步发送一个旧版查询请求。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="query">要发送的旧版异步查询。</param>
    /// <returns>查询结果。</returns>
    Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query);

    /// <summary>
    ///     异步发送一个新版 CQRS 查询并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">查询响应类型。</typeparam>
    /// <param name="query">要发送的 CQRS 查询。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含查询结果的值任务。</returns>
    ValueTask<TResponse> SendQueryAsync<TResponse>(GFramework.Cqrs.Abstractions.Cqrs.Query.IQuery<TResponse> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送一个事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型，必须具有无参构造函数</typeparam>
    void SendEvent<TEvent>() where TEvent : new();

    /// <summary>
    ///     发送一个带参数的事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">事件参数</param>
    void SendEvent<TEvent>(TEvent e) where TEvent : class;

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>事件注销接口</returns>
    IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler);

    /// <summary>
    ///     取消注册事件监听器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">要取消注册的事件回调方法</param>
    void UnRegisterEvent<TEvent>(Action<TEvent> onEvent);

    /// <summary>
    /// 发送新版 CQRS 请求，并统一处理命令与查询。
    /// </summary>
    /// <remarks>
    /// 这是自有 CQRS 运行时的主入口。新代码应优先通过该方法或 <see cref="SendAsync{TCommand}(TCommand,CancellationToken)" /> 进入 dispatcher。
    /// </remarks>
    ValueTask<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送新版 CQRS 请求的同步包装版本。
    /// </summary>
    /// <remarks>
    /// 仅为兼容同步调用链保留；新代码应优先使用异步入口，避免阻塞当前线程。
    /// </remarks>
    TResponse SendRequest<TResponse>(IRequest<TResponse> request);

    /// <summary>
    /// 发布新版 CQRS 通知。
    /// </summary>
    /// <remarks>
    /// 该入口用于一对多通知分发，与框架级 <c>EventBus</c> 事件系统并存，适合围绕请求处理过程传播领域通知。
    /// </remarks>
    ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification;

    /// <summary>
    /// 创建新版 CQRS 流式请求。
    /// </summary>
    /// <remarks>
    /// 适用于需要按序惰性产出大量结果的场景。调用方应消费返回的异步序列，而不是回退到旧版查询总线。
    /// </remarks>
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default);

    // === 便捷扩展方法 ===

    /// <summary>
    /// 发送一个无返回值的新版 CQRS 命令。
    /// </summary>
    ValueTask SendAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>;

    /// <summary>
    /// 发送一个有返回值的新版 CQRS 请求。
    /// </summary>
    ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> command,
        CancellationToken cancellationToken = default);


    /// <summary>
    ///     获取环境对象
    /// </summary>
    /// <returns>环境对象实例</returns>
    IEnvironment GetEnvironment();
}
