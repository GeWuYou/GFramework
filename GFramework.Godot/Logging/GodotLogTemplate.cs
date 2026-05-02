using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

internal sealed class GodotLogTemplate
{
    private static readonly ConcurrentDictionary<string, GodotLogTemplate> Cache = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, string> _categoryCache = new(StringComparer.Ordinal);
    private readonly int _literalLength;
    private readonly Action<StringBuilder, GodotLogRenderContext>[] _segments;

    private GodotLogTemplate(string template)
    {
        (_segments, _literalLength) = ParseCore(template);
    }

    public static GodotLogTemplate Parse(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Cache.GetOrAdd(template, static value => new GodotLogTemplate(value));
    }

    public string Render(GodotLogRenderContext context)
    {
        var builder = new StringBuilder(_literalLength + context.Category.Length + context.Message.Length + 48);
        foreach (var segment in _segments)
        {
            segment(builder, context);
        }

        return builder.ToString();
    }

    private (Action<StringBuilder, GodotLogRenderContext>[] Segments, int LiteralLength) ParseCore(string template)
    {
        var segments = new List<Action<StringBuilder, GodotLogRenderContext>>();
        var literalLength = 0;
        var position = 0;

        while (position < template.Length)
        {
            var open = template.IndexOf('{', position);
            if (open < 0)
            {
                AddLiteral(template[position..]);
                break;
            }

            if (open > position)
            {
                AddLiteral(template[position..open]);
            }

            var close = template.IndexOf('}', open + 1);
            if (close < 0)
            {
                AddLiteral(template[open..]);
                break;
            }

            var key = template.Substring(open + 1, close - open - 1);
            segments.Add(CreateSegment(key));
            position = close + 1;
        }

        return ([.. segments], literalLength);

        void AddLiteral(string literal)
        {
            if (literal.Length == 0)
            {
                return;
            }

            literalLength += literal.Length;
            segments.Add((builder, _) => builder.Append(literal));
        }
    }

    private Action<StringBuilder, GodotLogRenderContext> CreateSegment(string key)
    {
        return key switch
        {
            "category" => static (builder, context) => builder.Append(context.Category),
            "color" => static (builder, context) => builder.Append(context.Color),
            "level" => static (builder, context) => builder.Append(context.Level),
            "message" => static (builder, context) => builder.Append(context.Message),
            "properties" => static (builder, context) => builder.Append(context.Properties),
            "timestamp" => static (builder, context) => builder.Append(context.Timestamp.ToString(
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture)),
            not null when key.StartsWith("category:", StringComparison.Ordinal) => CreateCategorySegment(key[9..]),
            not null when key.StartsWith("level:", StringComparison.Ordinal) => CreateLevelSegment(key[6..]),
            not null when key.StartsWith("timestamp:", StringComparison.Ordinal) => CreateTimestampSegment(key[10..]),
            _ => (builder, _) => builder.Append('{').Append(key).Append('}')
        };
    }

    private Action<StringBuilder, GodotLogRenderContext> CreateTimestampSegment(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return static (builder, context) => builder.Append(context.Timestamp.ToString(
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture));
        }

        return (builder, context) => builder.Append(context.Timestamp.ToString(format, CultureInfo.InvariantCulture));
    }

    private static Action<StringBuilder, GodotLogRenderContext> CreateLevelSegment(string format)
    {
        return format switch
        {
            "u3" or "U3" => static (builder, context) => builder.Append(ToShortLevel(context.Level, upper: true)),
            "l3" or "L3" => static (builder, context) => builder.Append(ToShortLevel(context.Level, upper: false)),
            "padded" or "Padded" => static (builder, context) => builder.Append(ToPaddedLevel(context.Level)),
            _ => static (builder, context) => builder.Append(context.Level)
        };
    }

    private Action<StringBuilder, GodotLogRenderContext> CreateCategorySegment(string format)
    {
        if (format.Length < 2)
        {
            return static (builder, context) => builder.Append(context.Category);
        }

        var alignment = format[0];
        if (alignment is not 'l' and not 'r')
        {
            return static (builder, context) => builder.Append(context.Category);
        }

        if (!int.TryParse(format[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width <= 0)
        {
            return static (builder, context) => builder.Append(context.Category);
        }

        return alignment == 'l'
            ? (builder, context) => builder.Append(GetFormattedCategory(context.Category, format, width, padLeft: false))
            : (builder, context) => builder.Append(GetFormattedCategory(context.Category, format, width, padLeft: true));
    }

    private string GetFormattedCategory(string category, string format, int width, bool padLeft)
    {
        var cacheKey = string.Concat(format, "\0", category);
        return _categoryCache.GetOrAdd(cacheKey, _ =>
        {
            var abbreviated = AbbreviateCategory(category, width);
            return padLeft ? abbreviated.PadLeft(width) : abbreviated.PadRight(width);
        });
    }

    private static string AbbreviateCategory(string category, int maxLength)
    {
        if (category.Length <= maxLength)
        {
            return category;
        }

        var parts = category.Split('.');
        if (parts.Length == 1)
        {
            return category[..maxLength];
        }

        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Length > 1)
            {
                parts[i] = parts[i][..1];
            }
        }

        var start = 0;
        while (start < parts.Length - 1)
        {
            var joined = string.Join(".", parts, start, parts.Length - start);
            if (joined.Length <= maxLength)
            {
                return joined;
            }

            start++;
        }

        var last = parts[^1];
        return last.Length > maxLength ? last[..maxLength] : last;
    }

    private static string ToShortLevel(LogLevel level, bool upper)
    {
        var value = level switch
        {
            LogLevel.Trace => "trc",
            LogLevel.Debug => "dbg",
            LogLevel.Info => "inf",
            LogLevel.Warning => "wrn",
            LogLevel.Error => "err",
            LogLevel.Fatal => "ftl",
            _ => "unk"
        };

        return upper ? value.ToUpperInvariant() : value;
    }

    private static string ToPaddedLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRACE  ",
            LogLevel.Debug => "DEBUG  ",
            LogLevel.Info => "INFO   ",
            LogLevel.Warning => "WARNING",
            LogLevel.Error => "ERROR  ",
            LogLevel.Fatal => "FATAL  ",
            _ => level.ToString()
        };
    }
}
