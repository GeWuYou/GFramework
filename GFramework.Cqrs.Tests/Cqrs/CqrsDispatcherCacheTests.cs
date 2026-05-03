// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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
[NonParallelizable]
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
        _container.RegisterCqrsPipelineBehavior<DispatcherPipelineContextRefreshBehavior>();
        _container.RegisterCqrsPipelineBehavior<DispatcherPipelineOrderOuterBehavior>();
        _container.RegisterCqrsPipelineBehavior<DispatcherPipelineOrderInnerBehavior>();

        CqrsTestRuntime.RegisterHandlers(
            _container,
            typeof(CqrsDispatcherCacheTests).Assembly,
            typeof(ArchitectureContext).Assembly);

        _container.Freeze();
        _context = new ArchitectureContext(_container);
        DispatcherNotificationContextRefreshState.Reset();
        DispatcherPipelineContextRefreshState.Reset();
        DispatcherStreamContextRefreshState.Reset();
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
    ///     验证 request pipeline executor 会按行为数量在 binding 内首次创建并在后续分发中复用。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Request_Pipeline_Executors_Per_Behavior_Count()
    {
        var requestBindings = GetCacheField("RequestDispatchBindings");

        Assert.Multiple(() =>
        {
            Assert.That(
                GetRequestPipelineExecutorValue(
                    requestBindings,
                    typeof(DispatcherPipelineCacheRequest),
                    typeof(int),
                    1),
                Is.Null);
            Assert.That(
                GetRequestPipelineExecutorValue(
                    requestBindings,
                    typeof(DispatcherPipelineOrderCacheRequest),
                    typeof(int),
                    2),
                Is.Null);
        });

        await _context!.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());

        var singleBehaviorExecutor = GetRequestPipelineExecutorValue(
            requestBindings,
            typeof(DispatcherPipelineCacheRequest),
            typeof(int),
            1);
        var twoBehaviorExecutor = GetRequestPipelineExecutorValue(
            requestBindings,
            typeof(DispatcherPipelineOrderCacheRequest),
            typeof(int),
            2);

        await _context.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());

        Assert.Multiple(() =>
        {
            Assert.That(singleBehaviorExecutor, Is.Not.Null);
            Assert.That(twoBehaviorExecutor, Is.Not.Null);
            Assert.That(singleBehaviorExecutor, Is.Not.SameAs(twoBehaviorExecutor));
            Assert.That(
                GetRequestPipelineExecutorValue(
                    requestBindings,
                    typeof(DispatcherPipelineCacheRequest),
                    typeof(int),
                    1),
                Is.SameAs(singleBehaviorExecutor));
            Assert.That(
                GetRequestPipelineExecutorValue(
                    requestBindings,
                    typeof(DispatcherPipelineOrderCacheRequest),
                    typeof(int),
                    2),
                Is.SameAs(twoBehaviorExecutor));
        });
    }

    /// <summary>
    ///     验证复用缓存的 request pipeline executor 后，行为顺序和最终处理器顺序保持不变。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Preserve_Request_Pipeline_Order_When_Reusing_Cached_Executor()
    {
        DispatcherPipelineOrderState.Reset();

        await _context!.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());
        var firstInvocation = DispatcherPipelineOrderState.Steps.ToArray();

        DispatcherPipelineOrderState.Reset();

        await _context.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());
        var secondInvocation = DispatcherPipelineOrderState.Steps.ToArray();

        var expectedOrder = new[]
        {
            "Outer:Before",
            "Inner:Before",
            "Handler",
            "Inner:After",
            "Outer:After"
        };

        Assert.Multiple(() =>
        {
            Assert.That(firstInvocation, Is.EqualTo(expectedOrder));
            Assert.That(secondInvocation, Is.EqualTo(expectedOrder));
        });
    }

    /// <summary>
    ///     验证缓存的 request pipeline executor 在重复分发时仍会重新解析 handler/behavior，
    ///     并为当次实例重新注入当前架构上下文。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Reinject_Current_Context_When_Reusing_Cached_Request_Pipeline_Executor()
    {
        DispatcherPipelineContextRefreshState.Reset();

        var requestBindings = GetCacheField("RequestDispatchBindings");
        var firstContext = new ArchitectureContext(_container!);
        var secondContext = new ArchitectureContext(_container!);

        await firstContext.SendRequestAsync(new DispatcherPipelineContextRefreshRequest("first"));

        var executorAfterFirstDispatch = GetRequestPipelineExecutorValue(
            requestBindings,
            typeof(DispatcherPipelineContextRefreshRequest),
            typeof(int),
            1);

        await secondContext.SendRequestAsync(new DispatcherPipelineContextRefreshRequest("second"));

        var executorAfterSecondDispatch = GetRequestPipelineExecutorValue(
            requestBindings,
            typeof(DispatcherPipelineContextRefreshRequest),
            typeof(int),
            1);
        var behaviorSnapshots = DispatcherPipelineContextRefreshState.BehaviorSnapshots.ToArray();
        var handlerSnapshots = DispatcherPipelineContextRefreshState.HandlerSnapshots.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(executorAfterFirstDispatch, Is.Not.Null);
            Assert.That(executorAfterSecondDispatch, Is.SameAs(executorAfterFirstDispatch));

            Assert.That(behaviorSnapshots, Has.Length.EqualTo(2));
            Assert.That(handlerSnapshots, Has.Length.EqualTo(2));

            Assert.That(behaviorSnapshots[0].DispatchId, Is.EqualTo("first"));
            Assert.That(behaviorSnapshots[0].Context, Is.SameAs(firstContext));
            Assert.That(behaviorSnapshots[1].DispatchId, Is.EqualTo("second"));
            Assert.That(behaviorSnapshots[1].Context, Is.SameAs(secondContext));
            Assert.That(behaviorSnapshots[1].Context, Is.Not.SameAs(behaviorSnapshots[0].Context));

            Assert.That(handlerSnapshots[0].DispatchId, Is.EqualTo("first"));
            Assert.That(handlerSnapshots[0].Context, Is.SameAs(firstContext));
            Assert.That(handlerSnapshots[1].DispatchId, Is.EqualTo("second"));
            Assert.That(handlerSnapshots[1].Context, Is.SameAs(secondContext));
            Assert.That(handlerSnapshots[1].Context, Is.Not.SameAs(handlerSnapshots[0].Context));
            Assert.That(handlerSnapshots[1].InstanceId, Is.Not.EqualTo(handlerSnapshots[0].InstanceId));
        });
    }

    /// <summary>
    ///     验证缓存的 notification dispatch binding 在重复分发时仍会重新解析 handler，
    ///     并为当次实例重新注入当前架构上下文。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Reinject_Current_Context_When_Reusing_Cached_Notification_Dispatch_Binding()
    {
        DispatcherNotificationContextRefreshState.Reset();

        var notificationBindings = GetCacheField("NotificationDispatchBindings");
        var firstContext = new ArchitectureContext(_container!);
        var secondContext = new ArchitectureContext(_container!);

        await firstContext.PublishAsync(new DispatcherNotificationContextRefreshNotification("first"));

        var bindingAfterFirstDispatch = GetSingleKeyCacheValue(
            notificationBindings,
            typeof(DispatcherNotificationContextRefreshNotification));

        await secondContext.PublishAsync(new DispatcherNotificationContextRefreshNotification("second"));

        var bindingAfterSecondDispatch = GetSingleKeyCacheValue(
            notificationBindings,
            typeof(DispatcherNotificationContextRefreshNotification));
        var handlerSnapshots = DispatcherNotificationContextRefreshState.HandlerSnapshots.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(bindingAfterFirstDispatch, Is.Not.Null);
            Assert.That(bindingAfterSecondDispatch, Is.SameAs(bindingAfterFirstDispatch));

            Assert.That(handlerSnapshots, Has.Length.EqualTo(2));
            Assert.That(handlerSnapshots[0].DispatchId, Is.EqualTo("first"));
            Assert.That(handlerSnapshots[0].Context, Is.SameAs(firstContext));
            Assert.That(handlerSnapshots[1].DispatchId, Is.EqualTo("second"));
            Assert.That(handlerSnapshots[1].Context, Is.SameAs(secondContext));
            Assert.That(handlerSnapshots[1].Context, Is.Not.SameAs(handlerSnapshots[0].Context));
            Assert.That(handlerSnapshots[1].InstanceId, Is.Not.EqualTo(handlerSnapshots[0].InstanceId));
        });
    }

    /// <summary>
    ///     验证缓存的 stream dispatch binding 在重复建流时仍会重新解析 handler，
    ///     并为当次实例重新注入当前架构上下文。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Reinject_Current_Context_When_Reusing_Cached_Stream_Dispatch_Binding()
    {
        DispatcherStreamContextRefreshState.Reset();

        var streamBindings = GetCacheField("StreamDispatchBindings");
        var firstContext = new ArchitectureContext(_container!);
        var secondContext = new ArchitectureContext(_container!);

        var firstStream = firstContext.CreateStream(new DispatcherStreamContextRefreshRequest("first"));
        await DrainAsync(firstStream);

        var bindingAfterFirstDispatch = GetPairCacheValue(
            streamBindings,
            typeof(DispatcherStreamContextRefreshRequest),
            typeof(int));

        var secondStream = secondContext.CreateStream(new DispatcherStreamContextRefreshRequest("second"));
        await DrainAsync(secondStream);

        var bindingAfterSecondDispatch = GetPairCacheValue(
            streamBindings,
            typeof(DispatcherStreamContextRefreshRequest),
            typeof(int));
        var handlerSnapshots = DispatcherStreamContextRefreshState.HandlerSnapshots.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(bindingAfterFirstDispatch, Is.Not.Null);
            Assert.That(bindingAfterSecondDispatch, Is.SameAs(bindingAfterFirstDispatch));

            Assert.That(handlerSnapshots, Has.Length.EqualTo(2));
            Assert.That(handlerSnapshots[0].DispatchId, Is.EqualTo("first"));
            Assert.That(handlerSnapshots[0].Context, Is.SameAs(firstContext));
            Assert.That(handlerSnapshots[1].DispatchId, Is.EqualTo("second"));
            Assert.That(handlerSnapshots[1].Context, Is.SameAs(secondContext));
            Assert.That(handlerSnapshots[1].Context, Is.Not.SameAs(handlerSnapshots[0].Context));
            Assert.That(handlerSnapshots[1].InstanceId, Is.Not.EqualTo(handlerSnapshots[0].InstanceId));
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
    ///     读取 request dispatch binding 中指定行为数量的 pipeline executor 缓存项。
    /// </summary>
    /// <param name="requestBindings">dispatcher 内部的 request binding 缓存对象。</param>
    /// <param name="requestType">要读取的请求运行时类型。</param>
    /// <param name="responseType">要读取的响应运行时类型。</param>
    /// <param name="behaviorCount">目标 executor 对应的行为数量。</param>
    /// <returns>已缓存的 executor；若 binding 或 executor 尚未建立则返回 <see langword="null" />。</returns>
    private static object? GetRequestPipelineExecutorValue(
        object requestBindings,
        Type requestType,
        Type responseType,
        int behaviorCount)
    {
        var binding = GetRequestDispatchBindingValue(requestBindings, requestType, responseType);
        return binding is null
            ? null
            : InvokeInstanceMethod(binding, "GetPipelineExecutorForTesting", behaviorCount);
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
    ///     读取指定请求/响应类型对对应的强类型 request dispatch binding。
    /// </summary>
    /// <param name="requestBindings">dispatcher 内部的 request binding 缓存对象。</param>
    /// <param name="requestType">要读取的请求运行时类型。</param>
    /// <param name="responseType">要读取的响应运行时类型。</param>
    /// <returns>强类型 binding；若缓存尚未建立则返回 <see langword="null" />。</returns>
    private static object? GetRequestDispatchBindingValue(object requestBindings, Type requestType, Type responseType)
    {
        var bindingBox = GetPairCacheValue(requestBindings, requestType, responseType);
        if (bindingBox is null)
        {
            return null;
        }

        var method = bindingBox.GetType().GetMethod(
            "Get",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing request binding accessor on {bindingBox.GetType().FullName}.");

        return method!
            .MakeGenericMethod(responseType)
            .Invoke(bindingBox, Array.Empty<object>());
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
