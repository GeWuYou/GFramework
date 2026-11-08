// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Declares a package dependency and the versions its content author accepts.</summary>
public sealed class ContentPackDependency
{
    /// <summary>Gets the canonical dependency ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the accepted version range.</summary>
    public ContentVersionRange Versions { get; init; } = new();

    /// <summary>Gets whether absence is allowed; an enabled optional dependency still constrains order and version.</summary>
    public bool Optional
    {
        get; init;
    }
}
