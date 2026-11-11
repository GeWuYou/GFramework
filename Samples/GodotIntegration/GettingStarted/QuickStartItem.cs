// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Bases;
using Godot;

namespace GodotIntegration.GettingStarted;

/// <summary>Editor-authored data associated with a stable business key.</summary>
[GlobalClass]
public partial class QuickStartItem : Resource, IHasKey<string>
{
    /// <summary>Gets or sets the key used by the resource repository.</summary>
    [Export] public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the text displayed by the example page.</summary>
    [Export] public string Title { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Key => Id;
}
