using System.Diagnostics.CodeAnalysis;
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
/// ECS基础功能测试类，用于验证ECS系统的核心功能。
/// 包括实体创建、组件设置、系统更新、实体销毁等基本操作。
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsBasicTests
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
            LoggerFactoryResolver.Provider.CreateLogger(nameof(EcsBasicTests)));

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
    [Experimental("GFrameworkECS")]
    public void Test_01_InitializeEcs_Should_Create_EcsWorld()
    {
        _context!.InitializeEcs();
        var ecsWorld = _context.GetEcsWorld();

        Assert.That(ecsWorld, Is.Not.Null, "EcsWorld should be created");
        Assert.That(ecsWorld.EntityCount, Is.EqualTo(0), "Initial entity count should be 0");
    }

    /// <summary>
    /// 测试实体创建功能，验证能否成功创建带有指定组件的实体。
    /// </summary>
    [Test]
    public void Test_02_CreateEntity_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(1), "Entity count should be 1");
        Assert.That(_ecsWorld.IsAlive(entity), Is.True, "Entity should be alive");
    }

    /// <summary>
    /// 测试组件设置功能，验证能否正确存储和获取组件数据。
    /// </summary>
    [Test]
    public void Test_03_SetComponent_Should_Store_Data()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position));
        var world = _ecsWorld.InternalWorld;

        world.Set(entity, new Position(10, 20));

        Assert.That(world.Has<Position>(entity), Is.True, "Entity should have Position component");
        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10), "Position.X should be 10");
        Assert.That(pos.Y, Is.EqualTo(20), "Position.Y should be 20");
    }

    /// <summary>
    /// 测试移动系统功能，验证系统能否正确更新实体位置。
    /// </summary>
    [Test]
    public void Test_04_MovementSystem_Should_Update_Position()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var entity = _ecsWorld!.CreateEntity(typeof(Position), typeof(Velocity));

        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(0, 0));
        world.Set(entity, new Velocity(10, 5));

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        Assert.That(systems, Is.Not.Null);
        Assert.That(systems!.Count, Is.GreaterThan(0));

        var movementSystem = systems.First(s => s is MovementSystem) as MovementSystem;
        Assert.That(movementSystem, Is.Not.Null);

        movementSystem!.Update(1.0f);

        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10).Within(0.001f), "X position should be 10");
        Assert.That(pos.Y, Is.EqualTo(5).Within(0.001f), "Y position should be 5");
    }

    /// <summary>
    /// 测试实体销毁功能，验证能否正确销毁实体并更新实体计数。
    /// </summary>
    [Test]
    public void Test_05_DestroyEntity_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position));

        _ecsWorld.DestroyEntity(entity);

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(0), "Entity count should be 0");
        Assert.That(_ecsWorld.IsAlive(entity), Is.False, "Entity should not be alive");
    }

    /// <summary>
    /// 测试世界清理功能，验证能否清除所有实体。
    /// </summary>
    [Test]
    public void Test_06_ClearWorld_Should_Remove_All_Entities()
    {
        _ecsWorld = new EcsWorld();
        for (int i = 0; i < 10; i++)
        {
            _ecsWorld.CreateEntity(typeof(Position));
        }

        _ecsWorld.Clear();

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(0), "Entity count should be 0 after clear");
    }

    /// <summary>
    /// 测试多个实体的批量更新功能，验证系统能否正确处理多个实体的更新。
    /// </summary>
    [Test]
    public void Test_07_Multiple_Entities_Should_Update_Correctly()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var world = _ecsWorld!.InternalWorld;
        var entities = new Entity[10];

        for (var i = 0; i < 10; i++)
        {
            entities[i] = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));
            world.Set(entities[i], new Position(0, 0));
            world.Set(entities[i], new Velocity(i, i * 2));
        }

        var systems = _container!.Get<IReadOnlyList<IEcsSystem>>();
        var movementSystem = systems!.First(s => s is MovementSystem) as MovementSystem;

        movementSystem!.Update(1.0f);

        for (int i = 0; i < 10; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            Assert.That(pos.X, Is.EqualTo(i).Within(0.001f), $"Entity {i} X position should be {i}");
            Assert.That(pos.Y, Is.EqualTo(i * 2).Within(0.001f), $"Entity {i} Y position should be {i * 2}");
        }
    }

    /// <summary>
    /// 测试未初始化情况下获取ECS世界的异常处理。
    /// </summary>
    [Test]
    public void Test_08_GetEcsWorld_Without_Initialize_Should_Throw()
    {
        Assert.Throws<InvalidOperationException>(() => { _context!.GetEcsWorld(); },
            "Getting EcsWorld without initialization should throw");
    }
}