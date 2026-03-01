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
/// ECS 基础功能测试类 - 使用 Arch 原生 API
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsBasicTests
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
        // 注册系统（直接继承 ArchSystemAdapter，它继承自 AbstractSystem）
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
    public void Test_01_InitializeEcs_Should_Create_World()
    {
        InitializeEcsModule();

        Assert.That(_world, Is.Not.Null, "World should be created");
        Assert.That(_world!.Size, Is.EqualTo(0), "Initial entity count should be 0");
    }

    [Test]
    public void Test_02_CreateEntity_Should_Work()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(1, 1));

        Assert.That(_world.Size, Is.EqualTo(1), "Entity count should be 1");
        Assert.That(_world.IsAlive(entity), Is.True, "Entity should be alive");
    }

    [Test]
    public void Test_03_SetComponent_Should_Store_Data()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(10, 20));

        Assert.That(_world.Has<Position>(entity), Is.True, "Entity should have Position component");
        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10), "Position.X should be 10");
        Assert.That(pos.Y, Is.EqualTo(20), "Position.Y should be 20");
    }

    [Test]
    public void Test_04_MovementSystem_Should_Update_Position()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(10, 5));

        // 更新系统
        _ecsModule!.Update(1.0f);

        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10).Within(0.001f), "X position should be 10");
        Assert.That(pos.Y, Is.EqualTo(5).Within(0.001f), "Y position should be 5");
    }

    [Test]
    public void Test_05_DestroyEntity_Should_Work()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0));
        _world.Destroy(entity);

        Assert.That(_world.Size, Is.EqualTo(0), "Entity count should be 0");
        Assert.That(_world.IsAlive(entity), Is.False, "Entity should not be alive");
    }

    [Test]
    public void Test_06_ClearWorld_Should_Remove_All_Entities()
    {
        InitializeEcsModule();

        for (int i = 0; i < 10; i++)
        {
            _world!.Create(new Position(0, 0));
        }

        _world!.Clear();

        Assert.That(_world.Size, Is.EqualTo(0), "Entity count should be 0 after clear");
    }

    [Test]
    public void Test_07_Multiple_Entities_Should_Update_Correctly()
    {
        InitializeEcsModule();

        var entities = new Entity[10];
        for (var i = 0; i < 10; i++)
        {
            entities[i] = _world!.Create(new Position(0, 0), new Velocity(i, i * 2));
        }

        // 更新系统
        _ecsModule!.Update(1.0f);

        for (int i = 0; i < 10; i++)
        {
            ref var pos = ref _world!.Get<Position>(entities[i]);
            Assert.That(pos.X, Is.EqualTo(i).Within(0.001f), $"Entity {i} X position should be {i}");
            Assert.That(pos.Y, Is.EqualTo(i * 2).Within(0.001f), $"Entity {i} Y position should be {i * 2}");
        }
    }
}