// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using System.Threading;
using System;
using DodgeTheCreeps.Advanced.Content;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Architectures;
using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Pause;
using GFramework.Godot.Storage;
using GFramework.Godot.UI;
using JsonSerializer = GFramework.Game.Serializer.JsonSerializer;
using Microsoft.Extensions.DependencyInjection;

namespace DodgeTheCreeps.Advanced;

/// <summary>
/// Composition root for the complete example. All container access is confined to
/// registration factories and host lifetime hooks; features receive their actual dependencies.
/// </summary>
/// <param name="host">Borrowed Godot root; Godot owns its native lifetime.</param>
/// <param name="launch">Validated immutable launch inputs.</param>
/// <param name="dispatcher">Native thread ownership.</param>
internal sealed class AdvancedGameArchitecture(AdvancedGameRoot host, GameLaunchOptions launch,
    IGameDispatcher dispatcher) : Architecture, IAsyncArchitectureHost
{
    private GameApplication? _application;

    // #region advanced-composition
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(host);
        services.AddSingleton(launch);
        services.AddSingleton(dispatcher);
        services.AddSingleton<GameLifetime>();
        services.AddSingleton<WorldState>();
        services.AddSingleton<GameSession>();
        services.AddSingleton(_ => new DodgeLocalization(dispatcher));
        services.AddSingleton(provider => new GamePresentationState(provider.GetRequiredService<DodgeLocalization>()));
        services.AddSingleton(static _ => new GodotFileStorage(new JsonSerializer()));
        services.AddSingleton(static _ => new PauseStackManager());
        services.AddSingleton(provider => new ModService(dispatcher, provider.GetRequiredService<WorldState>()));
        services.AddSingleton(provider =>
        {
            var mods = provider.GetRequiredService<ModService>();
            var content = new DodgeContent(provider.GetRequiredService<GodotFileStorage>(), launch.DataRoot, mods.CreateOptions());
            mods.SetContent(content);
            return content;
        });
        services.AddSingleton(static _ => new GodotUiRoot { Name = "Ui" });
        services.AddSingleton(provider =>
        {
            // UiService snapshots registrations during construction. Register every typed route now,
            // but defer the menu instance so its application dependency cannot form a constructor cycle.
            var registry = new UiRegistry();
            DodgeMenus.Register(registry, () => provider.GetRequiredService<DodgeMenus>());
            return registry;
        });
        services.AddSingleton(provider => new UiService(provider.GetRequiredService<UiRegistry>(),
            provider.GetRequiredService<GodotUiRoot>(), dispatcher, provider.GetRequiredService<PauseStackManager>()));
        services.AddSingleton(provider => new GameSceneFactory(provider.GetRequiredService<WorldState>(),
            provider.GetRequiredService<DodgeContent>(), provider.GetRequiredService<ModService>(),
            provider.GetRequiredService<GameSession>(), provider.GetRequiredService<GamePresentationState>()));
        services.AddSingleton(provider => new SceneRouter(provider.GetRequiredService<GameSceneFactory>(), host, dispatcher));
        services.AddSingleton(provider => new SettingsService(host, provider.GetRequiredService<GodotFileStorage>(), launch,
            provider.GetRequiredService<DodgeLocalization>(), provider.GetRequiredService<GamePresentationState>(), provider.GetRequiredService<GameLifetime>()));
        services.AddSingleton(provider => new SaveService(provider.GetRequiredService<GodotFileStorage>(),
            provider.GetRequiredService<DodgeContent>(), provider.GetRequiredService<UiService>(),
            provider.GetRequiredService<GamePresentationState>(), provider.GetRequiredService<ModService>(), launch, provider.GetRequiredService<GameLifetime>()));
        services.AddSingleton(provider => new ContentSelectionService(provider.GetRequiredService<DodgeContent>(),
            provider.GetRequiredService<UiService>(), provider.GetRequiredService<DodgeLocalization>(), provider.GetRequiredService<GameLifetime>()));
        services.AddSingleton(provider => new GameBoardAdapter(provider.GetRequiredService<WorldState>(),
            provider.GetRequiredService<SceneRouter>(), provider.GetRequiredService<UiService>()));
        services.AddSingleton(provider => new RunUseCases(provider.GetRequiredService<GameSession>(),
            provider.GetRequiredService<GameBoardAdapter>(), provider.GetRequiredService<SaveService>()));
        services.AddSingleton(provider => new RunServices(provider.GetRequiredService<GameSession>(),
            provider.GetRequiredService<RunUseCases>(), provider.GetRequiredService<WorldState>()));
        services.AddSingleton(provider => new GameRuntimeServices(provider.GetRequiredService<PauseStackManager>(),
            provider.GetRequiredService<GodotFileStorage>(), provider.GetRequiredService<SceneRouter>(),
            provider.GetRequiredService<UiService>(), dispatcher));
        services.AddSingleton(provider => new GameFeatureServices(provider.GetRequiredService<DodgeContent>(),
            provider.GetRequiredService<SaveService>(), provider.GetRequiredService<SettingsService>(),
            provider.GetRequiredService<ModService>(), provider.GetRequiredService<ContentSelectionService>(),
            provider.GetRequiredService<RunServices>(), provider.GetRequiredService<GamePresentationState>()));
        services.AddSingleton(provider => new GameApplication(host, launch, provider.GetRequiredService<GameLifetime>(),
            provider.GetRequiredService<GameRuntimeServices>(), provider.GetRequiredService<GameFeatureServices>()));
        services.AddSingleton(provider => new DodgeMenus(provider.GetRequiredService<GameApplication>(),
            provider.GetRequiredService<GamePresentationState>(), dispatcher));
        services.AddComponent(provider => new ContentStartup(provider.GetRequiredService<DodgeContent>(), dispatcher), order: -100);
    }
    // #endregion advanced-composition

    /// <inheritdoc />
    ValueTask IAsyncArchitectureHost.StartHostAsync(CancellationToken cancellationToken) =>
        new(dispatcher.InvokeAsync(async () =>
        {
            var text = Services.GetRequiredService<DodgeLocalization>();
            text.Initialize();
            _application = Services.GetRequiredService<GameApplication>();
            host.AttachApplication(_application);
            var uiRoot = Services.GetRequiredService<GodotUiRoot>();
            host.AddChild(uiRoot);
            _ = Services.GetRequiredService<DodgeMenus>();
            var pause = Services.GetRequiredService<PauseStackManager>();
            pause.RegisterHandler(new GodotPauseHandler(host.GetTree()));
            GameApplication.ConfigureInput();
            host.AddChild(new GodotUiInputBridge
            {
                Service = Services.GetRequiredService<UiService>(),
                RunAction = _application.RunAction,
                UnhandledCancelAsync = () => _application.World is { IsRunning: true } world
                    ? _application.Ui.ShowAsync(DodgeRoutes.Pause, world.Score) : Task.CompletedTask,
            });
            await _application.InitializeAsync().ConfigureAwait(true);
        }, cancellationToken));

    /// <inheritdoc />
    protected override ValueTask OnStoppingAsync() => new(dispatcher.InvokeAsync(() => _application?.StopAsync() ?? Task.CompletedTask));

    /// <inheritdoc />
    protected override ValueTask OnStoppedAsync() => new(dispatcher.InvokeAsync(host.DrainNativeViewsAsync));
}
