// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json.Linq;

namespace GFramework.Game.Data;

/// <summary>A versioned plain-data payload. Conversion works on this block before a current CLR DTO exists.</summary>
public sealed class SaveDataBlock
{
    /// <summary>Gets or sets the game- or mod-owned schema version.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the complete JSON object, including fields unknown to the current DTO.</summary>
    public JObject Data { get; set; } = [];

    /// <summary>Creates an independent block for pure preparation or caller-owned inspection.</summary>
    /// <returns>A deep copy.</returns>
    public SaveDataBlock Clone() => new() { Version = Version, Data = (JObject)Data.DeepClone() };
}
