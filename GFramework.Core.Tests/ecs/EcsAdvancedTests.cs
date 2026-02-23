using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Arch.Core;
using GFramework.Core.Abstractions.ecs;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.architecture;
using GFramework.Core.ecs;
using GFramework.Core.ecs.components;
using GFramework.Core.ecs.systems;
using GFramework.Core.ioc;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.ecs;

[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsAdvancedTests
{
    [SetUp]
    public void Setup()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();

        _container = new MicrosoftDiContainer();
        var loggerField = typeof(MicrosoftDiContainer).GetField("_logger",
            BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(_container,
            LoggerFactoryResolver.Provider.CreateLogger(nameof(EcsAdvancedTests)));

        _context = new ArchitectureContext(_container);
    }

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

    private void InitializeEcsWithSystems(params Type[] systemTypes)
    {
        _ecsWorld = new EcsWorld();
        _container!.Register(_ecsWorld);
        _container.Register(_ecsWorld as IEcsWorld);

        var systems = new List<IEcsSystem>();
        foreach (var systemType in systemTypes)
        {
            var system = (IEcsSystem)Activator.CreateInstance(systemType)!;
            ((IContextAware)system).SetContext(_context!);
            system.Initialize();
            systems.Add(system);
            _container.RegisterPlurality(system);
        }

        _container.Register(systems as IReadOnlyList<IEcsSystem>);
    }

    private EcsSystemRunner CreateRunner()
    {
        var runner = new EcsSystemRunner();
        ((IContextAware)runner).SetContext(_context!);
        runner.Initialize();
        return runner;
    }

    [Test]
    public void EcsWorld_Dispose_Should_Be_Idempotent()
    {
        _ecsWorld = new EcsWorld();
        _ecsWorld.CreateEntity(typeof(Position));

        Assert.DoesNotThrow(() =>
        {
            _ecsWorld.Dispose();
            _ecsWorld.Dispose();
        });

        _ecsWorld = null;
    }

    [Test]
    public void EcsWorld_CreateEntity_WithNoComponents_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity();

        Assert.That(_ecsWorld.EntityCount, Is.EqualTo(1));
        Assert.That(_ecsWorld.IsAlive(entity), Is.True);
    }

    [Test]
    public void EcsWorld_CreateEntity_WithMultipleComponents_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));

        var world = _ecsWorld.InternalWorld;
        Assert.That(world.Has<Position>(entity), Is.True);
        Assert.That(world.Has<Velocity>(entity), Is.True);
    }

    [Test]
    public void EcsWorld_IsAlive_AfterDestroy_Should_ReturnFalse()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position));

        Assert.That(_ecsWorld.IsAlive(entity), Is.True);

        _ecsWorld.DestroyEntity(entity);

        Assert.That(_ecsWorld.IsAlive(entity), Is.False);
    }

    [Test]
    public void EcsSystemRunner_Update_WithoutStart_Should_NotUpdate()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var entity = _ecsWorld!.CreateEntity(typeof(Position), typeof(Velocity));
        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(0, 0));
        world.Set(entity, new Velocity(10, 5));

        var runner = CreateRunner();

        runner.Update(1.0f);

        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(0), "Position should not change without Start()");
        Assert.That(pos.Y, Is.EqualTo(0), "Position should not change without Start()");
    }

    [Test]
    public void EcsSystemRunner_StartStop_Should_ControlUpdates()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var entity = _ecsWorld!.CreateEntity(typeof(Position), typeof(Velocity));
        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(0, 0));
        world.Set(entity, new Velocity(10, 5));

        var runner = CreateRunner();

        runner.Start();
        runner.Update(1.0f);
        runner.Stop();
        runner.Update(1.0f);

        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(10).Within(0.001f), "Only first update should apply");
        Assert.That(pos.Y, Is.EqualTo(5).Within(0.001f), "Only first update should apply");
    }

    [Test]
    public void EcsSystemRunner_WithNoSystems_Should_NotThrow()
    {
        _ecsWorld = new EcsWorld();
        _container!.Register(_ecsWorld);
        _container.Register(new List<IEcsSystem>() as IReadOnlyList<IEcsSystem>);

        var runner = CreateRunner();

        Assert.DoesNotThrow(() =>
        {
            runner.Start();
            runner.Update(1.0f);
            runner.Stop();
        });
    }

    [Test]
    public void EcsSystemRunner_OnDestroy_Should_ClearSystems()
    {
        InitializeEcsWithSystems(typeof(MovementSystem));

        var entity = _ecsWorld!.CreateEntity(typeof(Position), typeof(Velocity));
        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(0, 0));
        world.Set(entity, new Velocity(10, 5));

        var runner = CreateRunner();
        runner.Start();

        // 销毁前先更新一次，记录初始位置
        runner.Update(1.0f);
        ref var posBeforeDestroy = ref world.Get<Position>(entity);
        var xBefore = posBeforeDestroy.X;
        var yBefore = posBeforeDestroy.Y;

        runner.Destroy();

        // 销毁后再更新，位置应该保持不变
        runner.Update(1.0f);

        ref var posAfterDestroy = ref world.Get<Position>(entity);
        Assert.That(posAfterDestroy.X, Is.EqualTo(xBefore), "Position should not change after Destroy()");
        Assert.That(posAfterDestroy.Y, Is.EqualTo(yBefore), "Position should not change after Destroy()");
    }

    [Test]
    public void MultipleSystems_Should_ExecuteInPriorityOrder()
    {
        var executionOrder = new List<string>();

        _ecsWorld = new EcsWorld();
        _container!.Register(_ecsWorld);

        var systemA = new OrderTrackingSystem("A", 10, executionOrder);
        var systemB = new OrderTrackingSystem("B", -10, executionOrder);
        var systemC = new OrderTrackingSystem("C", 0, executionOrder);

        foreach (var system in new[] { systemA, systemB, systemC })
        {
            ((IContextAware)system).SetContext(_context!);
            system.Initialize();
            _container.RegisterPlurality(system);
        }

        _container.Register(new List<IEcsSystem> { systemA, systemB, systemC } as IReadOnlyList<IEcsSystem>);

        var runner = CreateRunner();
        runner.Start();
        runner.Update(1.0f);

        Assert.That(executionOrder, Is.EqualTo(["B", "C", "A"]),
            "Systems should execute in priority order (B=-10, C=0, A=10)");
    }

    [Test]
    public void ChainedSystems_Should_PassDataBetweenSystems()
    {
        _ecsWorld = new EcsWorld();
        _container!.Register(_ecsWorld);

        var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));
        var world = _ecsWorld.InternalWorld;
        world.Set(entity, new Position(0, 0));
        world.Set(entity, new Velocity(10, 0));

        var movementSystem = new MovementSystem();
        ((IContextAware)movementSystem).SetContext(_context!);
        movementSystem.Initialize();
        _container.RegisterPlurality(movementSystem);

        _container.Register(new List<IEcsSystem> { movementSystem } as IReadOnlyList<IEcsSystem>);

        var runner = CreateRunner();
        runner.Start();
        runner.Update(1.0f);
        runner.Update(1.0f);

        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(20).Within(0.001f), "Position should accumulate over multiple updates");
    }

    [Test]
    public void InitializeEcs_CalledTwice_Should_BeIdempotent()
    {
        _context!.InitializeEcs();
        var ecsWorld1 = _context.GetEcsWorld();

        Assert.DoesNotThrow(() => _context.InitializeEcs());

        var ecsWorld2 = _context.GetEcsWorld();
        Assert.That(ecsWorld2, Is.SameAs(ecsWorld1), "Should return same world instance");
    }

    [Test]
    public void GetEcsWorld_Should_ReturnIEcsWorld()
    {
        _context!.InitializeEcs();
        var ecsWorld = _context.GetEcsWorld();

        Assert.That(ecsWorld, Is.InstanceOf<IEcsWorld>());
        Assert.That(ecsWorld, Is.InstanceOf<EcsWorld>());
    }

    [Test]
    public void Component_AddAfterCreation_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(Array.Empty<ComponentType>());
        var world = _ecsWorld.InternalWorld;

        world.Add(entity, new Position(5, 10));

        Assert.That(world.Has<Position>(entity), Is.True);
        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(5));
        Assert.That(pos.Y, Is.EqualTo(10));
    }

    [Test]
    public void Component_Remove_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position), typeof(Velocity));
        var world = _ecsWorld.InternalWorld;

        world.Remove<Velocity>(entity);

        Assert.That(world.Has<Position>(entity), Is.True);
        Assert.That(world.Has<Velocity>(entity), Is.False);
    }

    [Test]
    public void Component_Replace_Should_Work()
    {
        _ecsWorld = new EcsWorld();
        var entity = _ecsWorld.CreateEntity(typeof(Position));
        var world = _ecsWorld.InternalWorld;

        world.Set(entity, new Position(1, 1));
        world.Set(entity, new Position(100, 200));

        ref var pos = ref world.Get<Position>(entity);
        Assert.That(pos.X, Is.EqualTo(100));
        Assert.That(pos.Y, Is.EqualTo(200));
    }
}

internal class OrderTrackingSystem(string name, int priority, List<string> executionOrder) : EcsSystemBase
{
    public override int Priority { get; } = priority;

    protected override void OnEcsInit()
    {
    }

    public override void Update(float deltaTime)
    {
        executionOrder.Add(name);
    }
}