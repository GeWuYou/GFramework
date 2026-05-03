// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Notification;
using ICqrsRuntime = GFramework.Core.Abstractions.Cqrs.ICqrsRuntime;

namespace GFramework.Cqrs.Internal;

/// <summary>
///     GFramework 自有 CQRS 运行时分发器。
///     该类型负责解析请求/通知处理器，并在调用前为上下文感知对象注入当前 CQRS 分发上下文。
/// </summary>
internal sealed class CqrsDispatcher(
    IIocContainer container,
    ILogger logger,
    INotificationPublisher notificationPublisher) : ICqrsRuntime
{
    // 卸载安全的进程级缓存：当 generated registry 提供 request invoker 元数据时，
    // registrar 会按请求/响应类型对把它们写入这里；若类型被卸载，条目会自然失效。
    private static readonly WeakTypePairCache<GeneratedRequestInvokerMetadata>
        GeneratedRequestInvokers = new();

    // 卸载安全的进程级缓存：当 generated registry 提供 stream invoker 元数据时，
    // registrar 会按流式请求/响应类型对把它们写入这里；若类型被卸载，条目会自然失效。
    private static readonly WeakTypePairCache<GeneratedStreamInvokerMetadata>
        GeneratedStreamInvokers = new();

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
        .GetMethod(nameof(InvokeRequestPipelineExecutorAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo NotificationHandlerInvokerMethodDefinition = typeof(CqrsDispatcher)
        .GetMethod(nameof(InvokeNotificationHandlerAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo StreamHandlerInvokerMethodDefinition = typeof(CqrsDispatcher)
        .GetMethod(nameof(InvokeStreamHandler), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly INotificationPublisher _notificationPublisher = notificationPublisher
                                                                     ?? throw new ArgumentNullException(
                                                                         nameof(notificationPublisher));

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
            static notificationType => CreateNotificationDispatchBinding(notificationType));
        var handlers = container.GetAll(dispatchBinding.HandlerType);

        if (handlers.Count == 0)
        {
            logger.Debug($"No CQRS notification handler registered for {notificationType.FullName}.");
            return;
        }

        var publishContext = CreateNotificationPublishContext(notification, handlers, context, dispatchBinding.Invoker);
        await _notificationPublisher.PublishAsync(publishContext, cancellationToken).ConfigureAwait(false);
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
            return await dispatchBinding.RequestInvoker(handler, request, cancellationToken).ConfigureAwait(false);

        return await dispatchBinding.GetPipelineExecutor(behaviors.Count)
            .Invoke(handler, behaviors, request, cancellationToken)
            .ConfigureAwait(false);
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
            static (requestType, responseType) => CreateStreamDispatchBinding(requestType, responseType));
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
        var generatedDescriptor = TryGetGeneratedRequestInvokerDescriptor<TResponse>(requestType);
        if (generatedDescriptor is not null)
        {
            var resolvedGeneratedDescriptor = generatedDescriptor.Value;
            return new RequestDispatchBinding<TResponse>(
                resolvedGeneratedDescriptor.HandlerType,
                typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse)),
                resolvedGeneratedDescriptor.Invoker,
                requestType);
        }

        return new RequestDispatchBinding<TResponse>(
            typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse)),
            typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse)),
            CreateRequestInvoker<TResponse>(requestType),
            requestType);
    }

    /// <summary>
    ///     获取指定请求/响应类型对的 dispatch binding；若缓存未命中则按当前加载状态创建。
    /// </summary>
    private static RequestDispatchBinding<TResponse> GetRequestDispatchBinding<TResponse>(Type requestType)
    {
        var bindingBox = RequestDispatchBindings.GetOrAdd(
            requestType,
            typeof(TResponse),
            static (cachedRequestType, cachedResponseType) =>
                CreateRequestDispatchBindingBox<TResponse>(cachedRequestType, cachedResponseType));
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
    ///     尝试从容器已注册的 generated request invoker provider 中获取指定请求/响应类型对的元数据。
    /// </summary>
    /// <typeparam name="TResponse">当前请求响应类型。</typeparam>
    /// <param name="requestType">请求运行时类型。</param>
    /// <returns>命中时返回强类型化后的描述符；否则返回 <see langword="null" />。</returns>
    private static RequestInvokerDescriptor<TResponse>? TryGetGeneratedRequestInvokerDescriptor<TResponse>(Type requestType)
    {
        return GeneratedRequestInvokers.TryGetValue(requestType, typeof(TResponse), out var metadata) &&
               metadata is not null
            ? CreateRequestInvokerDescriptor<TResponse>(requestType, metadata)
            : null;
    }

    /// <summary>
    ///     把 provider 返回的弱类型描述符转换为 dispatcher 内部使用的强类型 request invoker 描述符。
    /// </summary>
    /// <typeparam name="TResponse">当前请求响应类型。</typeparam>
    /// <param name="requestType">请求运行时类型。</param>
    /// <param name="descriptor">provider 返回的弱类型描述符。</param>
    /// <returns>可直接用于创建 request dispatch binding 的强类型描述符。</returns>
    /// <exception cref="InvalidOperationException">当 provider 返回的委托签名与当前请求/响应类型对不匹配时抛出。</exception>
    private static RequestInvokerDescriptor<TResponse> CreateRequestInvokerDescriptor<TResponse>(
        Type requestType,
        GeneratedRequestInvokerMetadata descriptor)
    {
        if (!descriptor.InvokerMethod.IsStatic)
        {
            throw new InvalidOperationException(
                $"Generated CQRS request invoker provider returned a non-static invoker method for request type {requestType.FullName} and response type {typeof(TResponse).FullName}.");
        }

        try
        {
            if (Delegate.CreateDelegate(typeof(RequestInvoker<TResponse>), descriptor.InvokerMethod) is not
                RequestInvoker<TResponse> invoker)
            {
                throw new InvalidOperationException(
                    $"Generated CQRS request invoker provider returned an incompatible invoker for request type {requestType.FullName} and response type {typeof(TResponse).FullName}.");
            }

            return new RequestInvokerDescriptor<TResponse>(descriptor.HandlerType, invoker);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"Generated CQRS request invoker provider returned an incompatible invoker for request type {requestType.FullName} and response type {typeof(TResponse).FullName}.",
                exception);
        }
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
        var generatedDescriptor = TryGetGeneratedStreamInvokerDescriptor(requestType, responseType);
        if (generatedDescriptor is not null)
        {
            var resolvedGeneratedDescriptor = generatedDescriptor.Value;
            return new StreamDispatchBinding(
                resolvedGeneratedDescriptor.HandlerType,
                resolvedGeneratedDescriptor.Invoker);
        }

        return new StreamDispatchBinding(
            typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, responseType),
            CreateStreamInvoker(requestType, responseType));
    }

    /// <summary>
    ///     尝试从容器已注册的 generated stream invoker provider 中获取指定流式请求/响应类型对的元数据。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <returns>命中时返回强类型化后的描述符；否则返回 <see langword="null" />。</returns>
    private static StreamInvokerDescriptor? TryGetGeneratedStreamInvokerDescriptor(Type requestType, Type responseType)
    {
        return GeneratedStreamInvokers.TryGetValue(requestType, responseType, out var metadata) &&
               metadata is not null
            ? CreateStreamInvokerDescriptor(requestType, responseType, metadata)
            : null;
    }

    /// <summary>
    ///     把 provider 返回的弱类型描述符转换为 dispatcher 内部使用的 stream invoker 描述符。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <param name="descriptor">provider 返回的弱类型描述符。</param>
    /// <returns>可直接用于创建 stream dispatch binding 的描述符。</returns>
    /// <exception cref="InvalidOperationException">当 provider 返回的委托签名与当前流式请求/响应类型对不匹配时抛出。</exception>
    private static StreamInvokerDescriptor CreateStreamInvokerDescriptor(
        Type requestType,
        Type responseType,
        GeneratedStreamInvokerMetadata descriptor)
    {
        if (!descriptor.InvokerMethod.IsStatic)
        {
            throw new InvalidOperationException(
                $"Generated CQRS stream invoker provider returned a non-static invoker method for request type {requestType.FullName} and response type {responseType.FullName}.");
        }

        try
        {
            if (Delegate.CreateDelegate(typeof(StreamInvoker), descriptor.InvokerMethod) is not StreamInvoker invoker)
            {
                throw new InvalidOperationException(
                    $"Generated CQRS stream invoker provider returned an incompatible invoker for request type {requestType.FullName} and response type {responseType.FullName}.");
            }

            return new StreamInvokerDescriptor(descriptor.HandlerType, invoker);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"Generated CQRS stream invoker provider returned an incompatible invoker for request type {requestType.FullName} and response type {responseType.FullName}.",
                exception);
        }
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
    ///     生成通知处理器调用委托，避免每次发布都重复反射。
    /// </summary>
    private static NotificationInvoker CreateNotificationInvoker(Type notificationType)
    {
        var method = NotificationHandlerInvokerMethodDefinition
            .MakeGenericMethod(notificationType);
        return (NotificationInvoker)Delegate.CreateDelegate(typeof(NotificationInvoker), method);
    }

    /// <summary>
    ///     为当前通知发布调用创建发布上下文，把处理器集合与执行入口收敛到同一对象。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="notification">当前通知。</param>
    /// <param name="handlers">当前发布调用已解析到的处理器集合。</param>
    /// <param name="context">当前 CQRS 分发上下文。</param>
    /// <param name="invoker">执行单个通知处理器时复用的强类型调用委托。</param>
    /// <returns>供通知发布器消费的执行上下文。</returns>
    private static NotificationPublishContext<TNotification> CreateNotificationPublishContext<TNotification>(
        TNotification notification,
        IReadOnlyList<object> handlers,
        ICqrsContext context,
        NotificationInvoker invoker)
        where TNotification : INotification
    {
        return new DelegatingNotificationPublishContext<TNotification, NotificationDispatchState>(
            notification,
            handlers,
            new NotificationDispatchState(context, invoker),
            static (handler, currentNotification, state, currentCancellationToken) =>
                InvokePublishedNotificationHandlerAsync(handler, currentNotification, state, currentCancellationToken));
    }

    /// <summary>
    ///     执行通知发布器选中的单个处理器，并在调用前注入当前分发上下文。
    /// </summary>
    /// <typeparam name="TNotification">通知类型。</typeparam>
    /// <param name="handler">要执行的处理器实例。</param>
    /// <param name="notification">当前通知。</param>
    /// <param name="state">当前处理器执行所需的 dispatcher 状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示当前处理器执行完成的值任务。</returns>
    private static ValueTask InvokePublishedNotificationHandlerAsync<TNotification>(
        object handler,
        TNotification notification,
        NotificationDispatchState state,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        PrepareHandler(handler, state.Context);
        return state.Invoker(handler, notification!, cancellationToken);
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
    ///     执行指定行为数量的强类型 request pipeline executor。
    ///     该入口本身是缓存的固定 executor 形状；每次分发只绑定当前 handler 与 behaviors 实例。
    /// </summary>
    private static ValueTask<TResponse> InvokeRequestPipelineExecutorAsync<TRequest, TResponse>(
        object handler,
        IReadOnlyList<object> behaviors,
        object request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var invocation = new RequestPipelineInvocation<TRequest, TResponse>(
            (IRequestHandler<TRequest, TResponse>)handler,
            behaviors);
        return invocation.InvokeAsync((TRequest)request, cancellationToken);
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
    ///     保存通知发布器执行单个 handler 时需要复用的 dispatcher 状态。
    /// </summary>
    /// <param name="Context">当前 CQRS 分发上下文。</param>
    /// <param name="Invoker">执行单个通知处理器的强类型调用委托。</param>
    private readonly record struct NotificationDispatchState(
        ICqrsContext Context,
        NotificationInvoker Invoker);

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
    ///     该绑定同时覆盖“直接请求处理”和“按行为数量缓存 pipeline executor 形状”的两条路径。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    private sealed class RequestDispatchBinding<TResponse>(
        Type handlerType,
        Type behaviorType,
        RequestInvoker<TResponse> requestInvoker,
        Type requestType)
    {
        // 线程安全：该缓存按 behaviorCount 复用 pipeline executor 形状，GetPipelineExecutor 通过 ConcurrentDictionary
        // 的 GetOrAdd 支持并发读写。缓存项只保存委托形状，不保留 handler/behavior 实例；若行为数量组合持续增长，
        // 字典会随之增长且当前实现不提供回收。
        private readonly ConcurrentDictionary<int, RequestPipelineExecutor<TResponse>> _pipelineExecutors = new();
        private readonly RequestPipelineInvoker<TResponse> _pipelineInvoker = CreateRequestPipelineInvoker<TResponse>(requestType);

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
        ///     获取指定行为数量对应的 pipeline executor。
        ///     executor 形状会按请求/响应类型与行为数量缓存，但不会缓存 handler 或 behavior 实例。
        /// </summary>
        public RequestPipelineExecutor<TResponse> GetPipelineExecutor(int behaviorCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(behaviorCount);
            return _pipelineExecutors.GetOrAdd<RequestPipelineExecutorFactoryState<TResponse>>(
                behaviorCount,
                static (count, state) => CreateRequestPipelineExecutor(count, state.PipelineInvoker),
                new RequestPipelineExecutorFactoryState<TResponse>(_pipelineInvoker));
        }

        /// <summary>
        ///     仅供测试读取指定行为数量是否已存在缓存 executor。
        /// </summary>
        public object? GetPipelineExecutorForTesting(int behaviorCount)
        {
            _pipelineExecutors.TryGetValue(behaviorCount, out var executor);
            return executor;
        }
    }

    /// <summary>
    ///     为指定请求/响应类型与固定行为数量创建 pipeline executor。
    ///     行为数量用于表达缓存形状，实际分发仍会消费本次容器解析出的 handler 与 behaviors 实例。
    /// </summary>
    private static RequestPipelineExecutor<TResponse> CreateRequestPipelineExecutor<TResponse>(
        int behaviorCount,
        RequestPipelineInvoker<TResponse> invoker)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(behaviorCount);
        return new RequestPipelineExecutor<TResponse>(behaviorCount, invoker);
    }

    /// <summary>
    ///     为指定请求/响应类型创建可跨多个 behaviorCount 复用的 typed pipeline invoker。
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
    ///     保存固定行为数量下的 typed pipeline executor 形状。
    ///     该对象自身可跨分发复用，但每次调用都只绑定当前 handler 与 behavior 实例。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    private sealed class RequestPipelineExecutor<TResponse>(
        int behaviorCount,
        RequestPipelineInvoker<TResponse> invoker)
    {
        /// <summary>
        ///     获取此 executor 预期处理的行为数量。
        /// </summary>
        public int BehaviorCount { get; } = behaviorCount;

        /// <summary>
        ///     使用当前 handler / behaviors / request 执行缓存的 pipeline 形状。
        /// </summary>
        public ValueTask<TResponse> Invoke(
            object handler,
            IReadOnlyList<object> behaviors,
            object request,
            CancellationToken cancellationToken)
        {
            if (behaviors.Count != BehaviorCount)
            {
                throw new InvalidOperationException(
                    $"Cached request pipeline executor expected {BehaviorCount} behaviors, but received {behaviors.Count}.");
            }

            return invoker(handler, behaviors, request, cancellationToken);
        }
    }

    /// <summary>
    ///     为 pipeline executor 缓存携带当前请求类型，避免按行为数量建缓存时创建闭包。
    /// </summary>
    /// <typeparam name="TResponse">请求响应类型。</typeparam>
    private readonly record struct RequestPipelineExecutorFactoryState<TResponse>(
        RequestPipelineInvoker<TResponse> PipelineInvoker);

    /// <summary>
    ///     记录 registrar 写入的 generated request invoker 元数据。
    /// </summary>
    /// <param name="HandlerType">请求处理器在容器中的服务类型。</param>
    /// <param name="InvokerMethod">执行请求处理器的开放静态方法。</param>
    private sealed record GeneratedRequestInvokerMetadata(
        Type HandlerType,
        MethodInfo InvokerMethod);

    /// <summary>
    ///     记录 registrar 写入的 generated stream invoker 元数据。
    /// </summary>
    /// <param name="HandlerType">流式请求处理器在容器中的服务类型。</param>
    /// <param name="InvokerMethod">执行流式请求处理器的开放静态方法。</param>
    private sealed record GeneratedStreamInvokerMetadata(
        Type HandlerType,
        MethodInfo InvokerMethod);

    /// <summary>
    ///     保存 provider 返回的请求处理器服务类型与强类型 request invoker。
    /// </summary>
    /// <typeparam name="TResponse">当前请求响应类型。</typeparam>
    private readonly record struct RequestInvokerDescriptor<TResponse>(
        Type HandlerType,
        RequestInvoker<TResponse> Invoker);

    /// <summary>
    ///     保存 provider 返回的流式请求处理器服务类型与 stream invoker。
    /// </summary>
    /// <param name="HandlerType">流式请求处理器在容器中的服务类型。</param>
    /// <param name="Invoker">执行流式请求处理器的调用委托。</param>
    private readonly record struct StreamInvokerDescriptor(
        Type HandlerType,
        StreamInvoker Invoker);

    /// <summary>
    ///     供 registrar 在 generated registry 激活后登记 request invoker 元数据。
    /// </summary>
    /// <param name="requestType">请求运行时类型。</param>
    /// <param name="responseType">响应运行时类型。</param>
    /// <param name="descriptor">要登记的 generated request invoker 描述符。</param>
    internal static void RegisterGeneratedRequestInvokerDescriptor(
        Type requestType,
        Type responseType,
        CqrsRequestInvokerDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);
        ArgumentNullException.ThrowIfNull(descriptor);

        _ = GeneratedRequestInvokers.GetOrAdd(
            requestType,
            responseType,
            (_, _) => new GeneratedRequestInvokerMetadata(
                descriptor.HandlerType,
                descriptor.InvokerMethod));
    }

    /// <summary>
    ///     供 registrar 在 generated registry 激活后登记 stream invoker 元数据。
    /// </summary>
    /// <param name="requestType">流式请求运行时类型。</param>
    /// <param name="responseType">流式响应元素类型。</param>
    /// <param name="descriptor">要登记的 generated stream invoker 描述符。</param>
    internal static void RegisterGeneratedStreamInvokerDescriptor(
        Type requestType,
        Type responseType,
        CqrsStreamInvokerDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(responseType);
        ArgumentNullException.ThrowIfNull(descriptor);

        _ = GeneratedStreamInvokers.GetOrAdd(
            requestType,
            responseType,
            (_, _) => new GeneratedStreamInvokerMetadata(
                descriptor.HandlerType,
                descriptor.InvokerMethod));
    }

    /// <summary>
    ///     保存单次 request pipeline 分发所需的当前 handler、behavior 列表和 continuation 缓存。
    ///     该对象只存在于本次分发，不会跨请求保留容器解析出的实例。
    /// </summary>
    private sealed class RequestPipelineInvocation<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler,
        IReadOnlyList<object> behaviors)
        where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _handler = handler;
        private readonly IReadOnlyList<object> _behaviors = behaviors;
        private readonly MessageHandlerDelegate<TRequest, TResponse>?[] _continuations =
            new MessageHandlerDelegate<TRequest, TResponse>?[behaviors.Count + 1];

        /// <summary>
        ///     从 pipeline 起点执行当前请求。
        /// </summary>
        public ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken)
        {
            return GetContinuation(0)(request, cancellationToken);
        }

        /// <summary>
        ///     获取指定阶段的 continuation，并在首次请求时为该阶段绑定一次不可变调用入口。
        ///     同一行为多次调用 <c>next</c> 时会命中相同 continuation，保持与传统链式委托一致的语义。
        ///     线程模型上，该缓存仅假定单次分发链按顺序推进；若某个 behavior 并发调用多个 <c>next</c>，
        ///     这里可能重复创建等价 continuation，但不会跨分发共享，也不会缓存容器解析出的实例。
        /// </summary>
        private MessageHandlerDelegate<TRequest, TResponse> GetContinuation(int index)
        {
            var continuation = _continuations[index];
            if (continuation is not null)
            {
                return continuation;
            }

            continuation = index == _behaviors.Count
                ? InvokeHandlerAsync
                : new RequestPipelineContinuation<TRequest, TResponse>(this, index).InvokeAsync;
            _continuations[index] = continuation;
            return continuation;
        }

        /// <summary>
        ///     执行指定索引的 pipeline behavior。
        /// </summary>
        private ValueTask<TResponse> InvokeBehaviorAsync(
            int index,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var behavior = (IPipelineBehavior<TRequest, TResponse>)_behaviors[index];
            return behavior.Handle(request, GetContinuation(index + 1), cancellationToken);
        }

        /// <summary>
        ///     调用最终请求处理器。
        /// </summary>
        private ValueTask<TResponse> InvokeHandlerAsync(TRequest request, CancellationToken cancellationToken)
        {
            return _handler.Handle(request, cancellationToken);
        }

        /// <summary>
        ///     将固定阶段索引绑定为标准 <see cref="MessageHandlerDelegate{TRequest,TResponse}" />。
        ///     该包装只在单次分发生命周期内存在，用于把缓存 shape 套入当前实例。
        /// </summary>
        private sealed class RequestPipelineContinuation<TCurrentRequest, TCurrentResponse>(
            RequestPipelineInvocation<TCurrentRequest, TCurrentResponse> invocation,
            int index)
            where TCurrentRequest : IRequest<TCurrentResponse>
        {
            /// <summary>
            ///     执行当前阶段并跳转到下一个 continuation。
            /// </summary>
            public ValueTask<TCurrentResponse> InvokeAsync(
                TCurrentRequest request,
                CancellationToken cancellationToken)
            {
                return invocation.InvokeBehaviorAsync(index, request, cancellationToken);
            }
        }
    }
}
