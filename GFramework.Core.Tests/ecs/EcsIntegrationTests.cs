using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.architecture;
using GFramework.Core.ecs;
using GFramework.Core.ecs.components;
using GFramework.Core.ecs.systems;
using GFramework.Core.ioc;
using NUnit.Framework;

namespace GFramework.Core.Tests.ecs;

/// <summary>
/// ECS 集成测试类 - 使用 Arch 原生 API
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsIntegrationTests
{
    [SetUp]
    public void Setup()
    {
        _container = new MicrosoftDiContainer();
        _context = new ArchitectureContext(_container);
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null)
        {
            World.Destroy(_world);
            _world = null;
        }

        _container?.Clear();
        _context = null;
        _ecsModule = null;
    }

    private MicrosoftDiContainer? _container;
    private ArchitectureContext? _context;
    private World? _world;
    private ArchEcsModule? _ecsModule;

    /// <summary>
    /// 初始化 ECS 模块
    /// </summary>
    private void InitializeEcsModule()
    {
        // 注册系统
        var movementSystem = new MovementSystem();
        ((IContextAware)movementSystem).SetContext(_context!);
        _container!.RegisterPlurality(movementSystem);

        // 创建并注册 ArchEcsModule
        _ecsModule = new ArchEcsModule(enabled: true);
        _ecsModule.Register(_container);
        _ecsModule.Initialize();

        // 获取 World
        _world = _container.Get<World>();
    }

    [Test]
    public void InitializeEcs_Should_Create_World()
    {
        InitializeEcsModule();

        Assert.That(_world, Is.Not.Null);
        Assert.That(_world!.Size, Is.EqualTo(0));
    }

    [Test]
    public void CreateEntity_Should_Increase_EntityCount()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(1, 1));

        Assert.That(_world.Size, Is.EqualTo(1));
        Assert.That(_world.IsAlive(entity), Is.True);
    }

    [Test]
    public void DestroyEntity_Should_Decrease_EntityCount()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0));
        _world.Destroy(entity);

        Assert.That(_world.Size, Is.EqualTo(0));
        Assert.That(_world.IsAlive(entity), Is.False);
    }

    [Test]
    public void SetComponent_Should_Store_ComponentData()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(10, 20));

        Assert.That(_world.Has<Position>(entity), Is.True);
        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10));
        Assert.That(pos.Y, Is.EqualTo(20));
    }

    [Test]
    public void ClearWorld_Should_Remove_All_Entities()
    {
        InitializeEcsModule();

        for (int i = 0; i < 10; i++)
        {
            _world!.Create(new Position(0, 0));
        }

        _world!.Clear();

        Assert.That(_world.Size, Is.EqualTo(0));
    }

    [Test]
    public void RegisterEcsSystem_Should_Add_System_To_Module()
    {
        InitializeEcsModule();

        var adapters = _container!.GetAll<ArchSystemAdapter<float>>();
        Assert.That(adapters, Is.Not.Null);
        Assert.That(adapters.Count, Is.EqualTo(1));
        Assert.That(adapters[0], Is.InstanceOf<MovementSystem>());
    }

    [Test]
    public void MovementSystem_Should_Update_Position()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(10, 5));

        _ecsModule!.Update(1.0f);

        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10).Within(0.001f));
        Assert.That(pos.Y, Is.EqualTo(5).Within(0.001f));
    }

    [Test]
    public void MovementSystem_Should_Update_Multiple_Entities()
    {
        InitializeEcsModule();

        var entities = new Entity[100];
        for (var i = 0; i < 100; i++)
        {
            entities[i] = _world!.Create(new Position(0, 0), new Velocity(i, i * 2));
        }

        _ecsModule!.Update(0.5f);

        for (var i = 0; i < 100; i++)
        {
            ref var pos = ref _world!.Get<Position>(entities[i]);
            Assert.That(pos.X, Is.EqualTo(i * 0.5f).Within(0.001f));
            Assert.That(pos.Y, Is.EqualTo(i * 2 * 0.5f).Within(0.001f));
        }
    }

    [Test]
    public void Performance_Test_10000_Entities()
    {
        InitializeEcsModule();

        for (int i = 0; i < 10000; i++)
        {
            _world!.Create(new Position(0, 0), new Velocity(1, 1));
        }

        var startTime = DateTime.UtcNow;
        _ecsModule!.Update(0.016f);
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        Assert.That(_world!.Size, Is.EqualTo(10000));
        Assert.That(elapsed, Is.LessThan(100), $"Updating 10000 entities took: {elapsed}ms");
    }

    [Test]
    public void Performance_Test_1000_Entities_Creation()
    {
        InitializeEcsModule();

        var startTime = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            _world!.Create(new Position(0, 0), new Velocity(1, 1));
        }

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        Assert.That(_world!.Size, Is.EqualTo(1000));
        Assert.That(elapsed, Is.LessThan(50), $"Creating 1000 entities took: {elapsed}ms");
    }
}