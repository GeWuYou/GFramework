// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示一个节点上声明的单个 <c>enum</c> 候选值。
///     该模型同时保留稳定比较键与原始 JSON 文本，分别供运行时匹配和诊断输出复用。
/// </summary>
internal sealed class YamlConfigAllowedValue
{
    /// <summary>
    ///     初始化一个枚举候选值模型。
    /// </summary>
    /// <param name="comparableValue">用于与 YAML 节点比较的稳定键。</param>
    /// <param name="displayValue">用于诊断输出的原始 JSON 文本。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="comparableValue"/> 或 <paramref name="displayValue"/> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="comparableValue"/> 虽然非空但仅包含空白字符，或 <paramref name="displayValue"/> 为空或仅包含空白字符时抛出。</exception>
    public YamlConfigAllowedValue(string comparableValue, string displayValue)
    {
        ArgumentNullException.ThrowIfNull(comparableValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayValue);
        if (comparableValue.Length > 0 &&
            string.IsNullOrWhiteSpace(comparableValue))
        {
            throw new ArgumentException("The value cannot be composed entirely of whitespace.", nameof(comparableValue));
        }

        ComparableValue = comparableValue;
        DisplayValue = displayValue;
    }

    /// <summary>
    ///     获取用于运行时比较的稳定键。
    /// </summary>
    public string ComparableValue { get; }

    /// <summary>
    ///     获取用于诊断输出的原始 JSON 文本。
    /// </summary>
    public string DisplayValue { get; }
}
