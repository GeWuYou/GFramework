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
/// ECS 基础功能测试类 - 使用 Arch 原生 API
/// </summary>
[TestFixture]
[Experimental("GFrameworkECS")]
public class EcsBasicTests
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
