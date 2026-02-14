using System.Reflection;
using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.enums;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
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
        _container.RegisterMediator(options => { options.ServiceLifetime = ServiceLifetime.Singleton; });

        // ✅ 手动注册 Mediator Handlers
        _container.Services.AddSingleton<IRequestHandler<TestRequest, int>, TestRequestHandler>();
        _container.Services.AddSingleton<IRequestHandler<TestCommand, Unit>, TestCommandHandler>();
        _container.Services.AddSingleton<IRequestHandler<TestCommandWithResult, int>, TestCommandWithResultHandler>();
        _container.Services.AddSingleton<IRequestHandler<TestQuery, string>, TestQueryHandler>();
        _container.Services.AddSingleton<INotificationHandler<TestNotification>, TestNotificationHandler>();
        _container.Services.AddSingleton<IStreamRequestHandler<TestStreamRequest, int>, TestStreamRequestHandler>();

        // 注册测试组件（Legacy）
        _testSystem = new TestSystem();
        _testModel = new TestModel();
        _testUtility = new TestUtility();
        _testCommand = new TestTraditionalCommand();
        _testQuery = new TestTraditionalQuery { Result = 999 };

        _container.RegisterPlurality(_testSystem);
        _container.RegisterPlurality(_testModel);
        _container.RegisterPlurality(_testUtility);
        _container.RegisterPlurality(_testCommand);
        _container.RegisterPlurality(_testQuery);

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
        _testSystem = null;
        _testModel = null;
        _testUtility = null;
        _testCommand = null;
        _testQuery = null;
    }

    private ArchitectureContext? _context;
    private MicrosoftDiContainer? _container;
    private EventBus? _eventBus;
    private CommandExecutor? _commandBus;
    private QueryExecutor? _queryBus;
    private AsyncQueryExecutor? _asyncQueryBus;
    private DefaultEnvironment? _environment;
    private TestSystem? _testSystem;
    private TestModel? _testModel;
    private TestUtility? _testUtility;
    private TestTraditionalCommand? _testCommand;
    private TestTraditionalQuery? _testQuery;

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
    public async Task Mediator_And_CommandExecutor_Should_Coexist()
    {
        // 使用传统方式（Legacy）
        _context!.SendCommand(_testCommand!);
        Assert.That(_testCommand!.Executed, Is.True);

        // 使用 Mediator 方式
        var mediatorCommand = new TestCommandWithResult { ResultValue = 123 };
        var result = await _context.SendAsync(mediatorCommand);
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public async Task Mediator_And_QueryExecutor_Should_Coexist()
    {
        // 使用传统方式（Legacy）
        var traditionalResult = _context!.SendQuery(_testQuery!);
        Assert.That(traditionalResult, Is.EqualTo(999));

        // 使用 Mediator 方式
        var mediatorQuery = new TestQuery { QueryResult = "mediator result" };
        var mediatorResult = await _context.QueryAsync(mediatorQuery);
        Assert.That(mediatorResult, Is.EqualTo("mediator result"));
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
    public void Architecture_Component_Getters_Should_Work()
    {
        var system = _context!.GetSystem<TestSystem>();
        var model = _context.GetModel<TestModel>();
        var utility = _context.GetUtility<TestUtility>();
        var environment = _context.GetEnvironment();

        Assert.That(system, Is.SameAs(_testSystem));
        Assert.That(model, Is.SameAs(_testModel));
        Assert.That(utility, Is.SameAs(_testUtility));
        Assert.That(environment, Is.SameAs(_environment));
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

#region Test Classes - Legacy CQRS (旧实现)

public class TestSystem : ISystem
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;

    public void Init()
    {
    }

    public void Destroy()
    {
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}

public class TestModel : IModel
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;

    public void Init()
    {
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }

    public void Destroy()
    {
    }
}

public class TestUtility : IUtility
{
    private IArchitectureContext _context = null!;
    public int Id { get; init; }

    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

// ✅ 使用你框架的 ICommand
public class TestTraditionalCommand : ICommand
{
    private IArchitectureContext _context = null!;
    public bool Executed { get; private set; }

    public void Execute() => Executed = true;
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

// ✅ 使用你框架的 IQuery
public class TestTraditionalQuery : IQuery<int>
{
    private IArchitectureContext _context = null!;
    public int Result { get; init; }

    public int Do() => Result;
    public void SetContext(IArchitectureContext context) => _context = context;
    public IArchitectureContext GetContext() => _context;
}

#endregion