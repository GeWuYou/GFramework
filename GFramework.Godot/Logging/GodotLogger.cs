// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Godot platform logger implementation.
/// </summary>
/// <remarks>
///     This logger preserves the existing <see cref="ILogger"/> entry point while delegating output to
///     <see cref="GodotLogAppender"/> so Godot rendering remains compatible with the Core appender pipeline.
/// </remarks>
public sealed class GodotLogger : AbstractLogger
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyProperties =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    private readonly GodotLogAppender _appender;

    /// <summary>
    ///     Initializes a logger that preserves the historical fixed-format template.
    /// </summary>
    /// <param name="name">The logger name.</param>
    /// <param name="minLevel">The minimum enabled log level.</param>
    public GodotLogger(string? name = null, LogLevel minLevel = LogLevel.Info)
        : this(
            name ?? RootLoggerName,
            CreateFixedOptionsProvider(minLevel),
            () => minLevel)
    {
    }

    /// <summary>
    ///     Initializes a logger with Godot-specific formatting options.
    /// </summary>
    /// <param name="name">The logger name.</param>
    /// <param name="options">The logger options.</param>
    public GodotLogger(string? name, GodotLoggerOptions options)
        : this(
            name ?? RootLoggerName,
            CreateOptionsProvider(options),
            CreateMinLevelProvider(options))
    {
    }

    /// <summary>
    ///     Initializes the core logger with dynamic options and level providers.
    /// </summary>
    /// <param name="name">The resolved logger name used in rendered output.</param>
    /// <param name="optionsProvider">
    ///     The provider that supplies the latest rendering options for each write.
    /// </param>
    /// <param name="minLevelProvider">The provider that supplies the latest effective minimum level.</param>
    /// <remarks>
    ///     The Godot factory uses this constructor so cached logger instances can observe hot-reloaded settings without
    ///     being recreated. The default public constructor supplies a fixed provider to avoid allocation on the log
    ///     path.
    /// </remarks>
    internal GodotLogger(
        string name,
        Func<GodotLoggerOptions> optionsProvider,
        Func<LogLevel> minLevelProvider)
        : base(name, minLevelProvider ?? throw new ArgumentNullException(nameof(minLevelProvider)))
    {
        _appender = new GodotLogAppender(
            optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider)));
    }

    /// <summary>
    ///     Writes a log entry to Godot.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The rendered message body.</param>
    /// <param name="exception">The optional exception.</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        WriteEntry(level, message, exception, properties: null);
    }

    /// <summary>
    ///     Uses Godot-aware structured rendering instead of the base string concatenation fallback.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message body before Godot template rendering.</param>
    /// <param name="properties">Structured properties appended through the configured Godot template.</param>
    public override void Log(LogLevel level, string message, params (string Key, object? Value)[] properties)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        WriteEntry(level, message, exception: null, properties);
    }

    /// <summary>
    ///     Uses Godot-aware structured rendering instead of the base string concatenation fallback.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message body before Godot template rendering.</param>
    /// <param name="exception">The optional exception written after the rendered message.</param>
    /// <param name="properties">Structured properties appended through the configured Godot template.</param>
    public override void Log(
        LogLevel level,
        string message,
        Exception? exception,
        params (string Key, object? Value)[] properties)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        WriteEntry(level, message, exception, properties);
    }

    private void WriteEntry(
        LogLevel level,
        string message,
        Exception? exception,
        (string Key, object? Value)[]? properties)
    {
        var entry = new LogEntry(
            DateTime.UtcNow,
            level,
            Name(),
            message,
            exception,
            ToPropertiesDictionary(properties));

        _appender.Append(entry);
    }

    private static IReadOnlyDictionary<string, object?> ToPropertiesDictionary(
        (string Key, object? Value)[]? properties)
    {
        if (properties == null || properties.Length == 0)
        {
            return EmptyProperties;
        }

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (string.IsNullOrWhiteSpace(property.Key))
            {
                continue;
            }

            result[property.Key.Trim()] = property.Value;
        }

        return result.Count == 0 ? EmptyProperties : result;
    }

    private static Func<GodotLoggerOptions> CreateFixedOptionsProvider(LogLevel minLevel)
    {
        var options = GodotLoggerOptions.ForMinimumLevel(minLevel);
        return () => options;
    }

    private static Func<GodotLoggerOptions> CreateOptionsProvider(GodotLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return () => options;
    }

    private static Func<LogLevel> CreateMinLevelProvider(GodotLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return () => options.GetEffectiveMinLevel();
    }
}
