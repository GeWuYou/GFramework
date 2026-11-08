// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using GFramework.Core.Architectures;
using GFramework.Ecs.Arch.Components;
using GFramework.Ecs.Arch.Extensions;
using GFramework.Ecs.Arch.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Ecs.Arch.Tests.Integration;

/// <summary>Exercises ECS adoption through standard DI and the real architecture owner.</summary>
[TestFixture]
public sealed class ExplicitRegistrationTests
{
    /// <summary>World factories activate once and are isolated between architecture instances.</summary>
    [Test]
    public async Task WorldsAreSingleInstanceWithinAnArchitectureAndIndependentBetweenOwners()
    {
        await using var first = new TestArchitecture(services => services.AddArch());
        await using var second = new TestArchitecture(services => services.AddArch());
        await first.InitializeAsync();
        await second.InitializeAsync();
        var firstWorld = first.Services.GetRequiredService<World>();
        var secondWorld = second.Services.GetRequiredService<World>();
        Assert.That(firstWorld, Is.SameAs(first.Services.GetRequiredService<World>()));
        Assert.That(firstWorld, Is.Not.SameAs(secondWorld));
        await first.DestroyAsync();
        var entity = secondWorld.Create(new Position(1, 2));
        Assert.That(secondWorld.IsAlive(entity), Is.True);
    }

    /// <summary>Standard constructor injection supplies the exact owned world to systems.</summary>
    [Test]
    public async Task SystemsReceiveTheCanonicalWorld()
    {
        await using var architecture = new TestArchitecture(services =>
        {
            services.AddArch();
            services.AddSingleton<ArchSystemAdapter<float>, MovementSystem>();
        });
        await architecture.InitializeAsync();
        Assert.That(architecture.Services.GetRequiredService<ArchSystemAdapter<float>>().World,
            Is.SameAs(architecture.Services.GetRequiredService<World>()));
    }

    /// <summary>Partially started systems stop once before DI disposes their resources.</summary>
    [Test]
    public async Task PartialInitializationFailureStopsEveryAttemptedSystem()
    {
        StartedSystem? first = null;
        FailingSystem? second = null;
        var architecture = new TestArchitecture(services =>
        {
            services.AddArch();
            services.AddSingleton<ArchSystemAdapter<float>>(provider => first = new StartedSystem(provider.GetRequiredService<World>()));
            services.AddSingleton<ArchSystemAdapter<float>>(provider => second = new FailingSystem(provider.GetRequiredService<World>()));
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(first!.Stops, Is.EqualTo(1));
        Assert.That(second!.Stops, Is.EqualTo(1));
        await architecture.DestroyAsync();
        Assert.That(first.Stops, Is.EqualTo(1));
    }

    /// <summary>Disabled modules do not allocate worlds.</summary>
    [Test]
    public async Task DisabledModuleDoesNotCreateWorld()
    {
        await using var architecture = new TestArchitecture(services => services.AddModule(new ArchEcsModule(enabled: false)));
        await architecture.InitializeAsync();
        Assert.That(architecture.Services.GetService<World>(), Is.Null);
    }

    /// <summary>A production architecture with only fixture descriptions.</summary>
    private sealed class TestArchitecture(Action<IServiceCollection> configure) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services) => configure(services);
    }
    /// <summary>A system recording business stop independently of final resource cleanup.</summary>
    private class StartedSystem(World world) : ArchSystemAdapter<float>(world)
    {
        /// <summary>The number of business stop callbacks.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <inheritdoc />
        protected override void OnArchStopped() => Stops++;
    }
    /// <summary>A system that fails after activation and therefore still owes stop.</summary>
    private sealed class FailingSystem(World world) : StartedSystem(world)
    {
        /// <inheritdoc />
        protected override void OnArchInitialize() => throw new InvalidOperationException("Expected partial system failure.");
    }
}
