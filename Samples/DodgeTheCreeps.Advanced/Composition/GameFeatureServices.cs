// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Pause;
using GFramework.Game.Abstractions.Runtime;
using GFramework.Game.Scene;
using GFramework.Game.UI;
using GFramework.Godot.Storage;

using DodgeTheCreeps.Advanced.Content;
using DodgeTheCreeps.Advanced.Modding;
using DodgeTheCreeps.Advanced.Presentation;
using DodgeTheCreeps.Advanced.Progress;
using DodgeTheCreeps.Advanced.Settings;

namespace DodgeTheCreeps.Advanced.Composition;

/// <summary>Groups feature services by the application responsibilities they own.</summary>
/// <param name="Content">Content discovery and loading.</param>
/// <param name="Saves">Document and backup persistence.</param>
/// <param name="Settings">Native preference transactions.</param>
/// <param name="Mods">Game mod APIs and per-run state.</param>
/// <param name="Selection">Content selection and approval dialogs.</param>
/// <param name="Runs">Run policy and application use cases.</param>
/// <param name="Presentation">Typed display notifications.</param>
internal sealed record GameFeatureServices(Content.DodgeContent Content, SaveService Saves, SettingsService Settings,
    ModService Mods, ContentSelectionService Selection, RunServices Runs, GamePresentationState Presentation);
