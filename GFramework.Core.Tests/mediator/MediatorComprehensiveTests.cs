using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.events;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.query;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ICommand = GFramework.Core.Abstractions.command.ICommand;

// ✅ Mediator 库的命名空间

// ✅ 使用 global using 或别名来区分

namespace GFramework.Core.Tests.mediator;

[TestFixture]
public class MediatorComprehensiveTests
{
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new MicrosoftDiContainer();

        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(MediatorComprehensiveTests)));

        // 注册基础服务（Legacy CQRS）
        _eventBus = new EventBus();
        _commandBus = new CommandExecutor();
        _queryBus = new QueryExecutor();
        _asyncQueryBus = new AsyncQueryExecutor();
        _environment = new DefaultEnvironment();

        _container.RegisterPlurality(_eventBus);
        _container.RegisterPlurality(_commandBus);
        _container.RegisterPlurality(_queryBus);
        _container.RegisterPlurality(_asyncQueryBus);
        _container.RegisterPlurality(_environment);

        // ✅ 注册 Mediator
        _container.ExecuteServicesHook(configurator =>
        {
            configurator.AddMediator(options => { options.ServiceLifetime = ServiceLifetime.Singleton; });
        });

        // ✅ Freeze 容器
        _container.Freeze();

        _context = new ArchitectureContext(_container);
    }

    [TearDown]
    public void TearDown()
    {
        _context = null;
        _container = null;
        _eventBus = null;
        _commandBus = null;
        _queryBus = null;
        _asyncQueryBus = null;
        _environment = null;
    }

    private ArchitectureContext? _context;
    private MicrosoftDiContainer? _container;
    private EventBus? _eventBus;
    private CommandExecutor? _commandBus;
    private QueryExecutor? _queryBus;
    private AsyncQueryExecutor? _asyncQueryBus;
    private DefaultEnvironment? _environment;

    [Test]
    public async Task SendRequestAsync_Should_ReturnResult_When_Request_IsValid()
    {
        var testRequest = new TestRequest { Value = 42 };
        var result = await _context!.SendRequestAsync(testRequest);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void SendRequestAsync_Should_ThrowArgumentNullException_When_Request_IsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _context!.SendRequestAsync<int>(null!));
    }

    [Test]
    public void SendRequest_Should_ReturnResult_When_Request_IsValid()
    {
        var testRequest = new TestRequest { Value = 123 };
        var result = _context!.SendRequest(testRequest);

        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public async Task PublishAsync_Should_PublishNotification_When_Notification_IsValid()
    {
        TestNotificationHandler.LastReceivedMessage = null;
        var notification = new TestNotification { Message = "test" };

        await _context!.PublishAsync(notification);
        await Task.Delay(100);

        Assert.That(TestNotificationHandler.LastReceivedMessage, Is.EqualTo("test"));
    }

    [Test]
    public async Task CreateStream_Should_ReturnStream_When_StreamRequest_IsValid()
    {
        var testStreamRequest = new TestStreamRequest { Values = [1, 2, 3, 4, 5] };
        var stream = _context!.CreateStream(testStreamRequest);

        var results = new List<int>();
        await foreach (var item in stream)
        {
            results.Add(item);
        }

        Assert.That(results, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task SendAsync_CommandWithoutResult_Should_Execute_When_Command_IsValid()
    {
        var testCommand = new TestCommand { ShouldExecute = true };
        await _context!.SendAsync(testCommand);

        Assert.That(testCommand.Executed, Is.True);
    }

    [Test]
    public async Task SendAsync_CommandWithResult_Should_ReturnResult_When_Command_IsValid()
    {
        var testCommand = new TestCommandWithResult { ResultValue = 42 };
        var result = await _context!.SendAsync(testCommand);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task QueryAsync_Should_ReturnResult_When_Query_IsValid()
    {
        var testQuery = new TestQuery { QueryResult = "test result" };
        var result = await _context!.QueryAsync(testQuery);

        Assert.That(result, Is.EqualTo("test result"));
    }

    [Test]
    public async Task PublishEventAsync_Should_PublishNotification_When_Notification_IsValid()
    {
        TestNotificationHandler.LastReceivedMessage = null;
        var testNotification = new TestNotification { Message = "test event" };

        await _context!.PublishEventAsync(testNotification);
        await Task.Delay(100);

        Assert.That(TestNotificationHandler.LastReceivedMessage, Is.EqualTo("test event"));
    }

    [Test]
    public void GetService_Should_Use_Cache()
    {
        var firstResult = _context!.GetService<IEventBus>();
        Assert.That(firstResult, Is.Not.Null);
        Assert.That(firstResult, Is.SameAs(_eventBus));

        var secondResult = _context.GetService<IEventBus>();
        Assert.That(secondResult, Is.SameAs(firstResult));
    }


    [Test]
    public void Unregistered_Mediator_Should_Throw_InvalidOperationException()
    {
        var containerWithoutMediator = new MicrosoftDiContainer();
        containerWithoutMediator.Freeze();

        var contextWithoutMediator = new ArchitectureContext(containerWithoutMediator);
        var testRequest = new TestRequest { Value = 42 };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await contextWithoutMediator.SendRequestAsync(testRequest));
    }

    [Test]
    public async Task Multiple_Notification_Handlers_Should_All_Be_Invoked()
    {
        // 重置静态字段
        TestNotificationHandler.LastReceivedMessage = null;
        TestNotificationHandler2.LastReceivedMessage = null;
        TestNotificationHandler3.LastReceivedMessage = null;

        var notification = new TestNotification { Message = "multi-handler test" };
        await _context!.PublishAsync(notification);
        await Task.Delay(100);

        // 验证所有处理器都被调用
        Assert.That(TestNotificationHandler.LastReceivedMessage, Is.EqualTo("multi-handler test"));
        Assert.That(TestNotificationHandler2.LastReceivedMessage, Is.EqualTo("multi-handler test"));
        Assert.That(TestNotificationHandler3.LastReceivedMessage, Is.EqualTo("multi-handler test"));
    }

    [Test]
    public async Task CancellationToken_Should_Cancel_Long_Running_Request()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var longRequest = new TestLongRunningRequest { DelayMs = 1000 };

        // 应该在50ms后被取消
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _context!.SendRequestAsync(longRequest, cts.Token));
    }

    [Test]
    public async Task CancellationToken_Should_Cancel_Stream_Request()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var longStreamRequest = new TestLongStreamRequest { ItemCount = 1000 };

        var stream = _context!.CreateStream(longStreamRequest, cts.Token);
        var results = new List<int>();

        // 流应该在100ms后被取消
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await foreach (var item in stream.WithCancellation(cts.Token))
            {
                results.Add(item);
            }
        });

        // 验证只处理了部分数据
        Assert.That(results.Count, Is.LessThan(1000));
    }

    [Test]
    public async Task Concurrent_Mediator_Requests_Should_Not_Interfere()
    {
        const int requestCount = 10;
        var tasks = new List<Task<int>>();

        // 并发发送多个请求
        for (int i = 0; i < requestCount; i++)
        {
            var request = new TestRequest { Value = i };
            tasks.Add(_context!.SendRequestAsync(request).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // 验证所有结果都正确返回
        Assert.That(results.Length, Is.EqualTo(requestCount));
        Assert.That(results.OrderBy(x => x), Is.EqualTo(Enumerable.Range(0, requestCount)));
    }

    [Test]
    public async Task Handler_Exception_Should_Be_Propagated()
    {
        var faultyRequest = new TestFaultyRequest();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _context!.SendRequestAsync(faultyRequest));
    }

    [Test]
    public async Task Multiple_Command_Handlers_Can_Modify_Same_Object()
    {
        var sharedData = new SharedData();
        var command1 = new TestModifyDataCommand { Data = sharedData, Value = 10 };
        var command2 = new TestModifyDataCommand { Data = sharedData, Value = 20 };

        await _context!.SendAsync(command1);
        await _context.SendAsync(command2);

        // 验证数据被正确修改
        Assert.That(sharedData.Value, Is.EqualTo(30)); // 10 + 20
    }

    [Test]
    public async Task Query_Caching_With_Mediator()
    {
        var cache = new Dictionary<string, string>();
        var query1 = new TestCachingQuery { Key = "test1", Cache = cache };
        var query2 = new TestCachingQuery { Key = "test1", Cache = cache }; // 相同key

        var result1 = await _context!.QueryAsync(query1);
        var result2 = await _context.QueryAsync(query2);

        // 验证缓存生效（相同key返回相同结果）
        Assert.That(result1, Is.EqualTo(result2));
        Assert.That(cache.Count, Is.EqualTo(1)); // 只缓存了一次
    }

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
            await _context!.PublishAsync(notification);
        }

        await Task.Delay(200); // 等待所有处理完成

        // 验证接收顺序与发送顺序一致
        Assert.That(receivedOrder.Count, Is.EqualTo(3));
        Assert.That(receivedOrder[0], Is.EqualTo("First"));
        Assert.That(receivedOrder[1], Is.EqualTo("Second"));
        Assert.That(receivedOrder[2], Is.EqualTo("Third"));
    }

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

        await foreach (var item in stream)
        {
            results.Add(item);
        }

        // 验证只返回偶数
        Assert.That(results.All(x => x % 2 == 0), Is.True);
        Assert.That(results, Is.EqualTo(new[] { 2, 4, 6, 8, 10 }));
    }

    [Test]
    public async Task Request_Validation_With_Behaviors()
    {
        var invalidCommand = new TestValidatedCommand { Name = "" }; // 无效：空字符串

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _context!.SendAsync(invalidCommand));
    }

    [Test]
    public async Task Performance_Benchmark_For_Mediator()
    {
        const int iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var request = new TestRequest { Value = i };
            var result = await _context!.SendRequestAsync(request);
            Assert.That(result, Is.EqualTo(i));
        }

        stopwatch.Stop();
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;

        // 验证性能在合理范围内（平均每个请求不超过10ms）
        Assert.That(avgTime, Is.LessThan(10.0));
        Console.WriteLine($"Average time per request: {avgTime:F2}ms");
    }

    [Test]
    public async Task Mediator_And_Legacy_CQRS_Can_Coexist()
    {
        // 使用传统方式
        var legacyCommand = new TestLegacyCommand();
        _context!.SendCommand(legacyCommand);
        Assert.That(legacyCommand.Executed, Is.True);

        // 使用Mediator方式
        var mediatorCommand = new TestCommandWithResult { ResultValue = 999 };
        var result = await _context.SendAsync(mediatorCommand);
        Assert.That(result, Is.EqualTo(999));

        // 验证两者可以同时工作
        Assert.That(legacyCommand.Executed, Is.True);
        Assert.That(result, Is.EqualTo(999));
    }
}

#region Advanced Test Classes for Mediator Features

public sealed record TestLongRunningRequest : IRequest<string>
{
    public int DelayMs { get; init; }
}

public sealed class TestLongRunningRequestHandler : IRequestHandler<TestLongRunningRequest, string>
{
    public async ValueTask<string> Handle(TestLongRunningRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return "Completed";
    }
}

public sealed record TestLongStreamRequest : IStreamRequest<int>
{
    public int ItemCount { get; init; }
}

public sealed class TestLongStreamRequestHandler : IStreamRequestHandler<TestLongStreamRequest, int>
{
    public async IAsyncEnumerable<int> Handle(
        TestLongStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.ItemCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Delay(10, cancellationToken); // 模拟处理延迟
        }
    }
}

public sealed record TestFaultyRequest : IRequest<string>;

public sealed class TestFaultyRequestHandler : IRequestHandler<TestFaultyRequest, string>
{
    public ValueTask<string> Handle(TestFaultyRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler failed intentionally");
    }
}

public class SharedData
{
    public int Value { get; set; }
}

public sealed record TestModifyDataCommand : IRequest<Unit>
{
    public SharedData Data { get; init; } = null!;
    public int Value { get; init; }
}

public sealed class TestModifyDataCommandHandler : IRequestHandler<TestModifyDataCommand, Unit>
{
    public ValueTask<Unit> Handle(TestModifyDataCommand request, CancellationToken cancellationToken)
    {
        request.Data.Value += request.Value;
        return ValueTask.FromResult(Unit.Value);
    }
}

public sealed record TestCachingQuery : IRequest<string>
{
    public string Key { get; init; } = string.Empty;
    public Dictionary<string, string> Cache { get; init; } = new();
}

public sealed class TestCachingQueryHandler : IRequestHandler<TestCachingQuery, string>
{
    public ValueTask<string> Handle(TestCachingQuery request, CancellationToken cancellationToken)
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

public sealed record TestOrderedNotification : INotification
{
    public int Order { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class TestOrderedNotificationHandler : INotificationHandler<TestOrderedNotification>
{
    public static List<string> ReceivedMessages { get; set; } = new();

    public ValueTask Handle(TestOrderedNotification notification, CancellationToken cancellationToken)
    {
        ReceivedMessages.Add(notification.Message);
        return ValueTask.CompletedTask;
    }
}

// 额外的通知处理器来测试多处理器场景
public sealed class TestNotificationHandler2 : INotificationHandler<TestNotification>
{
    public static string? LastReceivedMessage { get; set; }

    public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        LastReceivedMessage = notification.Message;
        return ValueTask.CompletedTask;
    }
}

public sealed class TestNotificationHandler3 : INotificationHandler<TestNotification>
{
    public static string? LastReceivedMessage { get; set; }

    public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        LastReceivedMessage = notification.Message;
        return ValueTask.CompletedTask;
    }
}

public sealed record TestFilterStreamRequest : IStreamRequest<int>
{
    public int[] Values { get; init; } = [];
    public bool FilterEven { get; init; }
}

public sealed class TestFilterStreamRequestHandler : IStreamRequestHandler<TestFilterStreamRequest, int>
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

public sealed record TestValidatedCommand : IRequest<Unit>
{
    public string Name { get; init; } = string.Empty;
}

public sealed class TestValidatedCommandHandler : IRequestHandler<TestValidatedCommand, Unit>
{
    public ValueTask<Unit> Handle(TestValidatedCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(request.Name));
        }

        return ValueTask.FromResult(Unit.Value);
    }
}

// 传统命令用于共存测试
public class TestLegacyCommand : ICommand
{
    public bool Executed { get; private set; }

    public void Execute()
    {
        Executed = true;
    }

    public void SetContext(IArchitectureContext context)
    {
        // 不需要实现
    }

    public IArchitectureContext GetContext()
    {
        return null!;
    }
}

#endregion

#region Test Classes - Mediator (新实现)

// ✅ 这些类使用 Mediator.IRequest
public sealed record TestRequest : IRequest<int>
{
    public int Value { get; init; }
}

public sealed record TestCommand : IRequest<Unit>
{
    public bool ShouldExecute { get; init; }
    public bool Executed { get; set; }
}

public sealed record TestCommandWithResult : IRequest<int>
{
    public int ResultValue { get; init; }
}

public sealed record TestQuery : IRequest<string>
{
    public string QueryResult { get; init; } = string.Empty;
}

public sealed record TestNotification : INotification
{
    public string Message { get; init; } = string.Empty;
}

public sealed record TestStreamRequest : IStreamRequest<int>
{
    public int[] Values { get; init; } = [];
}

// ✅ 这些 Handler 使用 Mediator.IRequestHandler
public sealed class TestRequestHandler : IRequestHandler<TestRequest, int>
{
    public ValueTask<int> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return new ValueTask<int>(request.Value);
    }
}

public sealed class TestCommandHandler : IRequestHandler<TestCommand, Unit>
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

public sealed class TestCommandWithResultHandler : IRequestHandler<TestCommandWithResult, int>
{
    public ValueTask<int> Handle(TestCommandWithResult request, CancellationToken cancellationToken)
    {
        return new ValueTask<int>(request.ResultValue);
    }
}

public sealed class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public ValueTask<string> Handle(TestQuery request, CancellationToken cancellationToken)
    {
        return new ValueTask<string>(request.QueryResult);
    }
}

public sealed class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public static string? LastReceivedMessage { get; set; }

    public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        LastReceivedMessage = notification.Message;
        return ValueTask.CompletedTask;
    }
}

public sealed class TestStreamRequestHandler : IStreamRequestHandler<TestStreamRequest, int>
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