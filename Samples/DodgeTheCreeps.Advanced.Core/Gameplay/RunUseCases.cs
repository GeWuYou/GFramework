// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Core.Progress;

namespace DodgeTheCreeps.Advanced.Core.Gameplay;

/// <summary>
/// Coordinates run effects through explicit ports. One operation gate serializes board and
/// save mutations; UI dialog replies stay outside this boundary and can complete independently.
/// </summary>
/// <param name="session">The run's policy state.</param>
/// <param name="board">The engine boundary.</param>
/// <param name="store">The persistent document boundary.</param>
public sealed class RunUseCases(GameSession session, IRunBoard board, IRunStore store) : IDisposable
{
    private readonly SemaphoreSlim _operations = new(1, 1);

    /// <summary>Releases operation serialization after the host has stopped and awaited accepted work.</summary>
    public void Dispose() => _operations.Dispose();

    /// <summary>Loads before replacing a board, then starts only after scene preparation succeeds.</summary>
    /// <param name="restore">Whether the player requested continuation.</param>
    /// <param name="contentReady">Whether current content was accepted.</param>
    /// <param name="arenaId">Default arena for a new run.</param>
    /// <param name="cancellationToken">The application lifetime token.</param>
    /// <returns>False only when document recovery was cancelled.</returns>
    public async Task<bool> StartAsync(bool restore, bool contentReady, string arenaId, CancellationToken cancellationToken = default)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            session.RequireStartAllowed(contentReady, restore);
            var saved = restore ? await store.LoadAsync(cancellationToken).ConfigureAwait(true) : null;
            if (restore && saved is null)
                return false;
            cancellationToken.ThrowIfCancellationRequested();
            if (!restore)
                store.BeginNewRun();
            await board.PrepareAsync(saved?.ArenaId ?? arenaId, cancellationToken).ConfigureAwait(true);
            if (saved is null)
                board.StartNew();
            else
                board.Restore(saved);
            session.Begin(restore, saved);
            return true;

        }
        finally { _operations.Release(); }
    }

    /// <summary>Changes the checkpoint policy only after persistent commit has succeeded.</summary>
    /// <param name="cancellationToken">The application lifetime token.</param>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var expectedGeneration = session.Generation;
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            RequireGeneration(expectedGeneration);
            if (!board.IsRunning)
                throw new InvalidOperationException("Only a live run can be saved.");
            var observedVersion = session.ChangeVersion;
            var snapshot = board.Capture();
            RunRules.ValidateSnapshot(snapshot);
            await store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(true);
            session.MarkSaved(snapshot, observedVersion);

        }
        finally { _operations.Release(); }
    }

    /// <summary>Retires before deletion; even a failed delete cannot revive an ended run in this process.</summary>
    /// <param name="cancellationToken">The cleanup token; normal death uses a non-cancelled token.</param>
    public async Task RetireAsync(CancellationToken cancellationToken = default)
    {
        var expectedGeneration = session.Generation;
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            RequireGeneration(expectedGeneration);
            session.Retire();
            await store.RetireAsync(cancellationToken).ConfigureAwait(true);
            session.MarkCheckpointDeleted();

        }
        finally { _operations.Release(); }
    }

    /// <summary>Serializes discarding a board with saving, starting and retiring it.</summary>
    /// <param name="cancellationToken">The application's navigation lifetime.</param>
    public async Task LeaveAsync(CancellationToken cancellationToken = default)
    {
        var expectedGeneration = session.Generation;
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            RequireGeneration(expectedGeneration);
            await board.ClearAsync(cancellationToken).ConfigureAwait(true);
            session.MarkClean();
        }
        finally { _operations.Release(); }
    }

    /// <summary>Rejects queued effects belonging to a board replaced while they waited for the operation gate.</summary>
    private void RequireGeneration(long expectedGeneration)
    {
        if (session.Generation != expectedGeneration)
            throw new InvalidOperationException("The run changed while this operation was waiting.");
    }
}
