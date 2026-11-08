// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Logging;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch.Components;
using GFramework.Ecs.Arch.Systems;

using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch.Tests.Ecs;

/// <summary>
/// ECS 集成测试类 - 使用 Arch 原生 API
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsIntegrationTests
{
    private TestArchitecture? _architecture;
    private World? _world;
    private ArchEcsModule? _ecsModule;

    /// <summary>Stops the real participant and releases its provider-owned World.</summary>
    [TearDown]
    public async Task TearDown()
    {
        if (_architecture is not null)
            await _architecture.DestroyAsync().ConfigureAwait(false);
        _architecture = null;
        _world = null;
        _ecsModule = null;
    }

    /// <summary>Uses standard DI constructor injection and the production lifecycle adapter.</summary>
    private void InitializeEcsModule(bool withSystem = true)
    {
        _ecsModule = new ArchEcsModule();
        _architecture = new TestArchitecture(_ecsModule, withSystem);
        _architecture.Initialize();
        _world = _architecture.Services.GetRequiredService<World>();
    }

    /// <summary>Minimal ECS composition root for behavioral tests.</summary>
    private sealed class TestArchitecture(ArchEcsModule module, bool withSystem) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddModule(module);
            if (withSystem)
                services.AddSingleton<ArchSystemAdapter<float>, MovementSystem>();
        }
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

        var adapters = _architecture!.Services.GetServices<ArchSystemAdapter<float>>().ToArray();
        Assert.That(adapters, Is.Not.Null);
        Assert.That(adapters.Length, Is.EqualTo(1));
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
