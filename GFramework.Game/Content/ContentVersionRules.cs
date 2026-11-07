// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using GFramework.Game.Abstractions.Content;

namespace GFramework.Game.Content;

/// <summary>Validates the deliberately small three-component content version contract.</summary>
public static class ContentVersionRules
{
    /// <summary>Parses a canonical release version; prerelease and abbreviated versions are not accepted.</summary>
    /// <param name="value">Version in major.minor.patch form.</param>
    /// <returns>Comparable release version.</returns>
    public static Version Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var parts = value.Split('.');
        if (parts.Length != 3 || parts.Any(static part => part.Length == 0 ||
                (part.Length > 1 && part[0] == '0') ||
                !int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out _)))
            throw new ArgumentException("Content versions must use canonical numeric major.minor.patch syntax.", nameof(value));
        return new Version(int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture), int.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    /// <summary>Tests a version against inclusive minimum and exclusive maximum bounds.</summary>
    /// <param name="version">Version to test.</param>
    /// <param name="range">Accepted bounds.</param>
    /// <returns>Whether the version falls within the range.</returns>
    public static bool Contains(string version, ContentVersionRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        Validate(range);
        var parsed = Parse(version);
        return (range.MinimumInclusive is null || parsed >= Parse(range.MinimumInclusive)) &&
               (range.MaximumExclusive is null || parsed < Parse(range.MaximumExclusive));
    }

    /// <summary>Rejects malformed or empty compatibility ranges before loading any package content.</summary>
    /// <param name="range">Range to validate.</param>
    public static void Validate(ContentVersionRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        var minimum = range.MinimumInclusive is null ? null : Parse(range.MinimumInclusive);
        var maximum = range.MaximumExclusive is null ? null : Parse(range.MaximumExclusive);
        if (minimum is not null && maximum is not null && minimum >= maximum)
            throw new ArgumentException("A content version range must have a lower minimum than maximum.", nameof(range));
    }
}
