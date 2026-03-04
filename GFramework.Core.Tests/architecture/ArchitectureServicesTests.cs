using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.query;
using Mediator;
using NUnit.Framework;
using ICommand = GFramework.Core.Abstractions.command.ICommand;

namespace GFramework.Core.Tests.architecture;

/// <summary>
///     ArchitectureServices类的单元测试
///     测试内容包括：
///     - 服务容器初始化
///     - 所有服务实例创建（Container, EventBus, CommandExecutor, QueryExecutor）
///     - SetContext方法 - 设置上下文
///     - SetContext方法 - 重复设置上下文
///     - GetContext方法 - 获取已设置上下文
///     - GetContext方法 - 未设置上下文时返回null
///     - 上下文传播到容器
///     - IArchitectureServices接口实现验证
///     - 服务独立性验证（多个实例）
/// </summary>
[TestFixture]
public class ArchitectureServicesTests
{
    [SetUp]
    public void SetUp()
    {
        _services = new ArchitectureServices();
        _context = new TestArchitectureContextV3();
    }

    private ArchitectureServices? _services;
    private TestArchitectureContextV3? _context;

    private void RegisterBuiltInServices()
    {
        var properties = new ArchitectureProperties();
        _services!.ModuleManager.RegisterBuiltInModules(_services.Container, properties);
    }

    /// <summary>
    ///     测试构造函数初始化容器
    /// </summary>
    [Test]
    public void Constructor_Should_Initialize_Container()
    {
        Assert.That(_services!.Container, Is.Not.Null);
    }

    [Test]
    public void Container_Should_Be_Instance_Of_IocContainer()
    {
        Assert.That(_services!.Container, Is.InstanceOf<IIocContainer>());
        Assert.That(_services.Container, Is.InstanceOf<MicrosoftDiContainer>());
    }

    /// <summary>
    ///     测试注册内置服务后EventBus可用
    /// </summary>
    [Test]
    public void After_RegisterBuiltInModules_EventBus_Should_Be_Available()
    {
        RegisterBuiltInServices();

        Assert.That(_services!.EventBus, Is.InstanceOf<IEventBus>());
        Assert.That(_services.EventBus, Is.InstanceOf<EventBus>());
    }

    /// <summary>
    ///     测试注册内置服务后CommandExecutor可用
    /// </summary>
    [Test]
    public void After_RegisterBuiltInModules_CommandExecutor_Should_Be_Available()
    {
        RegisterBuiltInServices();

        Assert.That(_services!.CommandExecutor, Is.InstanceOf<ICommandExecutor>());
        Assert.That(_services.CommandExecutor, Is.InstanceOf<CommandExecutor>());
    }

    /// <summary>
    ///     测试注册内置服务后QueryExecutor可用
    /// </summary>
    [Test]
    public void After_RegisterBuiltInModules_QueryExecutor_Should_Be_Available()
    {
        RegisterBuiltInServices();

        Assert.That(_services!.QueryExecutor, Is.InstanceOf<IQueryExecutor>());
        Assert.That(_services.QueryExecutor, Is.InstanceOf<QueryExecutor>());
    }

    /// <summary>
    ///     测试注册内置服务后AsyncQueryExecutor可用
    /// </summary>
    [Test]
    public void After_RegisterBuiltInModules_AsyncQueryExecutor_Should_Be_Available()
    {
        RegisterBuiltInServices();

        Assert.That(_services!.AsyncQueryExecutor, Is.InstanceOf<IAsyncQueryExecutor>());
        Assert.That(_services.AsyncQueryExecutor, Is.InstanceOf<AsyncQueryExecutor>());
    }

    /// <summary>
    ///     测试未注册服务时EventBus为null
    /// </summary>
    [Test]
    public void Without_RegisterBuiltInModules_EventBus_Should_Be_Null()
    {
        Assert.That(_services!.EventBus, Is.Null);
    }

    /// <summary>
    ///     测试SetContext设置内部Context字段
    /// </summary>
    [Test]
    public void SetContext_Should_Set_Context_Internal_Field()
    {
        _services!.SetContext(_context!);

        var context = _services.GetContext();
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试SetContext将上下文传播到Container
    /// </summary>
    [Test]
    public void SetContext_Should_Propagate_Context_To_Container()
    {
        _services!.SetContext(_context!);

        var containerContext = _services.Container.GetContext();
        Assert.That(containerContext, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试GetContext在SetContext后返回上下文
    /// </summary>
    [Test]
    public void GetContext_Should_Return_Context_After_SetContext()
    {
        _services!.SetContext(_context!);

        var context = _services.GetContext();
        Assert.That(context, Is.Not.Null);
        Assert.That(context, Is.SameAs(_context));
    }

    /// <summary>
    ///     测试GetContext在未设置上下文时返回null
    /// </summary>
    [Test]
    public void GetContext_Should_ReturnNull_When_Context_Not_Set()
    {
        var context = _services!.GetContext();

        Assert.That(context, Is.Null);
    }

    /// <summary>
    ///     测试SetContext替换已存在的上下文
    /// </summary>
    [Test]
    public void SetContext_Should_Replace_Existing_Context()
    {
        var context1 = new TestArchitectureContextV3 { Id = 1 };
        var context2 = new TestArchitectureContextV3 { Id = 2 };

        _services!.SetContext(context1);
        _services.SetContext(context2);

        var context = _services.GetContext();
        Assert.That(context, Is.SameAs(context2));
    }

    /// <summary>
    ///     测试ArchitectureServices实现IArchitectureServices接口
    /// </summary>
    [Test]
    public void ArchitectureServices_Should_Implement_IArchitectureServices_Interface()
    {
        Assert.That(_services, Is.InstanceOf<IArchitectureServices>());
    }

    /// <summary>
    ///     测试多个实例有独立的Container
    /// </summary>
    [Test]
    public void Multiple_Instances_Should_Have_Independent_Container()
    {
        var services1 = new ArchitectureServices();
        var services2 = new ArchitectureServices();

        Assert.That(services1.Container, Is.Not.SameAs(services2.Container));
    }

    /// <summary>
    ///     测试多个实例有独立的EventBus
    /// </summary>
    [Test]
    public void Multiple_Instances_Should_Have_Independent_EventBus()
    {
        var properties = new ArchitectureProperties();

        var services1 = new ArchitectureServices();
        services1.ModuleManager.RegisterBuiltInModules(services1.Container, properties);

        var services2 = new ArchitectureServices();
        services2.ModuleManager.RegisterBuiltInModules(services2.Container, properties);

        Assert.That(services1.EventBus, Is.Not.SameAs(services2.EventBus));
    }

    /// <summary>
    ///     测试多个实例有独立的CommandBus
    /// </summary>
    [Test]
    public void Multiple_Instances_Should_Have_Independent_CommandBus()
    {
        var properties = new ArchitectureProperties();

        var services1 = new ArchitectureServices();
        services1.ModuleManager.RegisterBuiltInModules(services1.Container, properties);

        var services2 = new ArchitectureServices();
        services2.ModuleManager.RegisterBuiltInModules(services2.Container, properties);

        Assert.That(services1.CommandExecutor, Is.Not.SameAs(services2.CommandExecutor));
    }

    /// <summary>
    ///     测试多个实例有独立的QueryBus
    /// </summary>
    [Test]
    public void Multiple_Instances_Should_Have_Independent_QueryBus()
    {
        var properties = new ArchitectureProperties();

        var services1 = new ArchitectureServices();
        services1.ModuleManager.RegisterBuiltInModules(services1.Container, properties);

        var services2 = new ArchitectureServices();
        services2.ModuleManager.RegisterBuiltInModules(services2.Container, properties);

        Assert.That(services1.QueryExecutor, Is.Not.SameAs(services2.QueryExecutor));
    }

    /// <summary>
    ///     测试ModuleManager属性不为空
    /// </summary>
    [Test]
    public void ModuleManager_Should_Not_Be_Null()
    {
        Assert.That(_services!.ModuleManager, Is.Not.Null);
    }

    /// <summary>
    ///     测试EnableEcs配置开关
    /// </summary>
    [Test]
    public void EnableEcs_Should_Control_Ecs_Module_Registration()
    {
        var propertiesWithEcs = new ArchitectureProperties { EnableEcs = true };
        var propertiesWithoutEcs = new ArchitectureProperties { EnableEcs = false };

        var servicesWithEcs = new ArchitectureServices();
        servicesWithEcs.ModuleManager.RegisterBuiltInModules(servicesWithEcs.Container, propertiesWithEcs);

        var servicesWithoutEcs = new ArchitectureServices();
        servicesWithoutEcs.ModuleManager.RegisterBuiltInModules(servicesWithoutEcs.Container, propertiesWithoutEcs);

        var modulesWithEcs = servicesWithEcs.ModuleManager.GetModules();
        var modulesWithoutEcs = servicesWithoutEcs.ModuleManager.GetModules();

        Assert.That(modulesWithEcs.Count, Is.GreaterThan(modulesWithoutEcs.Count));
    }
}

#region Test Classes

public class TestArchitectureContextV3 : IArchitectureContext
{
    private readonly MicrosoftDiContainer _container = new();
    private readonly DefaultEnvironment _environment = new();
    public int Id { get; init; }

    public TService? GetService<TService>() where TService : class
    {
        return _container.Get<TService>();
    }

    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _container.GetAll<TService>();
    }

    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return _container.Get<TModel>();
    }

    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _container.GetAll<TModel>();
    }

    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _container.Get<TSystem>();
    }

    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAll<TSystem>();
    }

    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _container.Get<TUtility>();
    }

    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAll<TUtility>();
    }

    public void SendEvent<TEvent>() where TEvent : new()
    {
    }

    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
    }

    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return new DefaultUnRegister(() => { });
    }

    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
    }

    public ValueTask<TResponse> SendRequestAsync<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public TResponse SendRequest<TResponse>(IRequest<TResponse> request)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TResponse> SendCommandAsync<TResponse>(Mediator.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public TResponse SendCommand<TResponse>(Mediator.ICommand<TResponse> command)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TResponse> SendQueryAsync<TResponse>(Mediator.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public TResponse SendQuery<TResponse>(Mediator.IQuery<TResponse> query)
    {
        throw new NotImplementedException();
    }

    public ValueTask PublishAsync<TNotification>(TNotification notification,
        CancellationToken cancellationToken = default) where TNotification : INotification
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IRequest<Unit>
    {
        throw new NotImplementedException();
    }

    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void SendCommand(ICommand command)
    {
    }

    public TResult SendCommand<TResult>(Abstractions.command.ICommand<TResult> command)
    {
        return default!;
    }

    public Task SendCommandAsync(IAsyncCommand command)
    {
        return Task.CompletedTask;
    }

    public Task<TResult> SendCommandAsync<TResult>(IAsyncCommand<TResult> command)
    {
        return (Task<TResult>)Task.CompletedTask;
    }

    public TResult SendQuery<TResult>(Abstractions.query.IQuery<TResult> query)
    {
        return default!;
    }

    public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        return (Task<TResult>)Task.CompletedTask;
    }

    public IEnvironment GetEnvironment()
    {
        return _environment;
    }
}

#endregion