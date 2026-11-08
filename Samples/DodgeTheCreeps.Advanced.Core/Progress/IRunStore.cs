// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using DodgeTheCreeps.Advanced.Core.Gameplay;

namespace DodgeTheCreeps.Advanced.Core.Progress;

/// <summary>Storage port; the adapter handles document recovery, user confirmation and per-mod payloads.</summary>
public interface IRunStore
{
    /// <summary>Starts a fresh per-run storage session, including isolated mod state.</summary>
    void BeginNewRun();

    /// <summary>Prepares and, if confirmed, commits a saved document before returning its board.</summary>
    /// <param name="cancellationToken">Cancellation of the read/confirmation operation.</param>
    /// <returns>The validated board, or null when the player cancels recovery.</returns>
    Task<RunSnapshot?> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Persists a complete board and its host-owned mod session.</summary>
    /// <param name="snapshot">The captured board.</param>
    /// <param name="cancellationToken">Cancellation before commit.</param>
    Task SaveAsync(RunSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>Deletes the ended run and invalidates its backup entry.</summary>
    /// <param name="cancellationToken">The host's cleanup token.</param>
    Task RetireAsync(CancellationToken cancellationToken);
}
