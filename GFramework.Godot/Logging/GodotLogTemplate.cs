// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using GFramework.Core.Abstractions.Logging;

namespace GFramework.Godot.Logging;

/// <summary>
///     Parses and renders Godot logger output templates.
/// </summary>
/// <remarks>
///     Supported placeholders include <c>{timestamp}</c>, <c>{timestamp:format}</c>, <c>{level}</c>,
///     <c>{level:u3}</c>, <c>{level:l3}</c>, <c>{level:padded}</c>, <c>{category}</c>,
///     <c>{category:lN}</c>, <c>{category:rN}</c>, <c>{color}</c>, <c>{message}</c>, and
///     <c>{properties}</c>. Unknown placeholders are rendered back as <c>{key}</c> so configuration mistakes stay
///     visible instead of silently deleting text. Parsed templates and category formatting results use bounded
///     concurrent caches to avoid unbounded growth across hot reloads or dynamic category names.
/// </remarks>
internal sealed class GodotLogTemplate
{
    /// <summary>
    ///     Caches parsed template instances by the raw template text.
    /// </summary>
    /// <remarks>
    ///     The cache is process-wide because templates are immutable after parsing. It is bounded so repeated hot reloads
    ///     with unique template strings cannot grow memory without limit.
    /// </remarks>
    private static readonly BoundedCache<GodotLogTemplate> Cache = new(maxEntries: 256);

    /// <summary>
    ///     Caches formatted category names for this template instance.
    /// </summary>
    /// <remarks>
    ///     Category formatting depends on the template segment and category name. The per-template cache is bounded to
    ///     protect long-running hosts that create loggers with dynamic category names.
    /// </remarks>
    private readonly BoundedCache<string> _categoryCache = new(maxEntries: 1024);
    private readonly int _literalLength;
    private readonly Action<StringBuilder, GodotLogRenderContext>[] _segments;

    private GodotLogTemplate(string template)
    {
        (_segments, _literalLength) = ParseCore(template);
    }

    /// <summary>
    ///     Parses or retrieves a cached template.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <returns>An immutable parsed template.</returns>
    public static GodotLogTemplate Parse(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Cache.GetOrAdd(template, () => new GodotLogTemplate(template));
    }

    /// <summary>
    ///     Renders the template against a concrete log context.
    /// </summary>
    /// <param name="context">The resolved values for the log entry.</param>
    /// <returns>The rendered Godot log line.</returns>
    public string Render(GodotLogRenderContext context)
    {
        var builder = new StringBuilder(_literalLength + context.Category.Length + context.Message.Length + 48);
        foreach (var segment in _segments)
        {
            segment(builder, context);
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Converts template text into literal and placeholder render segments.
    /// </summary>
    /// <param name="template">The template text to parse.</param>
    /// <returns>The render segments and total literal length used to size the output builder.</returns>
    private (Action<StringBuilder, GodotLogRenderContext>[] Segments, int LiteralLength) ParseCore(string template)
    {
        var segments = new List<Action<StringBuilder, GodotLogRenderContext>>();
        var literalLength = 0;
        var position = 0;

        while (position < template.Length)
        {
            // The parser is deliberately small: scan literal runs, then turn balanced placeholders into delegates.
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
            // Capturing the literal string once avoids reparsing or slicing it on each rendered log entry.
            segments.Add((builder, _) => builder.Append(literal));
        }
    }

    /// <summary>
    ///     Creates the render delegate for one placeholder key.
    /// </summary>
    /// <param name="key">The placeholder name and optional format suffix.</param>
    /// <returns>A delegate that appends the placeholder value.</returns>
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
            // Preserve unknown placeholders so configuration errors are visible in the rendered log line.
            _ => (builder, _) => builder.Append('{').Append(key).Append('}')
        };
    }

    /// <summary>
    ///     Creates the render delegate for a timestamp placeholder.
    /// </summary>
    /// <param name="format">The optional .NET timestamp format.</param>
    /// <returns>A delegate that appends the formatted timestamp using invariant culture.</returns>
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

    /// <summary>
    ///     Creates the render delegate for a level placeholder.
    /// </summary>
    /// <param name="format">The level format, such as <c>u3</c>, <c>l3</c>, or <c>padded</c>.</param>
    /// <returns>A delegate that appends the formatted level.</returns>
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

    /// <summary>
    ///     Creates the render delegate for a category placeholder.
    /// </summary>
    /// <param name="format">The category alignment format, such as <c>l16</c> or <c>r32</c>.</param>
    /// <returns>A delegate that appends the category with optional abbreviation and padding.</returns>
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

    /// <summary>
    ///     Formats and caches one category for a category alignment segment.
    /// </summary>
    /// <param name="category">The full category name.</param>
    /// <param name="format">The original segment format used as part of the cache key.</param>
    /// <param name="width">The desired category width.</param>
    /// <param name="padLeft">Whether the result is left-padded instead of right-padded.</param>
    /// <returns>The abbreviated and padded category string.</returns>
    private string GetFormattedCategory(string category, string format, int width, bool padLeft)
    {
        // Include the format in the key because the same category can render differently per width and alignment.
        var cacheKey = string.Concat(format, "\0", category);
        return _categoryCache.GetOrAdd(cacheKey, () =>
        {
            var abbreviated = AbbreviateCategory(category, width);
            return padLeft ? abbreviated.PadLeft(width) : abbreviated.PadRight(width);
        });
    }

    /// <summary>
    ///     Abbreviates dotted category names to fit a target width.
    /// </summary>
    /// <param name="category">The category to abbreviate.</param>
    /// <param name="maxLength">The maximum rendered length.</param>
    /// <returns>The category shortened by initials, dropped prefixes, or final-segment truncation.</returns>
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
            // Collapse namespace-like prefixes first so the most specific final segment remains readable.
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

    /// <summary>
    ///     Converts a level to its three-character form.
    /// </summary>
    /// <param name="level">The level to format.</param>
    /// <param name="upper">Whether the result should use uppercase letters.</param>
    /// <returns>A three-character level label, or <c>unk</c> for undefined enum values.</returns>
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

    /// <summary>
    ///     Converts a level to the fixed-width historical Godot logger label.
    /// </summary>
    /// <param name="level">The level to format.</param>
    /// <returns>A padded level label, or <see cref="object.ToString"/> output for undefined enum values.</returns>
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

    private sealed class BoundedCache<TValue>
    {
        private readonly ConcurrentDictionary<string, CacheEntry<TValue>> _entries = new(StringComparer.Ordinal);
        private readonly int _maxEntries;
        private long _sequence;

        internal BoundedCache(int maxEntries)
        {
            _maxEntries = maxEntries;
        }

        internal TValue GetOrAdd(string key, Func<TValue> valueFactory)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                return existing.Value;
            }

            var created = new CacheEntry<TValue>(valueFactory(), Interlocked.Increment(ref _sequence));
            var stored = _entries.GetOrAdd(key, created);
            if (stored.Sequence == created.Sequence)
            {
                Trim();
            }

            return stored.Value;
        }

        private void Trim()
        {
            while (_entries.Count > _maxEntries)
            {
                var oldestKey = string.Empty;
                var oldestSequence = long.MaxValue;

                foreach (var pair in _entries)
                {
                    if (pair.Value.Sequence >= oldestSequence)
                    {
                        continue;
                    }

                    oldestKey = pair.Key;
                    oldestSequence = pair.Value.Sequence;
                }

                if (oldestSequence == long.MaxValue || !_entries.TryRemove(oldestKey, out _))
                {
                    break;
                }
            }
        }
    }

    private readonly record struct CacheEntry<TValue>(TValue Value, long Sequence);
}
