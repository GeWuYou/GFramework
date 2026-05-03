// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Godot logger formatting and routing options.
/// </summary>
public sealed class GodotLoggerOptions
{
    private static readonly IReadOnlyDictionary<LogLevel, string> DefaultColors = new Dictionary<LogLevel, string>
    {
        [LogLevel.Trace] = "gray",
        [LogLevel.Debug] = "cyan",
        [LogLevel.Info] = "white",
        [LogLevel.Warning] = "orange",
        [LogLevel.Error] = "red",
        [LogLevel.Fatal] = "deep_pink"
    };

    /// <summary>
    ///     Gets or sets the output mode.
    /// </summary>
    public GodotLoggerMode Mode { get; set; } = GodotLoggerMode.Debug;

    /// <summary>
    ///     Gets or sets the minimum level used by <see cref="GodotLoggerMode.Debug"/>.
    /// </summary>
    public LogLevel DebugMinLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    ///     Gets or sets the minimum level used by <see cref="GodotLoggerMode.Release"/>.
    /// </summary>
    public LogLevel ReleaseMinLevel { get; set; } = LogLevel.Info;

    /// <summary>
    ///     Gets or sets the BBCode-capable template used by <see cref="GodotLoggerMode.Debug"/>.
    /// </summary>
#pragma warning disable MA0016 // Keep configuration mutable for object initializer and serializer scenarios.
    public string DebugOutputTemplate { get; set; } =
        "[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [color={color}][{level:u3}][/color] [{category:l16}] {message}{properties}";
#pragma warning restore MA0016

    /// <summary>
    ///     Gets or sets the plain text template used by <see cref="GodotLoggerMode.Release"/>.
    /// </summary>
#pragma warning disable MA0016 // Keep configuration mutable for object initializer and serializer scenarios.
    public string ReleaseOutputTemplate { get; set; } =
        "[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level:u3}] [{category:l16}] {message}{properties}";
#pragma warning restore MA0016

    /// <summary>
    ///     Gets or sets Godot named colors by log level.
    /// </summary>
#pragma warning disable MA0016 // Keep configuration mutable for object initializer and serializer scenarios.
    public Dictionary<LogLevel, string> Colors { get; set; } = new(DefaultColors);
#pragma warning restore MA0016

    /// <summary>
    ///     Creates options that preserve the previous Godot logger defaults for a fixed minimum level.
    /// </summary>
    /// <param name="minLevel">The minimum enabled level.</param>
    /// <returns>Options equivalent to the previous fixed-format logger behavior.</returns>
    public static GodotLoggerOptions ForMinimumLevel(LogLevel minLevel)
    {
        return new GodotLoggerOptions
        {
            Mode = GodotLoggerMode.Debug,
            DebugMinLevel = minLevel,
            ReleaseMinLevel = minLevel,
            DebugOutputTemplate = "[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] {level:padded} [{category}] {message}{properties}",
            ReleaseOutputTemplate = "[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] {level:padded} [{category}] {message}{properties}",
            Colors = new Dictionary<LogLevel, string>(DefaultColors)
        };
    }

    /// <summary>
    ///     Returns the configured color for the specified level.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>The Godot named color.</returns>
    public string GetColor(LogLevel level)
    {
        if (Colors is { } colors && colors.TryGetValue(level, out var color) && !string.IsNullOrWhiteSpace(color))
        {
            return color;
        }

        return DefaultColors.TryGetValue(level, out var fallback) ? fallback : "white";
    }

    /// <summary>
    ///     Gets the active minimum level for the current <see cref="Mode"/>.
    /// </summary>
    /// <returns>
    ///     <see cref="DebugMinLevel"/> when <see cref="Mode"/> is <see cref="GodotLoggerMode.Debug"/>; otherwise
    ///     <see cref="ReleaseMinLevel"/>.
    /// </returns>
    /// <remarks>
    ///     Factories use this value as the option-level floor before category-specific settings are applied.
    /// </remarks>
    internal LogLevel GetEffectiveMinLevel()
    {
        return Mode == GodotLoggerMode.Debug ? DebugMinLevel : ReleaseMinLevel;
    }

    /// <summary>
    ///     Creates a copy whose debug and release floors are at least <paramref name="minLevel"/>.
    /// </summary>
    /// <param name="minLevel">The minimum level that both mode-specific floors must satisfy.</param>
    /// <returns>A normalized copy with stricter or equal mode-specific minimum levels.</returns>
    /// <remarks>
    ///     The operation can raise <see cref="DebugMinLevel"/> and <see cref="ReleaseMinLevel"/> through
    ///     <see cref="Max(LogLevel, LogLevel)"/>, but it never lowers them. <see cref="DebugOutputTemplate"/>,
    ///     <see cref="ReleaseOutputTemplate"/>, and <see cref="Colors"/> are preserved through a defensive copy.
    /// </remarks>
    internal GodotLoggerOptions WithMinimumLevelFloor(LogLevel minLevel)
    {
        return new GodotLoggerOptions
        {
            Mode = Mode,
            DebugMinLevel = Max(DebugMinLevel, minLevel),
            ReleaseMinLevel = Max(ReleaseMinLevel, minLevel),
            DebugOutputTemplate = DebugOutputTemplate,
            ReleaseOutputTemplate = ReleaseOutputTemplate,
            Colors = CopyColorsWithDefaults(Colors)
        };
    }

    /// <summary>
    ///     Creates a copy that replaces missing templates or color mappings with safe defaults.
    /// </summary>
    /// <returns>A normalized copy suitable for runtime rendering.</returns>
    /// <remarks>
    ///     JSON input can set <see cref="DebugOutputTemplate"/>, <see cref="ReleaseOutputTemplate"/>, or
    ///     <see cref="Colors"/> to null even though the public API treats them as non-null. This method keeps
    ///     deserialization and imperative configuration from publishing values that would fail during rendering.
    /// </remarks>
    internal GodotLoggerOptions CreateNormalizedCopy()
    {
        var defaults = new GodotLoggerOptions();

        return new GodotLoggerOptions
        {
            Mode = Mode,
            DebugMinLevel = DebugMinLevel,
            ReleaseMinLevel = ReleaseMinLevel,
            DebugOutputTemplate = string.IsNullOrWhiteSpace(DebugOutputTemplate)
                ? defaults.DebugOutputTemplate
                : DebugOutputTemplate,
            ReleaseOutputTemplate = string.IsNullOrWhiteSpace(ReleaseOutputTemplate)
                ? defaults.ReleaseOutputTemplate
                : ReleaseOutputTemplate,
            Colors = CopyColorsWithDefaults(Colors)
        };
    }

    private static LogLevel Max(LogLevel left, LogLevel right)
    {
        return left > right ? left : right;
    }

    private static Dictionary<LogLevel, string> CopyColorsWithDefaults(Dictionary<LogLevel, string>? colors)
    {
        var merged = new Dictionary<LogLevel, string>(DefaultColors);
        if (colors == null)
        {
            return merged;
        }

        foreach (var pair in colors)
        {
            if (!string.IsNullOrWhiteSpace(pair.Value))
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }
}
