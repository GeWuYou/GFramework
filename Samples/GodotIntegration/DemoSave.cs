// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Game.Abstractions.Data;

namespace GodotIntegration;

/// <summary>Slot data uses primitive coordinates so the save format has no Godot object dependency.</summary>
public sealed class DemoSave : IVersionedData
{
    /// <summary>Gets or sets the persisted format version.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the most recent explicit save time.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the player horizontal coordinate.</summary>
    public float X { get; set; } = 480;

    /// <summary>Gets or sets the player vertical coordinate.</summary>
    public float Y { get; set; } = 320;

    /// <summary>Gets or sets the number of collected circles.</summary>
    public int Collected { get; set; }
}
