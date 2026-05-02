using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using Godot;

namespace GFramework.Godot.Logging;

/// <summary>
///     Godot platform logger implementation.
/// </summary>
public sealed class GodotLogger : AbstractLogger
{
    private readonly Func<LogLevel> _minLevelProvider;
    private readonly Func<GodotLoggerOptions> _optionsProvider;

    /// <summary>
    ///     Initializes a logger that preserves the historical fixed-format template.
    /// </summary>
    /// <param name="name">The logger name.</param>
    /// <param name="minLevel">The minimum enabled log level.</param>
    public GodotLogger(string? name = null, LogLevel minLevel = LogLevel.Info)
        : this(
            name ?? RootLoggerName,
            () => GodotLoggerOptions.ForMinimumLevel(minLevel),
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
            () => options ?? throw new ArgumentNullException(nameof(options)),
            () => (options ?? throw new ArgumentNullException(nameof(options))).GetEffectiveMinLevel())
    {
    }

    internal GodotLogger(
        string name,
        Func<GodotLoggerOptions> optionsProvider,
        Func<LogLevel> minLevelProvider)
        : base(name, minLevelProvider ?? throw new ArgumentNullException(nameof(minLevelProvider)))
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
        _minLevelProvider = minLevelProvider;
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
    public override void Log(LogLevel level, string message, params (string Key, object? Value)[] properties)
    {
        if (level < _minLevelProvider())
        {
            return;
        }

        WriteEntry(level, message, exception: null, properties);
    }

    /// <summary>
    ///     Uses Godot-aware structured rendering instead of the base string concatenation fallback.
    /// </summary>
    public override void Log(
        LogLevel level,
        string message,
        Exception? exception,
        params (string Key, object? Value)[] properties)
    {
        if (level < _minLevelProvider())
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
        var options = _optionsProvider();
        var templateText = options.Mode == GodotLoggerMode.Debug
            ? options.DebugOutputTemplate
            : options.ReleaseOutputTemplate;
        var context = new GodotLogRenderContext(
            DateTime.UtcNow,
            level,
            Name(),
            message,
            options.GetColor(level),
            FormatProperties(properties));
        var rendered = GodotLogTemplate.Parse(templateText).Render(context);

        if (options.Mode == GodotLoggerMode.Debug)
        {
            WriteDebug(level, rendered);
        }
        else
        {
            GD.Print(rendered);
        }

        if (exception != null)
        {
            GD.PrintErr(exception.ToString());
        }
    }

    private static string FormatProperties((string Key, object? Value)[]? properties)
    {
        var merged = MergeProperties(properties);
        if (merged.Count == 0)
        {
            return string.Empty;
        }

        return " | " + string.Join(", ", merged.Select(static pair => $"{pair.Key}={FormatValue(pair.Value)}"));
    }

    private static IReadOnlyDictionary<string, object?> MergeProperties((string Key, object? Value)[]? properties)
    {
        var contextProperties = LogContext.Current;
        if ((properties == null || properties.Length == 0) && contextProperties.Count == 0)
        {
            return EmptyProperties;
        }

        var merged = new Dictionary<string, object?>(contextProperties, StringComparer.Ordinal);
        if (properties != null)
        {
            foreach (var property in properties)
            {
                merged[property.Key] = property.Value;
            }
        }

        return merged;
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyProperties =
        new Dictionary<string, object?>(StringComparer.Ordinal);

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
