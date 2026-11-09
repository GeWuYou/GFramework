// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using System.Threading;
using Arch.Core;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using Microsoft.Extensions.DependencyInjection;
using ArchSystem = Arch.System.ISystem<float>;

namespace GFramework.Ecs.Arch;

/// <summary>Configuration-only adapter joining the native runtime to one Architecture provider.</summary>
/// <param name="options">World creation, statistics and participant startup order; captured during configuration.</param>
/// <param name="enabled">Whether to register ECS for this composition.</param>
/// <param name="hostContext">Optional owning context for asynchronous host startup and stop.</param>
public sealed class ArchEcsModule(ArchOptions? options = null, bool enabled = true,
    SynchronizationContext? hostContext = null) : IArchitectureModule
{
    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!enabled)
            return;
        if (services.Any(descriptor => descriptor.ServiceType == typeof(World) ||
            descriptor.ServiceType == typeof(IArchRuntime)))
            throw new InvalidOperationException("This composition already contains an Arch World or runtime.");
        var captured = CaptureOptions(options);
        services.AddSingleton(_ => World.Create(captured.WorldCapacity));
        services.AddSingleton<IArchRuntime>(provider => new ArchRuntime(
            provider.GetRequiredService<World>(), provider.GetServices<ArchSystem>(), captured));
        if (hostContext is null)
            services.AddComponent(provider => new RuntimeLifetime(provider.GetRequiredService<IArchRuntime>()), captured.Priority);
        else
            services.AddComponent(provider => new ContextRuntimeLifetime(
                () => provider.GetRequiredService<IArchRuntime>(), hostContext), captured.Priority);
    }

    /// <summary>Copies and validates caller options before adding any service descriptions.</summary>
    /// <param name="options">Caller-owned options, or defaults.</param>
    /// <returns>The validated composition-local snapshot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">World capacity is not positive.</exception>
    private static ArchOptions CaptureOptions(ArchOptions? options)
    {
        var captured = new ArchOptions
        {
            WorldCapacity = options?.WorldCapacity ?? 1000,
            EnableStatistics = options?.EnableStatistics ?? false,
            Priority = options?.Priority ?? 50,
        };
        if (captured.WorldCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "World capacity must be positive.");
        return captured;
    }

    /// <summary>Owns business start/stop only; the provider owns systems and World final disposal.</summary>
    /// <param name="runtime">The same runtime injected into the host frame loop.</param>
    private sealed class RuntimeLifetime(IArchRuntime runtime) : ILifecycle
    {
        /// <inheritdoc />
        public void Initialize() => runtime.Initialize();
        /// <inheritdoc />
        public void Destroy() => runtime.Stop();
    }
    /// <summary>Activates and starts native dependencies on an explicit context even after earlier async participants.</summary>
    /// <param name="create">Deferred canonical runtime resolution on the owning context.</param>
    /// <param name="context">The host keeps this dispatcher alive through business stop.</param>
    private sealed class ContextRuntimeLifetime(Func<IArchRuntime> create, SynchronizationContext context)
        : IAsyncInitializable, IAsyncDestroyable
    {
        private IArchRuntime? _runtime;

        /// <inheritdoc />
        public Task InitializeAsync(CancellationToken cancellationToken = default) =>
            InvokeAsync(() =>
            {
                _runtime = create();
                _runtime.Initialize();
            }, cancellationToken);

        /// <inheritdoc />
        public ValueTask DestroyAsync() => new(InvokeAsync(() => _runtime?.Stop(), CancellationToken.None));

        /// <summary>Joins the actual posted callback; cancellation cannot leave native work running after startup ends.</summary>
        private Task InvokeAsync(Action action, CancellationToken cancellationToken)
        {
            if (ReferenceEquals(SynchronizationContext.Current, context))
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
                return Task.CompletedTask;
            }
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            context.Post(_ =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    action();
                    completion.SetResult();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    completion.SetCanceled(cancellationToken);
                }
                catch (Exception error)
                {
                    completion.SetException(error);
                }
            }, state: null);
            return completion.Task;
        }
    }

}
