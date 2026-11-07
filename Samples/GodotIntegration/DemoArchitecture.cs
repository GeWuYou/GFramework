// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Runtime;
using GFramework.Game.UI;
using GFramework.Godot.Architectures;
using GFramework.Godot.UI;

namespace GodotIntegration;

/// <summary>
/// Installs ordinary game services into the optional Godot Architecture host.
/// GameRuntimeModule joins UI, scene and pause shutdown before native host detachment.
/// </summary>
internal sealed class DemoArchitecture(
    DemoRoot root, UiRegistry routes, GodotUiRoot uiRoot, IGameDispatcher dispatcher) : AbstractArchitecture
{
    /// <summary>Describes registrations without creating or attaching native nodes.</summary>
    protected override void InstallModules()
    {
        RegisterServiceModule(new GameRuntimeModule(dispatcher, services =>
        {
            services.AddGamePause();
            services.AddGameUi(routes, uiRoot, dispatcher);
            services.AddGameScenes(new DemoSceneFactory(root), root, dispatcher);
        }));
    }
}
