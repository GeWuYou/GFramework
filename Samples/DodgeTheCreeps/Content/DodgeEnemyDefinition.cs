// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace DodgeTheCreeps.Content;

/// <summary>Data-only enemy parameters interpreted by the existing native creep template.</summary>
public sealed class DodgeEnemyDefinition
{
    /// <summary>Gets or sets the stable package-qualified identity.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets minimum spawn speed.</summary>
    public float SpeedMin
    {
        get; set;
    }
    /// <summary>Gets or sets maximum spawn speed.</summary>
    public float SpeedMax
    {
        get; set;
    }
    /// <summary>Gets or sets the built-in animation appearance, from zero through two.</summary>
    public int Appearance
    {
        get; set;
    }
    /// <summary>Gets or sets an optional package-qualified loose PNG reference.</summary>
    public string Texture { get; set; } = string.Empty;
}
