// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;

namespace DodgeTheCreeps.Content;

/// <summary>Data-only arena parameters interpreted by the built-in survival game.</summary>
public sealed class DodgeArenaDefinition
{
    /// <summary>Gets or sets the stable package-qualified identity persisted in saves.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the allowed enemy definition IDs.</summary>
    public string[] Enemies { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the starting spawn interval.</summary>
    public double SpawnInterval { get; set; }
    /// <summary>Gets or sets an RGB hexadecimal background color.</summary>
    public string Background { get; set; } = "091222";
    /// <summary>Gets or sets an optional package-qualified scene from a host-trusted PCK.</summary>
    public string OfficialScene { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional package-qualified loose OGG/WAV music file.</summary>
    public string Music { get; set; } = string.Empty;
}
