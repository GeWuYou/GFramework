using System.Reflection;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using GFramework.Core.Tests.system;
using NUnit.Framework;

namespace GFramework.Core.Tests.ioc;

/// <summary>
/// 测试 IoC 容器功能的单元测试类
/// </summary>
[TestFixture]
public class IocContainerTests
{
    /// <summary>
    /// 在每个测试方法执行前进行设置
    /// </summary>
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

    /// <summary>
    /// 测试注册单例实例的功能
    /// </summary>
    [Test]
    public void RegisterSingleton_Should_Register_Instance()
    {
        var instance = new TestService();

        Assert.DoesNotThrow(() => _container.RegisterSingleton(instance));
    }

    /// <summary>
    /// 测试重复注册单例时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void RegisterSingleton_WithDuplicate_Should_ThrowInvalidOperationException()
    {
        var instance1 = new TestService();
        var instance2 = new TestService();

        _container.RegisterSingleton(instance1);

        Assert.Throws<InvalidOperationException>(() => _container.RegisterSingleton(instance2));
    }

    /// <summary>
    /// 测试在容器冻结后注册单例时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void RegisterSingleton_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.RegisterSingleton(instance));
    }

    /// <summary>
    /// 测试注册多样性实例到所有类型的功能
    /// </summary>
    [Test]
    public void RegisterPlurality_Should_Register_Instance_To_All_Types()
    {
        var instance = new TestService();

        _container.RegisterPlurality(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
        Assert.That(_container.Contains<IService>(), Is.True);
    }

    /// <summary>
    /// 测试在容器冻结后注册多样性实例时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void RegisterPlurality_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.RegisterPlurality(instance));
    }

    /// <summary>
    /// 测试泛型注册实例的功能
    /// </summary>
    [Test]
    public void Register_Generic_Should_Register_Instance()
    {
        var instance = new TestService();

        _container.Register(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    /// <summary>
    /// 测试在容器冻结后使用泛型注册时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void Register_Generic_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.Register(instance));
    }

    /// <summary>
    /// 测试按类型注册实例的功能
    /// </summary>
    [Test]
    public void Register_Type_Should_Register_Instance()
    {
        var instance = new TestService();

        _container.Register(typeof(TestService), instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    /// <summary>
    /// 测试获取第一个实例的功能
    /// </summary>
    [Test]
    public void Get_Should_Return_First_Instance()
    {
        var instance = new TestService();
        _container.Register(instance);

        var result = _container.Get<TestService>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(instance));
    }

    /// <summary>
    /// 测试当没有实例时获取应返回 null 的功能
    /// </summary>
    [Test]
    public void Get_WithNoInstances_Should_ReturnNull()
    {
        var result = _container.Get<TestService>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// 测试获取必需的单个实例的功能
    /// </summary>
    [Test]
    public void GetRequired_Should_Return_Single_Instance()
    {
        var instance = new TestService();
        _container.Register(instance);

        var result = _container.GetRequired<TestService>();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(instance));
    }

    /// <summary>
    /// 测试当没有实例时获取必需实例应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void GetRequired_WithNoInstances_Should_ThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _container.GetRequired<TestService>());
    }

    /// <summary>
    /// 测试当有多个实例时获取必需实例应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void GetRequired_WithMultipleInstances_Should_ThrowInvalidOperationException()
    {
        _container.Register(new TestService());
        _container.Register(new TestService());

        Assert.Throws<InvalidOperationException>(() => _container.GetRequired<TestService>());
    }

    /// <summary>
    /// 测试获取所有实例的功能
    /// </summary>
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

    /// <summary>
    /// 测试当没有实例时获取所有实例应返回空数组的功能
    /// </summary>
    [Test]
    public void GetAll_WithNoInstances_Should_Return_Empty_Array()
    {
        var results = _container.GetAll<TestService>();

        Assert.That(results.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试获取排序后的所有实例的功能
    /// </summary>
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

    /// <summary>
    /// 测试当存在实例时检查包含关系应返回 true 的功能
    /// </summary>
    [Test]
    public void Contains_WithExistingInstance_Should_ReturnTrue()
    {
        var instance = new TestService();
        _container.Register(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    /// <summary>
    /// 测试当不存在实例时检查包含关系应返回 false 的功能
    /// </summary>
    [Test]
    public void Contains_WithNoInstances_Should_ReturnFalse()
    {
        Assert.That(_container.Contains<TestService>(), Is.False);
    }

    /// <summary>
    /// 测试当实例存在时检查实例包含关系应返回 true 的功能
    /// </summary>
    [Test]
    public void ContainsInstance_WithExistingInstance_Should_ReturnTrue()
    {
        var instance = new TestService();
        _container.Register(instance);

        Assert.That(_container.ContainsInstance(instance), Is.True);
    }

    /// <summary>
    /// 测试当实例不存在时检查实例包含关系应返回 false 的功能
    /// </summary>
    [Test]
    public void ContainsInstance_WithNonExistingInstance_Should_ReturnFalse()
    {
        var instance = new TestService();

        Assert.That(_container.ContainsInstance(instance), Is.False);
    }

    /// <summary>
    /// 测试清除所有实例的功能
    /// </summary>
    [Test]
    public void Clear_Should_Remove_All_Instances()
    {
        var instance = new TestService();
        _container.Register(instance);

        _container.Clear();

        Assert.That(_container.Contains<TestService>(), Is.False);
    }

    /// <summary>
    /// 测试冻结容器以防止进一步注册的功能
    /// </summary>
    [Test]
    public void Freeze_Should_Prevent_Further_Registrations()
    {
        var instance1 = new TestService();
        _container.Register(instance1);
        _container.Freeze();

        var instance2 = new TestService();
        Assert.Throws<InvalidOperationException>(() => _container.Register(instance2));
    }

    /// <summary>
    /// 测试注册系统实例的功能
    /// </summary>
    [Test]
    public void RegisterSystem_Should_Register_Instance()
    {
        var system = new TestSystem();

        _container.RegisterSystem(system);

        Assert.That(_container.Contains<TestSystem>(), Is.True);
    }
}

/// <summary>
/// 服务接口定义
/// </summary>
public interface IService;

/// <summary>
/// 测试服务类，实现 IService 接口
/// </summary>
public sealed class TestService : IService
{
    /// <summary>
    /// 获取或设置优先级
    /// </summary>
    public int Priority { get; set; }
}