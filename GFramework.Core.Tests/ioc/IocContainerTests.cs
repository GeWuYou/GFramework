using System.Reflection;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.Tests.system;
using NUnit.Framework;

namespace GFramework.Core.Tests.ioc;

[TestFixture]
public class IocContainerTests
{
    [SetUp]
    public void SetUp()
    {
        // 初始化 LoggerFactoryResolver 以支持 IocContainer
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new IocContainer();

        // 直接初始化 logger 字段
        var loggerField = typeof(IocContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(IocContainer)));
    }

    private IocContainer _container = null!;
    private readonly Dictionary<Type, object> _mockContextServices = new();

    [Test]
    public void RegisterSingleton_Should_Register_Instance()
    {
        var instance = new TestService();

        Assert.DoesNotThrow(() => _container.RegisterSingleton(instance));
    }

    [Test]
    public void RegisterSingleton_WithDuplicate_Should_ThrowInvalidOperationException()
    {
        var instance1 = new TestService();
        var instance2 = new TestService();

        _container.RegisterSingleton(instance1);

        Assert.Throws<InvalidOperationException>(() => _container.RegisterSingleton(instance2));
    }

    [Test]
    public void RegisterSingleton_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.RegisterSingleton(instance));
    }

    [Test]
    public void RegisterPlurality_Should_Register_Instance_To_All_Types()
    {
        var instance = new TestService();

        _container.RegisterPlurality(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
        Assert.That(_container.Contains<IService>(), Is.True);
    }

    [Test]
    public void RegisterPlurality_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.RegisterPlurality(instance));
    }

    [Test]
    public void Register_Generic_Should_Register_Instance()
    {
        var instance = new TestService();

        _container.Register(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    [Test]
    public void Register_Generic_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.Register(instance));
    }

    [Test]
    public void Register_Type_Should_Register_Instance()
    {
        var instance = new TestService();

        _container.Register(typeof(TestService), instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    [Test]
    public void Get_Should_Return_First_Instance()
    {
        var instance = new TestService();
        _container.Register(instance);

        var result = _container.Get<TestService>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(instance));
    }

    [Test]
    public void Get_WithNoInstances_Should_ReturnNull()
    {
        var result = _container.Get<TestService>();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetRequired_Should_Return_Single_Instance()
    {
        var instance = new TestService();
        _container.Register(instance);

        var result = _container.GetRequired<TestService>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(instance));
    }

    [Test]
    public void GetRequired_WithNoInstances_Should_ThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _container.GetRequired<TestService>());
    }

    [Test]
    public void GetRequired_WithMultipleInstances_Should_ThrowInvalidOperationException()
    {
        _container.Register(new TestService());
        _container.Register(new TestService());

        Assert.Throws<InvalidOperationException>(() => _container.GetRequired<TestService>());
    }

    [Test]
    public void GetAll_Should_Return_All_Instances()
    {
        var instance1 = new TestService();
        var instance2 = new TestService();

        _container.Register(instance1);
        _container.Register(instance2);

        var results = _container.GetAll<TestService>();

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results, Does.Contain(instance1));
        Assert.That(results, Does.Contain(instance2));
    }

    [Test]
    public void GetAll_WithNoInstances_Should_Return_Empty_Array()
    {
        var results = _container.GetAll<TestService>();

        Assert.That(results.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetAllSorted_Should_Return_Sorted_Instances()
    {
        _container.Register(new TestService { Priority = 3 });
        _container.Register(new TestService { Priority = 1 });
        _container.Register(new TestService { Priority = 2 });

        var results = _container.GetAllSorted<TestService>((a, b) => a.Priority.CompareTo(b.Priority));

        Assert.That(results.Count, Is.EqualTo(3));
        Assert.That(results[0].Priority, Is.EqualTo(1));
        Assert.That(results[1].Priority, Is.EqualTo(2));
        Assert.That(results[2].Priority, Is.EqualTo(3));
    }

    [Test]
    public void Contains_WithExistingInstance_Should_ReturnTrue()
    {
        var instance = new TestService();
        _container.Register(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    [Test]
    public void Contains_WithNoInstances_Should_ReturnFalse()
    {
        Assert.That(_container.Contains<TestService>(), Is.False);
    }

    [Test]
    public void ContainsInstance_WithExistingInstance_Should_ReturnTrue()
    {
        var instance = new TestService();
        _container.Register(instance);

        Assert.That(_container.ContainsInstance(instance), Is.True);
    }

    [Test]
    public void ContainsInstance_WithNonExistingInstance_Should_ReturnFalse()
    {
        var instance = new TestService();

        Assert.That(_container.ContainsInstance(instance), Is.False);
    }

    [Test]
    public void Clear_Should_Remove_All_Instances()
    {
        var instance = new TestService();
        _container.Register(instance);

        _container.Clear();

        Assert.That(_container.Contains<TestService>(), Is.False);
    }

    [Test]
    public void Freeze_Should_Prevent_Further_Registrations()
    {
        var instance1 = new TestService();
        _container.Register(instance1);
        _container.Freeze();

        var instance2 = new TestService();
        Assert.Throws<InvalidOperationException>(() => _container.Register(instance2));
    }

    [Test]
    public void RegisterSystem_Should_Register_Instance()
    {
        var system = new TestSystem();

        _container.RegisterSystem(system);

        Assert.That(_container.Contains<TestSystem>(), Is.True);
    }
}

public interface IService;

public sealed class TestService : IService
{
    public int Priority { get; set; }
}