// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Represents one immutable Godot logger configuration snapshot.
/// </summary>
/// <remarks>
///     A snapshot combines mode-specific <see cref="Options"/>, an optional default log level, and category overrides.
///     Category matching is ordinal and deterministic: exact matches win first, then the longest dotted prefix such as
///     <c>Game.Services</c> for <c>Game.Services.Inventory</c>, and finally <see cref="DefaultLogLevel"/> is used when
///     present.
/// </remarks>
internal sealed class GodotLoggerSettings
{
    private readonly IReadOnlyDictionary<string, LogLevel> _loggerLevels;

    /// <summary>
    ///     Gets the default settings snapshot used when no configuration file is available.
    /// </summary>
    public static GodotLoggerSettings Default { get; } = new(new GodotLoggerOptions());

    /// <summary>
    ///     Creates a settings snapshot from normalized options and optional category thresholds.
    /// </summary>
    /// <param name="options">The formatting and mode options for this snapshot.</param>
    /// <param name="defaultLogLevel">The optional fallback level used when no category override matches.</param>
    /// <param name="loggerLevels">Exact category names or dotted prefixes mapped to minimum levels.</param>
    public GodotLoggerSettings(
        GodotLoggerOptions options,
        LogLevel? defaultLogLevel = null,
        IReadOnlyDictionary<string, LogLevel>? loggerLevels = null)
    {
        Options = (options ?? throw new ArgumentNullException(nameof(options))).CreateNormalizedCopy();
        DefaultLogLevel = defaultLogLevel;
        _loggerLevels = loggerLevels ?? new Dictionary<string, LogLevel>(StringComparer.Ordinal);
    }

    /// <summary>
    ///     Gets the optional fallback minimum level for categories without exact or prefix overrides.
    /// </summary>
    public LogLevel? DefaultLogLevel { get; }

    /// <summary>
    ///     Gets normalized rendering and mode options for this snapshot.
    /// </summary>
    public GodotLoggerOptions Options { get; }

    /// <summary>
    ///     Gets exact and dotted-prefix category level overrides.
    /// </summary>
    /// <remarks>
    ///     Keys are interpreted with <see cref="StringComparer.Ordinal"/> semantics. A key only matches a child category
    ///     when the category starts with the key plus a dot, which prevents <c>Game.Service</c> from matching
    ///     <c>Game.Services</c> accidentally.
    /// </remarks>
    public IReadOnlyDictionary<string, LogLevel> LoggerLevels => _loggerLevels;

    /// <summary>
    ///     Creates a settings snapshot from options without any category overrides.
    /// </summary>
    /// <param name="options">The options to normalize and wrap.</param>
    /// <returns>A settings snapshot that relies only on the option-level minimum level.</returns>
    public static GodotLoggerSettings FromOptions(GodotLoggerOptions options)
    {
        return new GodotLoggerSettings(options);
    }

    /// <summary>
    ///     Calculates the effective minimum level for a category.
    /// </summary>
    /// <param name="categoryName">The logger category name.</param>
    /// <param name="providerMinLevel">The provider-level floor captured by the logger.</param>
    /// <returns>The strictest level selected from options, provider floor, and category configuration.</returns>
    /// <remarks>
    ///     The merge starts with <see cref="GodotLoggerOptions.GetEffectiveMinLevel"/> and
    ///     <paramref name="providerMinLevel"/>, then applies <see cref="GetConfiguredMinLevel"/> when it returns a
    ///     value. <see cref="Max(LogLevel, LogLevel)"/> is used at each step so configuration can only make a logger
    ///     stricter, never more verbose than the active floor.
    /// </remarks>
    public LogLevel GetEffectiveMinLevel(string categoryName, LogLevel providerMinLevel)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        var effective = Max(Options.GetEffectiveMinLevel(), providerMinLevel);
        var configuredLevel = GetConfiguredMinLevel(categoryName);
        return configuredLevel.HasValue ? Max(effective, configuredLevel.Value) : effective;
    }

    /// <summary>
    ///     Finds the configured category level using exact match, longest dotted-prefix match, then default fallback.
    /// </summary>
    /// <param name="categoryName">The category to resolve.</param>
    /// <returns>The configured level, or null when no default or override applies.</returns>
    private LogLevel? GetConfiguredMinLevel(string categoryName)
    {
        // Exact category configuration is the most specific and avoids unnecessary prefix scans.
        if (_loggerLevels.TryGetValue(categoryName, out var exactLevel))
        {
            return exactLevel;
        }

        var bestMatchLength = -1;
        LogLevel? bestMatchLevel = DefaultLogLevel;

        foreach (var pair in _loggerLevels)
        {
            // The dotted boundary keeps sibling categories from matching by raw string prefix alone.
            if (!categoryName.StartsWith(pair.Key + ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (pair.Key.Length <= bestMatchLength)
            {
                continue;
            }

            bestMatchLength = pair.Key.Length;
            bestMatchLevel = pair.Value;
        }

        return bestMatchLevel;
    }

    /// <summary>
    ///     Returns the stricter of two log levels.
    /// </summary>
    /// <param name="left">The first level.</param>
    /// <param name="right">The second level.</param>
    /// <returns>The level with the higher severity ordering.</returns>
    private static LogLevel Max(LogLevel left, LogLevel right)
    {
        return left > right ? left : right;
    }
}
