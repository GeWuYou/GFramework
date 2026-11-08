// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Data;

/// <summary>Stops ordinary load when explicit preparation/commit is required; the file remains unchanged.</summary>
/// <typeparam name="TSaveData">Current game DTO.</typeparam>
public sealed class SavePreparationRequiredException<TSaveData> : InvalidOperationException where TSaveData : class, IData, new()
{
    /// <summary>Creates a load decision carrying the immutable prepared candidate.</summary>
    /// <param name="prepared">Candidate to inspect and explicitly commit.</param>
    public SavePreparationRequiredException(PreparedSave<TSaveData> prepared)
        : base("The save requires explicit document conversion or content reconciliation before loading.")
    {
        Prepared = prepared ?? throw new ArgumentNullException(nameof(prepared));
    }
    /// <summary>Gets the prepared load decision.</summary>
    public PreparedSave<TSaveData> Prepared { get; }
}
