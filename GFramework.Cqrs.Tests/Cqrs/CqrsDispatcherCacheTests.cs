// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 CQRS dispatcher 会缓存热路径中的 dispatch binding。
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class CqrsDispatcherCacheTests
{
    private ServiceTestHost? _container;
    private CqrsTestHost? _context;

    /// <summary>
    ///     初始化测试上下文。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _container = new ServiceTestHost();
        ConfigureDispatcherCacheFixture(_container);

        _container.Freeze();
        _context = new CqrsTestHost(_container);
        DispatcherStreamPipelineOrderState.Reset();
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
        await _context!.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context!.PublishAsync(new DispatcherCacheNotification());
        await DrainAsync(_context!.CreateStream(new DispatcherCacheStreamRequest()));

        var notificationAfterFirstDispatch =
            GetSingleKeyCacheValue(notificationBindings, typeof(DispatcherCacheNotification));
        var requestAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int));
        var pipelineAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherPipelineCacheRequest), typeof(int));
        var streamAfterFirstDispatch =
            GetPairCacheValue(streamBindings, typeof(DispatcherCacheStreamRequest), typeof(int));

        await _context!.SendRequestAsync(new DispatcherCacheRequest());
        await _context!.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context!.PublishAsync(new DispatcherCacheNotification());
        await DrainAsync(_context!.CreateStream(new DispatcherCacheStreamRequest()));

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
        await _context!.SendRequestAsync(new DispatcherStringCacheRequest());

        var intAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherCacheRequest), typeof(int));
        var stringAfterFirstDispatch =
            GetPairCacheValue(requestBindings, typeof(DispatcherStringCacheRequest), typeof(string));

        await _context!.SendRequestAsync(new DispatcherCacheRequest());
        await _context!.SendRequestAsync(new DispatcherStringCacheRequest());

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
    ///     验证 request 的“是否存在 pipeline behavior”判定会按 dispatcher 实例缓存，
    ///     让零行为请求在首次分发后不再重复查询容器，同时不同 dispatcher 不共享该实例级状态。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Zero_Pipeline_Request_Presence_Per_Dispatcher_Instance()
    {
        var firstContext = new CqrsTestHost(_container!);
        var secondContext = new CqrsTestHost(_container!);
        var firstDispatcher = GetDispatcherFromContext(firstContext);
        var secondDispatcher = GetDispatcherFromContext(secondContext);
        await using var isolatedContainer = CreateFrozenContainer();
        var isolatedContext = new CqrsTestHost(isolatedContainer);
        var isolatedDispatcher = GetDispatcherFromContext(isolatedContext);

        AssertRequestBehaviorPresenceIsUnset(firstDispatcher, typeof(IPipelineBehavior<DispatcherCacheRequest, int>));
        AssertRequestBehaviorPresenceIsUnset(secondDispatcher, typeof(IPipelineBehavior<DispatcherCacheRequest, int>));
        AssertRequestBehaviorPresenceIsUnset(isolatedDispatcher, typeof(IPipelineBehavior<DispatcherCacheRequest, int>));
        AssertRequestBehaviorPresenceIsUnset(
            firstDispatcher,
            typeof(IPipelineBehavior<DispatcherPipelineCacheRequest, int>));

        await firstContext.SendRequestAsync(new DispatcherCacheRequest());
        await firstContext.SendRequestAsync(new DispatcherPipelineCacheRequest());

        var zeroPipelinePresence = GetRequestBehaviorPresenceCacheValue(
            firstDispatcher,
            typeof(IPipelineBehavior<DispatcherCacheRequest, int>));
        var onePipelinePresence = GetRequestBehaviorPresenceCacheValue(
            firstDispatcher,
            typeof(IPipelineBehavior<DispatcherPipelineCacheRequest, int>));

        AssertSharedDispatcherCacheState(
            firstDispatcher,
            secondDispatcher,
            isolatedDispatcher,
            zeroPipelinePresence,
            onePipelinePresence);

        await isolatedContext.SendRequestAsync(new DispatcherCacheRequest());

        AssertRequestBehaviorPresenceEquals(
            isolatedDispatcher,
            typeof(IPipelineBehavior<DispatcherCacheRequest, int>),
expected: false);
    }

    /// <summary>
    ///     验证 stream 的“是否存在 pipeline behavior”判定会按 dispatcher 实例缓存，
    ///     并与当前容器的实际服务可见性保持一致，同时不同 dispatcher 不共享该实例级状态。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Stream_Behavior_Presence_Per_Dispatcher_Instance()
    {
        var firstContext = new CqrsTestHost(_container!);
        var secondContext = new CqrsTestHost(_container!);
        var firstDispatcher = GetDispatcherFromContext(firstContext);
        var secondDispatcher = GetDispatcherFromContext(secondContext);
        await using var isolatedContainer = CreateFrozenContainer();
        var isolatedContext = new CqrsTestHost(isolatedContainer);
        var isolatedDispatcher = GetDispatcherFromContext(isolatedContext);
        var zeroPipelineBehaviorType = typeof(IStreamPipelineBehavior<DispatcherZeroPipelineStreamRequest, int>);
        var twoPipelineBehaviorType = typeof(IStreamPipelineBehavior<DispatcherStreamPipelineOrderRequest, int>);
        var expectedZeroPipelinePresence = _container!.HasRegistration(zeroPipelineBehaviorType);
        var expectedTwoPipelinePresence = _container.HasRegistration(twoPipelineBehaviorType);

        AssertStreamBehaviorPresenceIsUnset(firstDispatcher, zeroPipelineBehaviorType);
        AssertStreamBehaviorPresenceIsUnset(secondDispatcher, zeroPipelineBehaviorType);
        AssertStreamBehaviorPresenceIsUnset(isolatedDispatcher, zeroPipelineBehaviorType);
        AssertStreamBehaviorPresenceIsUnset(firstDispatcher, twoPipelineBehaviorType);

        await DrainAsync(firstContext.CreateStream(new DispatcherZeroPipelineStreamRequest()));
        await DrainAsync(firstContext.CreateStream(new DispatcherStreamPipelineOrderRequest()));

        var zeroPipelinePresence = GetStreamBehaviorPresenceCacheValue(
            firstDispatcher,
            zeroPipelineBehaviorType);
        var twoPipelinePresence = GetStreamBehaviorPresenceCacheValue(
            firstDispatcher,
            twoPipelineBehaviorType);

        AssertSharedStreamDispatcherCacheState(
            firstDispatcher,
            secondDispatcher,
            isolatedDispatcher,
            zeroPipelinePresence,
            twoPipelinePresence,
            zeroPipelineBehaviorType,
            expectedZeroPipelinePresence,
            expectedTwoPipelinePresence);

        await DrainAsync(isolatedContext.CreateStream(new DispatcherZeroPipelineStreamRequest()));

        AssertStreamBehaviorPresenceEquals(isolatedDispatcher, zeroPipelineBehaviorType, expectedZeroPipelinePresence);
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
        await _context!.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());

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

        await _context!.SendRequestAsync(new DispatcherPipelineCacheRequest());
        await _context!.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());

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
    ///     验证同一 request dispatch binding 先走零行为直连路径时不会提前创建 pipeline executor，
    ///     后续另一 dispatcher 命中相同 binding 且存在行为时，仍会按实际行为数量补建缓存 executor。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Create_Request_Pipeline_Executor_Only_When_Shared_Binding_Sees_Behaviors()
    {
        var requestBindings = GetCacheField("RequestDispatchBindings");
        var behaviorType = typeof(IPipelineBehavior<DispatcherPipelineCacheRequest, int>);
        await using var zeroBehaviorContainer = CreateFrozenContainer(
            new DispatcherCacheFixtureOptions
            {
                IncludeRequestPipelineCacheBehavior = false,
            });
        var zeroBehaviorContext = new CqrsTestHost(zeroBehaviorContainer);
        var behaviorContext = new CqrsTestHost(_container!);
        var zeroBehaviorDispatcher = GetDispatcherFromContext(zeroBehaviorContext);
        var behaviorDispatcher = GetDispatcherFromContext(behaviorContext);

        await zeroBehaviorContext.SendRequestAsync(new DispatcherPipelineCacheRequest());

        var bindingAfterZeroBehaviorDispatch = GetPairCacheValue(
            requestBindings,
            typeof(DispatcherPipelineCacheRequest),
            typeof(int));
        var executorAfterZeroBehaviorDispatch = GetRequestPipelineExecutorValue(
            requestBindings,
            typeof(DispatcherPipelineCacheRequest),
            typeof(int),
            1);

        await behaviorContext.SendRequestAsync(new DispatcherPipelineCacheRequest());

        var bindingAfterBehaviorDispatch = GetPairCacheValue(
            requestBindings,
            typeof(DispatcherPipelineCacheRequest),
            typeof(int));
        var executorAfterBehaviorDispatch = GetRequestPipelineExecutorValue(
            requestBindings,
            typeof(DispatcherPipelineCacheRequest),
            typeof(int),
            1);

        Assert.Multiple(() =>
        {
            Assert.That(bindingAfterZeroBehaviorDispatch, Is.Not.Null);
            Assert.That(bindingAfterBehaviorDispatch, Is.SameAs(bindingAfterZeroBehaviorDispatch));
            Assert.That(executorAfterZeroBehaviorDispatch, Is.Null);
            Assert.That(executorAfterBehaviorDispatch, Is.Not.Null);
            AssertRequestBehaviorPresenceEquals(zeroBehaviorDispatcher, behaviorType, expected: false);
            AssertRequestBehaviorPresenceEquals(behaviorDispatcher, behaviorType, expected: true);
        });
    }

    /// <summary>
    ///     验证 stream pipeline executor 会按行为数量在 binding 内首次创建并在后续建流中复用。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Cache_Stream_Pipeline_Executors_Per_Behavior_Count()
    {
        var streamBindings = GetCacheField("StreamDispatchBindings");

        Assert.Multiple(() =>
        {
            Assert.That(
                GetStreamPipelineExecutorValue(
                    streamBindings,
                    typeof(DispatcherCacheStreamRequest),
                    typeof(int),
                    1),
                Is.Null);
            Assert.That(
                GetStreamPipelineExecutorValue(
                    streamBindings,
                    typeof(DispatcherStreamPipelineOrderRequest),
                    typeof(int),
                    2),
                Is.Null);
        });

        await DrainAsync(_context!.CreateStream(new DispatcherCacheStreamRequest()));
        await DrainAsync(_context!.CreateStream(new DispatcherStreamPipelineOrderRequest()));

        var singleBehaviorExecutor = GetStreamPipelineExecutorValue(
            streamBindings,
            typeof(DispatcherCacheStreamRequest),
            typeof(int),
            1);
        var twoBehaviorExecutor = GetStreamPipelineExecutorValue(
            streamBindings,
            typeof(DispatcherStreamPipelineOrderRequest),
            typeof(int),
            2);

        await DrainAsync(_context!.CreateStream(new DispatcherCacheStreamRequest()));
        await DrainAsync(_context!.CreateStream(new DispatcherStreamPipelineOrderRequest()));

        Assert.Multiple(() =>
        {
            Assert.That(singleBehaviorExecutor, Is.Not.Null);
            Assert.That(twoBehaviorExecutor, Is.Not.Null);
            Assert.That(singleBehaviorExecutor, Is.Not.SameAs(twoBehaviorExecutor));
            Assert.That(
                GetStreamPipelineExecutorValue(
                    streamBindings,
                    typeof(DispatcherCacheStreamRequest),
                    typeof(int),
                    1),
                Is.SameAs(singleBehaviorExecutor));
            Assert.That(
                GetStreamPipelineExecutorValue(
                    streamBindings,
                    typeof(DispatcherStreamPipelineOrderRequest),
                    typeof(int),
                    2),
                Is.SameAs(twoBehaviorExecutor));
        });
    }

    /// <summary>
    ///     验证同一 stream dispatch binding 先走零行为直连路径时不会提前创建 pipeline executor，
    ///     后续另一 dispatcher 命中相同 binding 且存在行为时，仍会按实际行为数量补建缓存 executor。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Create_Stream_Pipeline_Executor_Only_When_Shared_Binding_Sees_Behaviors()
    {
        var streamBindings = GetCacheField("StreamDispatchBindings");
        var behaviorType = typeof(IStreamPipelineBehavior<DispatcherCacheStreamRequest, int>);
        await using var zeroBehaviorContainer = CreateFrozenContainer(
            new DispatcherCacheFixtureOptions
            {
                IncludeStreamPipelineCacheBehavior = false,
            });
        var zeroBehaviorContext = new CqrsTestHost(zeroBehaviorContainer);
        var behaviorContext = new CqrsTestHost(_container!);
        var zeroBehaviorDispatcher = GetDispatcherFromContext(zeroBehaviorContext);
        var behaviorDispatcher = GetDispatcherFromContext(behaviorContext);

        await DrainAsync(zeroBehaviorContext.CreateStream(new DispatcherCacheStreamRequest()));

        var bindingAfterZeroBehaviorDispatch = GetPairCacheValue(
            streamBindings,
            typeof(DispatcherCacheStreamRequest),
            typeof(int));
        var executorAfterZeroBehaviorDispatch = GetStreamPipelineExecutorValue(
            streamBindings,
            typeof(DispatcherCacheStreamRequest),
            typeof(int),
            1);

        await DrainAsync(behaviorContext.CreateStream(new DispatcherCacheStreamRequest()));

        var bindingAfterBehaviorDispatch = GetPairCacheValue(
            streamBindings,
            typeof(DispatcherCacheStreamRequest),
            typeof(int));
        var executorAfterBehaviorDispatch = GetStreamPipelineExecutorValue(
            streamBindings,
            typeof(DispatcherCacheStreamRequest),
            typeof(int),
            1);

        Assert.Multiple(() =>
        {
            Assert.That(bindingAfterZeroBehaviorDispatch, Is.Not.Null);
            Assert.That(bindingAfterBehaviorDispatch, Is.SameAs(bindingAfterZeroBehaviorDispatch));
            Assert.That(executorAfterZeroBehaviorDispatch, Is.Null);
            Assert.That(executorAfterBehaviorDispatch, Is.Not.Null);
            AssertStreamBehaviorPresenceEquals(zeroBehaviorDispatcher, behaviorType, expected: false);
            AssertStreamBehaviorPresenceEquals(behaviorDispatcher, behaviorType, expected: true);
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

        await _context!.SendRequestAsync(new DispatcherPipelineOrderCacheRequest());
        var secondInvocation = DispatcherPipelineOrderState.Steps.ToArray();

        var expectedOrder = new[]
        {
            "Outer:Before",
            "Inner:Before",
            "Handler",
            "Inner:After",
            "Outer:After",
        };

        Assert.Multiple(() =>
        {
            Assert.That(firstInvocation, Is.EqualTo(expectedOrder));
            Assert.That(secondInvocation, Is.EqualTo(expectedOrder));
        });
    }

    /// <summary>
    ///     验证复用缓存的 stream pipeline executor 后，行为顺序和最终处理器顺序保持不变。
    /// </summary>
    [Test]
    public async Task Dispatcher_Should_Preserve_Stream_Pipeline_Order_When_Reusing_Cached_Executor()
    {
        DispatcherStreamPipelineOrderState.Reset();

        await DrainAsync(_context!.CreateStream(new DispatcherStreamPipelineOrderRequest()));
        var firstInvocation = DispatcherStreamPipelineOrderState.Steps.ToArray();

        DispatcherStreamPipelineOrderState.Reset();

        await DrainAsync(_context!.CreateStream(new DispatcherStreamPipelineOrderRequest()));
        var secondInvocation = DispatcherStreamPipelineOrderState.Steps.ToArray();

        var expectedOrder = new[]
        {
            "Outer:Before",
            "Inner:Before",
            "Handler",
            "Inner:After",
            "Outer:After",
        };

        Assert.Multiple(() =>
        {
            Assert.That(firstInvocation, Is.EqualTo(expectedOrder));
            Assert.That(secondInvocation, Is.EqualTo(expectedOrder));
        });
    }


    /// <summary>
    ///     描述缓存测试 fixture 需要启用的可选 pipeline 行为集合，
    ///     用于构造“同一静态 binding 对应不同 dispatcher 注册可见性”的组合场景。
    /// </summary>
    private sealed class DispatcherCacheFixtureOptions
    {
        /// <summary>
        ///     获取是否注册 <see cref="DispatcherPipelineCacheBehavior" />。
        /// </summary>
        public bool IncludeRequestPipelineCacheBehavior { get; init; } = true;


        /// <summary>
        ///     获取是否注册 request 顺序验证所需的两层 pipeline 行为。
        /// </summary>
        public bool IncludeRequestPipelineOrderBehaviors { get; init; } = true;

        /// <summary>
        ///     获取是否注册 <see cref="DispatcherStreamPipelineCacheBehavior" />。
        /// </summary>
        public bool IncludeStreamPipelineCacheBehavior { get; init; } = true;


        /// <summary>
        ///     获取是否注册 stream 顺序验证所需的两层 pipeline 行为。
        /// </summary>
        public bool IncludeStreamPipelineOrderBehaviors { get; init; } = true;
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
    ///     从架构上下文绑定的容器中解析当前 CQRS runtime 单例，
    ///     便于验证其实例级热路径缓存；runtime 在 provider 构建后由标准 DI 惰性创建。
    /// </summary>
    private static object GetDispatcherFromContext(CqrsTestHost context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetService<ICqrsRuntime>()
               ?? throw new InvalidOperationException("Resolved CQRS runtime instance was null.");
    }

    /// <summary>
    ///     创建与当前 fixture 注册形状一致、但拥有独立 runtime 实例的冻结容器，
    ///     用于验证 dispatcher 的实例级缓存不会跨容器共享。
    /// </summary>
    /// <param name="options">控制当前隔离容器要启用哪些可选 pipeline 行为的配置。</param>
    private static ServiceTestHost CreateFrozenContainer(DispatcherCacheFixtureOptions? options = null)
    {
        var container = new ServiceTestHost();
        ConfigureDispatcherCacheFixture(container, options);

        container.Freeze();
        return container;
    }

    /// <summary>
    ///     组装当前 fixture 依赖的 CQRS 容器注册形状，确保默认上下文与隔离容器复用同一份装配基线。
    /// </summary>
    /// <param name="container">待补齐 CQRS 注册的目标容器。</param>
    /// <param name="options">控制是否跳过特定 pipeline 行为注册的可选配置。</param>
    private static void ConfigureDispatcherCacheFixture(
        ServiceTestHost container,
        DispatcherCacheFixtureOptions? options = null)
    {
        options ??= new DispatcherCacheFixtureOptions();

        if (options.IncludeRequestPipelineCacheBehavior)
        {
            container.Services.AddCqrsPipelineBehavior<DispatcherPipelineCacheBehavior>();
        }


        if (options.IncludeRequestPipelineOrderBehaviors)
        {
            container.Services.AddCqrsPipelineBehavior<DispatcherPipelineOrderOuterBehavior>();
            container.Services.AddCqrsPipelineBehavior<DispatcherPipelineOrderInnerBehavior>();
        }

        if (options.IncludeStreamPipelineCacheBehavior)
        {
            container.Services.AddCqrsStreamPipelineBehavior<DispatcherStreamPipelineCacheBehavior>();
        }


        if (options.IncludeStreamPipelineOrderBehaviors)
        {
            container.Services.AddCqrsStreamPipelineBehavior<DispatcherStreamPipelineOrderOuterBehavior>();
            container.Services.AddCqrsStreamPipelineBehavior<DispatcherStreamPipelineOrderInnerBehavior>();
        }

        CqrsTestRuntime.RegisterHandlers(
            container,
            typeof(CqrsDispatcherCacheTests).Assembly,
            typeof(CqrsTestHost).Assembly);
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
    ///     读取指定 dispatcher 实例中当前保存的 request behavior presence 缓存项。
    /// </summary>
    private static object? GetRequestBehaviorPresenceCacheValue(object dispatcher, Type behaviorType)
    {
        var field = dispatcher.GetType().GetField(
            "_requestBehaviorPresenceCache",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "Missing dispatcher request behavior presence cache field.");

        var cache = field!.GetValue(dispatcher)
                   ?? throw new InvalidOperationException(
                       "Dispatcher request behavior presence cache returned null.");
        var tryGetValueMethod = cache.GetType().GetMethod(
            "TryGetValue",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(tryGetValueMethod, Is.Not.Null, "Missing ConcurrentDictionary.TryGetValue accessor.");

        object?[] arguments = [behaviorType, null];
        var found = (bool)(tryGetValueMethod!.Invoke(cache, arguments)
                           ?? throw new InvalidOperationException(
                               "ConcurrentDictionary.TryGetValue returned null."));
        return found ? arguments[1] : null;
    }

    /// <summary>
    ///     读取指定 dispatcher 实例中当前保存的 stream behavior presence 缓存项。
    /// </summary>
    private static object? GetStreamBehaviorPresenceCacheValue(object dispatcher, Type behaviorType)
    {
        var field = dispatcher.GetType().GetField(
            "_streamBehaviorPresenceCache",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "Missing dispatcher stream behavior presence cache field.");

        var cache = field!.GetValue(dispatcher)
                   ?? throw new InvalidOperationException(
                       "Dispatcher stream behavior presence cache returned null.");
        var tryGetValueMethod = cache.GetType().GetMethod(
            "TryGetValue",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(tryGetValueMethod, Is.Not.Null, "Missing ConcurrentDictionary.TryGetValue accessor.");

        object?[] arguments = [behaviorType, null];
        var found = (bool)(tryGetValueMethod!.Invoke(cache, arguments)
                           ?? throw new InvalidOperationException(
                               "ConcurrentDictionary.TryGetValue returned null."));
        return found ? arguments[1] : null;
    }

    /// <summary>
    ///     断言指定 dispatcher 上某个 request behavior presence 缓存项尚未建立。
    /// </summary>
    private static void AssertRequestBehaviorPresenceIsUnset(object dispatcher, Type behaviorType)
    {
        Assert.That(GetRequestBehaviorPresenceCacheValue(dispatcher, behaviorType), Is.Null);
    }

    /// <summary>
    ///     断言指定 dispatcher 上某个 request behavior presence 缓存项等于预期值。
    /// </summary>
    private static void AssertRequestBehaviorPresenceEquals(object dispatcher, Type behaviorType, bool expected)
    {
        Assert.That(GetRequestBehaviorPresenceCacheValue(dispatcher, behaviorType), Is.EqualTo(expected));
    }

    /// <summary>
    ///     断言指定 dispatcher 上某个 stream behavior presence 缓存项尚未建立。
    /// </summary>
    private static void AssertStreamBehaviorPresenceIsUnset(object dispatcher, Type behaviorType)
    {
        Assert.That(GetStreamBehaviorPresenceCacheValue(dispatcher, behaviorType), Is.Null);
    }

    /// <summary>
    ///     断言指定 dispatcher 上某个 stream behavior presence 缓存项等于预期值。
    /// </summary>
    private static void AssertStreamBehaviorPresenceEquals(object dispatcher, Type behaviorType, bool expected)
    {
        Assert.That(GetStreamBehaviorPresenceCacheValue(dispatcher, behaviorType), Is.EqualTo(expected));
    }

    /// <summary>
    ///     断言同一容器解析出的 dispatcher 会共享实例级缓存，而另一独立容器的 dispatcher 不会提前命中。
    /// </summary>
    private static void AssertSharedDispatcherCacheState(
        object firstDispatcher,
        object secondDispatcher,
        object isolatedDispatcher,
        object? zeroPipelinePresence,
        object? onePipelinePresence)
    {
        Assert.Multiple(() =>
        {
            Assert.That(secondDispatcher, Is.SameAs(firstDispatcher));
            Assert.That(zeroPipelinePresence, Is.EqualTo(expected: false));
            Assert.That(onePipelinePresence, Is.EqualTo(expected: true));
            AssertRequestBehaviorPresenceEquals(
                secondDispatcher,
                typeof(IPipelineBehavior<DispatcherCacheRequest, int>),
expected: false);
            AssertRequestBehaviorPresenceIsUnset(
                isolatedDispatcher,
                typeof(IPipelineBehavior<DispatcherCacheRequest, int>));
        });
    }

    /// <summary>
    ///     断言同一容器解析出的 dispatcher 会共享 stream 的实例级缓存，而另一独立容器的 dispatcher 不会提前命中。
    /// </summary>
    private static void AssertSharedStreamDispatcherCacheState(
        object firstDispatcher,
        object secondDispatcher,
        object isolatedDispatcher,
        object? zeroPipelinePresence,
        object? twoPipelinePresence,
        Type zeroPipelineBehaviorType,
        bool expectedZeroPipelinePresence,
        bool expectedTwoPipelinePresence)
    {
        Assert.Multiple(() =>
        {
            Assert.That(secondDispatcher, Is.SameAs(firstDispatcher));
            Assert.That(zeroPipelinePresence, Is.EqualTo(expectedZeroPipelinePresence));
            Assert.That(twoPipelinePresence, Is.EqualTo(expectedTwoPipelinePresence));
            AssertStreamBehaviorPresenceEquals(secondDispatcher, zeroPipelineBehaviorType, expectedZeroPipelinePresence);
            AssertStreamBehaviorPresenceIsUnset(isolatedDispatcher, zeroPipelineBehaviorType);
        });
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
    ///     读取 stream dispatch binding 中指定行为数量的 pipeline executor 缓存项。
    /// </summary>
    /// <param name="streamBindings">dispatcher 内部的 stream binding 缓存对象。</param>
    /// <param name="requestType">要读取的流式请求运行时类型。</param>
    /// <param name="responseType">要读取的响应元素类型。</param>
    /// <param name="behaviorCount">目标 executor 对应的行为数量。</param>
    /// <returns>已缓存的 executor；若 binding 或 executor 尚未建立则返回 <see langword="null" />。</returns>
    private static object? GetStreamPipelineExecutorValue(
        object streamBindings,
        Type requestType,
        Type responseType,
        int behaviorCount)
    {
        var binding = GetStreamDispatchBindingValue(streamBindings, requestType, responseType);
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
            .Invoke(bindingBox, []);
    }

    /// <summary>
    ///     读取指定流式请求/响应类型对对应的强类型 stream dispatch binding。
    /// </summary>
    /// <param name="streamBindings">dispatcher 内部的 stream binding 缓存对象。</param>
    /// <param name="requestType">要读取的流式请求运行时类型。</param>
    /// <param name="responseType">要读取的响应元素类型。</param>
    /// <returns>强类型 binding；若缓存尚未建立则返回 <see langword="null" />。</returns>
    private static object? GetStreamDispatchBindingValue(object streamBindings, Type requestType, Type responseType)
    {
        var bindingBox = GetPairCacheValue(streamBindings, requestType, responseType);
        if (bindingBox is null)
        {
            return null;
        }

        var method = bindingBox.GetType().GetMethod(
            "Get",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing stream binding accessor on {bindingBox.GetType().FullName}.");

        return method!
            .MakeGenericMethod(responseType)
            .Invoke(bindingBox, []);
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
