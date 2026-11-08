// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Ioc;
using GFramework.Core.Abstractions.Pause;
using GFramework.Game.Abstractions.Scene;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Abstractions.UI;
using Microsoft.Extensions.DependencyInjection;

namespace GFramework.Game.Runtime;

/// <summary>Optional Architecture adapter that installs ordinary game services and stops them before native host detachment.</summary>
/// <remarks>Use AddGameUi/AddGameScenes in the supplied registration callback. The provider owns service disposal; this module only joins business shutdown.</remarks>
/// <remarks>Creates a per-Architecture module.</remarks>
/// <param name="dispatcher">Host dispatcher used for native pause cleanup.</param>
/// <param name="configure">Registers game services without resolving them.</param>
public sealed class GameRuntimeModule(IGameDispatcher dispatcher, Action<IServiceCollection> configure) : IServiceModule
{
    private readonly Action<IServiceCollection> _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    private readonly IGameDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private IIocContainer? _container;
    private IUiService? _ui;
    private ISceneRouter? _scenes;
    private IPauseStackManager? _pause;

    /// <inheritdoc />
    public string ModuleName => nameof(GameRuntimeModule);
    /// <inheritdoc />
    public int Priority => 30;
    /// <inheritdoc />
    public bool IsEnabled => true;
    /// <inheritdoc />
    public void Register(IIocContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        if (_container is not null)
            throw new InvalidOperationException("A GameRuntimeModule instance can only be installed once.");
        _container = container;
        container.ExecuteServicesHook(_configure);
    }
    /// <inheritdoc />
    public void Initialize()
    {
        var container = _container ?? throw new InvalidOperationException("Register the module before initialization.");
        _pause = container.Get<IPauseStackManager>();
        _ui = container.Get<IUiService>();
        _scenes = container.Get<ISceneRouter>();
    }
    /// <inheritdoc />
    public async ValueTask DestroyAsync()
    {
        var errors = new List<Exception>();
        // Each participant must stop even if another participant has failed cleanup.
        if (_ui is not null)
            await AttemptAsync(_ui.StopAsync, errors).ConfigureAwait(false);
        if (_scenes is not null)
            await AttemptAsync(_scenes.StopAsync, errors).ConfigureAwait(false);
        if (_pause is not null)
            await AttemptAsync(() => _dispatcher.InvokeAsync(() => _pause.DisposeAsync().AsTask()), errors).ConfigureAwait(false);
        if (errors.Count > 0)
            throw new AggregateException("Game service shutdown failed.", errors);
    }
    /// <summary>Preserves all cleanup failures while allowing the remaining participants to stop.</summary>
    private static async Task AttemptAsync(Func<Task> stop, List<Exception> errors)
    {
        try
        {
            await stop().ConfigureAwait(false);
        }
        catch (Exception exception) { errors.Add(exception); }
    }
}
