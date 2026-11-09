// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.Config;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Game.Config;

/// <summary>Describes a game configuration registry and one asynchronous initial-load participant.</summary>
/// <remarks>The module is instance-bound. Initial load precedes normal components through explicit order -100.</remarks>
/// <remarks>Creates descriptions without reading files.</remarks>
/// <param name="options">The game-owned loader and table configuration.</param>
public sealed class GameConfigModule(GameConfigBootstrapOptions options) : IArchitectureModule
{
    private readonly GameConfigBootstrap _bootstrap = new(options);
    private int _installed;

    /// <summary>The configured registry, populated before dependent participants start.</summary>
    public IConfigRegistry Registry => _bootstrap.Registry;
    /// <summary>Whether initial loading completed.</summary>
    public bool IsInitialized => _bootstrap.IsInitialized;
    /// <summary>Whether a development watcher is active.</summary>
    public bool IsHotReloadEnabled => _bootstrap.IsHotReloadEnabled;
    /// <summary>The initialized YAML loader.</summary>
    public YamlConfigLoader Loader => _bootstrap.Loader;

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (Interlocked.Exchange(ref _installed, 1) != 0)
            throw new InvalidOperationException("A GameConfigModule instance can only configure one architecture.");
        services.AddSingleton(Registry);
        services.AddSingleton(new BootstrapEntry(_bootstrap));
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(ConfigLifetime)))
            services.AddComponent<ConfigLifetime>(order: -100);
    }

    /// <summary>Starts explicit development hot reload after initial load.</summary>
    /// <param name="options">Watcher and dispatch policy.</param>
    public void StartHotReload(YamlConfigHotReloadOptions? options = null) => _bootstrap.StartHotReload(options);
    /// <summary>Stops the current watcher without disposing the registry owner.</summary>
    public void StopHotReload() => _bootstrap.StopHotReload();

    /// <summary>One module-owned bootstrap selected during the description phase.</summary>
    /// <param name="Bootstrap">The resource that the lifetime group owns.</param>
    private sealed record BootstrapEntry(GameConfigBootstrap Bootstrap);

    /// <summary>Starts multiple selected roots before ordinary components and owns their final release once.</summary>
    /// <param name="entries">All explicit configuration roots in module registration order.</param>
    private sealed class ConfigLifetime(IEnumerable<BootstrapEntry> entries) : IAsyncInitializable, IDestroyable, IDisposable
    {
        private readonly GameConfigBootstrap[] _bootstraps = [.. entries.Select(entry => entry.Bootstrap)];
        /// <inheritdoc />
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            foreach (var bootstrap in _bootstraps)
                await bootstrap.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public void Destroy()
        {
            foreach (var bootstrap in _bootstraps.Reverse())
                bootstrap.StopHotReload();
        }
        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var bootstrap in _bootstraps.Reverse())
                bootstrap.Dispose();
        }
    }
}
