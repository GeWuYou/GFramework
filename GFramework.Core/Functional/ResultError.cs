// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Functional;

/// <summary>Shares default-result failure identity and consistent failure value semantics across both result types.</summary>
internal static class ResultError
{
    /// <summary>Gets the shared marker; reading a default result does not allocate an exception.</summary>
    public static Exception Uninitialized { get; } = new InvalidOperationException("The result has not been initialized.");

    /// <summary>Compares failure classifications and their ordinal descriptions.</summary>
    public static bool Equals(Exception left, Exception right) => left.GetType() == right.GetType() &&
        string.Equals(left.Message, right.Message, StringComparison.Ordinal);

    /// <summary>Hashes precisely the failure values used by equality.</summary>
    public static int GetHashCode(Exception error) => HashCode.Combine(0, error.GetType(), error.Message);
}
