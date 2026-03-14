using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Model;
using GFramework.Core.Abstractions.Systems;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
/// RegistryInitializationHookBase 抽象基类的单元测试
/// 测试内容包括：
/// - 在目标阶段正确触发配置注册
/// - 在非目标阶段不触发配置注册
/// - 正确遍历所有配置项
/// - 注册表不存在时不抛出异常
/// - 支持自定义目标阶段
/// </summary>
[TestFixture]
public class RegistryInitializationHookBaseTests
{
    /// <summary>
    /// 测试在目标阶段时是否正确触发配置注册
    /// </summary>
    [Test]
    public void OnPhase_Should_Register_Configs_At_Target_Phase()
    {
        var registry = new TestRegistry();
        var configs = new[] { "config1", "config2", "config3" };
        var hook = new TestRegistryInitializationHook(configs);
        var architecture = new TestArchitectureWithRegistry(registry);

        hook.OnPhase(ArchitecturePhase.AfterSystemInit, architecture);

        Assert.That(registry.RegisteredConfigs.Count, Is.EqualTo(3));
        Assert.That(registry.RegisteredConfigs, Is.EquivalentTo(configs));
    }

    /// <summary>
    /// 测试在非目标阶段时不触发配置注册
    /// </summary>
    [Test]
    public void OnPhase_Should_Not_Register_Configs_At_Wrong_Phase()
    {
        var registry = new TestRegistry();
        var configs = new[] { "config1", "config2" };
        var hook = new TestRegistryInitializationHook(configs);
        var architecture = new TestArchitectureWithRegistry(registry);

        hook.OnPhase(ArchitecturePhase.BeforeSystemInit, architecture);

        Assert.That(registry.RegisteredConfigs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试支持自定义目标阶段
    /// </summary>
    [Test]
    public void OnPhase_Should_Support_Custom_Target_Phase()
    {
        var registry = new TestRegistry();
        var configs = new[] { "config1" };
        var hook = new TestRegistryInitializationHook(configs, ArchitecturePhase.AfterModelInit);
        var architecture = new TestArchitectureWithRegistry(registry);

        hook.OnPhase(ArchitecturePhase.AfterModelInit, architecture);

        Assert.That(registry.RegisteredConfigs.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// 测试当注册表不存在时不抛出异常
    /// </summary>
    [Test]
    public void OnPhase_Should_Not_Throw_When_Registry_Not_Found()
    {
        var configs = new[] { "config1" };
        var hook = new TestRegistryInitializationHook(configs);
        var architecture = new TestArchitectureWithoutRegistry();

        Assert.DoesNotThrow(() => hook.OnPhase(ArchitecturePhase.AfterSystemInit, architecture));
    }

    /// <summary>
    /// 测试空配置集合不会导致错误
    /// </summary>
    [Test]
    public void OnPhase_Should_Handle_Empty_Configs()
    {
        var registry = new TestRegistry();
        var configs = Array.Empty<string>();
        var hook = new TestRegistryInitializationHook(configs);
        var architecture = new TestArchitectureWithRegistry(registry);

        hook.OnPhase(ArchitecturePhase.AfterSystemInit, architecture);

        Assert.That(registry.RegisteredConfigs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试多次调用同一阶段会重复注册
    /// </summary>
    [Test]
    public void OnPhase_Should_Register_Multiple_Times_If_Called_Multiple_Times()
    {
        var registry = new TestRegistry();
        var configs = new[] { "config1" };
        var hook = new TestRegistryInitializationHook(configs);
        var architecture = new TestArchitectureWithRegistry(registry);

        hook.OnPhase(ArchitecturePhase.AfterSystemInit, architecture);
        hook.OnPhase(ArchitecturePhase.AfterSystemInit, architecture);

        Assert.That(registry.RegisteredConfigs.Count, Is.EqualTo(2));
    }
}

/// <summary>
/// 测试用的注册表初始化钩子实现
/// </summary>
public class TestRegistryInitializationHook : RegistryInitializationHookBase<TestRegistry, string>
{
    public TestRegistryInitializationHook(
        IEnumerable<string> configs,
        ArchitecturePhase targetPhase = ArchitecturePhase.AfterSystemInit)
        : base(configs, targetPhase)
    {
    }

    protected override void RegisterConfig(TestRegistry registry, string config)
    {
        registry.Register(config);
    }
}

/// <summary>
/// 测试用的注册表类
/// </summary>
public class TestRegistry : IUtility
{
    public List<string> RegisteredConfigs { get; } = new();

    public void Register(string config)
    {
        RegisteredConfigs.Add(config);
    }
}

/// <summary>
/// 测试用的架构类（包含注册表）
/// </summary>
public class TestArchitectureWithRegistry : IArchitecture
{
    private readonly TestRegistry _registry;

    public TestArchitectureWithRegistry(TestRegistry registry)
    {
        _registry = registry;
        Context = new TestArchitectureContextWithRegistry(registry);
    }

    public Action<IServiceCollection>? Configurator { get; }

    public IArchitectureContext Context { get; }
    Action<IServiceCollection>? IArchitecture.Configurator => Configurator;

    T IArchitecture.RegisterSystem<T>(T system)
    {
        throw new NotImplementedException();
    }

    T IArchitecture.RegisterModel<T>(T model)
    {
        throw new NotImplementedException();
    }

    T IArchitecture.RegisterUtility<T>(T utility)
    {
        throw new NotImplementedException();
    }

    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        throw new NotImplementedException();
    }

    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        throw new NotImplementedException();
    }

    IArchitectureLifecycleHook IArchitecture.RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        throw new NotImplementedException();
    }

    Task IArchitecture.WaitUntilReadyAsync()
    {
        return WaitUntilReadyAsync();
    }

    public void RegisterUtility<T>(Action<T>? onCreated = default(Action<T>?)) where T : class, IUtility
    {
        throw new NotImplementedException();
    }

    public void RegisterModel<T>(Action<T>? onCreated = default(Action<T>?)) where T : class, IModel
    {
        throw new NotImplementedException();
    }

    public void RegisterSystem<T>(Action<T>? onCreated = default(Action<T>?)) where T : class, ISystem
    {
        throw new NotImplementedException();
    }

    public void Initialize()
    {
    }

    public void Destroy()
    {
        throw new NotImplementedException();
    }

    Task IAsyncInitializable.InitializeAsync()
    {
        return InitializeAsync();
    }

    ValueTask IAsyncDestroyable.DestroyAsync()
    {
        return DestroyAsync();
    }

    public Task WaitUntilReadyAsync()
    {
        throw new NotImplementedException();
    }

    public void RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
    }

    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask DestroyAsync()
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 测试用的架构上下文类（包含注册表）
/// </summary>
public class TestArchitectureContextWithRegistry : TestArchitectureContext
{
    private readonly TestRegistry _registry;

    public TestArchitectureContextWithRegistry(TestRegistry registry)
    {
        _registry = registry;
    }

    public override TUtility GetUtility<TUtility>()
    {
        if (typeof(TUtility) == typeof(TestRegistry))
        {
            return _registry as TUtility;
        }

        return base.GetUtility<TUtility>();
    }
}

/// <summary>
/// 测试用的架构类（不包含注册表）
/// </summary>
public class TestArchitectureWithoutRegistry : IArchitecture
{
    public TestArchitectureWithoutRegistry()
    {
        Context = new TestArchitectureContext();
    }

    public IArchitectureContext Context { get; }
    public Action<IServiceCollection>? Configurator { get; }

    T IArchitecture.RegisterSystem<T>(T system)
    {
        throw new NotImplementedException();
    }

    T IArchitecture.RegisterModel<T>(T model)
    {
        throw new NotImplementedException();
    }

    T IArchitecture.RegisterUtility<T>(T utility)
    {
        throw new NotImplementedException();
    }

    public void RegisterMediatorBehavior<TBehavior>() where TBehavior : class
    {
        throw new NotImplementedException();
    }

    public IArchitectureModule InstallModule(IArchitectureModule module)
    {
        throw new NotImplementedException();
    }

    IArchitectureLifecycleHook IArchitecture.RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
        throw new NotImplementedException();
    }

    public Task WaitUntilReadyAsync()
    {
        throw new NotImplementedException();
    }

    public void RegisterUtility<T>(Action<T>? onCreated = default(Action<T>?)) where T : class, IUtility
    {
        throw new NotImplementedException();
    }

    public void RegisterModel<T>(Action<T>? onCreated = default(Action<T>?)) where T : class, IModel
    {
        throw new NotImplementedException();
    }

    public void RegisterSystem<T>(Action<T>? onCreated = default(Action<T>?)) where T : class, ISystem
    {
        throw new NotImplementedException();
    }

    public void Initialize()
    {
    }

    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask DestroyAsync()
    {
        throw new NotImplementedException();
    }

    public void Destroy()
    {
        throw new NotImplementedException();
    }

    public void RegisterLifecycleHook(IArchitectureLifecycleHook hook)
    {
    }
}