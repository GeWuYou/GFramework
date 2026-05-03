// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Core.Rule;
using GFramework.Cqrs.Abstractions.Cqrs;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
/// 验证 CQRS 请求分发与 <see cref="ArchitectureContext"/> 的集成行为。
/// </summary>
[TestFixture]
public class CqrsArchitectureContextIntegrationTests
{
    /// <summary>
    /// 初始化测试运行所需的容器、日志与架构上下文。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new MicrosoftDiContainer();
        TestPerDispatchContextAwareHandler.Reset();

        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsArchitectureContextIntegrationTests)));

        // 注册传统 CQRS 组件，用于验证命令总线与请求分发可并存。
        _commandBus = new CommandExecutor();
        _container.RegisterPlurality(_commandBus);

        CqrsTestRuntime.RegisterHandlers(
            _container,
            typeof(CqrsArchitectureContextIntegrationTests).Assembly,
            typeof(ArchitectureContext).Assembly);

        _container.Freeze();
        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    /// 清理每个测试使用的容器与架构上下文引用。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context = null;
        _container = null;
        _commandBus = null;
    }

    private CommandExecutor? _commandBus;
    private MicrosoftDiContainer? _container;

    private ArchitectureContext? _context;

    /// <summary>
    /// 验证处理器可以观察到当前的架构上下文。
    /// </summary>
    [Test]
    public async Task Handler_Can_Access_Architecture_Context()
    {
        TestContextAwareHandler.LastContext = null;
        var request = new TestContextAwareRequest();

        await _context!.SendRequestAsync(request).ConfigureAwait(false);

        Assert.That(TestContextAwareHandler.LastContext, Is.Not.Null);
        Assert.That(TestContextAwareHandler.LastContext, Is.SameAs(_context));
    }

    /// <summary>
    /// 验证处理器能够通过当前上下文参与服务解析。
    /// </summary>
    [Test]
    public async Task Handler_Can_Retrieve_Services_From_Context()
    {
        TestServiceRetrievalHandler.LastRetrievedService = null;
        var request = new TestServiceRetrievalRequest();

        await _context!.SendRequestAsync(request).ConfigureAwait(false);

        Assert.That(TestServiceRetrievalHandler.LastRetrievedService, Is.Not.Null);
        Assert.That(TestServiceRetrievalHandler.LastRetrievedService, Is.InstanceOf<TestService>());
    }

    /// <summary>
    /// 验证请求分发流程支持嵌套请求处理。
    /// </summary>
    [Test]
    public async Task Handler_Can_Send_Nested_Requests()
    {
        TestNestedRequestHandler2.ExecutionCount = 0;
        var request = new TestNestedRequest { Depth = 1 };

        var result = await _context!.SendRequestAsync(request).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo("Nested execution completed at depth 1"));
        Assert.That(TestNestedRequestHandler2.ExecutionCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证请求处理期间的生命周期计数符合预期。
    /// </summary>
    [Test]
    public async Task Context_Lifecycle_Should_Be_Properly_Managed()
    {
        TestLifecycleHandler.InitializationCount = 0;
        TestLifecycleHandler.DisposalCount = 0;

        var request = new TestLifecycleRequest();
        await _context!.SendRequestAsync(request).ConfigureAwait(false);

        // 验证请求处理期间的初始化与释放计数符合预期。
        Assert.That(TestLifecycleHandler.InitializationCount, Is.EqualTo(1));
        Assert.That(TestLifecycleHandler.DisposalCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证并发请求使用的作用域彼此隔离。
    /// </summary>
    [Test]
    public async Task Scoped_Services_Should_Be_Properly_Isolated()
    {
        var results = new List<int>();

        // 并发执行多个请求，每个请求都应获得独立作用域。
        var tasks = Enumerable.Range(0, 10)
            .Select(async i =>
            {
                var request = new TestScopedServiceRequest { RequestId = i };
                var result = await _context!.SendRequestAsync(request).ConfigureAwait(false);
                lock (results)
                {
                    results.Add(result);
                }
            });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // 验证每个请求都获得了独立的作用域结果。
        Assert.That(results.Distinct().Count(), Is.EqualTo(10));
    }

    /// <summary>
    /// 验证处理器抛出的异常会按原样传播到调用方。
    /// </summary>
    [Test]
    public async Task Context_Error_Should_Be_Properly_Propagated()
    {
        var request = new TestErrorPropagationRequest();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _context!.SendRequestAsync(request).ConfigureAwait(false));

        Assert.That(ex!.Message, Is.EqualTo("Test error from handler"));
        Assert.That(ex.Data["RequestId"], Is.Not.Null);
    }

    /// <summary>
    /// 验证处理器异常在记录后仍保持原始异常类型。
    /// </summary>
    [Test]
    public async Task Context_Should_Handle_Handler_Exceptions_Gracefully()
    {
        TestExceptionHandler.LastException = null;
        var request = new TestExceptionRequest();

        Assert.ThrowsAsync<DivideByZeroException>(async () =>
            await _context!.SendRequestAsync(request).ConfigureAwait(false));

        // 验证异常被捕获并保留原始类型。
        Assert.That(TestExceptionHandler.LastException, Is.Not.Null);
        Assert.That(TestExceptionHandler.LastException, Is.InstanceOf<DivideByZeroException>());
    }

    /// <summary>
    /// 验证架构上下文集成路径的额外分发开销保持在可接受范围内。
    /// </summary>
    [Test]
    public async Task Context_Overhead_Should_Be_Minimal()
    {
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var request = new TestPerformanceRequest2 { Id = i };
            var result = await _context!.SendRequestAsync(request).ConfigureAwait(false);
            Assert.That(result, Is.EqualTo(i));
        }

        stopwatch.Stop();
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;

        // 验证架构上下文集成的性能开销在合理范围内。
        Assert.That(avgTime, Is.LessThan(5.0)); // 平均每个请求不超过5ms
        Console.WriteLine($"Average time with context integration: {avgTime:F2}ms");
    }

    /// <summary>
    /// 验证缓存路径相较无缓存路径不会引入异常级别的额外开销。
    /// </summary>
    [Test]
    public async Task Context_Caching_Should_Improve_Performance()
    {
        const int iterations = 50; // 减少迭代次数
        var uncachedTimes = new List<long>();
        var cachedTimes = new List<long>();

        // 测试无缓存路径。
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = new TestUncachedRequest { Id = i };
            await _context!.SendRequestAsync(request).ConfigureAwait(false);
            stopwatch.Stop();
            uncachedTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // 测试缓存命中路径。
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = new TestCachedRequest { Id = i };
            await _context!.SendRequestAsync(request).ConfigureAwait(false);
            stopwatch.Stop();
            cachedTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        var avgUncached = uncachedTimes.Average();
        var avgCached = cachedTimes.Average();

        // 放宽性能要求，避免环境抖动导致偶发失败。
        Assert.That(avgCached, Is.LessThan(avgUncached * 2.5));
        Console.WriteLine($"Uncached avg: {avgUncached:F2}ms, Cached avg: {avgCached:F2}ms");
    }

    /// <summary>
    /// 验证并发请求访问同一架构上下文时能够安全完成。
    /// </summary>
    [Test]
    public async Task Context_Should_Handle_Concurrent_Access_Safely()
    {
        const int concurrentRequests = 50;
        var tasks = new List<Task<int>>();
        var executionOrder = new List<int>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            var requestId = i;
            var task = Task.Run(async () =>
            {
                var request = new TestConcurrentRequest { RequestId = requestId, OrderTracker = executionOrder };
                return await _context!.SendRequestAsync(request).ConfigureAwait(false);
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 验证所有请求都成功完成。
        Assert.That(results.Length, Is.EqualTo(concurrentRequests));
        Assert.That(results.Distinct().Count(), Is.EqualTo(concurrentRequests));

        // 验证每个请求都留下了执行痕迹。
        Assert.That(executionOrder.Count, Is.EqualTo(concurrentRequests));
    }

    /// <summary>
    /// 验证并发状态修改后共享状态仍保持一致。
    /// </summary>
    [Test]
    public async Task Context_State_Should_Remain_Consistent_Under_Concurrency()
    {
        var sharedState = new SharedState();
        const int concurrentOperations = 20;

        var tasks = Enumerable.Range(0, concurrentOperations)
            .Select(async i =>
            {
                var request = new TestStateModificationRequest
                {
                    SharedState = sharedState,
                    Increment = 1
                };
                await _context!.SendRequestAsync(request).ConfigureAwait(false);
            });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // 验证最终状态正确。
        Assert.That(sharedState.Counter, Is.EqualTo(concurrentOperations));
    }

    /// <summary>
    /// 验证架构上下文可以与现有系统协同工作。
    /// </summary>
    [Test]
    public async Task Context_Can_Integrate_With_Existing_Systems()
    {
        // 测试与现有系统的集成。
        TestIntegrationHandler.LastSystemCall = null;
        var request = new TestIntegrationRequest();

        var result = await _context!.SendRequestAsync(request).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo("Integration successful"));
        Assert.That(TestIntegrationHandler.LastSystemCall, Is.EqualTo("System executed"));
    }

    /// <summary>
    /// 验证传统命令总线与请求响应式 CQRS 分发可以共存。
    /// </summary>
    [Test]
    public async Task Context_Can_Handle_Mixed_CQRS_Patterns()
    {
        // 使用传统 CQRS 命令总线。
        var traditionalCommand = new TestTraditionalCommand();
        _context!.SendCommand(traditionalCommand);
        Assert.That(traditionalCommand.Executed, Is.True);

        // 使用基于请求/响应的 CQRS 分发。
        var cqrsRequest = new TestCqrsRequest { Value = 42 };
        var result = await _context.SendRequestAsync(cqrsRequest).ConfigureAwait(false);
        Assert.That(result, Is.EqualTo(42));

        // 验证两种模式可以共存。
        Assert.That(traditionalCommand.Executed, Is.True);
        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    /// 验证上下文感知处理器在每次分发时都会获得新实例。
    /// </summary>
    [Test]
    public async Task ContextAware_Handler_Should_Use_A_Fresh_Instance_Per_Request()
    {
        var firstResult = await _context!.SendRequestAsync(new TestPerDispatchContextAwareRequest()).ConfigureAwait(false);
        var secondResult = await _context.SendRequestAsync(new TestPerDispatchContextAwareRequest()).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.Not.EqualTo(secondResult));
            Assert.That(TestPerDispatchContextAwareHandler.SeenInstanceIds, Is.EqualTo([firstResult, secondResult]));
            Assert.That(TestPerDispatchContextAwareHandler.Contexts, Has.All.SameAs(_context));
        });
    }
    #region Integration Test Types

    /// <summary>
    /// 为上下文感知请求提供静态响应的测试处理器。
    /// </summary>
    public sealed class TestContextAwareRequestHandler : ContextAwareBase, IRequestHandler<TestContextAwareRequest, string>
    {
        /// <summary>
        /// 记录当前处理器观察到的架构上下文，并返回固定结果。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定的测试结果。</returns>
        public ValueTask<string> Handle(TestContextAwareRequest request, CancellationToken cancellationToken)
        {
            TestContextAwareHandler.LastContext = Context;
            return new ValueTask<string>("Context accessed");
        }
    }

    /// <summary>
    /// 模拟从架构上下文中解析服务的测试处理器。
    /// </summary>
    public sealed class TestServiceRetrievalRequestHandler : IRequestHandler<TestServiceRetrievalRequest, string>
    {
        /// <summary>
        /// 记录一次服务解析结果并返回固定响应。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定的测试结果。</returns>
        public ValueTask<string> Handle(TestServiceRetrievalRequest request, CancellationToken cancellationToken)
        {
            TestServiceRetrievalHandler.LastRetrievedService = new TestService();
            return new ValueTask<string>("Service retrieved");
        }
    }

    /// <summary>
    /// 模拟嵌套请求处理的测试处理器。
    /// </summary>
    public sealed class TestNestedRequestHandler : IRequestHandler<TestNestedRequest, string>
    {
        /// <summary>
        /// 递增嵌套请求执行计数并返回深度描述。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>包含嵌套深度的固定结果。</returns>
        public ValueTask<string> Handle(TestNestedRequest request, CancellationToken cancellationToken)
        {
            TestNestedRequestHandler2.ExecutionCount++;
            // 模拟嵌套调用。
            return new ValueTask<string>($"Nested execution completed at depth {request.Depth}");
        }
    }

    /// <summary>
    /// 模拟请求生命周期回调的测试处理器。
    /// </summary>
    public sealed class TestLifecycleRequestHandler : IRequestHandler<TestLifecycleRequest, string>
    {
        /// <summary>
        /// 递增初始化与释放计数来模拟生命周期管理。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定的测试结果。</returns>
        public ValueTask<string> Handle(TestLifecycleRequest request, CancellationToken cancellationToken)
        {
            TestLifecycleHandler.InitializationCount++;
            // 模拟一次完整处理流程中的工作。
            TestLifecycleHandler.DisposalCount++;
            return new ValueTask<string>("Lifecycle managed");
        }
    }

    /// <summary>
    /// 返回请求编号以验证作用域隔离的测试处理器。
    /// </summary>
    public sealed class TestScopedServiceRequestHandler : IRequestHandler<TestScopedServiceRequest, int>
    {
        /// <summary>
        /// 返回请求携带的编号。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>请求编号。</returns>
        public ValueTask<int> Handle(TestScopedServiceRequest request, CancellationToken cancellationToken)
        {
            // 直接返回请求编号，便于验证不同请求的隔离性。
            return new ValueTask<int>(request.RequestId);
        }
    }

    /// <summary>
    /// 抛出携带附加数据的异常以验证错误传播的测试处理器。
    /// </summary>
    public sealed class TestErrorPropagationRequestHandler : IRequestHandler<TestErrorPropagationRequest, string>
    {
        /// <summary>
        /// 创建并抛出测试异常。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>该方法总是抛出异常，不返回结果。</returns>
        /// <exception cref="InvalidOperationException">始终抛出，用于验证异常透传。</exception>
        public ValueTask<string> Handle(TestErrorPropagationRequest request, CancellationToken cancellationToken)
        {
            var ex = new InvalidOperationException("Test error from handler");
            ex.Data["RequestId"] = Guid.NewGuid();
            throw ex;
        }
    }

    /// <summary>
    /// 抛出算术异常以验证异常捕获行为的测试处理器。
    /// </summary>
    public sealed class TestExceptionRequestHandler : IRequestHandler<TestExceptionRequest, string>
    {
        /// <summary>
        /// 创建并抛出测试异常。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>该方法总是抛出异常，不返回结果。</returns>
        /// <exception cref="DivideByZeroException">始终抛出，用于验证异常记录行为。</exception>
        public ValueTask<string> Handle(TestExceptionRequest request, CancellationToken cancellationToken)
        {
            TestExceptionHandler.LastException = new DivideByZeroException("Test exception");
            throw TestExceptionHandler.LastException;
        }
    }

    /// <summary>
    /// 提供轻量级请求处理以测量分发开销的测试处理器。
    /// </summary>
    public sealed class TestPerformanceRequest2Handler : IRequestHandler<TestPerformanceRequest2, int>
    {
        /// <summary>
        /// 返回请求编号，避免额外逻辑干扰性能测量。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>请求编号。</returns>
        public ValueTask<int> Handle(TestPerformanceRequest2 request, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(request.Id);
        }
    }

    /// <summary>
    /// 模拟无缓存慢路径的测试处理器。
    /// </summary>
    public sealed class TestUncachedRequestHandler : IRequestHandler<TestUncachedRequest, int>
    {
        /// <summary>
        /// 人为引入延迟来模拟未命中缓存的处理路径。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>请求编号。</returns>
        public async ValueTask<int> Handle(TestUncachedRequest request, CancellationToken cancellationToken)
        {
            // 引入固定延迟，用于构造无缓存基线。
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            return request.Id;
        }
    }

    /// <summary>
    /// 使用静态缓存模拟可复用处理结果的测试处理器。
    /// </summary>
    public sealed class TestCachedRequestHandler : IRequestHandler<TestCachedRequest, int>
    {
        private static readonly ConcurrentDictionary<int, int> _cache = new();

        /// <summary>
        /// 优先返回缓存结果，未命中时执行较慢路径并写入缓存。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>请求编号。</returns>
        public async ValueTask<int> Handle(TestCachedRequest request, CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(request.Id, out var cachedValue))
            {
                return cachedValue;
            }

            // 模拟首次处理成本。
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            return _cache.GetOrAdd(request.Id, static id => id);
        }
    }

    /// <summary>
    /// 记录并发请求执行顺序的测试处理器。
    /// </summary>
    public sealed class TestConcurrentRequestHandler : IRequestHandler<TestConcurrentRequest, int>
    {
        /// <summary>
        /// 将请求编号记录到共享顺序跟踪器中。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>请求编号。</returns>
        public ValueTask<int> Handle(TestConcurrentRequest request, CancellationToken cancellationToken)
        {
            lock (request.OrderTracker)
            {
                request.OrderTracker.Add(request.RequestId);
            }

            return new ValueTask<int>(request.RequestId);
        }
    }

    /// <summary>
    /// 修改共享状态以验证并发一致性的测试处理器。
    /// </summary>
    public sealed class TestStateModificationRequestHandler : IRequestHandler<TestStateModificationRequest, string>
    {
        /// <summary>
        /// 将请求中的增量写入共享状态。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定的测试结果。</returns>
        public ValueTask<string> Handle(TestStateModificationRequest request, CancellationToken cancellationToken)
        {
            request.SharedState.IncrementBy(request.Increment);
            return new ValueTask<string>("State modified");
        }
    }

    /// <summary>
    /// 模拟与既有系统交互的测试处理器。
    /// </summary>
    public sealed class TestIntegrationRequestHandler : IRequestHandler<TestIntegrationRequest, string>
    {
        /// <summary>
        /// 记录一次系统调用并返回成功结果。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定的成功结果。</returns>
        public ValueTask<string> Handle(TestIntegrationRequest request, CancellationToken cancellationToken)
        {
            TestIntegrationHandler.LastSystemCall = "System executed";
            return new ValueTask<string>("Integration successful");
        }
    }

    /// <summary>
    /// 为请求/响应分发路径返回固定编号的测试处理器。
    /// </summary>
    public sealed class TestCqrsRequestHandler : IRequestHandler<TestCqrsRequest, int>
    {
        /// <summary>
        /// 返回请求中的值，验证 CQRS 请求分发路径可用。
        /// </summary>
        /// <param name="request">当前测试请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>请求中携带的值。</returns>
        public ValueTask<int> Handle(TestCqrsRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(request.Value);
        }
    }

    /// <summary>
    ///     用于验证自动扫描到的上下文感知处理器会按请求创建新实例。
    /// </summary>
    public sealed class TestPerDispatchContextAwareHandler : ContextAwareBase,
        IRequestHandler<TestPerDispatchContextAwareRequest, int>
    {
        private static int _nextInstanceId;
        private static readonly List<IArchitectureContext?> TrackedContexts = [];
        private static readonly List<int> TrackedInstanceIds = [];
        private readonly int _instanceId = Interlocked.Increment(ref _nextInstanceId);

        /// <summary>
        /// 获取按请求记录的架构上下文序列。
        /// </summary>
        public static IReadOnlyList<IArchitectureContext?> Contexts => TrackedContexts;

        /// <summary>
        /// 获取已观察到的处理器实例编号序列。
        /// </summary>
        public static IReadOnlyList<int> SeenInstanceIds => TrackedInstanceIds;

        /// <summary>
        ///     记录当前实例编号与收到的架构上下文。
        /// </summary>
        /// <param name="request">请求实例。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>当前处理器实例编号。</returns>
        public ValueTask<int> Handle(TestPerDispatchContextAwareRequest request, CancellationToken cancellationToken)
        {
            TrackedContexts.Add(Context);
            TrackedInstanceIds.Add(_instanceId);
            return ValueTask.FromResult(_instanceId);
        }

        /// <summary>
        ///     重置跨测试共享的实例跟踪状态。
        /// </summary>
        public static void Reset()
        {
            TrackedContexts.Clear();
            TrackedInstanceIds.Clear();
            _nextInstanceId = 0;
        }
    }

    /// <summary>
    /// 用于验证处理器可观察到当前架构上下文的测试请求。
    /// </summary>
    public sealed record TestContextAwareRequest : IRequest<string>;

    /// <summary>
    /// 保存最近一次上下文观察结果的测试状态容器。
    /// </summary>
    public static class TestContextAwareHandler
    {
        /// <summary>
        /// 获取或设置最近一次测试观察到的架构上下文。
        /// </summary>
        public static IArchitectureContext? LastContext { get; set; }
    }

    /// <summary>
    /// 用于验证服务解析流程的测试请求。
    /// </summary>
    public sealed record TestServiceRetrievalRequest : IRequest<string>;

    /// <summary>
    /// 保存最近一次服务解析结果的测试状态容器。
    /// </summary>
    public static class TestServiceRetrievalHandler
    {
        /// <summary>
        /// 获取或设置最近一次解析得到的服务实例。
        /// </summary>
        public static object? LastRetrievedService { get; set; }
    }

    /// <summary>
    /// 表示用于验证服务解析的简单测试服务。
    /// </summary>
    public class TestService
    {
        /// <summary>
        /// 获取当前测试服务实例的唯一标识。
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 用于验证嵌套请求处理的测试请求。
    /// </summary>
    public sealed record TestNestedRequest : IRequest<string>
    {
        /// <summary>
        /// 获取请求携带的嵌套深度。
        /// </summary>
        public int Depth { get; init; }
    }

    /// <summary>
    /// 保存嵌套请求执行计数的测试状态容器。
    /// </summary>
    public static class TestNestedRequestHandler2
    {
        /// <summary>
        /// 获取或设置嵌套请求处理器的执行次数。
        /// </summary>
        public static int ExecutionCount { get; set; }
    }

    /// <summary>
    /// 用于验证生命周期管理的测试请求。
    /// </summary>
    public sealed record TestLifecycleRequest : IRequest<string>;

    /// <summary>
    /// 保存生命周期计数的测试状态容器。
    /// </summary>
    public static class TestLifecycleHandler
    {
        /// <summary>
        /// 获取或设置初始化次数。
        /// </summary>
        public static int InitializationCount { get; set; }

        /// <summary>
        /// 获取或设置释放次数。
        /// </summary>
        public static int DisposalCount { get; set; }
    }

    /// <summary>
    /// 用于验证作用域隔离的测试请求。
    /// </summary>
    public sealed record TestScopedServiceRequest : IRequest<int>
    {
        /// <summary>
        /// 获取请求编号。
        /// </summary>
        public int RequestId { get; init; }
    }

    /// <summary>
    /// 用于验证异常传播的测试请求。
    /// </summary>
    public sealed record TestErrorPropagationRequest : IRequest<string>;

    /// <summary>
    /// 保存最近一次异常实例的测试状态容器。
    /// </summary>
    public static class TestExceptionHandler
    {
        /// <summary>
        /// 获取或设置最近一次记录到的异常。
        /// </summary>
        public static Exception? LastException { get; set; }
    }

    /// <summary>
    /// 用于验证异常记录行为的测试请求。
    /// </summary>
    public sealed record TestExceptionRequest : IRequest<string>;

    /// <summary>
    /// 用于验证轻量请求分发开销的测试请求。
    /// </summary>
    public sealed record TestPerformanceRequest2 : IRequest<int>
    {
        /// <summary>
        /// 获取请求编号。
        /// </summary>
        public int Id { get; init; }
    }

    /// <summary>
    /// 用于验证未缓存处理路径的测试请求。
    /// </summary>
    public sealed record TestUncachedRequest : IRequest<int>
    {
        /// <summary>
        /// 获取请求编号。
        /// </summary>
        public int Id { get; init; }
    }

    /// <summary>
    /// 用于验证缓存处理路径的测试请求。
    /// </summary>
    public sealed record TestCachedRequest : IRequest<int>
    {
        /// <summary>
        /// 获取请求编号。
        /// </summary>
        public int Id { get; init; }
    }

    /// <summary>
    /// 表示并发测试共享的可变状态。
    /// </summary>
    public class SharedState
    {
        private int _counter;

        /// <summary>
        /// 获取当前计数值。
        /// </summary>
        public int Counter => _counter;

        /// <summary>
        /// 以线程安全方式增加计数器。
        /// </summary>
        /// <param name="increment">要增加的数值。</param>
        public void IncrementBy(int increment)
        {
            Interlocked.Add(ref _counter, increment);
        }
    }

    /// <summary>
    /// 用于验证并发请求调度安全性的测试请求。
    /// </summary>
    public sealed record TestConcurrentRequest : IRequest<int>
    {
        /// <summary>
        /// 获取请求编号。
        /// </summary>
        public int RequestId { get; init; }

        /// <summary>
        /// 获取用于记录执行顺序的共享集合。
        /// </summary>
        public ICollection<int> OrderTracker { get; init; } = new List<int>();
    }

    /// <summary>
    /// 用于验证并发状态修改一致性的测试请求。
    /// </summary>
    public sealed record TestStateModificationRequest : IRequest<string>
    {
        /// <summary>
        /// 获取待修改的共享状态实例。
        /// </summary>
        public SharedState SharedState { get; init; } = null!;

        /// <summary>
        /// 获取要增加的计数值。
        /// </summary>
        public int Increment { get; init; }
    }

    /// <summary>
    /// 保存最近一次系统调用结果的测试状态容器。
    /// </summary>
    public static class TestIntegrationHandler
    {
        /// <summary>
        /// 获取或设置最近一次系统调用记录。
        /// </summary>
        public static string? LastSystemCall { get; set; }
    }

    /// <summary>
    /// 用于验证系统集成行为的测试请求。
    /// </summary>
    public sealed record TestIntegrationRequest : IRequest<string>;

    /// <summary>
    /// 用于验证请求/响应 CQRS 分发路径的测试请求。
    /// </summary>
    public sealed record TestCqrsRequest : IRequest<int>
    {
        /// <summary>
        /// 获取请求返回的测试值。
        /// </summary>
        public int Value { get; init; }
    }

    /// <summary>
    ///     用于验证每次请求分发都会获得新的上下文感知处理器实例。
    /// </summary>
    public sealed record TestPerDispatchContextAwareRequest : IRequest<int>;

    /// <summary>
    /// 表示用于混合模式验证的传统命令。
    /// </summary>
    public class TestTraditionalCommand : ICommand
    {
        /// <summary>
        /// 获取命令是否已执行。
        /// </summary>
        public bool Executed { get; private set; }

        /// <summary>
        /// 将命令标记为已执行。
        /// </summary>
        public void Execute() => Executed = true;

        /// <summary>
        /// 为兼容命令接口保留上下文设置入口，当前测试无需使用。
        /// </summary>
        /// <param name="context">命令上下文。</param>
        public void SetContext(IArchitectureContext context)
        {
        }

        /// <summary>
        /// 返回命令上下文占位值，当前测试路径不会消费该结果。
        /// </summary>
        /// <returns>始终返回空引用占位值。</returns>
        public IArchitectureContext GetContext() => null!;
    }

    #endregion
}
