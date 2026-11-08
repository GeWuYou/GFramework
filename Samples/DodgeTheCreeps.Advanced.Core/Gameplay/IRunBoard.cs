// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;

namespace DodgeTheCreeps.Advanced.Core.Gameplay;

/// <summary>Application port for a board. The engine adapter owns scene creation and thread affinity.</summary>
public interface IRunBoard
{
    /// <summary>Whether the adapter currently has an active board.</summary>
    bool IsRunning
    {
        get;
    }

    /// <summary>Creates and attaches a board for a validated arena without starting its simulation.</summary>
    /// <param name="arenaId">The arena selected by a new or saved run.</param>
    /// <param name="cancellationToken">Cancellation before starting the run.</param>
    Task PrepareAsync(string arenaId, CancellationToken cancellationToken);

    /// <summary>Starts a freshly prepared board.</summary>
    void StartNew();

    /// <summary>Restores an already validated checkpoint on the host thread.</summary>
    /// <param name="snapshot">The full board and random continuation state.</param>
    void Restore(RunSnapshot snapshot);

    /// <summary>Captures a full active board on the host thread.</summary>
    /// <returns>A detached serializable checkpoint.</returns>
    RunSnapshot Capture();

    /// <summary>Stops and removes the current board without saving it.</summary>
    /// <param name="cancellationToken">Cancellation of the host navigation operation.</param>
    Task ClearAsync(CancellationToken cancellationToken);

    /// <summary>Stops simulation while retaining the scene for its normal owner to remove.</summary>
    void Stop();
}
