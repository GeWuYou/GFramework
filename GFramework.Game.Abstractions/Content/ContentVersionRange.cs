// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>A declared content compatibility range; omitted endpoints are unbounded.</summary>
public sealed class ContentVersionRange
{
    /// <summary>Gets the inclusive lower bound in three-component numeric version syntax.</summary>
    public string? MinimumInclusive
    {
        get; init;
    }

    /// <summary>Gets the exclusive upper bound in three-component numeric version syntax.</summary>
    public string? MaximumExclusive
    {
        get; init;
    }
}
