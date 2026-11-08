// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Architectures;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GFramework.Core.Tests.Architectures;

/// <summary>Regression coverage for readiness cancellation and the documented CQRS module configuration boundary.</summary>
[TestFixture]
public sealed class ArchitectureReviewRegressionTests
{
    /// <summary>Cancellation in the final Ready callback fails startup and its waiters only after provider cleanup.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task ReadyCallbackCancellation_DoesNotReportSuccessfulStartup(bool asyncHook)
    {
        using var source = new CancellationTokenSource();
        var resource = new OwnedResource();
        var architecture = new CancellationArchitecture(resource, source.Cancel, asyncHook);
        var ready = architecture.WaitUntilReadyAsync();
        await Assert.CatchAsync<OperationCanceledException>(async () =>
            await architecture.InitializeAsync(source.Token).ConfigureAwait(false)).ConfigureAwait(false);
        await Assert.CatchAsync<OperationCanceledException>(async () =>
            await ready.ConfigureAwait(false)).ConfigureAwait(false);
        // 自动失败清理保留失败终态，资源释放完成与“正常 Destroyed”相位是不同契约。
        Assert.That(architecture.CurrentPhase, Is.EqualTo(ArchitecturePhase.FailedInitialization));
        Assert.That(resource.DisposeCalls, Is.EqualTo(1));
        await architecture.DestroyAsync().ConfigureAwait(false);
        Assert.That(resource.DisposeCalls, Is.EqualTo(1));
    }

    /// <summary>The actual module entry point works both in ConfigureServices and under the Configurator container write lock.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task CqrsModule_RegistersWithoutResolvingDuringConfiguration(bool configurator)
    {
        var architecture = new ModuleArchitecture(!configurator);
        await architecture.InitializeAsync().ConfigureAwait(false);
        var first = architecture.Context.GetService<ICqrsRuntime>();
        Assert.That(first, Is.Not.Null);
        Assert.That(architecture.Context.GetService<ICqrsRuntime>(), Is.SameAs(first));
        await architecture.DestroyAsync().ConfigureAwait(false);
    }

    private sealed class ModuleArchitecture(bool configureModule) : Architecture
    {
        /// <inheritdoc />
        public override Action<IServiceCollection>? Configurator => configureModule ? null : _ => this.UseCqrsRuntime();

        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            if (configureModule)
            {
                this.UseCqrsRuntime();
            }
        }
    }

    private sealed class CancellationArchitecture(OwnedResource resource, Action cancel, bool asyncHook) : Architecture
    {
        /// <inheritdoc />
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_ => resource);
            if (asyncHook)
            {
                RegisterLifecycleHook(new ReadyHook(cancel));
            }
            else
            {
                PhaseChanged += (_, args) =>
                {
                    if (args.Phase == ArchitecturePhase.Ready)
                    {
                        cancel();
                    }
                };
            }
        }

        /// <inheritdoc />
        protected override void OnStarted() => _ = Context.GetService<OwnedResource>();
    }

    private sealed class ReadyHook(Action cancel) : IAsyncArchitectureLifecycleHook
    {
        /// <inheritdoc />
        public void OnPhase(ArchitecturePhase phase, IArchitecture architecture)
        {
        }

        /// <inheritdoc />
        public async ValueTask OnPhaseAsync(ArchitecturePhase phase, IArchitecture architecture,
            CancellationToken cancellationToken = default)
        {
            if (phase == ArchitecturePhase.Ready)
            {
                await Task.Yield();
                cancel();
            }
        }
    }

    private sealed class OwnedResource : IDisposable
    {
        /// <summary>Number of actual provider-owned releases.</summary>
        public int DisposeCalls
        {
            get; private set;
        }
        /// <inheritdoc />
        public void Dispose() => DisposeCalls++;
    }
}
