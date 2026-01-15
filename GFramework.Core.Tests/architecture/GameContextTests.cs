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

[TestFixture]
public class GameContextTests
{
    [SetUp]
    public void SetUp()
    {
        GameContext.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
    }

    [Test]
    public void ArchitectureReadOnlyDictionary_Should_Return_Empty_At_Start()
    {
        var dict = GameContext.ArchitectureReadOnlyDictionary;

        Assert.That(dict.Count, Is.EqualTo(0));
    }

    [Test]
    public void Bind_Should_Add_Context_To_Dictionary()
    {
        var context = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), context);

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(1));
    }

    [Test]
    public void Bind_WithDuplicateType_Should_ThrowInvalidOperationException()
    {
        var context1 = new TestArchitectureContext();
        var context2 = new TestArchitectureContext();

        GameContext.Bind(typeof(TestArchitecture), context1);

        Assert.Throws<InvalidOperationException>(() =>
            GameContext.Bind(typeof(TestArchitecture), context2));
    }

    [Test]
    public void GetByType_Should_Return_Correct_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetByType(typeof(TestArchitecture));

        Assert.That(result, Is.SameAs(context));
    }

    [Test]
    public void GetByType_Should_Throw_When_Not_Found()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GameContext.GetByType(typeof(TestArchitecture)));
    }

    [Test]
    public void GetGeneric_Should_Return_Correct_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var result = GameContext.Get<TestArchitectureContext>();

        Assert.That(result, Is.SameAs(context));
    }

    [Test]
    public void TryGet_Should_ReturnTrue_When_Found()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitectureContext), context);

        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.True);
        Assert.That(foundContext, Is.SameAs(context));
    }

    [Test]
    public void TryGet_Should_ReturnFalse_When_Not_Found()
    {
        var result = GameContext.TryGet(out TestArchitectureContext? foundContext);

        Assert.That(result, Is.False);
        Assert.That(foundContext, Is.Null);
    }

    [Test]
    public void GetFirstArchitectureContext_Should_Return_When_Exists()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        var result = GameContext.GetFirstArchitectureContext();

        Assert.That(result, Is.SameAs(context));
    }

    [Test]
    public void GetFirstArchitectureContext_Should_Throw_When_Empty()
    {
        Assert.Throws<InvalidOperationException>(() =>
            GameContext.GetFirstArchitectureContext());
    }

    [Test]
    public void Unbind_Should_Remove_Context()
    {
        var context = new TestArchitectureContext();
        GameContext.Bind(typeof(TestArchitecture), context);

        GameContext.Unbind(typeof(TestArchitecture));

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }

    [Test]
    public void Clear_Should_Remove_All_Contexts()
    {
        GameContext.Bind(typeof(TestArchitecture), new TestArchitectureContext());
        GameContext.Bind(typeof(TestArchitectureContext), new TestArchitectureContext());

        GameContext.Clear();

        Assert.That(GameContext.ArchitectureReadOnlyDictionary.Count, Is.EqualTo(0));
    }
}

public class TestArchitecture : Architecture
{
    protected override void Init()
    {
    }
}

public class TestArchitectureContext : IArchitectureContext
{
    private readonly IocContainer _container = new();

    public IIocContainer Container => _container;
    public IEventBus EventBus => new EventBus();
    public ICommandBus CommandBus => new CommandBus();
    public IQueryBus QueryBus => new QueryBus();
    public IEnvironment Environment => new DefaultEnvironment();

    public TModel? GetModel<TModel>() where TModel : class, IModel => _container.Get<TModel>();
    public TSystem? GetSystem<TSystem>() where TSystem : class, ISystem => _container.Get<TSystem>();
    public TUtility? GetUtility<TUtility>() where TUtility : class, IUtility => _container.Get<TUtility>();

    public void SendEvent<TEvent>() where TEvent : new()
    {
    }

    public void SendEvent<TEvent>(TEvent e) where TEvent : class
    {
    }

    public IUnRegister RegisterEvent<TEvent>(Action<TEvent> handler) => new DefaultUnRegister(() => { });

    public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
    {
    }

    public void SendCommand(ICommand command)
    {
    }

    public TResult SendCommand<TResult>(ICommand<TResult> command) => default!;
    public TResult SendQuery<TResult>(IQuery<TResult> query) => default!;
    public IEnvironment GetEnvironment() => Environment;
}