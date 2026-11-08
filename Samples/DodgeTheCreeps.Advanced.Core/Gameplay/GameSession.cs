// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace DodgeTheCreeps.Advanced.Core.Gameplay;

/// <summary>
/// Owns the current run's unsaved-change policy and the persisted checkpoint's retirement state. It contains no engine,
/// storage or container access; the application updates it after successful effects.
/// </summary>
public sealed class GameSession
{
    private (float X, float Y)? _savedPosition;
    private long _changeVersion;
    private long _generation;

    /// <summary>Identity of the currently started run; queued effects must not cross into a newer generation.</summary>
    public long Generation => _generation;

    /// <summary>Monotonic identity of gameplay changes observed before an asynchronous save.</summary>
    public long ChangeVersion => _changeVersion;

    /// <summary>Whether the currently observed board ended; a retry resets this independently of the persisted slot.</summary>
    public bool IsRetired
    {
        get; private set;
    }

    /// <summary>
    /// Whether the persisted slot belongs to an ended run whose deletion has not succeeded.
    /// Starting another run does not make that old checkpoint safe to continue or restore.
    /// </summary>
    public bool IsCheckpointRetired
    {
        get; private set;
    }

    /// <summary>Whether non-position gameplay state changed after the last successful save.</summary>
    public bool HasChanges
    {
        get; private set;
    }

    /// <summary>Begins a board without claiming it was saved or reviving an undeleted checkpoint from an ended run.</summary>
    /// <param name="restored">Whether the board came from a successfully loaded checkpoint.</param>
    /// <param name="snapshot">The restored board, required only for continuation.</param>
    public void Begin(bool restored, RunSnapshot? snapshot = null)
    {
        if (restored && snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));
        _generation = checked(_generation + 1);
        IsRetired = false;
        // A retry changes the board, not the old slot. Only deletion or a committed replacement clears its retirement.
        HasChanges = !restored;
        _savedPosition = restored ? (snapshot!.X, snapshot.Y) : null;
    }

    /// <summary>Records a score or other world change; movement is checked separately at navigation time.</summary>
    public void MarkChanged()
    {
        _changeVersion = checked(_changeVersion + 1);
        HasChanges = true;
    }

    /// <summary>Clears pending-change feedback after explicitly abandoning a board.</summary>
    public void MarkClean() => HasChanges = false;

    /// <summary>Commits the checkpoint only after storage has successfully completed.</summary>
    /// <param name="snapshot">The board that storage accepted.</param>
    /// <param name="observedChangeVersion">The version captured before the persistent write began.</param>
    public void MarkSaved(RunSnapshot snapshot, long observedChangeVersion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        IsRetired = false;
        IsCheckpointRetired = false;
        HasChanges = _changeVersion != observedChangeVersion;
        _savedPosition = (snapshot.X, snapshot.Y);
    }

    /// <summary>Retires a dead run before attempting deletion, so a failed write cannot re-enable continuation.</summary>
    public void Retire()
    {
        IsRetired = true;
        IsCheckpointRetired = true;
        HasChanges = false;
        _savedPosition = null;
    }

    /// <summary>
    /// Records successful deletion of the ended checkpoint. The ended board remains retired until a new run begins.
    /// Call only after the persistent slot has been removed, never merely after starting a retry.
    /// </summary>
    public void MarkCheckpointDeleted() => IsCheckpointRetired = false;

    /// <summary>Determines whether leaving a live board requires a discard decision.</summary>
    /// <param name="isRunning">Whether a board is active.</param>
    /// <param name="x">Current player horizontal position.</param>
    /// <param name="y">Current player vertical position.</param>
    /// <returns>True for an unsaved new run, changed state or movement after the checkpoint.</returns>
    public bool NeedsDiscardConfirmation(bool isRunning, float x, float y) =>
        isRunning && (HasChanges || (_savedPosition is { } saved && (saved.X != x || saved.Y != y)));

    /// <summary>Checks a requested start before any world or storage side effect.</summary>
    /// <param name="contentReady">Whether the host has validated the selected content.</param>
    /// <param name="restore">Whether continuation was requested.</param>
    public void RequireStartAllowed(bool contentReady, bool restore)
    {
        if (!contentReady)
            throw new InvalidOperationException("Repair content selection and restart before playing.");
        if (restore && (IsRetired || IsCheckpointRetired))
            throw new InvalidOperationException("This run has ended and cannot be continued.");
    }
}
