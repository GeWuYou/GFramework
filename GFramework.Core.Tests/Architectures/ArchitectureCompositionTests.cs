// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>Exercises standard DI identity and ownership through real architecture operations and failure paths.</summary>
[TestFixture]
public sealed class ArchitectureCompositionTests
{
    /// <summary>Configuration cannot accidentally resolve partially described services.</summary>
    [Test]
    public async Task ServicesAreUnavailableBeforeBuildAndAfterStop()
    {
        var architecture = new TestArchitecture(services => services.AddSingleton<Dependency>());
        Assert.Throws<InvalidOperationException>(() => architecture.Services.GetRequiredService<Dependency>());
        await architecture.InitializeAsync();
        var provider = architecture.Services;
        Assert.That(provider.GetRequiredService<Dependency>(), Is.Not.Null);
        await architecture.DestroyAsync();
        Assert.Throws<ObjectDisposedException>(() => provider.GetRequiredService<Dependency>());
    }

    /// <summary>Component construction and ordinary resolution use the same real singleton.</summary>
    [Test]
    public async Task ComponentDependenciesHaveOneIdentityAndNoAutomaticAliases()
    {
        await using var architecture = new TestArchitecture(services =>
        {
            services.AddSingleton<Dependency>();
            services.AddComponent<IConsumer, Consumer>();
        });
        await architecture.InitializeAsync();
        var consumer = architecture.Services.GetRequiredService<IConsumer>();
        Assert.That(consumer.Dependency, Is.SameAs(architecture.Services.GetRequiredService<Dependency>()));
        Assert.That(architecture.Services.GetService<Consumer>(), Is.Null);
    }

    /// <summary>Plain DI services do not implicitly join startup.</summary>
    [Test]
    public async Task OrdinaryServiceIsNotAutomaticallyStarted()
    {
        var probe = new SyncParticipant();
        await using var architecture = new TestArchitecture(services => services.AddSingleton(probe));
        await architecture.InitializeAsync();
        _ = architecture.Services.GetRequiredService<SyncParticipant>();
        Assert.That(probe.Starts, Is.Zero);
    }

    /// <summary>Explicit order is stable and stopping reverses the actual startup order.</summary>
    [Test]
    public async Task OrderIsIndependentOfClassRoleAndStableWithinTies()
    {
        var calls = new List<string>();
        var first = new Participant(() => calls.Add("start-first"), () => calls.Add("stop-first"));
        var second = new SecondParticipant(() => calls.Add("start-second"), () => calls.Add("stop-second"));
        var third = new ThirdParticipant(() => calls.Add("start-third"), () => calls.Add("stop-third"));
        var architecture = new TestArchitecture(services =>
        {
            services.AddComponent(first, order: 5);
            services.AddComponent(second, order: -1);
            services.AddComponent(third, order: 5);
        });
        await architecture.InitializeAsync();
        await architecture.DestroyAsync();
        Assert.That(calls, Is.EqualTo(new[] { "start-second", "start-first", "start-third", "stop-third", "stop-first", "stop-second" }));
    }

    /// <summary>A borrowed participant is stopped but its final resource remains caller-owned.</summary>
    [Test]
    public async Task BorrowedParticipantIsNotDisposedByProvider()
    {
        var probe = new ResourceParticipant();
        var architecture = new TestArchitecture(services => services.AddComponent(probe));
        await architecture.InitializeAsync();
        await architecture.DestroyAsync();
        Assert.That(probe.Stops, Is.EqualTo(1));
        Assert.That(probe.Disposals, Is.Zero);
        await probe.DisposeAsync();
        Assert.That(probe.Disposals, Is.EqualTo(1));
    }

    /// <summary>Factory-created async-only resources are disposed by their sole provider exactly once.</summary>
    [Test]
    public async Task OwnedParticipantIsStoppedAndDisposedOnce()
    {
        ResourceParticipant? probe = null;
        var architecture = new TestArchitecture(services => services.AddComponent(_ => probe = new ResourceParticipant()));
        await architecture.InitializeAsync();
        var stop1 = architecture.DestroyAsync().AsTask();
        var stop2 = architecture.DisposeAsync().AsTask();
        Assert.That(stop2, Is.SameAs(stop1));
        await Task.WhenAll(stop1, stop2);
        Assert.That(probe!.Starts, Is.EqualTo(1));
        Assert.That(probe.Stops, Is.EqualTo(1));
        Assert.That(probe.Disposals, Is.EqualTo(1));
    }

    /// <summary>Aliases cannot silently replace a participant's canonical DI key.</summary>
    [Test]
    public void DuplicateComponentKeyIsRejected()
    {
        var services = new ServiceCollection();
        services.AddComponent<SyncParticipant>();
        Assert.Throws<InvalidOperationException>(() => services.AddComponent<SyncParticipant>());
    }

    /// <summary>Later modules cannot append a second key or replace a component with a scoped/transient binding.</summary>
    [TestCase(ServiceLifetime.Singleton, false)]
    [TestCase(ServiceLifetime.Scoped, false)]
    [TestCase(ServiceLifetime.Transient, false)]
    [TestCase(ServiceLifetime.Scoped, true)]
    [TestCase(ServiceLifetime.Transient, true)]
    public async Task FinalCompositionRejectsInvalidComponentBindings(ServiceLifetime lifetime, bool replace)
    {
        var original = new SyncParticipant();
        var activations = 0;
        var architecture = new TestArchitecture(services =>
        {
            services.AddComponent(original);
            if (replace)
            {
                var binding = services.Single(description => description.ServiceType == typeof(SyncParticipant));
                services.Remove(binding);
            }
            services.Add(new ServiceDescriptor(typeof(SyncParticipant), _ =>
            {
                activations++;
                return new SyncParticipant();
            }, lifetime));
        });
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(error!.Message, Does.Contain("exactly one unkeyed singleton"));
        Assert.That(original.Starts, Is.Zero);
        Assert.That(activations, Is.Zero, "Invalid composition must fail before activating services.");
        Assert.That(architecture.IsReady, Is.False);
        await architecture.DestroyAsync();
    }

    /// <summary>Removing a participant's canonical description is rejected before any activation.</summary>
    [Test]
    public async Task FinalCompositionRejectsMissingComponentBinding()
    {
        var architecture = new TestArchitecture(services =>
        {
            services.AddComponent<SyncParticipant>();
            services.Remove(services.Single(description => description.ServiceType == typeof(SyncParticipant)));
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        await architecture.DestroyAsync();
    }

    /// <summary>A keyed service is independent of the unkeyed component key and retains standard DI behavior.</summary>
    [Test]
    public async Task FinalCompositionAllowsIndependentKeyedService()
    {
        var component = new SyncParticipant();
        await using var architecture = new TestArchitecture(services =>
        {
            services.AddComponent(component);
            services.AddKeyedTransient<SyncParticipant>("other");
        });
        await architecture.InitializeAsync();
        Assert.That(architecture.Services.GetRequiredService<SyncParticipant>(), Is.SameAs(component));
        Assert.That(component.Starts, Is.EqualTo(1));
    }

    /// <summary>Provider validation rejects scoped services captured by singleton participants.</summary>
    [Test]
    public async Task RootScopedCaptureFailsBeforeReady()
    {
        var architecture = new TestArchitecture(services =>
        {
            services.AddScoped<Dependency>();
            services.AddComponent<IConsumer, Consumer>();
        });
        await Assert.ThrowsAsync<AggregateException>(() => architecture.InitializeAsync());
        await Assert.ThrowsAsync<AggregateException>(() => architecture.WaitUntilReadyAsync());
        Assert.That(architecture.IsReady, Is.False);
        await architecture.DestroyAsync();
    }

    /// <summary>Standard scopes retain their normal per-scope instance boundaries.</summary>
    [Test]
    public async Task ScopesRemainIndependent()
    {
        await using var architecture = new TestArchitecture(services => services.AddScoped<Dependency>());
        await architecture.InitializeAsync();
        using var first = architecture.Services.CreateScope();
        using var second = architecture.Services.CreateScope();
        Assert.That(first.ServiceProvider.GetRequiredService<Dependency>(), Is.SameAs(first.ServiceProvider.GetRequiredService<Dependency>()));
        Assert.That(first.ServiceProvider.GetRequiredService<Dependency>(), Is.Not.SameAs(second.ServiceProvider.GetRequiredService<Dependency>()));
        Assert.Throws<InvalidOperationException>(() => architecture.Services.GetRequiredService<Dependency>());
    }

    /// <summary>A failed participant still stops, previous participants stop, and later participants never start.</summary>
    [Test]
    public async Task PartialFailureCleansStartedParticipantsAndOwnedResources()
    {
        var first = new ResourceParticipant();
        var failure = new SecondParticipant(() => throw new InvalidOperationException("initialization"));
        var last = new ThirdParticipant();
        var architecture = new TestArchitecture(services =>
        {
            services.AddComponent(_ => first);
            services.AddComponent(failure);
            services.AddComponent(last);
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(first.Stops, Is.EqualTo(1));
        Assert.That(first.Disposals, Is.EqualTo(1));
        Assert.That(failure.Stops, Is.EqualTo(1));
        Assert.That(last.Starts, Is.Zero);
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Faulted));
        await architecture.DestroyAsync();
        Assert.That(first.Stops, Is.EqualTo(1));
    }

    /// <summary>Cancellation waits for the participant to observe its token and for cleanup to complete.</summary>
    [Test]
    public async Task CancellationCompletesReadinessOnlyAfterCleanup()
    {
        using var cancellation = new CancellationTokenSource();
        var probe = new WaitingParticipant();
        var architecture = new TestArchitecture(services => services.AddComponent(probe));
        var start = architecture.InitializeAsync(cancellation.Token);
        await probe.Entered.Task;
        cancellation.Cancel();
        await Assert.CatchAsync<OperationCanceledException>(() => start);
        Assert.That(probe.Stops, Is.EqualTo(1));
        await Assert.CatchAsync<OperationCanceledException>(() => architecture.WaitUntilReadyAsync());
        await architecture.DestroyAsync();
    }

    /// <summary>Stop cancels and joins in-flight startup instead of racing provider release.</summary>
    [Test]
    public async Task StopDuringStartupJoinsTheSameCleanup()
    {
        var probe = new WaitingParticipant();
        var architecture = new TestArchitecture(services => services.AddComponent(probe));
        var start = architecture.InitializeAsync();
        await probe.Entered.Task;
        await architecture.DestroyAsync();
        await Assert.CatchAsync<OperationCanceledException>(() => start);
        Assert.That(probe.Stops, Is.EqualTo(1));
    }

    /// <summary>Cleanup errors never skip another participant or provider-owned resource.</summary>
    [Test]
    public async Task CleanupFailuresAreAggregatedAfterAllResourcesAreAttempted()
    {
        var owned = new ResourceParticipant();
        var failing = new SecondParticipant(stop: () => throw new InvalidOperationException("stop"));
        var architecture = new TestArchitecture(services => { services.AddComponent(_ => owned); services.AddComponent(failing); });
        await architecture.InitializeAsync();
        await Assert.ThrowsAsync<AggregateException>(async () => await architecture.DestroyAsync().ConfigureAwait(false));
        Assert.That(owned.Stops, Is.EqualTo(1));
        Assert.That(owned.Disposals, Is.EqualTo(1));
        await Assert.ThrowsAsync<AggregateException>(async () => await architecture.DestroyAsync().ConfigureAwait(false));
        Assert.That(failing.Stops, Is.EqualTo(1));
    }

    /// <summary>Concurrent stop callers remain pending until cleanup ends, while external root access closes immediately.</summary>
    [Test]
    public async Task ConcurrentStopWaitsForCleanupAndDeniesExternalResolution()
    {
        var probe = new BlockingStopParticipant();
        var architecture = new TestArchitecture(services => { services.AddSingleton<Dependency>(); services.AddComponent(probe); });
        await architecture.InitializeAsync();
        var first = architecture.DestroyAsync().AsTask();
        await probe.Entered.Task;
        var second = architecture.DestroyAsync().AsTask();
        try
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.IsCompleted, Is.False);
            Assert.Throws<ObjectDisposedException>(() => architecture.Services.GetRequiredService<Dependency>());
        }
        finally { probe.Release.TrySetResult(); }
        await Task.WhenAll(first, second);
        Assert.That(probe.Stops, Is.EqualTo(1));
    }

    /// <summary>Constructor failure still releases dependencies created by the final provider without stopping an unstarted component.</summary>
    [Test]
    public async Task ActivationFailureReleasesProviderOwnedDependencies()
    {
        var resource = new ResourceParticipant();
        var architecture = new TestArchitecture(services =>
        {
            services.AddSingleton(_ => resource);
            services.AddComponent<ThrowingParticipant>();
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(resource.Starts, Is.Zero);
        Assert.That(resource.Stops, Is.Zero);
        Assert.That(resource.Disposals, Is.EqualTo(1));
        await architecture.DestroyAsync();
        Assert.That(resource.Disposals, Is.EqualTo(1));
    }

    /// <summary>Independent architectures neither share roots nor invalidate one another.</summary>
    [Test]
    public async Task MultipleInstancesHaveIndependentProviders()
    {
        var first = new TestArchitecture(services => services.AddSingleton<Dependency>());
        await using var second = new TestArchitecture(services => services.AddSingleton<Dependency>());
        await first.InitializeAsync();
        await second.InitializeAsync();
        var secondValue = second.Services.GetRequiredService<Dependency>();
        Assert.That(first.Services.GetRequiredService<Dependency>(), Is.Not.SameAs(secondValue));
        await first.DestroyAsync();
        Assert.That(second.Services.GetRequiredService<Dependency>(), Is.SameAs(secondValue));
    }

    /// <summary>Synchronous entry never blocks on an asynchronous participant.</summary>
    [Test]
    public async Task SyncStartupRejectsAsyncParticipants()
    {
        var probe = new ResourceParticipant();
        var architecture = new TestArchitecture(services => services.AddComponent(probe));
        Assert.Throws<InvalidOperationException>(architecture.Initialize);
        Assert.That(probe.Starts, Is.Zero);
        await architecture.DestroyAsync();
    }

    /// <summary>Simple synchronous participants remain supported.</summary>
    [Test]
    public async Task SynchronousStartupAndSingleUseAreExplicit()
    {
        var probe = new SyncParticipant();
        var architecture = new TestArchitecture(services => services.AddComponent(probe));
        architecture.Initialize();
        Assert.That(probe.Starts, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(architecture.Initialize);
        await architecture.DestroyAsync();
        Assert.That(probe.Stops, Is.EqualTo(1));
    }

    /// <summary>Lifecycle callbacks cannot wait for their own architecture's shutdown task.</summary>
    [Test]
    public async Task SameChainDestructionIsRejectedWithoutDeadlock()
    {
        TestArchitecture? architecture = null;
        var rejected = false;
        var participant = new Participant(() =>
        {
            rejected = Assert.Throws<InvalidOperationException>(() => { _ = architecture!.DestroyAsync(); }) is not null;
        });
        architecture = new TestArchitecture(services => services.AddComponent(participant));
        await architecture.InitializeAsync();
        Assert.That(rejected, Is.True);
        await architecture.DestroyAsync();
    }

    /// <summary>A captured callback ExecutionContext must not retain a reentrancy prohibition after startup completes.</summary>
    [Test]
    public async Task CapturedStartupContextCanStopAfterItsOriginalOperationCompletes()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TestArchitecture? architecture = null;
        Task? detached = null;
        var participant = new Participant(() => detached = Task.Run(async () =>
        {
            await release.Task.ConfigureAwait(false);
            await architecture!.DestroyAsync().ConfigureAwait(false);
        }));
        architecture = new TestArchitecture(services => services.AddComponent(participant));
        await architecture.InitializeAsync();
        release.TrySetResult();
        await detached!.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Assert.That(participant.Stops, Is.EqualTo(1));
    }

    /// <summary>All observable state transitions include the terminal stop and preserve the architecture publisher.</summary>
    [Test]
    public async Task PhaseObservationsIncludeTheTerminalState()
    {
        var phases = new List<ArchitecturePhase>();
        var architecture = new TestArchitecture(_ => { });
        architecture.PhaseChanged += (sender, args) =>
        {
            Assert.That(sender, Is.SameAs(architecture));
            if (args.Phase == ArchitecturePhase.Stopped)
                Assert.Throws<InvalidOperationException>(() => { _ = architecture.DestroyAsync(); });
            phases.Add(args.Phase);
        };
        await architecture.InitializeAsync();
        await architecture.DestroyAsync();
        Assert.That(phases, Is.EqualTo(new[] { ArchitecturePhase.Configuring, ArchitecturePhase.Initializing,
            ArchitecturePhase.Ready, ArchitecturePhase.Stopping, ArchitecturePhase.Stopped }));
    }

    /// <summary>Host shutdown retains dependency access internally and orders detachment after business stop.</summary>
    [Test]
    public async Task HostHooksSurroundBusinessStopAndKeepProviderAlive()
    {
        var calls = new List<string>();
        var participant = new Participant(stop: () => calls.Add("component"));
        var architecture = new TestArchitecture(services => { services.AddSingleton<Dependency>(); services.AddComponent(participant); }, calls);
        await architecture.InitializeAsync();
        await architecture.DestroyAsync();
        Assert.That(calls, Is.EqualTo(new[] { "stopping", "component", "stopped" }));
    }

    /// <summary>Records fixture descriptions while letting production Architecture own all runtime behavior.</summary>
    private sealed class TestArchitecture(Action<IServiceCollection> configure, List<string>? hostCalls = null) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services) => configure(services);
        /// <inheritdoc />
        protected override ValueTask OnStoppingAsync()
        {
            if (hostCalls is not null)
            {
                _ = Services.GetRequiredService<Dependency>();
                hostCalls.Add("stopping");
            }
            return ValueTask.CompletedTask;
        }
        /// <inheritdoc />
        protected override ValueTask OnStoppedAsync()
        {
            if (hostCalls is not null)
            {
                _ = Services.GetRequiredService<Dependency>();
                hostCalls.Add("stopped");
            }
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>A plain constructor dependency without framework roles.</summary>
    private sealed class Dependency;
    /// <summary>The one canonical participant contract.</summary>
    private interface IConsumer
    { /// <summary>The injected singleton.</summary>
        Dependency Dependency
        {
            get;
        }
    }
    /// <summary>A constructor-injected participant with no optional framework behavior.</summary>
    private sealed class Consumer(Dependency dependency) : IConsumer
    { /// <inheritdoc />
        public Dependency Dependency { get; } = dependency;
    }
    /// <summary>A synchronous participant used for stable order observations.</summary>
    private class Participant(Action? start = null, Action? stop = null) : ILifecycle
    {
        /// <summary>Initialization attempts.</summary>
        public int Starts
        {
            get; private set;
        }
        /// <summary>Stop attempts.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Initialize()
        {
            Starts++;
            start?.Invoke();
        }
        /// <inheritdoc />
        public void Destroy()
        {
            Stops++;
            stop?.Invoke();
        }
    }
    /// <summary>A second canonical type for order and failure observations.</summary>
    private sealed class SecondParticipant(Action? start = null, Action? stop = null) : Participant(start, stop);
    /// <summary>A third canonical type for order observations.</summary>
    private sealed class ThirdParticipant(Action? start = null, Action? stop = null) : Participant(start, stop);
    /// <summary>An independently registered synchronous participant.</summary>
    private sealed class SyncParticipant : Participant;
    /// <summary>A provider-owned async-only disposable participant.</summary>
    private sealed class ResourceParticipant : IAsyncInitializable, IAsyncDestroyable, IAsyncDisposable
    {
        /// <summary>Starts observed.</summary>
        public int Starts
        {
            get; private set;
        }
        /// <summary>Stops observed.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <summary>Final disposals observed.</summary>
        public int Disposals
        {
            get; private set;
        }
        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Starts++;
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public ValueTask DestroyAsync()
        {
            Stops++;
            return ValueTask.CompletedTask;
        }
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }
    }
    /// <summary>Exposes a real pending cleanup operation to concurrent callers.</summary>
    private sealed class BlockingStopParticipant : IAsyncDestroyable
    {
        /// <summary>Signals entry to business stop.</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>The owner's explicit cleanup barrier.</summary>
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>The observed stop count.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <inheritdoc />
        public async ValueTask DestroyAsync()
        {
            Stops++;
            Entered.TrySetResult();
            await Release.Task.ConfigureAwait(false);
        }
    }

    /// <summary>A constructor failure after DI has created its disposable dependency.</summary>
    private sealed class ThrowingParticipant
    {
        /// <summary>Fails activation after the canonical dependency has been resolved.</summary>
        /// <param name="resource">The resource whose provider ownership must survive failed activation.</param>
        public ThrowingParticipant(ResourceParticipant resource)
        {
            _ = resource;
            throw new InvalidOperationException("Expected activation failure.");
        }
    }

    /// <summary>A startup barrier that stops only after cancellation is observed.</summary>
    private sealed class WaitingParticipant : IAsyncInitializable, IDestroyable
    {
        /// <summary>Signals that startup entered user code.</summary>
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>The observed cleanup count.</summary>
        public int Stops
        {
            get; private set;
        }
        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public void Destroy() => Stops++;
    }
}
