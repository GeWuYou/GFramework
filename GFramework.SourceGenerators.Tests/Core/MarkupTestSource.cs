// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace GFramework.SourceGenerators.Tests.Core;

/// <summary>
///     为源生成器测试提供轻量的源码标记解析能力。
/// </summary>
public sealed class MarkupTestSource
{
    private readonly SourceText _sourceText;
    private readonly IReadOnlyDictionary<string, TextSpan> _spans;

    private MarkupTestSource(
        string source,
        SourceText sourceText,
        IReadOnlyDictionary<string, TextSpan> spans)
    {
        Source = source;
        _sourceText = sourceText;
        _spans = spans;
    }

    /// <summary>
    ///     获取移除标记后的源码文本。
    /// </summary>
    public string Source { get; }

    /// <summary>
    ///     解析形如 <c>{|#0:identifier|}</c> 的单层标记，并保留去标记后的源码。
    /// </summary>
    /// <param name="markupSource">包含测试标记的源码。</param>
    /// <returns>可用于测试输入和诊断定位的解析结果。</returns>
    /// <exception cref="InvalidOperationException">标记格式不合法，或存在重复标记编号时抛出。</exception>
    public static MarkupTestSource Parse(string markupSource)
    {
        var builder = new StringBuilder(markupSource.Length);
        var spans = new Dictionary<string, TextSpan>(StringComparer.Ordinal);

        for (var index = 0; index < markupSource.Length; index++)
        {
            if (!StartsWithMarker(markupSource, index))
            {
                builder.Append(markupSource[index]);
                continue;
            }

            index += 3;
            var markerIdStart = index;
            while (index < markupSource.Length && markupSource[index] != ':')
                index++;

            if (index >= markupSource.Length)
                throw new InvalidOperationException("Unterminated markup marker identifier.");

            var markerId = markupSource.Substring(markerIdStart, index - markerIdStart);
            if (markerId.Length == 0)
                throw new InvalidOperationException("Markup marker identifier cannot be empty.");

            var spanStart = builder.Length;
            index++;

            while (index < markupSource.Length && !EndsWithMarker(markupSource, index))
            {
                builder.Append(markupSource[index]);
                index++;
            }

            if (index >= markupSource.Length)
                throw new InvalidOperationException($"Unterminated markup marker '{markerId}'.");

            if (!spans.TryAdd(markerId, TextSpan.FromBounds(spanStart, builder.Length)))
                throw new InvalidOperationException($"Duplicate markup marker '{markerId}'.");

            index++;
        }

        var source = builder.ToString();
        return new MarkupTestSource(source, SourceText.From(source), spans);
    }

    /// <summary>
    ///     将标记位置应用到诊断断言，避免测试依赖硬编码行列号。
    /// </summary>
    /// <param name="diagnosticResult">要补全定位信息的诊断断言。</param>
    /// <param name="markerId">标记编号。</param>
    /// <returns>包含定位信息的诊断断言。</returns>
    /// <exception cref="KeyNotFoundException">指定标记不存在时抛出。</exception>
    public DiagnosticResult WithSpan(
        DiagnosticResult diagnosticResult,
        string markerId)
    {
        var span = _spans[markerId];
        var lineSpan = _sourceText.Lines.GetLinePositionSpan(span);

        return diagnosticResult.WithSpan(
            lineSpan.Start.Line + 1,
            lineSpan.Start.Character + 1,
            lineSpan.End.Line + 1,
            lineSpan.End.Character + 1);
    }

    private static bool StartsWithMarker(
        string text,
        int index)
    {
        return index + 3 < text.Length &&
               text[index] == '{' &&
               text[index + 1] == '|' &&
               text[index + 2] == '#';
    }

    private static bool EndsWithMarker(
        string text,
        int index)
    {
        return index + 1 < text.Length &&
               text[index] == '|' &&
               text[index + 1] == '}';
    }
}