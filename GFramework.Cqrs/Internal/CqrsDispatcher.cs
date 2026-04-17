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
    // 卸载安全的进程级缓存：通知类型只以弱键语义保留。
    // 若插件/热重载程序集中的通知类型被卸载，对应分发绑定会自然失效，下次命中时再重新计算。
    private static readonly WeakKeyCache<Type, NotificationDispatchBinding>
        NotificationDispatchBindings = new();

    // 卸载安全的进程级缓存：请求/响应类型对采用弱键缓存，避免流式消息类型被静态字典永久保留。
    private static readonly WeakTypePairCache<StreamDispatchBinding>
        StreamDispatchBindings = new();

    // 卸载安全的进程级缓存：请求/响应类型对命中后复用强类型 dispatch binding；
    // 若任一类型被回收，后续首次发送时会按当前加载状态重新生成。
    private static readonly WeakTypePairCache<RequestDispatchBindingBox>
        RequestDispatchBindings = new();

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
        var dispatchBinding = NotificationDispatchBindings.GetOrAdd(
            notificationType,
            CreateNotificationDispatchBinding);
        var handlers = container.GetAll(dispatchBinding.HandlerType);

        if (handlers.Count == 0)
        {
            logger.Debug($"No CQRS notification handler registered for {notificationType.FullName}.");
            return;
        }

        foreach (var handler in handlers)
        {
            PrepareHandler(handler, context);
            await dispatchBinding.Invoker(handler, notification, cancellationToken);
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
        var dispatchBinding = GetRequestDispatchBinding<TResponse>(requestType);
        var handler = container.Get(dispatchBinding.HandlerType)
                      ?? throw new InvalidOperationException(
                          $"No CQRS request handler registered for {requestType.FullName}.");

        PrepareHandler(handler, context);
        var behaviors = container.GetAll(dispatchBinding.BehaviorType);

        foreach (var behavior in behaviors)
            PrepareHandler(behavior, context);

        if (behaviors.Count == 0)
            return await dispatchBinding.RequestInvoker(handler, request, cancellationToken);

        return await dispatchBinding.PipelineInvoker(handler, behaviors, request, cancellationToken);
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
        var dispatchBinding = StreamDispatchBindings.GetOrAdd(
            requestType,
            typeof(TResponse),
            CreateStreamDispatchBinding);
        var handler = container.Get(dispatchBinding.HandlerType)
                      ?? throw new InvalidOperationException(
                          $"No CQRS stream handler registered for {requestType.FullName}.");

        PrepareHandler(handler, context);

        return (IAsyncEnumerable<TResponse>)dispatchBinding.Invoker(handler, request, cancellationToken);
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
    ///     为指定请求类型构造完整分发绑定，把服务类型与强类型调用委托一次性收敛到同一缓存项。
    /// </summary>
    private static RequestDispatchBinding<TResponse> CreateRequestDispatchBinding<TResponse>(Type requestType)
    {
        return new RequestDispatchBinding<TResponse>(
            typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse)),
            typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse)),
            CreateRequestInvoker<TResponse>(requestType),
            CreateRequestPipelineInvoker<TResponse>(requestType));
    }

    /// <summary>
    ///     获取指定请求/响应类型对的 dispatch binding；若缓存未命中则按当前加载状态创建。
    /// </summary>
    private static RequestDispatchBinding<TResponse> GetRequestDispatchBinding<TResponse>(Type requestType)
    {
        var bindingBox = RequestDispatchBindings.GetOrAdd(
            requestType,
            typeof(TResponse),
            CreateRequestDispatchBindingBox<TResponse>);
        return bindingBox.Get<TResponse>();
    }

    /// <summary>
    ///     为弱键请求缓存创建强类型 binding 盒子，避免 value-type 响应走 object 结果桥接。
    /// </summary>
    private static RequestDispatchBindingBox CreateRequestDispatchBindingBox<TResponse>(
        Type requestType,
        Type responseType)
    {
        if (responseType != typeof(TResponse))
            throw new InvalidOperationException(
                $"Request dispatch binding cache expected response type {typeof(TResponse).FullName}, but received {responseType.FullName}.");

        return RequestDispatchBindingBox.Create(CreateRequestDispatchBinding<TResponse>(requestType));
    }

    /// <summary>
    ///     为指定通知类型构造完整分发绑定，把服务类型与调用委托聚合到同一缓存项。
    /// </summary>
    private static NotificationDispatchBinding CreateNotificationDispatchBinding(Type notificationType)
    {
        return new NotificationDispatchBinding(
            typeof(INotificationHandler<>).MakeGenericType(notificationType),
            CreateNotificationInvoker(notificationType));
    }

    /// <summary>
    ///     为指定流式请求类型构造完整分发绑定，把服务类型与调用委托聚合到同一缓存项。
    /// </summary>
    private static StreamDispatchBinding CreateStreamDispatchBinding(Type requestType, Type responseType)
    {
        return new StreamDispatchBinding(
            typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, responseType),
            CreateStreamInvoker(requestType, responseType));
    }

    /// <summary>
    ///     生成请求处理器调用委托，避免每次发送都重复反射。
    /// </summary>
    private static RequestInvoker<TResponse> CreateRequestInvoker<TResponse>(Type requestType)
    {
        var method = RequestHandlerInvokerMethodDefinition
            .MakeGenericMethod(requestType, typeof(TResponse));
        return (RequestInvoker<TResponse>)Delegate.CreateDelegate(typeof(RequestInvoker<TResponse>), method);
    }

    /// <summary>
    ///     生成带管道行为的请求处理委托，避免每次发送都重复反射。
    /// </summary>
    private static RequestPipelineInvoker<TResponse> CreateRequestPipelineInvoker<TResponse>(Type requestType)
    {
        var method = RequestPipelineInvokerMethodDefinition
            .MakeGenericMethod(requestType, typeof(TResponse));
        return (RequestPipelineInvoker<TResponse>)Delegate.CreateDelegate(
            typeof(RequestPipelineInvoker<TResponse>),
            method);
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
    private static ValueTask<TResponse> InvokeRequestHandlerAsync<TRequest, TResponse>(
        object handler,
        object request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var typedHandler = (IRequestHandler<TRequest, TResponse>)handler;
        var typedRequest = (TRequest)request;
        return typedHandler.Handle(typedRequest, cancellationToken);
    }

    /// <summary>
    ///     执行包含管道行为链的请求处理。
    /// </summary>
    private static ValueTask<TResponse> InvokeRequestPipelineAsync<TRequest, TResponse>(
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

        return next(typedRequest, cancellationToken);
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

    private delegate ValueTask<TResponse> RequestInvoker<TResponse>(
        object handler,
        object request,
        CancellationToken cancellationToken);

    private delegate ValueTask<TResponse> RequestPipelineInvoker<TResponse>(
        object handler,
        IReadOnlyList<object> behaviors,
        object request,
        CancellationToken cancellationToken);

    private delegate ValueTask NotificationInvoker(object handler, object notification,
        CancellationToken cancellationToken);

    private delegate object StreamInvoker(object handler, object request, CancellationToken cancellationToken);

    /// <summary>
    ///     将不同响应类型的 request dispatch binding 包装到统一弱缓存值中，
    ///     同时保留强类型委托，避免值类型响应退化为 object 桥接。
    /// </summary>
    private abstract class RequestDispatchBindingBox
    {
        /// <summary>
        ///     创建一个新的强类型 dispatch binding 盒子。
        /// </summary>
        public static RequestDispatchBindingBox Create<TResponse>(RequestDispatchBinding<TResponse> binding)
        {
            ArgumentNullException.ThrowIfNull(binding);
            return new RequestDispatchBindingBox<TResponse>(binding);
        }

        /// <summary>
        ///     读取指定响应类型的 request dispatch binding。
        /// </summary>
        public abstract RequestDispatchBinding<TResponse> Get<TResponse>();
    }

    /// <summary>
    ///     保存特定响应类型的 request dispatch binding。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    private sealed class RequestDispatchBindingBox<TResponse>(RequestDispatchBinding<TResponse> binding)
        : RequestDispatchBindingBox
    {
        private readonly RequestDispatchBinding<TResponse> _binding = binding;

        /// <summary>
        ///     以原始强类型返回当前 binding；若请求的响应类型不匹配则抛出异常。
        /// </summary>
        public override RequestDispatchBinding<TRequestedResponse> Get<TRequestedResponse>()
        {
            if (typeof(TRequestedResponse) != typeof(TResponse))
            {
                throw new InvalidOperationException(
                    $"Cached request dispatch binding for {typeof(TResponse).FullName} cannot be used as {typeof(TRequestedResponse).FullName}.");
            }

            return (RequestDispatchBinding<TRequestedResponse>)(object)_binding;
        }
    }

    /// <summary>
    ///     保存通知分发路径所需的服务类型与强类型调用委托。
    ///     该绑定把“容器解析哪个服务类型”与“如何调用处理器”聚合到同一缓存项中。
    /// </summary>
    private sealed class NotificationDispatchBinding(Type handlerType, NotificationInvoker invoker)
    {
        /// <summary>
        ///     获取通知处理器在容器中的服务类型。
        /// </summary>
        public Type HandlerType { get; } = handlerType;

        /// <summary>
        ///     获取执行通知处理器的强类型调用委托。
        /// </summary>
        public NotificationInvoker Invoker { get; } = invoker;
    }

    /// <summary>
    ///     保存流式请求分发路径所需的服务类型与调用委托。
    ///     该绑定让建流热路径只需一次缓存命中即可获得解析与调用所需元数据。
    /// </summary>
    private sealed class StreamDispatchBinding(Type handlerType, StreamInvoker invoker)
    {
        /// <summary>
        ///     获取流式请求处理器在容器中的服务类型。
        /// </summary>
        public Type HandlerType { get; } = handlerType;

        /// <summary>
        ///     获取执行流式请求处理器的调用委托。
        /// </summary>
        public StreamInvoker Invoker { get; } = invoker;
    }

    /// <summary>
    ///     保存普通请求分发路径所需的 handler 服务类型、pipeline 服务类型与强类型调用委托。
    ///     该绑定同时覆盖“直接请求处理”和“带 pipeline 的请求处理”两条路径。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    private sealed class RequestDispatchBinding<TResponse>(
        Type handlerType,
        Type behaviorType,
        RequestInvoker<TResponse> requestInvoker,
        RequestPipelineInvoker<TResponse> pipelineInvoker)
    {
        /// <summary>
        ///     获取请求处理器在容器中的服务类型。
        /// </summary>
        public Type HandlerType { get; } = handlerType;

        /// <summary>
        ///     获取 pipeline 行为在容器中的服务类型。
        /// </summary>
        public Type BehaviorType { get; } = behaviorType;

        /// <summary>
        ///     获取直接调用请求处理器的强类型委托。
        /// </summary>
        public RequestInvoker<TResponse> RequestInvoker { get; } = requestInvoker;

        /// <summary>
        ///     获取执行 pipeline 行为链的强类型委托。
        /// </summary>
        public RequestPipelineInvoker<TResponse> PipelineInvoker { get; } = pipelineInvoker;
    }
}
