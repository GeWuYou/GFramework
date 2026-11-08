// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Arch.Core;
using GFramework.Ecs.Arch.Abstractions;
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
            services.AddSingleton<global::Arch.System.ISystem<float>, MovementSystem>();
        });
        await architecture.InitializeAsync();
        Assert.That(architecture.Services.GetRequiredService<global::Arch.System.ISystem<float>>() is ArchSystemAdapter<float> adapter ? adapter.World : null,
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
            services.AddSingleton<global::Arch.System.ISystem<float>>(provider => first = new StartedSystem(provider.GetRequiredService<World>()));
            services.AddSingleton<global::Arch.System.ISystem<float>>(provider => second = new FailingSystem(provider.GetRequiredService<World>()));
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

    /// <summary>A configuration-only module can describe independent owners without sharing mutable runtime state.</summary>
    [Test]
    public void ReusedModuleCreatesIndependentRuntimes()
    {
        var module = new ArchEcsModule();
        var first = new TestArchitecture(services => services.AddModule(module));
        var second = new TestArchitecture(services => services.AddModule(module));
        try
        {
            first.Initialize();
            second.Initialize();
            Assert.That(first.Services.GetRequiredService<IArchRuntime>(),
                Is.Not.SameAs(second.Services.GetRequiredService<IArchRuntime>()));
        }
        finally
        {
            first.DestroyAsync().GetAwaiter().GetResult();
            second.DestroyAsync().GetAwaiter().GetResult();
        }
    }

    /// <summary>The provider stops business once, disposes a native root once, and leaves its World alive until then.</summary>
    [Test]
    public void ProviderOwnsNativeFinalRelease()
    {
        OwnedNativeSystem? native = null;
        var architecture = new TestArchitecture(services =>
        {
            services.AddArch();
            services.AddSingleton<global::Arch.System.ISystem<float>>(provider =>
                native = new OwnedNativeSystem(provider.GetRequiredService<World>()));
        });
        architecture.Initialize();
        architecture.DestroyAsync().GetAwaiter().GetResult();
        architecture.DestroyAsync().GetAwaiter().GetResult();
        Assert.That(native!.DisposeCalls, Is.EqualTo(1));
        Assert.That(native.Stopped, Is.True);
        Assert.That(native.WorldWasAliveAtDispose, Is.True);
    }

    /// <summary>A native system records ownership without depending on the optional adapter base.</summary>
    private sealed class OwnedNativeSystem(World world) : global::Arch.System.BaseSystem<World, float>(world),
        GFramework.Core.Abstractions.Lifecycle.IDestroyable
    {
        /// <summary>Number of provider-owned final releases.</summary>
        public int DisposeCalls
        {
            get; private set;
        }
        /// <summary>Whether business work has stopped.</summary>
        public bool Stopped
        {
            get; private set;
        }
        /// <summary>Whether disposal could still use the provider-owned World.</summary>
        public bool WorldWasAliveAtDispose
        {
            get; private set;
        }
        /// <inheritdoc />
        public override void Update(in float t)
        {
        }
        /// <inheritdoc />
        public void Destroy() => Stopped = true;
        /// <inheritdoc />
        public override void Dispose()
        {
            DisposeCalls++;
            var entity = World.Create(new Position(1, 1));
            WorldWasAliveAtDispose = World.IsAlive(entity);
        }
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
