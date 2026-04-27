using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS dispatcher 会缓存热路径中的 dispatch binding。
/// </summary>
[TestFixture]
internal sealed class CqrsDispatcherCacheTests
{
    private MicrosoftDiContainer? _container;
    private ArchitectureContext? _context;

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
        ClearDispatcherCaches();
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

    /// <summary>
    ///     验证相同消息类型重复分发时，不会重复扩张 dispatch binding 缓存。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Dispatch_Bindings_After_First_Dispatch()
    {
        var notificationBindings = GetCacheField("NotificationDispatchBindings");
        var requestBindings = GetCacheField("RequestDispatchBindings");
        var streamBindings = GetCacheField("StreamDispatchBindings");

        Assert.Multiple(() =>
        {
            Assert.That(
                GetSingleKeyCacheValue(notificationBindings, typeof(DispatcherCacheNotification)),
                Is.Null);
            Assert.That(
                GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int)),
                Is.Null);
            Assert.That(
                GetPairCacheValue(requestBindings, typeof(DispatcherPipelineCacheRequest), typeof(int)),
                Is.Null);
            Assert.That(
                GetPairCacheValue(streamBindings, typeof(DispatcherCacheStreamRequest), typeof(int)),
                Is.Null);
        });

        await _context!.SendRequestAsync(new DispatcherCacheRequest());
        await _context.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context.PublishAsync(new DispatcherCacheNotification());
        await DrainAsync(_context.CreateStream(new DispatcherCacheStreamRequest()));

        var notificationAfterFirstDispatch =
            GetSingleKeyCacheValue(notificationBindings, typeof(DispatcherCacheNotification));
        var requestAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int));
        var pipelineAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherPipelineCacheRequest), typeof(int));
        var streamAfterFirstDispatch =
            GetPairCacheValue(streamBindings, typeof(DispatcherCacheStreamRequest), typeof(int));

        await _context.SendRequestAsync(new DispatcherCacheRequest());
        await _context.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context.PublishAsync(new DispatcherCacheNotification());
        await DrainAsync(_context.CreateStream(new DispatcherCacheStreamRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(notificationAfterFirstDispatch, Is.Not.Null);
            Assert.That(requestAfterFirstDispatch, Is.Not.Null);
            Assert.That(pipelineAfterFirstDispatch, Is.Not.Null);
            Assert.That(streamAfterFirstDispatch, Is.Not.Null);

            Assert.That(
                GetSingleKeyCacheValue(notificationBindings, typeof(DispatcherCacheNotification)),
                Is.SameAs(notificationAfterFirstDispatch));
            Assert.That(
                GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int)),
                Is.SameAs(requestAfterFirstDispatch));
            Assert.That(
                GetPairCacheValue(requestBindings, typeof(DispatcherPipelineCacheRequest), typeof(int)),
                Is.SameAs(pipelineAfterFirstDispatch));
            Assert.That(
                GetPairCacheValue(streamBindings, typeof(DispatcherCacheStreamRequest), typeof(int)),
                Is.SameAs(streamAfterFirstDispatch));
        });
    }

    /// <summary>
    ///     验证 request dispatch binding 会按响应类型分别缓存，避免不同响应类型共用 object 结果桥接。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Request_Dispatch_Bindings_Per_Response_Type()
    {
        var requestBindings = GetCacheField("RequestDispatchBindings");

        await _context!.SendRequestAsync(new DispatcherCacheRequest());
        await _context.SendRequestAsync(new DispatcherStringCacheRequest());

        var intAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int));
        var stringAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherStringCacheRequest), typeof(string));

        await _context.SendRequestAsync(new DispatcherCacheRequest());
        await _context.SendRequestAsync(new DispatcherStringCacheRequest());

        Assert.Multiple(() =>
        {
            Assert.That(intAfterFirstDispatch, Is.Not.Null);
            Assert.That(stringAfterFirstDispatch, Is.Not.Null);
            Assert.That(intAfterFirstDispatch, Is.Not.SameAs(stringAfterFirstDispatch));
            Assert.That(
                GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int)),
                Is.SameAs(intAfterFirstDispatch));
            Assert.That(
                GetPairCacheValue(requestBindings, typeof(DispatcherStringCacheRequest), typeof(string)),
                Is.SameAs(stringAfterFirstDispatch));
        });
    }

    /// <summary>
    ///     通过反射读取 dispatcher 的静态缓存对象。
    /// </summary>
    private static object GetCacheField(string fieldName)
    {
        var dispatcherType = GetDispatcherType();
        var field = dispatcherType.GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing dispatcher cache field {fieldName}.");

        return field!.GetValue(null)
               ?? throw new InvalidOperationException(
                   $"Dispatcher cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     清空本测试依赖的 dispatcher 静态缓存，避免跨用例共享进程级状态导致断言漂移。
    /// </summary>
    private static void ClearDispatcherCaches()
    {
        ClearCache(GetCacheField("NotificationDispatchBindings"));
        ClearCache(GetCacheField("RequestDispatchBindings"));
        ClearCache(GetCacheField("StreamDispatchBindings"));
    }

    /// <summary>
    ///     读取单键缓存中当前保存的对象。
    /// </summary>
    private static object? GetSingleKeyCacheValue(object cache, Type key)
    {
        return InvokeInstanceMethod(cache, "GetValueOrDefaultForTesting", key);
    }

    /// <summary>
    ///     读取双键缓存中当前保存的对象。
    /// </summary>
    private static object? GetPairCacheValue(object cache, Type primaryType, Type secondaryType)
    {
        return InvokeInstanceMethod(cache, "GetValueOrDefaultForTesting", primaryType, secondaryType);
    }

    /// <summary>
    ///     调用缓存实例上的无参清理方法。
    /// </summary>
    private static void ClearCache(object cache)
    {
        _ = InvokeInstanceMethod(cache, "Clear");
    }

    /// <summary>
    ///     调用缓存对象上的实例方法。
    /// </summary>
    private static object? InvokeInstanceMethod(object target, string methodName, params object[] arguments)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing cache method {target.GetType().FullName}.{methodName}.");

        return method!.Invoke(target, arguments);
    }

    /// <summary>
    ///     获取 CQRS dispatcher 运行时类型。
    /// </summary>
    private static Type GetDispatcherType()
    {
        return typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!;
    }

    /// <summary>
    ///     消费整个异步流，确保建流路径被真实执行。
    /// </summary>
    private static async Task DrainAsync<T>(IAsyncEnumerable<T> stream)
    {
        await foreach (var _ in stream.ConfigureAwait(false))
        {
        }
    }
}
