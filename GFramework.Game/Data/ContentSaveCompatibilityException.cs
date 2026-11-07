// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Data;

/// <summary>Stops ordinary loading before any migration or write when content is unavailable.</summary>
public sealed class ContentSaveCompatibilityException : InvalidOperationException
{
    /// <summary>Creates an exception carrying a structured recovery report.</summary>
    /// <param name="report">Inspection performed before ordinary save migration.</param>
    public ContentSaveCompatibilityException(ContentSaveReport report)
        : base("Save content is incompatible. Inspect the report and explicitly recover or restore the required packages.")
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
    }

    /// <summary>Gets the exact differences to display before requesting recovery.</summary>
    public ContentSaveReport Report { get; }
}
