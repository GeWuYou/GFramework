// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Storage;

namespace DodgeTheCreeps.Advanced;

/// <summary>Groups run-specific application services and the native board boundary.</summary>
/// <param name="Session">Pure policy state.</param>
/// <param name="UseCases">Pure application use cases.</param>
/// <param name="World">The host's currently attached world.</param>
internal sealed record RunServices(GameSession Session, RunUseCases UseCases, WorldState World);
