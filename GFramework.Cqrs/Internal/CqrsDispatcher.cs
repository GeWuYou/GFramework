using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICqrsRuntime = GFramework.Core.Abstractions.Cqrs.ICqrsRuntime;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     GFramework 自有 CQRS 运行时分发器。
///     该类型负责解析请求/通知处理器，并在调用前为上下文感知对象注入当前 CQRS 分发上下文。
/// </summary>
internal sealed class CqrsDispatcher(
    IIocContainer container,
    ILogger logger) : ICqrsRuntime
{
    // 进程级缓存：按请求/响应类型缓存直接处理器调用委托，避免热路径重复反射。
    // 线程安全依赖 ConcurrentDictionary；缓存与进程同寿命，默认假设请求类型集合有限且稳定。
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestInvoker>
        RequestInvokers = new();

    // 进程级缓存：缓存带 pipeline 的请求调用委托，减少每次分发时的反射与表达式重建开销。
    // 若后续引入动态生成请求类型，需要重新评估该缓存的增长边界。
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestPipelineInvoker>
        RequestPipelineInvokers = new();

    // 进程级缓存：缓存通知调用委托，复用并发安全字典以支撑多线程发布路径。
    private static readonly ConcurrentDictionary<Type, NotificationInvoker> NotificationInvokers = new();

    // 进程级缓存：缓存通知处理器服务类型，避免每次发布都重复 MakeGenericType。
    private static readonly ConcurrentDictionary<Type, Type> NotificationHandlerServiceTypes = new();

    // 进程级缓存：缓存流式请求调用委托，避免每次创建流时重复解析反射签名。
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), StreamInvoker> StreamInvokers =
        new();

    // 进程级缓存：缓存请求处理器与 pipeline 行为的服务类型，减少热路径中的泛型类型构造。
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestServiceTypeSet>
        RequestServiceTypes = new();

    // 进程级缓存：缓存流式请求处理器服务类型，避免每次建流时重复 MakeGenericType。
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type>
        StreamHandlerServiceTypes =
            new();

    // 静态方法定义缓存：这些反射查找与消息类型无关，只需解析一次即可复用。
    private static readonly MethodInfo RequestHandlerInvokerMethodDefinition = typeof(CqrsDispatcher)
        .GetMethod(nameof(InvokeRequestHandlerAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo RequestPipelineInvokerMethodDefinition = typeof(CqrsDispatcher)
        .GetMethod(nameof(InvokeRequestPipelineAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo NotificationHandlerInvokerMethodDefinition = typeof(CqrsDispatcher)
        .GetMethod(nameof(InvokeNotificationHandlerAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo StreamHandlerInvokerMethodDefinition = typeof(CqrsDispatcher)
        .GetMethod(nameof(InvokeStreamHandler), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    ///     发布通知到所有已注册处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="context">当前 CQRS 分发上下文，用于上下文感知处理器注入。</param>
    /// <param name="notification">通知对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async ValueTask PublishAsync<TNotification>(
        ICqrsContext context,
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var handlerType = NotificationHandlerServiceTypes.GetOrAdd(
            notificationType,
            static type => typeof(INotificationHandler<>).MakeGenericType(type));
        var handlers = container.GetAll(handlerType);

        if (handlers.Count == 0)
        {
            logger.Debug($"No CQRS notification handler registered for {notificationType.FullName}.");
            return;
        }

        var invoker = NotificationInvokers.GetOrAdd(
            notificationType,
            CreateNotificationInvoker);

        foreach (var handler in handlers)
        {
            PrepareHandler(handler, context);
            await invoker(handler, notification, cancellationToken);
        }
    }

    /// <summary>
    ///     发送请求并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="context">当前 CQRS 分发上下文，用于上下文感知处理器注入。</param>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应。</returns>
    public async ValueTask<TResponse> SendAsync<TResponse>(
        ICqrsContext context,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var serviceTypes = RequestServiceTypes.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => new RequestServiceTypeSet(
                typeof(IRequestHandler<,>).MakeGenericType(key.RequestType, key.ResponseType),
                typeof(IPipelineBehavior<,>).MakeGenericType(key.RequestType, key.ResponseType)));
        var handlerType = serviceTypes.HandlerType;
        var handler = container.Get(handlerType)
                      ?? throw new InvalidOperationException(
                          $"No CQRS request handler registered for {requestType.FullName}.");

        PrepareHandler(handler, context);
        var behaviors = container.GetAll(serviceTypes.BehaviorType);

        foreach (var behavior in behaviors)
            PrepareHandler(behavior, context);

        if (behaviors.Count == 0)
        {
            var invoker = RequestInvokers.GetOrAdd(
                (requestType, typeof(TResponse)),
                static key => CreateRequestInvoker(key.RequestType, key.ResponseType));

            var result = await invoker(handler, request, cancellationToken);
            return result is null ? default! : (TResponse)result;
        }

        var pipelineInvoker = RequestPipelineInvokers.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => CreateRequestPipelineInvoker(key.RequestType, key.ResponseType));

        var pipelineResult = await pipelineInvoker(handler, behaviors, request, cancellationToken);
        return pipelineResult is null ? default! : (TResponse)pipelineResult;
    }

    /// <summary>
    ///     创建流式请求并返回异步响应序列。
    /// </summary>
    /// <typeparam name="TResponse">响应元素类型。</typeparam>
    /// <param name="context">当前 CQRS 分发上下文，用于上下文感知处理器注入。</param>
    /// <param name="request">流式请求对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步响应序列。</returns>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        ICqrsContext context,
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = StreamHandlerServiceTypes.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => typeof(IStreamRequestHandler<,>).MakeGenericType(key.RequestType, key.ResponseType));
        var handler = container.Get(handlerType)
                      ?? throw new InvalidOperationException(
                          $"No CQRS stream handler registered for {requestType.FullName}.");

        PrepareHandler(handler, context);

        var invoker = StreamInvokers.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => CreateStreamInvoker(key.RequestType, key.ResponseType));

        return (IAsyncEnumerable<TResponse>)invoker(handler, request, cancellationToken);
    }

    /// <summary>
    ///     为上下文感知处理器注入当前 CQRS 分发上下文。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    /// <param name="context">当前 CQRS 分发上下文。</param>
    private static void PrepareHandler(object handler, ICqrsContext context)
    {
        if (handler is IContextAware contextAware)
        {
            if (context is not IArchitectureContext architectureContext)
                throw new InvalidOperationException(
                    "The current CQRS context does not implement IArchitectureContext, so it cannot be injected into IContextAware handlers.");

            contextAware.SetContext(architectureContext);
        }
    }

    /// <summary>
    ///     生成请求处理器调用委托，避免每次发送都重复反射。
    /// </summary>
    private static RequestInvoker CreateRequestInvoker(Type requestType, Type responseType)
    {
        var method = RequestHandlerInvokerMethodDefinition
            .MakeGenericMethod(requestType, responseType);
        return (RequestInvoker)Delegate.CreateDelegate(typeof(RequestInvoker), method);
    }

    /// <summary>
    ///     生成带管道行为的请求处理委托，避免每次发送都重复反射。
    /// </summary>
    private static RequestPipelineInvoker CreateRequestPipelineInvoker(Type requestType, Type responseType)
    {
        var method = RequestPipelineInvokerMethodDefinition
            .MakeGenericMethod(requestType, responseType);
        return (RequestPipelineInvoker)Delegate.CreateDelegate(typeof(RequestPipelineInvoker), method);
    }

    /// <summary>
    ///     生成通知处理器调用委托，避免每次发布都重复反射。
    /// </summary>
    private static NotificationInvoker CreateNotificationInvoker(Type notificationType)
    {
        var method = NotificationHandlerInvokerMethodDefinition
            .MakeGenericMethod(notificationType);
        return (NotificationInvoker)Delegate.CreateDelegate(typeof(NotificationInvoker), method);
    }

    /// <summary>
    ///     生成流式处理器调用委托，避免每次创建流都重复反射。
    /// </summary>
    private static StreamInvoker CreateStreamInvoker(Type requestType, Type responseType)
    {
        var method = StreamHandlerInvokerMethodDefinition
            .MakeGenericMethod(requestType, responseType);
        return (StreamInvoker)Delegate.CreateDelegate(typeof(StreamInvoker), method);
    }

    /// <summary>
    ///     执行已强类型化的请求处理器调用。
    /// </summary>
    private static async ValueTask<object?> InvokeRequestHandlerAsync<TRequest, TResponse>(
        object handler,
        object request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
        var typedRequest = (TRequest)request;
        var result = await typedHandler.Handle(typedRequest, cancellationToken);
        return result;
    }

    /// <summary>
    ///     执行包含管道行为链的请求处理。
    /// </summary>
    private static async ValueTask<object?> InvokeRequestPipelineAsync<TRequest, TResponse>(
        object handler,
        IReadOnlyList<object> behaviors,
        object request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
        var typedRequest = (TRequest)request;

        MessageHandlerDelegate<TRequest, TResponse> next =
            (message, token) => typedHandler.Handle(message, token);

        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = (IPipelineBehavior<TRequest, TResponse>)behaviors[i];
            var currentNext = next;
            next = (message, token) => behavior.Handle(message, currentNext, token);
        }

        var result = await next(typedRequest, cancellationToken);
        return result;
    }

    /// <summary>
    ///     执行已强类型化的通知处理器调用。
    /// </summary>
    private static ValueTask InvokeNotificationHandlerAsync<TNotification>(
        object handler,
        object notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var typedHandler = (INotificationHandler<TNotification>)handler;
        var typedNotification = (TNotification)notification;
        return typedHandler.Handle(typedNotification, cancellationToken);
    }

    /// <summary>
    ///     执行已强类型化的流式处理器调用。
    /// </summary>
    private static object InvokeStreamHandler<TRequest, TResponse>(
        object handler,
        object request,
        CancellationToken cancellationToken)
        where TRequest : IStreamRequest<TResponse>
    {
        var typedHandler = (IStreamRequestHandler<TRequest, TResponse>)handler;
        var typedRequest = (TRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }

    private delegate ValueTask<object?> RequestInvoker(object handler, object request,
        CancellationToken cancellationToken);

    private delegate ValueTask<object?> RequestPipelineInvoker(
        object handler,
        IReadOnlyList<object> behaviors,
        object request,
        CancellationToken cancellationToken);

    private delegate ValueTask NotificationInvoker(object handler, object notification,
        CancellationToken cancellationToken);

    private delegate object StreamInvoker(object handler, object request, CancellationToken cancellationToken);

    private readonly record struct RequestServiceTypeSet(Type HandlerType, Type BehaviorType);
}
