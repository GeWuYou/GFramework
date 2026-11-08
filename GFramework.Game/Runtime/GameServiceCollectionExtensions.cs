// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.UI;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GFramework.Game.Runtime;

/// <summary>Composes game services through standard DI without requiring framework component roles.</summary>
public static class GameServiceCollectionExtensions
{
    /// <summary>Registers one provider-owned pause manager, respecting an explicitly registered implementation.</summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddGamePause(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IPauseStackManager>(provider =>
            new PauseStackManager(provider.GetService<ILogger<PauseStackManager>>()));
        return services;
    }

    /// <summary>Registers one UI service; its native host and dispatcher remain owned by the caller.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="routes">Typed route definitions and factories.</param>
    /// <param name="host">Native mounting and focus host, kept alive until service disposal.</param>
    /// <param name="dispatcher">Host operation dispatcher.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddGameUi(this IServiceCollection services, UiRegistry routes, IUiHost host, IGameDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IUiService)))
            throw new InvalidOperationException("A UI service is already registered in this collection.");
        services.AddSingleton<IUiService>(provider => new UiService(routes, host, dispatcher,
            provider.GetService<IPauseStackManager>(), provider.GetService<ILogger<UiService>>(), provider.GetServices<IUiGuard>()));
        return services;
    }

    /// <summary>Registers one scene navigator whose host and factory remain caller-owned.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="factory">Native scene factory.</param>
    /// <param name="root">Native scene root.</param>
    /// <param name="dispatcher">Host operation dispatcher.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddGameScenes(this IServiceCollection services, ISceneFactory factory, ISceneRoot root, IGameDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ISceneRouter)))
            throw new InvalidOperationException("A scene navigator is already registered in this collection.");
        services.AddSingleton<ISceneRouter>(provider =>
            new SceneRouter(factory, root, dispatcher, provider.GetService<ILogger<SceneRouter>>()));
        return services;
    }
}
