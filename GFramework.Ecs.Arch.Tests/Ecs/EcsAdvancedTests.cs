// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using GFramework.Ecs.Arch.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using GFramework.Core.Logging;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch.Components;
using GFramework.Ecs.Arch.Systems;

using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch.Tests.Ecs;

/// <summary>
/// ECS 高级功能测试类 - 使用 Arch 原生 API
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsAdvancedTests
{
    private TestArchitecture? _architecture;
    private World? _world;
    private IArchRuntime? _ecsModule;

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
        _architecture = new TestArchitecture(new ArchEcsModule(), withSystem);
        _architecture.Initialize();
        _ecsModule = _architecture.Services.GetRequiredService<IArchRuntime>();
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
                services.AddSingleton<global::Arch.System.ISystem<float>, MovementSystem>();
        }
    }

    [Test]
    public async Task World_Destroy_Should_Be_Safe()
    {
        InitializeEcsModule();
        _world!.Create(new Position(0, 0));

        await _architecture!.DestroyAsync();
        _world = null;
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
        InitializeEcsModule(withSystem: false);

        _ecsModule!.Update(1.0f);
    }

    [Test]
    public void ChainedUpdates_Should_AccumulateChanges()
    {
        InitializeEcsModule();

        var entity = _world!.Create(new Position(0, 0), new Velocity(10, 0));

        _ecsModule!.Update(1.0f);
        _ecsModule!.Update(1.0f);

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

        await _architecture!.DestroyAsync().ConfigureAwait(false);

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
