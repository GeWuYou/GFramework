// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Architectures;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Extensions;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     覆盖 <see cref="ArchitectureContext" /> 通过可选安装的 CQRS runtime 分发请求、通知与流式请求的综合行为。
/// </summary>
[TestFixture]
public sealed class ArchitectureContextComprehensiveTests
{
    /// <summary>
    ///     测试初始化方法，在每个测试方法执行前运行。
    ///     负责初始化日志工厂、依赖注入容器、自有 CQRS 处理器以及架构基础设施服务。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new MicrosoftDiContainer();

        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(ArchitectureContextComprehensiveTests)));

        // 注册架构自身的基础设施服务，验证其与 CQRS 上下文扩展分发可以共存。
        _eventBus = new EventBus();
        _environment = new DefaultEnvironment();

        _container.RegisterPlurality(_eventBus);
        _container.RegisterPlurality(_environment);

        CqrsTestRuntime.RegisterHandlers(
            _container,
            typeof(ArchitectureContextComprehensiveTests).Assembly,
            typeof(ArchitectureContext).Assembly);

        _container.Freeze();

        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    ///     测试清理方法，在每个测试方法执行后运行。
    ///     负责释放容器和上下文资源。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _context = null;
        _container = null;
        _eventBus = null;
        _environment = null;
    }

    private MicrosoftDiContainer? _container;

    private ArchitectureContext? _context;
    private DefaultEnvironment? _environment;
    private EventBus? _eventBus;

    /// <summary>
    ///     测试SendRequestAsync方法在请求有效时返回结果
    /// </summary>
    [Test]
    public async Task SendRequestAsync_Should_ReturnResult_When_Request_IsValid()
    {
        var testRequest = new TestRequest { Value = 42 };
        var result = await _context!.SendRequestAsync(testRequest).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试SendRequestAsync方法在请求为null时抛出ArgumentNullException
    /// </summary>
    [Test]
    public void SendRequestAsync_Should_ThrowArgumentNullException_When_Request_IsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _context!.SendRequestAsync<int>(null!).ConfigureAwait(false));
    }

    /// <summary>
    ///     测试PublishAsync方法在通知有效时发布通知
    /// </summary>
    [Test]
    public async Task PublishAsync_Should_PublishNotification_When_Notification_IsValid()
    {
        TestNotificationHandler.LastReceivedMessage = null;
        var notification = new TestNotification { Message = "test" };

        await _context!.PublishAsync(notification).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        Assert.That(TestNotificationHandler.LastReceivedMessage, Is.EqualTo("test"));
    }

    /// <summary>
    ///     测试CreateStream方法在流请求有效时返回流
    /// </summary>
    [Test]
    public async Task CreateStream_Should_ReturnStream_When_StreamRequest_IsValid()
    {
        var testStreamRequest = new TestStreamRequest { Values = [1, 2, 3, 4, 5] };
        var stream = _context!.CreateStream(testStreamRequest);

        var results = new List<int>();
        await foreach (var item in stream.ConfigureAwait(false))
        {
            results.Add(item);
        }

        Assert.That(results, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    /// <summary>
    ///     测试SendAsync方法（无返回值请求）在请求有效时执行
    /// </summary>
    [Test]
    public async Task SendAsync_RequestWithoutResult_Should_Execute_When_Request_IsValid()
    {
        var testRequest = new TestCommand { ShouldExecute = true };
        await _context!.SendAsync(testRequest).ConfigureAwait(false);

        Assert.That(testRequest.Executed, Is.True);
    }

    /// <summary>
    ///     测试SendAsync方法（带返回值请求）在请求有效时返回结果
    /// </summary>
    [Test]
    public async Task SendAsync_RequestWithResult_Should_ReturnResult_When_Request_IsValid()
    {
        var testRequest = new TestCommandWithResult { ResultValue = 42 };
        var result = await _context!.SendAsync(testRequest).ConfigureAwait(false);

        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试GetService方法使用缓存
    /// </summary>
    [Test]
    public void GetService_Should_Use_Cache()
    {
        var firstResult = _context!.GetService<IEventBus>();
        Assert.That(firstResult, Is.Not.Null);
        Assert.That(firstResult, Is.SameAs(_eventBus));

        var secondResult = _context.GetService<IEventBus>();
        Assert.That(secondResult, Is.SameAs(firstResult));
    }

    /// <summary>
    ///     测试架构未安装 CQRS runtime 时，上下文分发入口会显式抛出缺失 runtime 异常。
    /// </summary>
    [Test]
    public void Missing_Cqrs_Runtime_Should_Throw_InvalidOperationException()
    {
        var containerWithoutRuntime = new MicrosoftDiContainer();
        containerWithoutRuntime.Freeze();

        var contextWithoutRuntime = new ArchitectureContext(containerWithoutRuntime);
        var testRequest = new TestRequest { Value = 42 };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await contextWithoutRuntime.SendRequestAsync(testRequest).ConfigureAwait(false));
    }

    /// <summary>
    ///     测试多个通知处理器都被调用
    /// </summary>
    [Test]
    public async Task Multiple_Notification_Handlers_Should_All_Be_Invoked()
    {
        // 重置静态字段
        TestNotificationHandler.LastReceivedMessage = null;
        TestNotificationHandler2.LastReceivedMessage = null;
        TestNotificationHandler3.LastReceivedMessage = null;

        var notification = new TestNotification { Message = "multi-handler test" };
        await _context!.PublishAsync(notification).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);

        // 验证所有处理器都被调用
        Assert.That(TestNotificationHandler.LastReceivedMessage, Is.EqualTo("multi-handler test"));
        Assert.That(TestNotificationHandler2.LastReceivedMessage, Is.EqualTo("multi-handler test"));
        Assert.That(TestNotificationHandler3.LastReceivedMessage, Is.EqualTo("multi-handler test"));
    }

    /// <summary>
    ///     测试CancellationToken取消长时间运行的请求
    /// </summary>
    [Test]
    public async Task CancellationToken_Should_Cancel_Long_Running_Request()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var longRequest = new TestLongRunningRequest { DelayMs = 1000 };

        // 应该在50ms后被取消
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _context!.SendRequestAsync(longRequest, cts.Token).ConfigureAwait(false));
    }

    /// <summary>
    ///     测试CancellationToken取消流请求
    /// </summary>
    [Test]
    public async Task CancellationToken_Should_Cancel_Stream_Request()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var longStreamRequest = new TestLongStreamRequest { ItemCount = 1000 };

        var stream = _context!.CreateStream(longStreamRequest, cts.Token);
        var results = new List<int>();

        // 流应该在100ms后被取消（TaskCanceledException 继承自 OperationCanceledException）
        Assert.CatchAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in stream.ConfigureAwait(false))
            {
                results.Add(item);
            }
        });

        // 验证只处理了部分数据
        Assert.That(results.Count, Is.LessThan(1000));
    }

    /// <summary>
    ///     测试并发 CQRS 请求不会相互干扰
    /// </summary>
    [Test]
    public async Task Concurrent_Cqrs_Requests_Should_Not_Interfere()
    {
        const int requestCount = 10;
        var tasks = new List<Task<int>>();

        // 并发发送多个请求
        for (int i = 0; i < requestCount; i++)
        {
            var request = new TestRequest { Value = i };
            tasks.Add(_context!.SendRequestAsync(request).AsTask());
        }

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 验证所有结果都正确返回
        Assert.That(results.Length, Is.EqualTo(requestCount));
        Assert.That(results.OrderBy(x => x), Is.EqualTo(Enumerable.Range(0, requestCount)));
    }

    /// <summary>
    ///     测试处理器异常被正确传播
    /// </summary>
    [Test]
    public async Task Handler_Exception_Should_Be_Propagated()
    {
        var faultyRequest = new TestFaultyRequest();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _context!.SendRequestAsync(faultyRequest).ConfigureAwait(false));
    }

    /// <summary>
    ///     测试多个请求处理器可以修改同一对象
    /// </summary>
    [Test]
    public async Task Multiple_Request_Handlers_Can_Modify_Same_Object()
    {
        var sharedData = new SharedData();
        var request1 = new TestModifyDataRequest { Data = sharedData, Value = 10 };
        var request2 = new TestModifyDataRequest { Data = sharedData, Value = 20 };

        await _context!.SendAsync(request1).ConfigureAwait(false);
        await _context!.SendAsync(request2).ConfigureAwait(false);

        // 验证数据被正确修改
        Assert.That(sharedData.Value, Is.EqualTo(30)); // 10 + 20
    }

    /// <summary>
    ///     测试通知顺序被保留
    /// </summary>
    [Test]
    public async Task Notification_Ordering_Should_Be_Preserved()
    {
        var receivedOrder = new List<string>();
        TestOrderedNotificationHandler.ReceivedMessages = receivedOrder;

        var notifications = new[]
        {
            new TestOrderedNotification { Order = 1, Message = "First" },
            new TestOrderedNotification { Order = 2, Message = "Second" },
            new TestOrderedNotification { Order = 3, Message = "Third" }
        };

        foreach (var notification in notifications)
        {
            await _context!.PublishAsync(notification).ConfigureAwait(false);
        }

        await Task.Delay(200).ConfigureAwait(false); // 等待所有处理完成

        // 验证接收顺序与发送顺序一致
        Assert.That(receivedOrder.Count, Is.EqualTo(3));
        Assert.That(receivedOrder[0], Is.EqualTo("First"));
        Assert.That(receivedOrder[1], Is.EqualTo("Second"));
        Assert.That(receivedOrder[2], Is.EqualTo("Third"));
    }

    /// <summary>
    ///     测试流请求带过滤功能
    /// </summary>
    [Test]
    public async Task Stream_Request_With_Filtering()
    {
        var filterRequest = new TestFilterStreamRequest
        {
            Values = Enumerable.Range(1, 10).ToArray(),
            FilterEven = true
        };

        var stream = _context!.CreateStream(filterRequest);
        var results = new List<int>();

        await foreach (var item in stream.ConfigureAwait(false))
        {
            results.Add(item);
        }

        // 验证只返回偶数
        Assert.That(results.All(x => x % 2 == 0), Is.True);
        Assert.That(results, Is.EqualTo(new[] { 2, 4, 6, 8, 10 }));
    }

    /// <summary>
    ///     测试请求验证使用Behaviors
    /// </summary>
    [Test]
    public async Task Request_Validation_With_Behaviors()
    {
        var invalidRequest = new TestValidatedRequest { Name = "" }; // 无效：空字符串

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _context!.SendAsync(invalidRequest).ConfigureAwait(false));
    }

    /// <summary>
    ///     测试 CQRS 性能基准
    /// </summary>
    [Test]
    public async Task Performance_Benchmark_For_Cqrs()
    {
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var request = new TestRequest { Value = i };
            var result = await _context!.SendRequestAsync(request).ConfigureAwait(false);
            Assert.That(result, Is.EqualTo(i));
        }

        stopwatch.Stop();
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;

        // 验证性能在合理范围内（平均每个请求不超过10ms）
        Assert.That(avgTime, Is.LessThan(10.0));
        Console.WriteLine($"Average time per request: {avgTime:F2}ms");
    }

    #region ArchitectureContext CQRS Test Helpers

    private sealed record TestLongRunningRequest : IRequest<string>
    {
        public int DelayMs { get; init; }
    }

    private sealed class TestLongRunningRequestHandler : IRequestHandler<TestLongRunningRequest, string>
    {
        public async ValueTask<string> Handle(TestLongRunningRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(request.DelayMs, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return "Completed";
        }
    }

    private sealed record TestLongStreamRequest : IStreamRequest<int>
    {
        public int ItemCount { get; init; }
    }

    private sealed class TestLongStreamRequestHandler : IStreamRequestHandler<TestLongStreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(
            TestLongStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.ItemCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return i;
                await Task.Delay(10, cancellationToken).ConfigureAwait(false); // 模拟处理延迟
            }
        }
    }

    private sealed record TestFaultyRequest : IRequest<string>;

    private sealed class TestFaultyRequestHandler : IRequestHandler<TestFaultyRequest, string>
    {
        public ValueTask<string> Handle(TestFaultyRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler failed intentionally");
        }
    }

    private sealed class SharedData
    {
        public int Value { get; set; }
    }

    private sealed record TestModifyDataRequest : IRequest<Unit>
    {
        public SharedData Data { get; init; } = null!;
        public int Value { get; init; }
    }

    private sealed class TestModifyDataRequestHandler : IRequestHandler<TestModifyDataRequest, Unit>
    {
        public ValueTask<Unit> Handle(TestModifyDataRequest request, CancellationToken cancellationToken)
        {
            request.Data.Value += request.Value;
            return ValueTask.FromResult(Unit.Value);
        }
    }

    private sealed record TestCachingRequest : IRequest<string>
    {
        public string Key { get; init; } = string.Empty;
        public IDictionary<string, string> Cache { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private sealed class TestCachingRequestHandler : IRequestHandler<TestCachingRequest, string>
    {
        public ValueTask<string> Handle(TestCachingRequest request, CancellationToken cancellationToken)
        {
            if (request.Cache.TryGetValue(request.Key, out var cachedValue))
            {
                return new ValueTask<string>(cachedValue);
            }

            var newValue = $"Value_for_{request.Key}";
            request.Cache[request.Key] = newValue;
            return new ValueTask<string>(newValue);
        }
    }

    private sealed record TestOrderedNotification : INotification
    {
        public int Order { get; init; }
        public string Message { get; init; } = string.Empty;
    }

    private sealed class TestOrderedNotificationHandler : INotificationHandler<TestOrderedNotification>
    {
        public static ICollection<string> ReceivedMessages { get; set; } = new List<string>();

        public ValueTask Handle(TestOrderedNotification notification, CancellationToken cancellationToken)
        {
            ReceivedMessages.Add(notification.Message);
            return ValueTask.CompletedTask;
        }
    }

    // 额外的通知处理器用于验证多处理器通知分发场景。
    private sealed class TestNotificationHandler2 : INotificationHandler<TestNotification>
    {
        public static string? LastReceivedMessage { get; set; }

        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            LastReceivedMessage = notification.Message;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestNotificationHandler3 : INotificationHandler<TestNotification>
    {
        public static string? LastReceivedMessage { get; set; }

        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            LastReceivedMessage = notification.Message;
            return ValueTask.CompletedTask;
        }
    }

    private sealed record TestFilterStreamRequest : IStreamRequest<int>
    {
        public int[] Values { get; init; } = [];
        public bool FilterEven { get; init; }
    }

    private sealed class TestFilterStreamRequestHandler : IStreamRequestHandler<TestFilterStreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(
            TestFilterStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var value in request.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (request.FilterEven && value % 2 != 0)
                    continue;

                yield return value;
                await Task.Yield();
            }
        }
    }

    private sealed record TestValidatedRequest : IRequest<Unit>
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestValidatedRequestHandler : IRequestHandler<TestValidatedRequest, Unit>
    {
        public ValueTask<Unit> Handle(TestValidatedRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Name cannot be empty.", nameof(request));
            }

            return ValueTask.FromResult(Unit.Value);
        }
    }

    #endregion

    #region ArchitectureContext CQRS Runtime Types

    private sealed record TestRequest : IRequest<int>
    {
        public int Value { get; init; }
    }

    private sealed record TestCommand : IRequest<Unit>
    {
        public bool ShouldExecute { get; init; }
        public bool Executed { get; set; }
    }

    private sealed record TestCommandWithResult : IRequest<int>
    {
        public int ResultValue { get; init; }
    }

    private sealed record TestQuery : IRequest<string>
    {
        public string QueryResult { get; init; } = string.Empty;
    }

    private sealed record TestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed record TestStreamRequest : IStreamRequest<int>
    {
        public int[] Values { get; init; } = [];
    }

    private sealed class TestRequestHandler : IRequestHandler<TestRequest, int>
    {
        public ValueTask<int> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(request.Value);
        }
    }

    private sealed class TestCommandHandler : IRequestHandler<TestCommand, Unit>
    {
        public ValueTask<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            if (request.ShouldExecute)
            {
                request.Executed = true;
            }

            return ValueTask.FromResult(Unit.Value);
        }
    }

    private sealed class TestCommandWithResultHandler : IRequestHandler<TestCommandWithResult, int>
    {
        public ValueTask<int> Handle(TestCommandWithResult request, CancellationToken cancellationToken)
        {
            return new ValueTask<int>(request.ResultValue);
        }
    }

    private sealed class TestQueryHandler : IRequestHandler<TestQuery, string>
    {
        public ValueTask<string> Handle(TestQuery request, CancellationToken cancellationToken)
        {
            return new ValueTask<string>(request.QueryResult);
        }
    }

    private sealed class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public static string? LastReceivedMessage { get; set; }

        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            LastReceivedMessage = notification.Message;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestStreamRequestHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<int> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var value in request.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return value;
                await Task.Yield();
            }
        }
    }

    #endregion

}
