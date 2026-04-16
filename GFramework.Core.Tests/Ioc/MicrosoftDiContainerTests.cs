using System.Reflection;
using GFramework.Core.Abstractions.Bases;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GFramework.Core.Tests.Cqrs;
using GFramework.Core.Tests.Systems;
using GFramework.Cqrs.Abstractions.Cqrs;
using LegacyICqrsRuntime = GFramework.Core.Abstractions.Cqrs.ICqrsRuntime;

namespace GFramework.Core.Tests.Ioc;

/// <summary>
///     测试 IoC 容器功能的单元测试类
/// </summary>
[TestFixture]
public class MicrosoftDiContainerTests
{
    /// <summary>
    ///     在每个测试方法执行前进行设置
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        // 初始化 LoggerFactoryResolver 以支持 MicrosoftDiContainer
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        _container = new MicrosoftDiContainer();

        // 直接初始化 logger 字段
        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(MicrosoftDiContainer)));

        CqrsTestRuntime.RegisterInfrastructure(_container);
    }

    private MicrosoftDiContainer _container = null!;

    /// <summary>
    ///     测试注册单例实例的功能
    /// </summary>
    [Test]
    public void RegisterSingleton_Should_Register_Instance()
    {
        var instance = new TestService();

        Assert.DoesNotThrow(() => _container.RegisterSingleton(instance));
    }

    /// <summary>
    ///     测试重复注册单例时应抛出 InvalidOperationException 异常
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
    ///     测试在容器冻结后注册单例时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void RegisterSingleton_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.RegisterSingleton(instance));
    }

    /// <summary>
    ///     测试注册多样性实例到所有类型的功能
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
    ///     测试在容器冻结后注册多样性实例时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void RegisterPlurality_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.RegisterPlurality(instance));
    }

    /// <summary>
    ///     测试泛型注册实例的功能
    /// </summary>
    [Test]
    public void Register_Generic_Should_Register_Instance()
    {
        var instance = new TestService();

        _container.Register(instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    /// <summary>
    ///     测试在容器冻结后使用泛型注册时应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void Register_Generic_AfterFreeze_Should_ThrowInvalidOperationException()
    {
        var instance = new TestService();
        _container.Freeze();

        Assert.Throws<InvalidOperationException>(() => _container.Register(instance));
    }

    /// <summary>
    ///     测试按类型注册实例的功能
    /// </summary>
    [Test]
    public void Register_Type_Should_Register_Instance()
    {
        var instance = new TestService();

        _container.Register(typeof(TestService), instance);

        Assert.That(_container.Contains<TestService>(), Is.True);
    }

    /// <summary>
    ///     测试获取第一个实例的功能
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
    ///     测试当 CQRS 基础设施已手动接线后，再调用处理器注册入口不会重复注册 runtime seam。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Not_Duplicate_Cqrs_Infrastructure_When_It_Is_Already_Registered()
    {
        Assert.That(_container.GetAll<ICqrsRuntime>(), Has.Count.EqualTo(1));
        Assert.That(_container.GetAll<LegacyICqrsRuntime>(), Has.Count.EqualTo(1));
        Assert.That(_container.GetAll<ICqrsHandlerRegistrar>(), Has.Count.EqualTo(1));
        Assert.That(_container.Get<ICqrsRuntime>(), Is.SameAs(_container.Get<LegacyICqrsRuntime>()));

        CqrsTestRuntime.RegisterHandlers(_container);

        Assert.That(_container.GetAll<ICqrsRuntime>(), Has.Count.EqualTo(1));
        Assert.That(_container.GetAll<LegacyICqrsRuntime>(), Has.Count.EqualTo(1));
        Assert.That(_container.GetAll<ICqrsHandlerRegistrar>(), Has.Count.EqualTo(1));
        Assert.That(_container.Get<ICqrsRuntime>(), Is.SameAs(_container.Get<LegacyICqrsRuntime>()));
    }

    /// <summary>
    ///     测试当没有实例时获取应返回 null 的功能
    /// </summary>
    [Test]
    public void Get_WithNoInstances_Should_ReturnNull()
    {
        var result = _container.Get<TestService>();

        Assert.That(result, Is.Null);
    }

    /// <summary>
    ///     测试获取必需的单个实例的功能
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
    ///     测试当没有实例时获取必需实例应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void GetRequired_WithNoInstances_Should_ThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _container.GetRequired<TestService>());
    }

    /// <summary>
    ///     测试当有多个实例时获取必需实例应抛出 InvalidOperationException 异常
    /// </summary>
    [Test]
    public void GetRequired_WithMultipleInstances_Should_ThrowInvalidOperationException()
    {
        _container.Register(new TestService());
        _container.Register(new TestService());

        Assert.Throws<InvalidOperationException>(() => _container.GetRequired<TestService>());
    }

    /// <summary>
    ///     测试获取所有实例的功能
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
    ///     测试当没有实例时获取所有实例应返回空数组的功能
    /// </summary>
    [Test]
    public void GetAll_WithNoInstances_Should_Return_Empty_Array()
    {
        var results = _container.GetAll<TestService>();

        Assert.That(results.Count, Is.EqualTo(0));
    }

    /// <summary>
    ///     测试容器未冻结时，会折叠“不同服务类型指向同一实例”的兼容别名重复，
    ///     但会保留同一服务类型的重复显式注册。
    /// </summary>
    [Test]
    public void GetAll_Should_Preserve_Duplicate_Registrations_For_The_Same_ServiceType_While_Deduplicating_Aliases()
    {
        var instance = new AliasAwareService();

        _container.Register<IPrimaryAliasService>(instance);
        _container.Register<IPrimaryAliasService>(instance);
        _container.Register<ISecondaryAliasService>(instance);

        var results = _container.GetAll<ISharedAliasService>();

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0], Is.SameAs(instance));
        Assert.That(results[1], Is.SameAs(instance));
    }

    /// <summary>
    ///     测试非泛型 GetAll 在容器未冻结时与泛型重载保持相同的别名去重语义。
    /// </summary>
    [Test]
    public void
        GetAll_Type_Should_Preserve_Duplicate_Registrations_For_The_Same_ServiceType_While_Deduplicating_Aliases()
    {
        var instance = new AliasAwareService();

        _container.Register<IPrimaryAliasService>(instance);
        _container.Register<IPrimaryAliasService>(instance);
        _container.Register<ISecondaryAliasService>(instance);

        var results = _container.GetAll(typeof(ISharedAliasService));

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0], Is.SameAs(instance));
        Assert.That(results[1], Is.SameAs(instance));
    }

    /// <summary>
    ///     测试获取排序后的所有实例的功能
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
    ///     测试当存在实例时检查包含关系应返回 true 的功能
    /// </summary>
    [Test]
    public void Contains_WithExistingInstance_Should_ReturnTrue()
    {
        // 使用 RegisterSingleton 方法来避免与其他测试方法重复
        var instance = new TestService();

        _container.RegisterSingleton(instance);

        // 验证容器包含该实例
        Assert.That(_container.Contains<TestService>(), Is.True);
        // 验证实例确实是单例
        Assert.That(_container.Get<TestService>(), Is.SameAs(instance));
    }


    /// <summary>
    ///     测试当不存在实例时检查包含关系应返回 false 的功能
    /// </summary>
    [Test]
    public void Contains_WithNoInstances_Should_ReturnFalse()
    {
        Assert.That(_container.Contains<TestService>(), Is.False);
    }

    /// <summary>
    ///     测试当实例存在时检查实例包含关系应返回 true 的功能
    /// </summary>
    [Test]
    public void ContainsInstance_WithExistingInstance_Should_ReturnTrue()
    {
        var instance = new TestService();
        _container.Register(instance);

        Assert.That(_container.ContainsInstance(instance), Is.True);
    }

    /// <summary>
    ///     测试当实例不存在时检查实例包含关系应返回 false 的功能
    /// </summary>
    [Test]
    public void ContainsInstance_WithNonExistingInstance_Should_ReturnFalse()
    {
        var instance = new TestService();

        Assert.That(_container.ContainsInstance(instance), Is.False);
    }

    /// <summary>
    ///     测试清除所有实例的功能
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
    ///     测试清空容器后可以重新接入同一程序集中的 CQRS 处理器。
    /// </summary>
    [Test]
    public void Clear_Should_Reset_Cqrs_Assembly_Deduplication_State()
    {
        var assembly = typeof(DeterministicOrderNotification).Assembly;

        _container.RegisterCqrsHandlersFromAssembly(assembly);
        Assert.That(
            _container.GetServicesUnsafe.Any(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<DeterministicOrderNotification>)),
            Is.True);

        _container.Clear();
        Assert.That(
            _container.GetServicesUnsafe.Any(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<DeterministicOrderNotification>)),
            Is.False);

        // Clear 会移除测试手工补齐的 CQRS seam，需要先恢复基础设施再验证程序集去重状态是否已重置。
        CqrsTestRuntime.RegisterInfrastructure(_container);
        _container.RegisterCqrsHandlersFromAssembly(assembly);

        Assert.That(
            _container.GetServicesUnsafe.Any(static descriptor =>
                descriptor.ServiceType == typeof(INotificationHandler<DeterministicOrderNotification>)),
            Is.True);
    }

    /// <summary>
    ///     测试当程序集集合中包含空元素时，CQRS handler 注册入口会在委托给注册服务前直接失败。
    /// </summary>
    [Test]
    public void RegisterCqrsHandlersFromAssemblies_WithNullAssemblyItem_Should_ThrowArgumentNullException()
    {
        var assemblies = new Assembly[] { typeof(DeterministicOrderNotification).Assembly, null! };

        Assert.Throws<ArgumentNullException>(() => _container.RegisterCqrsHandlersFromAssemblies(assemblies));
    }

    /// <summary>
    ///     测试冻结容器以防止进一步注册的功能
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
    ///     测试注册系统实例的功能
    /// </summary>
    [Test]
    public void RegisterSystem_Should_Register_Instance()
    {
        var system = new TestSystem();

        _container.RegisterSystem(system);

        Assert.That(_container.Contains<TestSystem>(), Is.True);
    }

    /// <summary>
    ///     测试在容器未冻结时调用 CreateScope 应抛出异常
    /// </summary>
    [Test]
    public void CreateScope_Should_Throw_When_Not_Frozen()
    {
        // Arrange
        var container = new MicrosoftDiContainer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => container.CreateScope());
    }

    /// <summary>
    ///     测试 CreateScope 在多线程环境下的线程安全性
    /// </summary>
    [Test]
    public void CreateScope_Should_Be_Thread_Safe()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<IService>(new TestService());
        container.Freeze();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            using var scope = container.CreateScope();
            Assert.That(scope, Is.Not.Null);
        })).ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    /// <summary>
    ///     测试 Get 方法在多线程环境下的线程安全性
    /// </summary>
    [Test]
    public void Get_Should_Be_Thread_Safe()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<IService>(new TestService());
        container.Freeze();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var service = container.Get<IService>();
            Assert.That(service, Is.Not.Null);
        })).ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    /// <summary>
    ///     测试 GetAll 方法在多线程环境下的线程安全性
    /// </summary>
    [Test]
    public void GetAll_Should_Be_Thread_Safe()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<IService>(new TestService());
        container.Freeze();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var services = container.GetAll<IService>();
            Assert.That(services, Is.Not.Null);
        })).ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    /// <summary>
    ///     测试 Contains 方法在多线程环境下的线程安全性
    /// </summary>
    [Test]
    public void Contains_Should_Be_Thread_Safe()
    {
        // Arrange
        var container = new MicrosoftDiContainer();
        container.RegisterSingleton<IService>(new TestService());
        container.Freeze();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var contains = container.Contains<IService>();
            Assert.That(contains, Is.True);
        })).ToArray();

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks));
    }

    /// <summary>
    ///     测试按优先级排序功能 - 升序排序
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Sort_By_Priority_Ascending()
    {
        // Arrange
        var service1 = new PrioritizedService { Priority = 30 };
        var service2 = new PrioritizedService { Priority = 10 };
        var service3 = new PrioritizedService { Priority = 20 };

        _container.Register<IPrioritizedService>(service1);
        _container.Register<IPrioritizedService>(service2);
        _container.Register<IPrioritizedService>(service3);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IPrioritizedService>();

        // Assert
        Assert.That(services, Has.Count.EqualTo(3));
        Assert.That(services[0].Priority, Is.EqualTo(10)); // 最小优先级在前
        Assert.That(services[1].Priority, Is.EqualTo(20));
        Assert.That(services[2].Priority, Is.EqualTo(30));
    }

    /// <summary>
    ///     测试未实现 IPrioritized 的服务使用默认优先级 0
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Use_Default_Priority_For_Non_Prioritized()
    {
        // Arrange
        var service1 = new PrioritizedService { Priority = 10 };
        var service2 = new NonPrioritizedService();
        var service3 = new PrioritizedService { Priority = -10 };

        _container.Register<IMixedService>(service1);
        _container.Register<IMixedService>(service2);
        _container.Register<IMixedService>(service3);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IMixedService>();

        // Assert
        Assert.That(services, Has.Count.EqualTo(3));
        Assert.That(services[0], Is.SameAs(service3)); // -10
        Assert.That(services[1], Is.SameAs(service2)); // 0 (默认)
        Assert.That(services[2], Is.SameAs(service1)); // 10
    }

    /// <summary>
    ///     测试相同优先级保持注册顺序（稳定排序）
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Preserve_Registration_Order_For_Equal_Priorities()
    {
        // Arrange
        var service1 = new PrioritizedService { Priority = 10, Name = "First" };
        var service2 = new PrioritizedService { Priority = 10, Name = "Second" };
        var service3 = new PrioritizedService { Priority = 10, Name = "Third" };

        _container.Register<IPrioritizedService>(service1);
        _container.Register<IPrioritizedService>(service2);
        _container.Register<IPrioritizedService>(service3);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IPrioritizedService>();

        // Assert
        Assert.That(services, Has.Count.EqualTo(3));
        Assert.That(services[0].Name, Is.EqualTo("First"));
        Assert.That(services[1].Name, Is.EqualTo("Second"));
        Assert.That(services[2].Name, Is.EqualTo("Third"));
    }

    /// <summary>
    ///     测试空列表处理
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Handle_Empty_List()
    {
        // Arrange
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IPrioritizedService>();

        // Assert
        Assert.That(services, Is.Empty);
    }

    /// <summary>
    ///     测试单项列表处理
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Handle_Single_Item()
    {
        // Arrange
        var service = new PrioritizedService { Priority = 42 };
        _container.RegisterSingleton<IPrioritizedService>(service);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IPrioritizedService>();

        // Assert
        Assert.That(services, Has.Count.EqualTo(1));
        Assert.That(services[0], Is.SameAs(service));
    }

    /// <summary>
    ///     测试负数优先级处理
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Handle_Negative_Priorities()
    {
        // Arrange
        var service1 = new PrioritizedService { Priority = -100 };
        var service2 = new PrioritizedService { Priority = 0 };
        var service3 = new PrioritizedService { Priority = 100 };
        var service4 = new PrioritizedService { Priority = -50 };

        _container.Register<IPrioritizedService>(service1);
        _container.Register<IPrioritizedService>(service2);
        _container.Register<IPrioritizedService>(service3);
        _container.Register<IPrioritizedService>(service4);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IPrioritizedService>();

        // Assert
        Assert.That(services, Has.Count.EqualTo(4));
        Assert.That(services[0].Priority, Is.EqualTo(-100));
        Assert.That(services[1].Priority, Is.EqualTo(-50));
        Assert.That(services[2].Priority, Is.EqualTo(0));
        Assert.That(services[3].Priority, Is.EqualTo(100));
    }

    /// <summary>
    ///     测试混合优先级和非优先级服务
    /// </summary>
    [Test]
    public void GetAllByPriority_Should_Handle_Mixed_Prioritized_And_Non_Prioritized()
    {
        // Arrange
        var service1 = new PrioritizedService { Priority = 50, Name = "P50" };
        var service2 = new NonPrioritizedService { Name = "NP1" };
        var service3 = new PrioritizedService { Priority = -50, Name = "P-50" };
        var service4 = new NonPrioritizedService { Name = "NP2" };

        _container.Register<IMixedService>(service1);
        _container.Register<IMixedService>(service2);
        _container.Register<IMixedService>(service3);
        _container.Register<IMixedService>(service4);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority<IMixedService>();

        // Assert
        Assert.That(services, Has.Count.EqualTo(4));
        Assert.That(services[0].Name, Is.EqualTo("P-50")); // -50
        Assert.That(services[1].Name, Is.EqualTo("NP1")); // 0 (默认)
        Assert.That(services[2].Name, Is.EqualTo("NP2")); // 0 (默认)
        Assert.That(services[3].Name, Is.EqualTo("P50")); // 50
    }

    /// <summary>
    ///     测试 GetAllByPriority(Type) 重载方法
    /// </summary>
    [Test]
    public void GetAllByPriority_Type_Should_Sort_Correctly()
    {
        // Arrange
        var service1 = new PrioritizedService { Priority = 30 };
        var service2 = new PrioritizedService { Priority = 10 };

        _container.Register<IPrioritizedService>(service1);
        _container.Register<IPrioritizedService>(service2);
        _container.Freeze();

        // Act
        var services = _container.GetAllByPriority(typeof(IPrioritizedService));

        // Assert
        Assert.That(services, Has.Count.EqualTo(2));
        Assert.That(((IPrioritizedService)services[0]).Priority, Is.EqualTo(10));
        Assert.That(((IPrioritizedService)services[1]).Priority, Is.EqualTo(30));
    }
}

/// <summary>
///     服务接口定义
/// </summary>
public interface IService;

/// <summary>
///     测试服务类，实现 IService 接口
/// </summary>
public sealed class TestService : IService
{
    /// <summary>
    ///     获取或设置优先级
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
///     优先级服务接口
/// </summary>
public interface IPrioritizedService : IPrioritized
{
    string? Name { get; set; }
}

/// <summary>
///     混合服务接口（用于测试优先级和非优先级混合）
/// </summary>
public interface IMixedService
{
    string? Name { get; set; }
}

/// <summary>
///     用于验证未冻结查询路径中的服务别名去重行为。
/// </summary>
public interface ISharedAliasService;

/// <summary>
///     主服务别名接口。
/// </summary>
public interface IPrimaryAliasService : ISharedAliasService;

/// <summary>
///     次级兼容别名接口。
/// </summary>
public interface ISecondaryAliasService : ISharedAliasService;

/// <summary>
///     同时实现多个别名接口的测试服务。
/// </summary>
public sealed class AliasAwareService : IPrimaryAliasService, ISecondaryAliasService
{
}

/// <summary>
///     实现优先级的服务
/// </summary>
public sealed class PrioritizedService : IPrioritizedService, IMixedService
{
    public int Priority { get; set; }
    public string? Name { get; set; }
}

/// <summary>
///     不实现优先级的服务
/// </summary>
public sealed class NonPrioritizedService : IMixedService
{
    public string? Name { get; set; }
}
