using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS dispatcher 会缓存热路径中的服务类型构造结果。
/// </summary>
[TestFixture]
internal sealed class CqrsDispatcherCacheTests
{
    /// <summary>
    ///     初始化测试上下文。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new MicrosoftDiContainer();
        _container.RegisterCqrsPipelineBehavior<DispatcherPipelineCacheBehavior>();

        CqrsTestRuntime.RegisterHandlers(
            _container,
            typeof(CqrsDispatcherCacheTests).Assembly,
            typeof(ArchitectureContext).Assembly);

        _container.Freeze();
        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    ///     清理测试上下文引用。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context = null;
        _container = null;
    }

    private MicrosoftDiContainer? _container;
    private ArchitectureContext? _context;

    /// <summary>
    ///     验证相同消息类型重复分发时，不会重复扩张服务类型缓存。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Service_Types_After_First_Dispatch()
    {
        var notificationServiceTypes = GetCacheField("NotificationHandlerServiceTypes");
        var requestServiceTypes = GetCacheField("RequestServiceTypes");
        var streamServiceTypes = GetCacheField("StreamHandlerServiceTypes");
        var requestInvokers = GetCacheField("RequestInvokers");
        var requestPipelineInvokers = GetCacheField("RequestPipelineInvokers");
        var notificationInvokers = GetCacheField("NotificationInvokers");
        var streamInvokers = GetCacheField("StreamInvokers");

        var notificationBefore = notificationServiceTypes.Count;
        var requestBefore = requestServiceTypes.Count;
        var streamBefore = streamServiceTypes.Count;
        var requestInvokersBefore = requestInvokers.Count;
        var requestPipelineInvokersBefore = requestPipelineInvokers.Count;
        var notificationInvokersBefore = notificationInvokers.Count;
        var streamInvokersBefore = streamInvokers.Count;

        await _context!.SendRequestAsync(new DispatcherCacheRequest());
        await _context.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context.PublishAsync(new DispatcherCacheNotification());
        await DrainAsync(_context.CreateStream(new DispatcherCacheStreamRequest()));

        var notificationAfterFirstDispatch = notificationServiceTypes.Count;
        var requestAfterFirstDispatch = requestServiceTypes.Count;
        var streamAfterFirstDispatch = streamServiceTypes.Count;
        var requestInvokersAfterFirstDispatch = requestInvokers.Count;
        var requestPipelineInvokersAfterFirstDispatch = requestPipelineInvokers.Count;
        var notificationInvokersAfterFirstDispatch = notificationInvokers.Count;
        var streamInvokersAfterFirstDispatch = streamInvokers.Count;

        await _context.SendRequestAsync(new DispatcherCacheRequest());
        await _context.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context.PublishAsync(new DispatcherCacheNotification());
        await DrainAsync(_context.CreateStream(new DispatcherCacheStreamRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(notificationAfterFirstDispatch, Is.EqualTo(notificationBefore + 1));
            Assert.That(requestAfterFirstDispatch, Is.EqualTo(requestBefore + 2));
            Assert.That(streamAfterFirstDispatch, Is.EqualTo(streamBefore + 1));
            Assert.That(requestInvokersAfterFirstDispatch, Is.EqualTo(requestInvokersBefore + 1));
            Assert.That(requestPipelineInvokersAfterFirstDispatch, Is.EqualTo(requestPipelineInvokersBefore + 1));
            Assert.That(notificationInvokersAfterFirstDispatch, Is.EqualTo(notificationInvokersBefore + 1));
            Assert.That(streamInvokersAfterFirstDispatch, Is.EqualTo(streamInvokersBefore + 1));

            Assert.That(notificationServiceTypes.Count, Is.EqualTo(notificationAfterFirstDispatch));
            Assert.That(requestServiceTypes.Count, Is.EqualTo(requestAfterFirstDispatch));
            Assert.That(streamServiceTypes.Count, Is.EqualTo(streamAfterFirstDispatch));
            Assert.That(requestInvokers.Count, Is.EqualTo(requestInvokersAfterFirstDispatch));
            Assert.That(requestPipelineInvokers.Count, Is.EqualTo(requestPipelineInvokersAfterFirstDispatch));
            Assert.That(notificationInvokers.Count, Is.EqualTo(notificationInvokersAfterFirstDispatch));
            Assert.That(streamInvokers.Count, Is.EqualTo(streamInvokersAfterFirstDispatch));
        });
    }

    /// <summary>
    ///     通过反射读取 dispatcher 的静态缓存字典。
    /// </summary>
    private static IDictionary GetCacheField(string fieldName)
    {
        var dispatcherType = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!;

        var field = dispatcherType.GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing dispatcher cache field {fieldName}.");

        return field!.GetValue(null) as IDictionary
               ?? throw new InvalidOperationException(
                   $"Dispatcher cache field {fieldName} does not implement IDictionary.");
    }

    /// <summary>
    ///     消费整个异步流，确保建流路径被真实执行。
    /// </summary>
    private static async Task DrainAsync<T>(IAsyncEnumerable<T> stream)
    {
        await foreach (var _ in stream)
        {
        }
    }
}

/// <summary>
///     用于验证 request 服务类型缓存的测试请求。
/// </summary>
internal sealed record DispatcherCacheRequest : IRequest<int>;

/// <summary>
///     用于验证 notification 服务类型缓存的测试通知。
/// </summary>
internal sealed record DispatcherCacheNotification : INotification;

/// <summary>
///     用于验证 stream 服务类型缓存的测试请求。
/// </summary>
internal sealed record DispatcherCacheStreamRequest : IStreamRequest<int>;

/// <summary>
///     用于验证 pipeline invoker 缓存的测试请求。
/// </summary>
internal sealed record DispatcherPipelineCacheRequest : IRequest<int>;

/// <summary>
///     处理 <see cref="DispatcherCacheRequest" />。
/// </summary>
internal sealed class DispatcherCacheRequestHandler : IRequestHandler<DispatcherCacheRequest, int>
{
    /// <summary>
    ///     返回固定结果，供缓存测试验证 dispatcher 请求路径。
    /// </summary>
    public ValueTask<int> Handle(DispatcherCacheRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(1);
    }
}

/// <summary>
///     处理 <see cref="DispatcherCacheNotification" />。
/// </summary>
internal sealed class DispatcherCacheNotificationHandler : INotificationHandler<DispatcherCacheNotification>
{
    /// <summary>
    ///     消费通知，不执行额外副作用。
    /// </summary>
    public ValueTask Handle(DispatcherCacheNotification notification, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     处理 <see cref="DispatcherCacheStreamRequest" />。
/// </summary>
internal sealed class DispatcherCacheStreamHandler : IStreamRequestHandler<DispatcherCacheStreamRequest, int>
{
    /// <summary>
    ///     返回一个最小流，供缓存测试命中 stream 分发路径。
    /// </summary>
    public async IAsyncEnumerable<int> Handle(
        DispatcherCacheStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return 1;
        await Task.CompletedTask;
    }
}

/// <summary>
///     处理 <see cref="DispatcherPipelineCacheRequest" />。
/// </summary>
internal sealed class DispatcherPipelineCacheRequestHandler : IRequestHandler<DispatcherPipelineCacheRequest, int>
{
    /// <summary>
    ///     返回固定结果，供 pipeline 缓存测试使用。
    /// </summary>
    public ValueTask<int> Handle(DispatcherPipelineCacheRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(2);
    }
}

/// <summary>
///     为 <see cref="DispatcherPipelineCacheRequest" /> 提供最小 pipeline 行为，
///     用于命中 dispatcher 的 pipeline invoker 缓存分支。
/// </summary>
internal sealed class DispatcherPipelineCacheBehavior : IPipelineBehavior<DispatcherPipelineCacheRequest, int>
{
    /// <summary>
    ///     直接转发到下一个处理器。
    /// </summary>
    public ValueTask<int> Handle(
        DispatcherPipelineCacheRequest request,
        MessageHandlerDelegate<DispatcherPipelineCacheRequest, int> next,
        CancellationToken cancellationToken)
    {
        return next(request, cancellationToken);
    }
}
