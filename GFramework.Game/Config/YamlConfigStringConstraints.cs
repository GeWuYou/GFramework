// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace GFramework.Game.Config;

/// <summary>
///     表示标量节点上声明的字符串长度、模式与 format 约束。
///     该模型将正则原文、预编译正则和共享 format 枚举绑定保存，
///     保证诊断内容与运行时匹配逻辑保持一致。
/// </summary>
internal sealed class YamlConfigStringConstraints
{
    /// <summary>
    ///     初始化字符串约束模型。
    /// </summary>
    /// <param name="minLength">最小长度约束。</param>
    /// <param name="maxLength">最大长度约束。</param>
    /// <param name="pattern">正则模式约束原文。</param>
    /// <param name="patternRegex">已编译的正则表达式。</param>
    /// <param name="formatConstraint">字符串 format 约束。</param>
    /// <exception cref="ArgumentException">当 <paramref name="pattern"/> 与 <paramref name="patternRegex"/> 未成对出现时抛出。</exception>
    public YamlConfigStringConstraints(
        int? minLength,
        int? maxLength,
        string? pattern,
        Regex? patternRegex,
        YamlConfigStringFormatConstraint? formatConstraint)
    {
        if ((pattern is null) != (patternRegex is null))
        {
            throw new ArgumentException("pattern 与 patternRegex 必须同时为空或同时提供。", nameof(pattern));
        }

        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = pattern;
        PatternRegex = patternRegex;
        FormatConstraint = formatConstraint;
    }

    /// <summary>
    ///     获取最小长度约束。
    /// </summary>
    public int? MinLength { get; }

    /// <summary>
    ///     获取最大长度约束。
    /// </summary>
    public int? MaxLength { get; }

    /// <summary>
    ///     获取正则模式约束原文。
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    ///     获取已编译的正则表达式。
    /// </summary>
    public Regex? PatternRegex { get; }

    /// <summary>
    ///     获取字符串 format 约束。
    /// </summary>
    public YamlConfigStringFormatConstraint? FormatConstraint { get; }
}
