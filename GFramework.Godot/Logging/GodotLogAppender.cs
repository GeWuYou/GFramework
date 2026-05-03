// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GFramework.Core.Abstractions.Logging;
using Godot;

namespace GFramework.Godot.Logging;

/// <summary>
///     Writes Core <see cref="LogEntry"/> instances to the Godot output APIs.
/// </summary>
/// <remarks>
///     This appender is the Godot-specific edge of the Core logging pipeline. It keeps formatting, color selection, and
///     Godot debugger routing in the host package while allowing consumers to compose Godot output with Core
///     <see cref="ILogAppender"/> features such as <c>CompositeLogger</c>, filters, and async appenders. The appender
///     does not own unmanaged resources; <see cref="Flush"/> and <see cref="Dispose"/> are therefore no-op lifecycle
///     hooks that satisfy the shared appender contract.
/// </remarks>
public sealed class GodotLogAppender : ILogAppender
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyProperties =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    private readonly Func<GodotLoggerOptions> _optionsProvider;

    /// <summary>
    ///     Initializes a Godot appender with default Godot logger options.
    /// </summary>
    public GodotLogAppender()
        : this(new GodotLoggerOptions())
    {
    }

    /// <summary>
    ///     Initializes a Godot appender with fixed Godot logger options.
    /// </summary>
    /// <param name="options">The formatting and routing options used for every appended entry.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public GodotLogAppender(GodotLoggerOptions options)
        : this(CreateFixedOptionsProvider(options))
    {
    }

    /// <summary>
    ///     Initializes a Godot appender with a dynamic options provider.
    /// </summary>
    /// <param name="optionsProvider">
    ///     Provides the latest formatting and routing options for each append operation.
    /// </param>
    /// <remarks>
    ///     The Godot logger provider uses this constructor so cached loggers observe hot-reloaded settings without
    ///     being recreated. The provider must be fast and thread-safe because it is called on the logging path.
    /// </remarks>
    internal GodotLogAppender(Func<GodotLoggerOptions> optionsProvider)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }

    /// <summary>
    ///     Appends one Core log entry to Godot's console and debugger output.
    /// </summary>
    /// <param name="entry">The Core log entry to render.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    public void Append(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var options = _optionsProvider();
        var rendered = Render(entry, options);

        if (options.Mode == GodotLoggerMode.Debug)
        {
            WriteDebug(entry.Level, rendered);
        }
        else
        {
            GD.Print(rendered);
        }

        if (entry.Exception != null)
        {
            GD.PrintErr(entry.Exception.ToString());
        }
    }

    /// <summary>
    ///     Completes pending writes.
    /// </summary>
    /// <remarks>
    ///     Godot output APIs are synchronous from this appender's point of view, so there is no buffered state to
    ///     flush.
    /// </remarks>
    public void Flush()
    {
    }

    /// <summary>
    ///     Releases appender resources.
    /// </summary>
    /// <remarks>
    ///     The appender does not own disposable Godot resources. This method exists to honor the Core appender
    ///     lifecycle contract and to remain composable with factories that dispose appenders uniformly.
    /// </remarks>
    public void Dispose()
    {
    }

    /// <summary>
    ///     Formats structured properties for the <c>{properties}</c> template placeholder.
    /// </summary>
    /// <param name="properties">The already-merged property set from a Core <see cref="LogEntry"/>.</param>
    /// <returns>
    ///     A leading separator plus formatted properties, or an empty string when no valid properties exist.
    /// </returns>
    /// <remarks>
    ///     Blank keys are ignored because they cannot produce useful structured output and can come from
    ///     caller-provided tuples. Valid keys are trimmed at render time so the appender never mutates the original
    ///     property dictionary.
    /// </remarks>
    internal static string FormatProperties(IReadOnlyDictionary<string, object?>? properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return string.Empty;
        }

        var formattedProperties = properties
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .Select(static pair => $"{pair.Key.Trim()}={FormatValue(pair.Value)}")
            .ToArray();

        return formattedProperties.Length == 0
            ? string.Empty
            : " | " + string.Join(", ", formattedProperties);
    }

    /// <summary>
    ///     Renders a Core log entry without writing it to Godot.
    /// </summary>
    /// <param name="entry">The Core log entry to render.</param>
    /// <returns>The line that would be sent to the selected Godot output API.</returns>
    /// <remarks>
    ///     Tests use this method to verify template and structured-property behavior without depending on Godot's
    ///     static output APIs.
    /// </remarks>
    internal string Render(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return Render(entry, _optionsProvider());
    }

    private static Func<GodotLoggerOptions> CreateFixedOptionsProvider(GodotLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return () => options;
    }

    private static string Render(LogEntry entry, GodotLoggerOptions options)
    {
        var templateText = options.Mode == GodotLoggerMode.Debug
            ? options.DebugOutputTemplate
            : options.ReleaseOutputTemplate;
        var context = new GodotLogRenderContext(
            entry.Timestamp,
            entry.Level,
            entry.LoggerName,
            entry.Message,
            options.GetColor(entry.Level),
            FormatProperties(GetMergedProperties(entry)));

        return GodotLogTemplate.Parse(templateText).Render(context);
    }

    private static IReadOnlyDictionary<string, object?> GetMergedProperties(LogEntry entry)
    {
        var allProperties = entry.GetAllProperties();
        return allProperties.Count == 0 ? EmptyProperties : allProperties;
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static void WriteDebug(LogLevel level, string rendered)
    {
        GD.PrintRich(rendered);

        switch (level)
        {
            case LogLevel.Fatal:
            case LogLevel.Error:
                GD.PushError(rendered);
                break;
            case LogLevel.Warning:
                GD.PushWarning(rendered);
                break;
        }
    }
}
