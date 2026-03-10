using GFramework.Core.Abstractions.Architecture;
using GFramework.Core.Abstractions.Command;
using GFramework.Core.Abstractions.Environment;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.IoC;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Query;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Command;
using GFramework.Core.Environment;
using GFramework.Core.Events;
using GFramework.Core.IoC;
using GFramework.Core.Query;
using Mediator;
using ICommand = GFramework.Core.Abstractions.Command.ICommand;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     GameContext类的单元测试
///     测试内容包括：
///     - ArchitectureReadOnlyDictionary在启动时为空
///     - Bind方法添加上下文到字典
///     - Bind重复类型时抛出异常
///     - GetByType返回正确的上下文
///     - GetByType未找到时抛出异常
///     - Get泛型方法返回正确的上下文
///     - TryGet方法在找到时返回true
///     - TryGet方法在未找到时返回false
///     - GetFirstArchitectureContext在存在时返回
///     - GetFirstArchitectureContext为空时抛出异常
///     - Unbind移除上下文
///     - Clear移除所有上下文
/// </summary>
[TestFixture]
public class GameContextTests
{
    /// <summary>
    ///     测试初始化方法，在每个测试方法执行前清空GameContext
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     测试清理方法，在每个测试方法执行后清空GameContext
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    /// <summary>
    ///     测试ArchitectureReadOnlyDictionary在启动时返回空字典
    /// </summary>
    [Test]
    public void ArchitectureReadOnlyDictionary_Should_Return_Empty_At_Start()
    {
        var dict = GameContext.ArchitectureReadOnlyDictionary;

        Assert.That(dict.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试Bind方法是否正确将上下文添加到字典中
    /// </summary>
    [Test]
    public void Bind_Should_Add_Context_To_Dictionary()
    {
        var context = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), context);

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试Bind方法在绑定重复类型时是否抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void Bind_WithDuplicateType_Should_ThrowInvalidOperationException()
    {
        var context1 = new TestArchitectureContext();
        var context2 = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), context1);

        Assert.Throws<InvalidOperationException>(() =>
            GameContext.Bind(typeof(TestArchitecture), context2));
    }

    /// <summary>
    ///     测试GetByType方法是否返回正确的上下文
    /// </summary>
    [Test]
    public void GetByType_Should_Return_Correct_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetByType(typeof(TestArchitecture));

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试GetByType方法在未找到对应类型时是否抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void GetByType_Should_Throw_When_Not_Found()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GameContext.GetByType(typeof(TestArchitecture)));
    }

    /// <summary>
    ///     测试Get泛型方法是否返回正确的上下文
    /// </summary>
    [Test]
    public void GetGeneric_Should_Return_Correct_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var result = GameContext.Get<TestArchitectureContext>();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试TryGet方法在找到上下文时是否返回true并正确设置输出参数
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnTrue_When_Found()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }

    /// <summary>
    ///     测试TryGet方法在未找到上下文时是否返回false且输出参数为null
    /// </summary>
    [Test]
    public void TryGet_Should_ReturnFalse_When_Not_Found()
    {
        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.False);
        Assert.That(foundContext, Is.Null);
    }

    /// <summary>
    ///     测试GetFirstArchitectureContext方法在存在上下文时是否返回正确的上下文
    /// </summary>
    [Test]
    public void GetFirstArchitectureContext_Should_Return_When_Exists()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetFirstArchitectureContext();

        Assert.That(result, Is.SameAs(context));
    }

    /// <summary>
    ///     测试GetFirstArchitectureContext方法在没有上下文时是否抛出InvalidOperationException异常
    /// </summary>
    [Test]
    public void GetFirstArchitectureContext_Should_Throw_When_Empty()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GameContext.GetFirstArchitectureContext());
    }

    /// <summary>
    ///     测试Unbind方法是否正确移除指定类型的上下文
    /// </summary>
    [Test]
    public void Unbind_Should_Remove_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        GameContext.Unbind(typeof(TestArchitecture));

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试Clear方法是否正确移除所有上下文
    /// </summary>
    [Test]
    public void Clear_Should_Remove_All_Contexts()
    {
        GameContext.Bind(typeof(TestArchitecture), new TestArchitectureContext());
        GameContext.Bind(typeof(TestArchitectureContext), new TestArchitectureContext());

        GameContext.Clear();

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }
}

/// <summary>
///     测试用的架构类，继承自Architecture
/// </summary>
public class TestArchitecture : Architecture
{
    /// <summary>
    ///     初始化方法，当前为空实现
    /// </summary>
    protected override void OnInitialize()
    {
    }
}

/// <summary>
///     测试用的架构上下文类，实现了IArchitectureContext接口
/// </summary>
public class TestArchitectureContext : IArchitectureContext
{
    private readonly MicrosoftDiContainer _container = new();

    /// <summary>
    ///     获取依赖注入容器
    /// </summary>
    public IIocContainer Container => _container;

    /// <summary>
    ///     获取事件总线
    /// </summary>
    public IEventBus EventBus => new EventBus();

    /// <summary>
    ///     获取命令总线
    /// </summary>
    public ICommandExecutor CommandExecutor => new CommandExecutor();

    /// <summary>
    ///     获取查询总线
    /// </summary>
    public IQueryExecutor QueryExecutor => new QueryExecutor();

    /// <summary>
    ///     获取环境对象
    /// </summary>
    public IEnvironment Environment => new DefaultEnvironment();

    /// <summary>
    ///     获取指定类型的服务
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>服务实例或null</returns>
    public TService? GetService<TService>() where TService : class
    {
        return _container.Get<TService>();
    }

    /// <summary>
    ///     获取指定类型的所有服务
    /// </summary>
    /// <typeparam name="TService">服务类型</typeparam>
    /// <returns>所有服务实例列表</returns>
    public IReadOnlyList<TService> GetServices<TService>() where TService : class
    {
        return _container.GetAll<TService>();
    }

    /// <summary>
    ///     获取指定类型的模型
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <returns>模型实例或null</returns>
    public TModel? GetModel<TModel>() where TModel : class, IModel
    {
        return _container.Get<TModel>();
    }

    /// <summary>
    ///     获取指定类型的所有模型
    /// </summary>
    /// <typeparam name="TModel">模型类型</typeparam>
    /// <returns>所有模型实例列表</returns>
    public IReadOnlyList<TModel> GetModels<TModel>() where TModel : class, IModel
    {
        return _container.GetAll<TModel>();
    }

    /// <summary>
    ///     获取指定类型的系统
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <returns>系统实例或null</returns>
    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem
    {
        return _container.Get<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的所有系统
    /// </summary>
    /// <typeparam name="TSystem">系统类型</typeparam>
    /// <returns>所有系统实例列表</returns>
    public IReadOnlyList<TSystem> GetSystems<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAll<TSystem>();
    }

    /// <summary>
    ///     获取指定类型的工具
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <returns>工具实例或null</returns>
    public virtual TUtility? GetUtility<TUtility>() where TUtility : class, IUtility
    {
        return _container.Get<TUtility>();
    }

    /// <summary>
    ///     获取指定类型的所有工具
    /// </summary>
    /// <typeparam name="TUtility">工具类型</typeparam>
    /// <returns>所有工具实例列表</returns>
    public IReadOnlyList<TUtility> GetUtilities<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAll<TUtility>();
    }

    public IReadOnlyList<TService> GetServicesByPriority<TService>() where TService : class
    {
        return _container.GetAllByPriority<TService>();
    }

    public IReadOnlyList<TSystem> GetSystemsByPriority<TSystem>() where TSystem : class, ISystem
    {
        return _container.GetAllByPriority<TSystem>();
    }

    public IReadOnlyList<TModel> GetModelsByPriority<TModel>() where TModel : class, IModel
    {
        return _container.GetAllByPriority<TModel>();
    }

    public IReadOnlyList<TUtility> GetUtilitiesByPriority<TUtility>() where TUtility : class, IUtility
    {
        return _container.GetAllByPriority<TUtility>();
    }

    /// <summary>
    ///     发送事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public void SendEvent<TEvent>() where TEvent : new()
    {
    }

    /// <summary>
    ///     发送事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="e">事件实例</param>
    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
    }

    /// <summary>
    ///     注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <returns>取消注册接口</returns>
    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler)
    {
        return new DefaultUnRegister(() => { });
    }

    /// <summary>
    ///     取消注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="onEvent">事件处理委托</param>
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

    public ValueTask<TResponse> SendCommandAsync<TResponse>(global::Mediator.ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public TResponse SendCommand<TResponse>(global::Mediator.ICommand<TResponse> command)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TResponse> SendQueryAsync<TResponse>(global::Mediator.IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public TResponse SendQuery<TResponse>(global::Mediator.IQuery<TResponse> query)
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

    /// <summary>
    ///     发送命令
    /// </summary>
    /// <param name="command">命令对象</param>
    public void SendCommand(ICommand command)
    {
    }

    /// <summary>
    ///     发送带返回值的命令
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="command">命令对象</param>
    /// <returns>命令执行结果</returns>
    public TResult SendCommand<TResult>(Abstractions.Command.ICommand<TResult> command)
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

    /// <summary>
    ///     发送查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <returns>查询结果</returns>
    public TResult SendQuery<TResult>(Abstractions.Query.IQuery<TResult> query)
    {
        return default!;
    }

    /// <summary>
    ///     异步发送查询请求
    /// </summary>
    /// <typeparam name="TResult">查询结果类型</typeparam>
    /// <param name="query">异步查询对象</param>
    /// <returns>查询结果</returns>
    public Task<TResult> SendQueryAsync<TResult>(IAsyncQuery<TResult> query)
    {
        return (Task<TResult>)Task.CompletedTask;
    }

    /// <summary>
    ///     获取环境对象
    /// </summary>
    /// <returns>环境对象</returns>
    public IEnvironment GetEnvironment()
    {
        return Environment;
    }
}