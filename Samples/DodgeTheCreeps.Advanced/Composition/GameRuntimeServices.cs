// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Storage;

namespace DodgeTheCreeps.Advanced;

/// <summary>Groups the already composed host services without exposing a service locator.</summary>
/// <param name="Pause">Native pause ownership.</param>
/// <param name="Storage">The host's atomic storage boundary.</param>
/// <param name="Scenes">Scene lifecycle service.</param>
/// <param name="Ui">Typed UI lifecycle service.</param>
/// <param name="Dispatcher">Godot-thread dispatcher.</param>
internal sealed record GameRuntimeServices(PauseStackManager Pause, GodotFileStorage Storage,
    SceneRouter Scenes, UiService Ui, IGameDispatcher Dispatcher);
