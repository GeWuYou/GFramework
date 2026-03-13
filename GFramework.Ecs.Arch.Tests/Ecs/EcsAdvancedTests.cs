using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Ecs.Arch.Components;
using GFramework.Ecs.Arch.Systems;

namespace GFramework.Ecs.Arch.Tests.Ecs;

/// <summary>
/// ECS 高级功能测试类 - 使用 Arch 原生 API
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsAdvancedTests
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

    private void InitializeEcsModule()
    {
        var movementSystem = new MovementSystem();
        ((IContextAware)movementSystem).SetContext(_context!);
        _container!.RegisterPlurality(movementSystem);

        _ecsModule = new ArchEcsModule(enabled: true);
        _ecsModule.Register(_container);
        _ecsModule.Initialize();

        _world = _container.Get<World>();
    }

    [Test]
    public void World_Destroy_Should_Be_Safe()
    {
        InitializeEcsModule();
        _world!.Create(new Position(0, 0));

        Assert.DoesNotThrow(() =>
        {
            World.Destroy(_world);
            _world = null;
        });
    }

    [Test]
    public void World_CreateEntity_WithNoComponents_Should_Work()
    {
        InitializeEcsModule();
        var entity = _world!.Create();

        Assert.That(_world.Size, Is.EqualTo(1));
        Assert.That(_world.IsAlive(entity), Is.True);
    }

    [Test]
    public void World_CreateEntity_WithMultipleComponents_Should_Work()
    {
        InitializeEcsModule();
        var entity = _world!.Create(new Position(0, 0), new Velocity(1, 1));

        Assert.That(_world.Has<Position>(entity), Is.True);
        Assert.That(_world.Has<Velocity>(entity), Is.True);
    }

    [Test]
    public void World_IsAlive_AfterDestroy_Should_ReturnFalse()
    {
        InitializeEcsModule();
        var entity = _world!.Create(new Position(0, 0));

        Assert.That(_world.IsAlive(entity), Is.True);

        _world.Destroy(entity);

        Assert.That(_world.IsAlive(entity), Is.False);
    }

    [Test]
    public void ArchEcsModule_Update_Should_UpdateSystems()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(10, 5));

        _ecsModule!.Update(1.0f);

        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10).Within(0.001f));
        Assert.That(pos.Y, Is.EqualTo(5).Within(0.001f));
    }

    [Test]
    public void ArchEcsModule_WithNoSystems_Should_NotThrow()
    {
        // 不注册任何系统
        _ecsModule = new ArchEcsModule(enabled: true);
        _ecsModule.Register(_container!);
        _ecsModule.Initialize();

        _world = _container!.Get<World>();

        Assert.DoesNotThrow(() => _ecsModule.Update(1.0f));
    }

    [Test]
    public void ChainedUpdates_Should_AccumulateChanges()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(10, 0));

        _ecsModule!.Update(1.0f);
        _ecsModule.Update(1.0f);

        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(20).Within(0.001f), "Position should accumulate over multiple updates");
    }

    [Test]
    public void Component_AddAfterCreation_Should_Work()
    {
        InitializeEcsModule();
        var entity = _world!.Create();

        _world.Add(entity, new Position(5, 10));

        Assert.That(_world.Has<Position>(entity), Is.True);
        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(5));
        Assert.That(pos.Y, Is.EqualTo(10));
    }

    [Test]
    public void Component_Remove_Should_Work()
    {
        InitializeEcsModule();
        var entity = _world!.Create(new Position(0, 0), new Velocity(1, 1));

        _world.Remove<Velocity>(entity);

        Assert.That(_world.Has<Position>(entity), Is.True);
        Assert.That(_world.Has<Velocity>(entity), Is.False);
    }

    [Test]
    public void Component_Replace_Should_Work()
    {
        InitializeEcsModule();
        var entity = _world!.Create(new Position(1, 1));

        _world.Set(entity, new Position(100, 200));

        ref var pos = ref _world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(100));
        Assert.That(pos.Y, Is.EqualTo(200));
    }

    [Test]
    public async Task ArchEcsModule_DestroyAsync_Should_CleanupResources()
    {
        InitializeEcsModule();

        _world!.Create(new Position(0, 0));
        Assert.That(_world.Size, Is.EqualTo(1));

        await _ecsModule!.DestroyAsync();

        // World 应该已经被销毁
        _world = null;
    }

    [Test]
    public void MultipleEntities_WithDifferentComponents_Should_CoExist()
    {
        InitializeEcsModule();

        var entity1 = _world!.Create(new Position(0, 0), new Velocity(1, 1));
        var entity2 = _world.Create(new Position(10, 10));
        var entity3 = _world.Create(new Velocity(5, 5));

        Assert.That(_world.Size, Is.EqualTo(3));
        Assert.That(_world.Has<Position>(entity1), Is.True);
        Assert.That(_world.Has<Velocity>(entity1), Is.True);
        Assert.That(_world.Has<Position>(entity2), Is.True);
        Assert.That(_world.Has<Velocity>(entity2), Is.False);
        Assert.That(_world.Has<Position>(entity3), Is.False);
        Assert.That(_world.Has<Velocity>(entity3), Is.True);
    }
}