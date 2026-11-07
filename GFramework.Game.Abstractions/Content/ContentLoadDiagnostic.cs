// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Stable, package-local error data for logs and mod-management interfaces.</summary>
public sealed class ContentLoadDiagnostic
{
    /// <summary>Gets a stable error category.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets the package responsible for the failure, if known.</summary>
    public string? PackId { get; init; }
    /// <summary>Gets its declared version, if available.</summary>
    public string? PackVersion { get; init; }
    /// <summary>Gets the source file, if applicable.</summary>
    public string? FilePath { get; init; }
    /// <summary>Gets the extensible table name, if applicable.</summary>
    public string? TableName { get; init; }
    /// <summary>Gets the stable record identity, if applicable.</summary>
    public string? ContentId { get; init; }
    /// <summary>Gets the field path, if applicable.</summary>
    public string? FieldPath { get; init; }
    /// <summary>Gets another package participating in a dependency or conflict.</summary>
    public string? RelatedPackId { get; init; }
    /// <summary>Gets details suitable for presenting alongside the structured fields.</summary>
    public string Detail { get; init; } = string.Empty;
}
