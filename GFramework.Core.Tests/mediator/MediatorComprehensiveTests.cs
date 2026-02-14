using System.Reflection;
using System.Runtime.CompilerServices;
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
}

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