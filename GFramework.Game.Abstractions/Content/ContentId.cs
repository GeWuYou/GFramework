// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>A persistent, case-sensitive identity independent of paths, display names and load order.</summary>
public readonly record struct ContentId
{
    /// <summary>Creates an identity using canonical lowercase <c>pack:local</c> syntax.</summary>
    /// <param name="value">Canonical identifier.</param>
    /// <exception cref="ArgumentException">The identifier is not canonical.</exception>
    public ContentId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Content IDs cannot be empty.", nameof(value));
        var separator = value.IndexOf(':');
        if (separator <= 0 || separator != value.LastIndexOf(':') ||
            !IsCanonicalSegment(value[..separator]) || !IsCanonicalSegment(value[(separator + 1)..]))
        {
            throw new ArgumentException("Content IDs must use lowercase pack:local syntax.", nameof(value));
        }
        PackId = value[..separator];
        LocalId = value[(separator + 1)..];
    }

    /// <summary>Gets the package that originally owns this identifier.</summary>
    public string PackId { get; }

    /// <summary>Gets the stable identifier inside the owning package.</summary>
    public string LocalId { get; }

    /// <summary>Checks package and local names without treating them as filesystem paths.</summary>
    /// <param name="value">Name to check.</param>
    /// <returns>Whether all characters satisfy the canonical identifier contract.</returns>
    public static bool IsCanonicalSegment(string? value)
    {
        if (string.IsNullOrEmpty(value) || value[0] is not (>= 'a' and <= 'z' or >= '0' and <= '9'))
            return false;
        return value.All(static character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-');
    }

    /// <inheritdoc />
    public override string ToString() => string.IsNullOrEmpty(PackId) ? string.Empty : $"{PackId}:{LocalId}";
}
