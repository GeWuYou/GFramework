// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>Protects component identity, host capabilities and the deliberately bounded service access window.</summary>
[TestFixture]
public sealed class ArchitectureBoundaryRegressionTests
{
    private static readonly ArchitecturePhase[] ExpectedPhases =
    [
        ArchitecturePhase.Configuring,
        ArchitecturePhase.Initializing,
        ArchitecturePhase.Ready,
        ArchitecturePhase.Stopping,
        ArchitecturePhase.Stopped,
    ];

    /// <summary>A replacement singleton must be rejected before either original or replacement can start.</summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task ReplacingAnyComponentDescriptorFailsBeforeActivation(int registrationKind)
    {
        var activations = 0;
        var original = new Worker();
        var architecture = new TestArchitecture(services =>
        {
            if (registrationKind == 0)
                services.AddComponent(original);
            else if (registrationKind == 1)
                services.AddComponent<Worker>();
            else
                services.AddComponent(_ => { activations++; return original; });
            services.Replace(ServiceDescriptor.Singleton<Worker>(_ => { activations++; return new Worker(); }));
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => architecture.InitializeAsync());
        Assert.That(activations, Is.Zero);
        Assert.That(original.Starts, Is.Zero);
        await architecture.DestroyAsync();
    }

    /// <summary>All phases remain visible to later subscribers despite failures in the first subscriber.</summary>
    [Test]
    public async Task PhaseObserversCannotVetoStartupOrShutdown()
    {
        var phases = new List<ArchitecturePhase>();
        var architecture = new TestArchitecture(_ => { });
        architecture.PhaseChanged += (_, _) => throw new InvalidOperationException("observer");
        architecture.PhaseChanged += (sender, args) =>
        {
            Assert.That(sender, Is.SameAs(architecture));
            phases.Add(args.Phase);
        };
        await architecture.InitializeAsync();
        Assert.That(architecture.IsReady, Is.True);
        await architecture.DestroyAsync();
        Assert.That(phases, Is.EqualTo(ExpectedPhases));
    }

    /// <summary>Root identity and keyed resolution use the same admission guard; child scopes keep standard DI ownership.</summary>
    [Test]
    public async Task KeyedAndCachedRootAccessStopWhileExistingScopesRemainCallerOwned()
    {
        var stopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var architecture = new TestArchitecture(services =>
        {
            services.AddSingleton<Worker>();
            services.AddKeyedSingleton<Worker>("worker");
        }, async () => { stopping.SetResult(); await release.Task.ConfigureAwait(false); });
        await architecture.InitializeAsync();
        var root = architecture.Services.GetRequiredService<IServiceProvider>();
        Assert.That(root, Is.SameAs(architecture.Services));
        Assert.That(root.GetRequiredKeyedService<IServiceProvider>(serviceKey: null), Is.SameAs(root));
        var keyed = root.GetRequiredKeyedService<Worker>("worker");
        Assert.That(root.GetKeyedService<Worker>("missing"), Is.Null);
        Assert.Throws<InvalidOperationException>(() => root.GetRequiredKeyedService<Worker>("missing"));
        var factory = root.GetRequiredService<IServiceScopeFactory>();
        using var scope = factory.CreateScope();
        var shutdown = architecture.DestroyAsync().AsTask();
        await stopping.Task;
        Assert.Throws<ObjectDisposedException>(() => root.GetRequiredService<Worker>());
        Assert.Throws<ObjectDisposedException>(() => root.GetRequiredKeyedService<Worker>("worker"));
        Assert.That(scope.ServiceProvider.GetRequiredKeyedService<Worker>("worker"), Is.SameAs(keyed));
        using var newScope = factory.CreateScope();
        Assert.That(newScope.ServiceProvider.GetRequiredService<Worker>(), Is.Not.Null);
        release.SetResult();
        await shutdown;
    }

    /// <summary>Constructing an optional diagnostic logger cannot veto otherwise valid lifecycle work.</summary>
    [Test]
    public async Task FailingObserverLoggerFactoryCannotFailStartup()
    {
        var architecture = new TestArchitecture(services =>
            services.AddSingleton<ILoggerFactory>(new FailingLoggerFactory()));
        architecture.PhaseChanged += (_, _) => throw new InvalidOperationException("observer");
        await architecture.InitializeAsync();
        Assert.That(architecture.IsReady, Is.True);
        await architecture.DestroyAsync();
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.Stopped));
    }

    /// <summary>A declared asynchronous host is rejected before configuration can construct application services.</summary>
    [Test]
    public async Task SynchronousEntryRejectsAsyncHostBeforeConfiguration()
    {
        var host = new AsyncHost();
        Assert.Throws<InvalidOperationException>(host.Initialize);
        Assert.That(host.Configurations, Is.Zero);
        Assert.That(host.HostStarts, Is.Zero);
        await host.DestroyAsync();
    }

    /// <summary>Required host setup still participates in normal failure and rollback semantics.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task ExplicitAsyncHostControlsRequiredAttachment(bool fail)
    {
        var host = new AsyncHost(fail);
        if (fail)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.InitializeAsync());
            Assert.That(host.CurrentPhase, Is.EqualTo(ArchitecturePhase.Faulted));
        }
        else
        {
            await host.InitializeAsync();
            Assert.That(host.IsReady, Is.True);
        }
        Assert.That(host.HostStarts, Is.EqualTo(1));
        await host.DestroyAsync();
    }

    /// <summary>Uses the production lifecycle with thread-neutral test host callbacks.</summary>
    private sealed class TestArchitecture(Action<IServiceCollection> configure, Func<Task>? stop = null) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.ClearProviders());
            configure(services);
        }
        /// <inheritdoc />
        protected override ValueTask OnStoppingAsync() => stop is null ? ValueTask.CompletedTask : new ValueTask(stop());
    }

    /// <summary>A host advertises its asynchronous capability through a typed interface.</summary>
    private sealed class AsyncHost(bool fail = false) : Architecture, IAsyncArchitectureHost
    {
        /// <summary>Configuration calls.</summary>
        public int Configurations
        {
            get; private set;
        }
        /// <summary>Required host attachment calls.</summary>
        public int HostStarts
        {
            get; private set;
        }
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            Configurations++;
            services.AddLogging(builder => builder.ClearProviders());
        }
        /// <inheritdoc />
        ValueTask IAsyncArchitectureHost.StartHostAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HostStarts++;
            if (fail)
                throw new InvalidOperationException("host attachment");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>A borrowed logging dependency that cannot provide a diagnostics logger.</summary>
    private sealed class FailingLoggerFactory : ILoggerFactory
    {
        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => throw new InvalidOperationException("diagnostics setup");
        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider)
        {
        }
        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>A component whose startup is observable.</summary>
    private sealed class Worker : IInitializable
    {
        /// <summary>The number of initialization calls.</summary>
        public int Starts
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Initialize() => Starts++;
    }
}
