using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.command;
using GFramework.Core.Abstractions.environment;
using GFramework.Core.Abstractions.events;
using GFramework.Core.Abstractions.ioc;
using GFramework.Core.Abstractions.model;
using GFramework.Core.Abstractions.query;
using GFramework.Core.Abstractions.system;
using GFramework.Core.Abstractions.utility;
using GFramework.Core.architecture;
using GFramework.Core.command;
using GFramework.Core.environment;
using GFramework.Core.events;
using GFramework.Core.ioc;
using GFramework.Core.query;
using NUnit.Framework;

namespace GFramework.Core.Tests.architecture;

/// <summary>
///     ArchitectureServices类的单元测试
///     测试内容包括：
///     - 服务容器初始化
///     - 所有服务实例创建（Container, EventBus, CommandBus, QueryBus）
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

    /// <summary>
    ///     测试构造函数初始化所有服务
    /// </summary>
    [Test]
    public void Constructor_Should_Initialize_AllServices()
    {
        Assert.That(_services!.Container, Is.Not.Null);
        Assert.That(_services.EventBus, Is.Not.Null);
        Assert.That(_services.CommandBus, Is.Not.Null);
        Assert.That(_services.QueryBus, Is.Not.Null);
    }

    [Test]
    public void Container_Should_Be_Instance_Of_IocContainer()
    {
        Assert.That(_services!.Container, Is.InstanceOf<IIocContainer>());
        Assert.That(_services.Container, Is.InstanceOf<IocContainer>());
    }

    /// <summary>
    ///     测试EventBus是EventBus的实例
    /// </summary>
    [Test]
    public void EventBus_Should_Be_Instance_Of_EventBus()
    {
        Assert.That(_services!.EventBus, Is.InstanceOf<IEventBus>());
        Assert.That(_services.EventBus, Is.InstanceOf<EventBus>());
    }

    /// <summary>
    ///     测试CommandBus是CommandBus的实例
    /// </summary>
    [Test]
    public void CommandBus_Should_Be_Instance_Of_CommandBus()
    {
        Assert.That(_services!.CommandBus, Is.InstanceOf<ICommandBus>());
        Assert.That(_services.CommandBus, Is.InstanceOf<CommandBus>());
    }

    /// <summary>
    ///     测试QueryBus是QueryBus的实例
    /// </summary>
    [Test]
    public void QueryBus_Should_Be_Instance_Of_QueryBus()
    {
        Assert.That(_services!.QueryBus, Is.InstanceOf<IQueryBus>());
        Assert.That(_services.QueryBus, Is.InstanceOf<QueryBus>());
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
        var services1 = new ArchitectureServices();
        var services2 = new ArchitectureServices();

        Assert.That(services1.EventBus, Is.Not.SameAs(services2.EventBus));
    }

    /// <summary>
    ///     测试多个实例有独立的CommandBus
    /// </summary>
    [Test]
    public void Multiple_Instances_Should_Have_Independent_CommandBus()
    {
        var services1 = new ArchitectureServices();
        var services2 = new ArchitectureServices();

        Assert.That(services1.CommandBus, Is.Not.SameAs(services2.CommandBus));
    }

    /// <summary>
    ///     测试多个实例有独立的QueryBus
    /// </summary>
    [Test]
    public void Multiple_Instances_Should_Have_Independent_QueryBus()
    {
        var services1 = new ArchitectureServices();
        var services2 = new ArchitectureServices();

        Assert.That(services1.QueryBus, Is.Not.SameAs(services2.QueryBus));
    }
}

#region Test Classes

public class TestArchitectureContextV3 : IArchitectureContext
{
    private readonly IocContainer _container = new();
    private readonly DefaultEnvironment _environment = new();
    public int Id { get; init; }

    public IIocContainer Container => _container;
    public IEventBus EventBus => new EventBus();
    public ICommandBus CommandBus => new CommandBus();
    public IQueryBus QueryBus => new QueryBus();

    public TService? GetService<TService>() where TService : class
    {
        return _container.Get<TService>();
    }

    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return _container.Get<TModel>();
    }

    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _container.Get<TSystem>();
    }

    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _container.Get<TUtility>();
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

    public void SendCommand(ICommand command)
    {
    }

    public TResult SendCommand<TResult>(ICommand<TResult> command)
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

    public TResult SendQuery<TResult>(IQuery<TResult> query)
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