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
        if (Colors.TryGetValue(level, out var color) && !string.IsNullOrWhiteSpace(color))
        {
            return color;
        }

        return DefaultColors[level];
    }

    internal LogLevel GetEffectiveMinLevel()
    {
        return Mode == GodotLoggerMode.Debug ? DebugMinLevel : ReleaseMinLevel;
    }

    internal GodotLoggerOptions WithMinimumLevelFloor(LogLevel minLevel)
    {
        return new GodotLoggerOptions
        {
            Mode = Mode,
            DebugMinLevel = Max(DebugMinLevel, minLevel),
            ReleaseMinLevel = Max(ReleaseMinLevel, minLevel),
            DebugOutputTemplate = DebugOutputTemplate,
            ReleaseOutputTemplate = ReleaseOutputTemplate,
            Colors = new Dictionary<LogLevel, string>(Colors)
        };
    }

    private static LogLevel Max(LogLevel left, LogLevel right)
    {
        return left > right ? left : right;
    }
}
