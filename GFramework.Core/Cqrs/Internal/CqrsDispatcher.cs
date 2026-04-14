using System.Collections.Concurrent;
using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Cqrs;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;

namespace GFramework.Core.Cqrs.Internal;

/// <summary>
///     GFramework 自有 CQRS 运行时分发器。
///     该类型负责解析请求/通知处理器，并在调用前为上下文感知对象注入当前架构上下文。
/// </summary>
internal sealed class CqrsDispatcher(
    IIocContainer container,
    IArchitectureContext context,
    ILogger logger)
{
    private delegate ValueTask<object?> RequestInvoker(object handler, object request, CancellationToken cancellationToken);
    private delegate ValueTask<object?> RequestPipelineInvoker(
        object handler,
        IReadOnlyList<object> behaviors,
        object request,
        CancellationToken cancellationToken);
    private delegate ValueTask NotificationInvoker(object handler, object notification, CancellationToken cancellationToken);
    private delegate object StreamInvoker(object handler, object request, CancellationToken cancellationToken);

    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestInvoker> RequestInvokers = new();
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestPipelineInvoker> RequestPipelineInvokers = new();
    private static readonly ConcurrentDictionary<Type, NotificationInvoker> NotificationInvokers = new();
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), StreamInvoker> StreamInvokers = new();

    /// <summary>
    ///     发送请求并返回结果。
    /// </summary>
    /// <typeparam name="TResponse">响应类型。</typeparam>
    /// <param name="request">请求对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>请求响应。</returns>
    public async ValueTask<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = container.Get(handlerType)
                      ?? throw new InvalidOperationException(
                          $"No CQRS request handler registered for {requestType.FullName}.");

        PrepareHandler(handler);
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = container.GetAll(behaviorType);

        foreach (var behavior in behaviors)
            PrepareHandler(behavior);

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
    ///     发布通知到所有已注册处理器。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="notification">通知对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
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
            PrepareHandler(handler);
            await invoker(handler, notification, cancellationToken);
        }
    }

    /// <summary>
    ///     创建流式请求并返回异步响应序列。
    /// </summary>
    /// <typeparam name="TResponse">响应元素类型。</typeparam>
    /// <param name="request">流式请求对象。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>异步响应序列。</returns>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = container.Get(handlerType)
                      ?? throw new InvalidOperationException(
                          $"No CQRS stream handler registered for {requestType.FullName}.");

        PrepareHandler(handler);

        var invoker = StreamInvokers.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => CreateStreamInvoker(key.RequestType, key.ResponseType));

        return (IAsyncEnumerable<TResponse>)invoker(handler, request, cancellationToken);
    }

    /// <summary>
    ///     为上下文感知处理器注入当前架构上下文。
    /// </summary>
    /// <param name="handler">处理器实例。</param>
    private void PrepareHandler(object handler)
    {
        if (handler is IContextAware contextAware)
            contextAware.SetContext(context);
    }

    /// <summary>
    ///     生成请求处理器调用委托，避免每次发送都重复反射。
    /// </summary>
    private static RequestInvoker CreateRequestInvoker(Type requestType, Type responseType)
    {
        var method = typeof(CqrsDispatcher)
            .GetMethod(nameof(InvokeRequestHandlerAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType);
        return (RequestInvoker)Delegate.CreateDelegate(typeof(RequestInvoker), method);
    }

    /// <summary>
    ///     生成带管道行为的请求处理委托，避免每次发送都重复反射。
    /// </summary>
    private static RequestPipelineInvoker CreateRequestPipelineInvoker(Type requestType, Type responseType)
    {
        var method = typeof(CqrsDispatcher)
            .GetMethod(nameof(InvokeRequestPipelineAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType);
        return (RequestPipelineInvoker)Delegate.CreateDelegate(typeof(RequestPipelineInvoker), method);
    }

    /// <summary>
    ///     生成通知处理器调用委托，避免每次发布都重复反射。
    /// </summary>
    private static NotificationInvoker CreateNotificationInvoker(Type notificationType)
    {
        var method = typeof(CqrsDispatcher)
            .GetMethod(nameof(InvokeNotificationHandlerAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(notificationType);
        return (NotificationInvoker)Delegate.CreateDelegate(typeof(NotificationInvoker), method);
    }

    /// <summary>
    ///     生成流式处理器调用委托，避免每次创建流都重复反射。
    /// </summary>
    private static StreamInvoker CreateStreamInvoker(Type requestType, Type responseType)
    {
        var method = typeof(CqrsDispatcher)
            .GetMethod(nameof(InvokeStreamHandler), BindingFlags.NonPublic | BindingFlags.Static)!
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
}
