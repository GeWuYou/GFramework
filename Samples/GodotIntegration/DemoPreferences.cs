// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using GFramework.Game.Abstractions.Input;
using GFramework.Game.Abstractions.Setting;

namespace GodotIntegration;

/// <summary>Settings section persisted through the existing unified settings repository.</summary>
public sealed class DemoPreferences : ISettingsData
{
    /// <summary>Gets or sets the settings format version.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the most recent save time.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets whether the playfield grid is visible.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Gets or sets the framework input bindings snapshot, including the pause binding.</summary>
    public InputBindingSnapshot? Bindings
    {
        get; set;
    }

    /// <summary>Restores this section to its initial values.</summary>
    public void Reset()
    {
        ShowGrid = true;
        Bindings = null;
    }

    /// <summary>Copies a previously loaded settings section into this instance.</summary>
    /// <param name="source">Settings loaded by the repository.</param>
    public void LoadFrom(ISettingsData source)
    {
        if (source is not DemoPreferences preferences)
            throw new ArgumentException("Expected sample preferences.", nameof(source));
        ShowGrid = preferences.ShowGrid;
        Bindings = preferences.Bindings;
        Version = preferences.Version;
        LastModified = preferences.LastModified;
    }
}
