// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using GFramework.Core.Systems;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>Verifies that asynchronous business cleanup retains its host and provider until the proper stop boundary.</summary>
[TestFixture]
public sealed class ArchitectureHostStopOrderTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Host detachment waits for the complete component stop and occurs before provider disposal.</summary>
    /// <returns>The ordered destruction and its repeated caller have completed.</returns>
    [Test]
    public async Task DestroyAsync_KeepsHostAliveUntilComponentsAndModulesStop()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var architecture = new HostArchitecture();
        architecture.ComponentStop = async () =>
        {
            Assert.That(architecture.HostAlive, Is.True);
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            Assert.That(architecture.HostAlive, Is.True);
        };
        await architecture.InitializeAsync().ConfigureAwait(false);

        var stopping = architecture.DestroyAsync().AsTask();
        await entered.Task.WaitAsync(OperationTimeout).ConfigureAwait(false);
        var repeated = architecture.DestroyAsync().AsTask();
        Assert.Multiple(() =>
        {
            Assert.That(architecture.Events, Is.EqualTo(new[] { "host-stop-admission" }));
            Assert.That(architecture.ResourceDisposed, Is.False);
            Assert.That(stopping.IsCompleted, Is.False);
            Assert.That(repeated.IsCompleted, Is.False);
        });

        release.TrySetResult();
        await Task.WhenAll(stopping, repeated).WaitAsync(OperationTimeout).ConfigureAwait(false);

        Assert.That(architecture.Events, Is.EqualTo(ExpectedStopOrder));
        Assert.That(architecture.HostAlive, Is.False);
    }

    /// <summary>Errors in any stop boundary cannot skip host finalization or provider disposal.</summary>
    /// <param name="failingStep">The stop boundary that fails.</param>
    /// <returns>Every later cleanup boundary has still been attempted.</returns>
    [TestCase("host-stop-admission")]
    [TestCase("component-stop")]
    [TestCase("module-stop")]
    [TestCase("host-detach")]
    public async Task DestroyAsync_WhenStopBoundaryFails_AttemptsRemainingCleanup(string failingStep)
    {
        var architecture = new HostArchitecture { FailingStep = failingStep };
        await architecture.InitializeAsync().ConfigureAwait(false);

        await Assert.CatchAsync<AggregateException>(async () =>
            await architecture.DestroyAsync().AsTask().WaitAsync(OperationTimeout).ConfigureAwait(false)).ConfigureAwait(false);

        Assert.That(architecture.Events, Is.EqualTo(ExpectedStopOrder));
        Assert.That(architecture.ResourceDisposed, Is.True);
        Assert.That(architecture.HostAlive, Is.False);
    }

    /// <summary>Failed startup finalizes the host once and repeated destruction reuses its success or failure.</summary>
    /// <param name="failFinalization">Whether host finalization itself fails.</param>
    /// <returns>Startup cleanup and both destruction calls have been observed.</returns>
    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedStartup_FinalizesHostOnceBeforeProviderDisposal(bool failFinalization)
    {
        var architecture = new HostArchitecture
        {
            FailStartup = true,
            FailingStep = failFinalization ? "host-detach" : null
        };

        await Assert.CatchAsync<Exception>(async () =>
            await architecture.InitializeAsync().WaitAsync(OperationTimeout).ConfigureAwait(false)).ConfigureAwait(false);
        Assert.That(architecture.Events, Is.EqualTo(ExpectedStopOrder));

        if (failFinalization)
        {
            await Assert.CatchAsync<AggregateException>(async () =>
                await architecture.DestroyAsync().ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.CatchAsync<AggregateException>(async () =>
                await architecture.DestroyAsync().ConfigureAwait(false)).ConfigureAwait(false);
        }
        else
        {
            await architecture.DestroyAsync().ConfigureAwait(false);
            await architecture.DestroyAsync().ConfigureAwait(false);
        }

        Assert.That(architecture.Events, Is.EqualTo(ExpectedStopOrder), "Host cleanup must not run twice.");
        Assert.That(architecture.ResourceDisposed, Is.True);
    }

    /// <summary>The observable order of host admission, business cleanup, host detachment and resource disposal.</summary>
    private static string[] ExpectedStopOrder =>
        ["host-stop-admission", "component-stop", "module-stop", "host-detach", "provider-dispose"];

    /// <summary>A managed host exposes whether roots and provider resources remain usable during cleanup.</summary>
    private sealed class HostArchitecture : Architecture
    {
        private readonly OwnedResource _resource;

        /// <summary>Creates an isolated host with a provider-owned resource.</summary>
        public HostArchitecture()
        {
            _resource = new OwnedResource(() => Record("provider-dispose"));
        }

        /// <summary>Ordered cleanup observations.</summary>
        public List<string> Events { get; } = [];

        /// <summary>Whether the simulated native host root remains usable.</summary>
        public bool HostAlive { get; private set; } = true;

        /// <summary>Whether the provider has disposed its resource.</summary>
        public bool ResourceDisposed => _resource.Disposed;

        /// <summary>Optional asynchronous work performed by the registered component.</summary>
        public Func<ValueTask>? ComponentStop { get; set; }

        /// <summary>The cleanup stage that deliberately reports an error.</summary>
        public string? FailingStep { get; init; }

        /// <summary>Whether startup fails after components and host resources were started.</summary>
        public bool FailStartup { get; init; }

        /// <summary>Registers a normal component, a service module and a provider-owned dependency.</summary>
        /// <param name="services">The architecture's service collection.</param>
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => _resource);
            RegisterSystem(new AsyncStopSystem(async () =>
            {
                if (ComponentStop is not null)
                    await ComponentStop().ConfigureAwait(false);
                Assert.That(HostAlive, Is.True);
                Record("component-stop");
            }));
            RegisterServiceModule(new StopModule(() =>
            {
                Assert.That(HostAlive, Is.True);
                Record("module-stop");
            }));
        }

        /// <summary>Activates the dependency and optionally simulates a partially started host failure.</summary>
        protected override void OnStarted()
        {
            _ = Context.GetService<OwnedResource>();
            if (FailStartup)
                throw new InvalidOperationException("Host startup failed.");
        }

        /// <summary>Closes admission without prematurely destroying the root.</summary>
        /// <returns>The admission stop is complete.</returns>
        protected override ValueTask OnStoppingAsync()
        {
            Record("host-stop-admission");
            return ValueTask.CompletedTask;
        }

        /// <summary>Detaches only after business cleanup, while dependencies can still be resolved.</summary>
        /// <returns>The host has detached or reported a deliberate finalization error.</returns>
        protected override ValueTask OnStoppedAsync()
        {
            Assert.That(Context.GetService<OwnedResource>().Disposed, Is.False);
            HostAlive = false;
            Record("host-detach");
            return ValueTask.CompletedTask;
        }

        /// <summary>Records attempted stages and injects deterministic failure without skipping later stages.</summary>
        /// <param name="step">The current cleanup stage.</param>
        private void Record(string step)
        {
            Events.Add(step);
            if (string.Equals(step, FailingStep, StringComparison.Ordinal))
                throw new InvalidOperationException(step + " failed.");
        }
    }

    /// <summary>A system with asynchronous business cleanup.</summary>
    /// <param name="stop">The complete component cleanup callback.</param>
    private sealed class AsyncStopSystem(Func<ValueTask> stop) : AbstractSystem, IAsyncDestroyable
    {
        /// <summary>No initialization work is needed for this test participant.</summary>
        protected override void OnInit() { }

        /// <summary>Waits for the test-controlled asynchronous cleanup.</summary>
        /// <returns>The component has fully stopped.</returns>
        public ValueTask DestroyAsync() => stop();
    }

    /// <summary>A service module that must also stop before host detachment.</summary>
    /// <param name="stop">The module cleanup callback.</param>
    private sealed class StopModule(Action stop) : IServiceModule
    {
        /// <inheritdoc />
        public string ModuleName => nameof(StopModule);

        /// <inheritdoc />
        public int Priority => 0;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public void Register(IIocContainer container) { }

        /// <inheritdoc />
        public void Initialize() { }

        /// <inheritdoc />
        public ValueTask DestroyAsync()
        {
            stop();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>A provider-owned resource records final disposal separately from business stopping.</summary>
    /// <param name="dispose">The disposal observation callback.</param>
    private sealed class OwnedResource(Action dispose) : IDisposable
    {
        /// <summary>Whether the provider has released this dependency.</summary>
        public bool Disposed { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Disposed = true;
            dispose();
        }
    }
}
