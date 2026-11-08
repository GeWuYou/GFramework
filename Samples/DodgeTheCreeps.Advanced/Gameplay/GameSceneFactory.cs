// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using DodgeTheCreeps.Advanced.Content;
using DodgeTheCreeps.Advanced.Gameplay;
using GFramework.Game.Abstractions.Content;
using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.Scene;

namespace DodgeTheCreeps.Advanced;

/// <summary>Creates native board presentation from explicit content, scoring and feedback dependencies.</summary>
/// <param name="state">The host's world reference.</param>
/// <param name="content">Validated data and asset loading.</param>
/// <param name="mods">The scoring extension boundary.</param>
/// <param name="session">The pure run's unsaved-change state.</param>
/// <param name="presentation">Typed feedback notifications.</param>
internal sealed class GameSceneFactory(WorldState state, DodgeContent content, ModService mods,
    GameSession session, GamePresentationState presentation) : ISceneFactory
{
    /// <inheritdoc />
    public ISceneBehavior Create(string sceneKey)
    {
        if (!string.Equals(sceneKey, "play", StringComparison.Ordinal))
            throw new InvalidOperationException("Unknown scene: " + sceneKey);
        var world = new DodgeWorld { Name = "World" };
        world.ConfigureContent(content, content.Catalog.GetTable<DodgeArenaDefinition>("arenas").Get(new ContentId(state.ArenaId)));
        world.ConfigureScoring(mods.ModifyScore);
        world.ScoreChanged += (_, score) =>
        {
            session.MarkChanged();
            presentation.SetScore(score.Score);
        };
        world.RunEnded += (_, _) => presentation.EndRun();
        state.World = world;
        return SceneBehaviorFactory.Create(world, sceneKey);
    }
}
