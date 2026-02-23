using System.Reflection;
using Arch.Core;
using GFramework.Core.Abstractions.ecs;
using GFramework.Core.architecture;
using GFramework.Core.ecs;
using GFramework.Core.ecs.components;
using GFramework.Core.ecs.systems;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.ecs;

/// <summary>
/// ECS集成测试类，用于验证ECS系统的整体功能和性能表现。
/// 包括实体管理、组件操作、系统调度、优先级控制以及性能基准测试。
/// </summary>
[TestFixture]
public class EcsIntegrationTests
{
    /// <summary>
    /// 测试初始化方法，在每个测试方法执行前运行。
    /// 负责初始化日志工厂、依赖注入容器和架构上下文。
    /// </summary>
    [SetUp]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        _container = new MicrosoftDiContainer();
        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(EcsIntegrationTests)));

        _context = new ArchitectureContext(_container);
    }

    /// <summary>
    /// 测试清理方法，在每个测试方法执行后运行。
    /// 负责释放ECS世界资源并清空容器和上下文。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _ecsWorld?.Dispose();
        _ecsWorld = null;
        _container?.Clear();
        _context = null;
    }

    private MicrosoftDiContainer? _container;
    private ArchitectureContext? _context;
    private EcsWorld? _ecsWorld;

    /// <summary>
    /// 初始化ECS系统并注册指定类型的系统实例。
    /// </summary>
    /// <param name="systemTypes">需要注册的系统类型数组</param>
    private void InitializeEcsWithSystems(params Type[] systemTypes)
    {
        _ecsWorld = new EcsWorld();
        _container!.Register(_ecsWorld);
        _container.Register(_ecsWorld as IEcsWorld);

        var systems = new List<IEcsSystem>();
        foreach (var systemType in systemTypes)
        {
            var system = (IEcsSystem)Activator.CreateInstance(systemType)!;
            system.SetContext(_context!);
            system.Initialize();
            systems.Add(system);
            _container.RegisterPlurality(system);
        }

        _container.Register(systems as IReadOnlyList<IEcsSystem>);
    }

    /// <summary>
    /// 测试ECS初始化功能，验证是否能正确创建EcsWorld实例。
    /// </summary>
    [Test]
    public void InitializeEcs_Should_Create_EcsWorld()
    {
        _context!.InitializeEcs();
        var ecsWorld = _context.GetEcsWorld();

        Assert.That(ecsWorld, Is.Not.Null);
        Assert.That(ecsWorld.EntityCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试实体创建功能，验证创建实体后实体计数是否正确增加。
    /// </summary>
    [Test]
    public void CreateEntity_Should_Increase_EntityCount()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(1));
        Assert.That(_ecsWorld.IsAlive(entity), Is.True);
    }

    /// <summary>
    /// 测试实体销毁功能，验证销毁实体后实体计数是否正确减少。
    /// </summary>
    [Test]
    public void DestroyEntity_Should_Decrease_EntityCount()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position));

        _ecsWorld.DestroyEntity(entity);

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(0));
        Assert.That(_ecsWorld.IsAlive(entity), Is.False);
    }

    /// <summary>
    /// 测试组件设置功能，验证能否正确存储和获取组件数据。
    /// </summary>
    [Test]
    public void SetComponent_Should_Store_ComponentData()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position));

        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(10, 20));

        Assert.That(world.Has<Position>(entity), Is.True);
        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10));
        Assert.That(pos.Y, Is.EqualTo(20));
    }

    /// <summary>
    /// 测试世界清理功能，验证能否清除所有实体。
    /// </summary>
    [Test]
    public void ClearWorld_Should_Remove_All_Entities()
    {
        _ecsWorld = new EcsWorld();
        for (int i = 0; i < 10; i++)
        {
            _ecsWorld.CreateEntity(typeof(Position));
        }

        _ecsWorld.Clear();

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试ECS系统注册功能，验证系统能否正确添加到运行器中。
    /// </summary>
    [Test]
    public void RegisterEcsSystem_Should_Add_System_To_Runner()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        Assert.That(systems, Is.Not.Null);
        Assert.That(systems!.Count, Is.EqualTo(1));
        Assert.That(systems[0], Is.InstanceOf<MovementSystem>());
    }

    /// <summary>
    /// 测试移动系统功能，验证系统能否正确更新单个实体的位置。
    /// </summary>
    [Test]
    public void MovementSystem_Should_Update_Position()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var entity = _ecsWorld!.CreateEntity(typeof(Position), typeof(Velocity));

        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(0, 0));
        world.Set(entity, new Velocity(10, 5));

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        var movementSystem = systems!.First(s => s is MovementSystem) as MovementSystem;

        movementSystem!.Update(1.0f);

        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10).Within(0.001f));
        Assert.That(pos.Y, Is.EqualTo(5).Within(0.001f));
    }

    /// <summary>
    /// 测试移动系统功能，验证系统能否正确批量更新多个实体的位置。
    /// </summary>
    [Test]
    public void MovementSystem_Should_Update_Multiple_Entities()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var world = _ecsWorld!.InternalWorld;
        var entities = new Entity[100];

        for (var i = 0; i < 100; i++)
        {
            entities[i] = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));
            world.Set(entities[i], new Position(0, 0));
            world.Set(entities[i], new Velocity(i, i * 2));
        }

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        var movementSystem = systems!.First(s => s is MovementSystem) as MovementSystem;

        movementSystem!.Update(0.5f);

        for (var i = 0; i < 100; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            Assert.That(pos.X, Is.EqualTo(i * 0.5f).Within(0.001f));
            Assert.That(pos.Y, Is.EqualTo(i * 2 * 0.5f).Within(0.001f));
        }
    }

    /// <summary>
    /// 测试ECS系统运行器的优先级调度功能，验证系统是否按优先级顺序执行。
    /// </summary>
    [Test]
    public void EcsSystemRunner_Should_Respect_Priority()
    {
        InitializeEcsWithSystems(typeof(LowPrioritySystem), typeof(HighPrioritySystem));

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        Assert.That(systems, Is.Not.Null);
        Assert.That(systems!.Count, Is.EqualTo(2));

        var sortedSystems = systems.OrderBy(s => s.Priority).ToList();
        Assert.That(sortedSystems[0], Is.InstanceOf<HighPrioritySystem>());
        Assert.That(sortedSystems[1], Is.InstanceOf<LowPrioritySystem>());
    }

    /// <summary>
    /// 测试未初始化情况下获取ECS世界的异常处理。
    /// </summary>
    [Test]
    public void GetEcsWorld_Without_Initialize_Should_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => { _context!.GetEcsWorld(); });
    }

    /// <summary>
    /// 性能基准测试：验证更新10000个实体的性能表现。
    /// </summary>
    [Test]
    public void Performance_Test_10000_Entities()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var world = _ecsWorld!.InternalWorld;

        for (int i = 0; i < 10000; i++)
        {
            var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));
            world.Set(entity, new Position(0, 0));
            world.Set(entity, new Velocity(1, 1));
        }

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        var movementSystem = systems!.First(s => s is MovementSystem) as MovementSystem;

        var startTime = DateTime.UtcNow;
        movementSystem!.Update(0.016f);
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(10000));
        Assert.That(elapsed, Is.LessThan(100), $"Updating 10000 entities took: {elapsed}ms");
    }

    /// <summary>
    /// 性能基准测试：验证创建1000个实体的性能表现。
    /// </summary>
    [Test]
    public void Performance_Test_1000_Entities_Creation()
    {
        _ecsWorld = new EcsWorld();
        var world = _ecsWorld.InternalWorld;

        var startTime = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));
            world.Set(entity, new Position(0, 0));
            world.Set(entity, new Velocity(1, 1));
        }

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(1000));
        Assert.That(elapsed, Is.LessThan(50), $"Creating 1000 entities took: {elapsed}ms");
    }
}

/// <summary>
/// 高优先级系统示例，用于测试系统调度优先级功能。
/// </summary>
public class HighPrioritySystem : EcsSystemBase
{
    /// <summary>
    /// 获取系统优先级，数值越小优先级越高。
    /// </summary>
    public override int Priority => -100;

    /// <summary>
    /// ECS初始化回调方法。
    /// </summary>
    protected override void OnEcsInit()
    {
    }

    /// <summary>
    /// 系统更新方法。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public override void Update(float deltaTime)
    {
    }
}

/// <summary>
/// 低优先级系统示例，用于测试系统调度优先级功能。
/// </summary>
public class LowPrioritySystem : EcsSystemBase
{
    /// <summary>
    /// 获取系统优先级，数值越大优先级越低。
    /// </summary>
    public override int Priority => 100;

    /// <summary>
    /// ECS初始化回调方法。
    /// </summary>
    protected override void OnEcsInit()
    {
    }

    /// <summary>
    /// 系统更新方法。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public override void Update(float deltaTime)
    {
    }
}