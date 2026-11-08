// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Data;

namespace GFramework.Game.Data;

/// <summary>Stops ordinary load when explicit preparation/commit is required; the file remains unchanged.</summary>
/// <typeparam name="TSaveData">Current game DTO.</typeparam>
/// <remarks>Creates a load decision carrying the immutable prepared candidate.</remarks>
/// <param name="prepared">Candidate to inspect and explicitly commit.</param>
public sealed class SavePreparationRequiredException<TSaveData>(PreparedSave<TSaveData> prepared) : InvalidOperationException("The save requires explicit document conversion or content reconciliation before loading.") where TSaveData : class, IData, new()
{
    /// <summary>Gets the prepared load decision.</summary>
    public PreparedSave<TSaveData> Prepared { get; } = prepared ?? throw new ArgumentNullException(nameof(prepared));
}
