// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Game.Abstractions.Data;

namespace DodgeTheCreeps;

/// <summary>Portable slot DTO; no native objects are serialized.</summary>
public sealed class DodgeProgress : IVersionedData
{
    /// <inheritdoc />
    public int Version { get; set; } = 1;
    /// <inheritdoc />
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets completed survival seconds.</summary>
    public int Score { get; set; }
    /// <summary>Gets or sets logical horizontal position.</summary>
    public float X { get; set; } = 240;
    /// <summary>Gets or sets logical vertical position.</summary>
    public float Y { get; set; } = 580;
}
