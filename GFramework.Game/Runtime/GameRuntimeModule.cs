// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Architectures;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.UI;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Game.Runtime;

/// <summary>Configuration-only adapter for ordinary game services and their ordered business shutdown.</summary>
/// <param name="dispatcher">The explicit native-host callback dispatcher.</param>
/// <param name="configure">Descriptions of the selected game capabilities.</param>
public sealed class GameRuntimeModule(IGameDispatcher dispatcher, Action<IServiceCollection> configure) : IArchitectureModule
{
    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(configure);
        configure(services);
        services.AddComponent(provider => new GameRuntimeLifetime(dispatcher,
            provider.GetService<IUiService>(), provider.GetService<ISceneRouter>(), provider.GetService<IPauseStackManager>()), order: 30);
    }

    /// <summary>Joins UI and scene shutdown before the host is detached; resources remain owned by DI.</summary>
    private sealed class GameRuntimeLifetime(IGameDispatcher dispatcher, IUiService? ui, ISceneRouter? scenes,
        IPauseStackManager? pause) : IAsyncDestroyable
    {
        /// <inheritdoc />
        public async ValueTask DestroyAsync()
        {
            var errors = new List<Exception>();
            if (ui is not null)
                await AttemptAsync(ui.StopAsync, errors).ConfigureAwait(false);
            if (scenes is not null)
                await AttemptAsync(scenes.StopAsync, errors).ConfigureAwait(false);
            if (pause is not null)
                await AttemptAsync(() => dispatcher.InvokeAsync(() => pause.DisposeAsync().AsTask()), errors).ConfigureAwait(false);
            if (errors.Count > 0)
                throw new AggregateException("Game service shutdown failed.", errors);
        }

        /// <summary>Attempts every business stop and retains all errors for the owner.</summary>
        private static async Task AttemptAsync(Func<Task> stop, List<Exception> errors)
        {
            try
            {
                await stop().ConfigureAwait(false);
            }
            catch (Exception error) { errors.Add(error); }
        }
    }
}
