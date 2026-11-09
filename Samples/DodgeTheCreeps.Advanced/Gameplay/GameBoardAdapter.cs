// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using GFramework.Game.Scene;
using GFramework.Game.UI;

namespace DodgeTheCreeps.Advanced.Gameplay;

/// <summary>Adapts engine scene operations to the independent run application's board port.</summary>
/// <param name="state">Explicit current-world state.</param>
/// <param name="scenes">The host-owned scene lifecycle.</param>
/// <param name="ui">The host-owned UI lifecycle.</param>
internal sealed class GameBoardAdapter(WorldState state, SceneRouter scenes, UiService ui) : IRunBoard
{
    /// <inheritdoc />
    public bool IsRunning => state.World is { IsRunning: true };

    /// <inheritdoc />
    public async Task PrepareAsync(string arenaId, CancellationToken cancellationToken)
    {
        state.World?.StopRun();
        await ui.ClearAsync(cancellationToken).ConfigureAwait(true);
        state.ArenaId = arenaId;
        await scenes.ReplaceAsync("play", cancellationToken: cancellationToken).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public void StartNew() => state.World!.StartRun();

    /// <inheritdoc />
    public void Restore(RunSnapshot snapshot) => state.World!.RestoreProgress(DodgeProgress.FromSnapshot(snapshot));

    /// <inheritdoc />
    public RunSnapshot Capture() => state.World!.CaptureProgress();

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        state.World?.StopRun();
        await ui.ClearAsync(cancellationToken).ConfigureAwait(true);
        await scenes.ClearAsync(cancellationToken).ConfigureAwait(true);
        state.World = null;
    }

    /// <inheritdoc />
    public void Stop() => state.World?.StopRun();
}
