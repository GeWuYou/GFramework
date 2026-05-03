// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Text;

namespace GFramework.Godot.Text;

/// <summary>
///     提供语义化的富文本标签构建辅助方法。
///     该工具层用于减少业务代码直接手写原始 BBCode 字符串的重复工作。
/// </summary>
public static class RichTextMarkup
{
    /// <summary>
    ///     使用指定标签包裹文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="tag">标签名。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="tag" /> 为空、仅包含空白字符，或包含 BBCode token 不允许的控制字符时抛出。
    /// </exception>
    public static string Color(string text, string tag)
    {
        return Wrap(text, tag);
    }

    /// <summary>
    ///     使用 `green` 标签包裹文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    public static string Green(string text)
    {
        return Wrap(text, "green");
    }

    /// <summary>
    ///     使用 `red` 标签包裹文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    public static string Red(string text)
    {
        return Wrap(text, "red");
    }

    /// <summary>
    ///     使用 `gold` 标签包裹文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    public static string Gold(string text)
    {
        return Wrap(text, "gold");
    }

    /// <summary>
    ///     使用 `blue` 标签包裹文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    public static string Blue(string text)
    {
        return Wrap(text, "blue");
    }

    /// <summary>
    ///     使用指定效果标签包裹文本，并可附带参数环境。
    ///     环境参数会按键名进行稳定排序，避免不同字典实现导致输出顺序漂移。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="tag">标签名。</param>
    /// <param name="env">可选的标签参数集合。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="tag" /> 为空、仅包含空白字符，包含 BBCode token 不允许的控制字符，
    ///     或 <paramref name="env" /> 中存在包含非法控制字符的参数键时抛出。
    /// </exception>
    public static string Effect(string text, string tag, IReadOnlyDictionary<string, object?>? env = null)
    {
        ValidateToken(tag, nameof(tag));

        var builder = new StringBuilder();
        builder.Append('[');
        builder.Append(tag);

        if (env is not null)
        {
            foreach (var pair in CollectEnvironmentPairs(env))
            {
                builder.Append(' ');
                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(FormatValue(pair.Value));
            }
        }

        builder.Append(']');
        builder.Append(text ?? string.Empty);
        builder.Append("[/");
        builder.Append(tag);
        builder.Append(']');
        return builder.ToString();
    }

    /// <summary>
    ///     使用指定标签包裹文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="tag">标签名。</param>
    /// <returns>包裹后的 BBCode 文本。</returns>
    private static string Wrap(string text, string tag)
    {
        ValidateToken(tag, nameof(tag));
        return $"[{tag}]{text ?? string.Empty}[/{tag}]";
    }

    /// <summary>
    ///     收集并排序可写入 BBCode 的环境参数。
    /// </summary>
    /// <param name="env">原始环境参数。</param>
    /// <returns>按键名稳定排序后的参数集合。</returns>
    /// <exception cref="ArgumentException">
    ///     当参数键包含 BBCode token 不允许的控制字符时抛出。
    /// </exception>
    private static IReadOnlyList<KeyValuePair<string, object>> CollectEnvironmentPairs(
        IReadOnlyDictionary<string, object?> env)
    {
        var pairs = new List<KeyValuePair<string, object>>(env.Count);
        foreach (var pair in env)
        {
            if (pair.Value is null || string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            ValidateToken(pair.Key, nameof(env));
            pairs.Add(new KeyValuePair<string, object>(pair.Key, pair.Value));
        }

        pairs.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));
        return pairs;
    }

    /// <summary>
    ///     验证 BBCode 标签或参数键是否满足 token 约束。
    /// </summary>
    /// <param name="token">待验证的 token。</param>
    /// <param name="paramName">异常参数名。</param>
    /// <exception cref="ArgumentException">
    ///     当 token 为空、仅包含空白字符，或包含 BBCode token 不允许的控制字符时抛出。
    /// </exception>
    private static void ValidateToken(string token, string paramName)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("BBCode token cannot be null, empty, or whitespace.", paramName);
        }

        foreach (var character in token)
        {
            if (char.IsWhiteSpace(character) || character is '[' or ']' or '=')
            {
                throw new ArgumentException("BBCode token contains invalid control characters.", paramName);
            }
        }
    }

    /// <summary>
    ///     将标签参数值格式化为稳定的 BBCode 字符串表示。
    /// </summary>
    /// <param name="value">待格式化的值。</param>
    /// <returns>适用于 BBCode 参数的字符串。</returns>
    private static string FormatValue(object value)
    {
        return value switch
        {
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
