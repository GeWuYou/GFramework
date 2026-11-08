// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Rejects a content session without silently skipping the failing package.</summary>
public sealed class ContentLoadException : Exception
{
    /// <summary>Creates a structured content failure.</summary>
    /// <param name="diagnostic">Stable package-local error details.</param>
    /// <param name="innerException">Underlying parser or IO failure, if any.</param>
    public ContentLoadException(ContentLoadDiagnostic diagnostic, Exception? innerException = null)
        : base(diagnostic?.Detail, innerException)
    {
        if (diagnostic is null)
            throw new ArgumentNullException(nameof(diagnostic));
        Diagnostic = diagnostic;
    }
    /// <summary>Gets the structured failure details.</summary>
    public ContentLoadDiagnostic Diagnostic
    {
        get;
    }
}
