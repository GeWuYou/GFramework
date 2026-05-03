// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示一个已归一化的字符串 format 约束。
///     该模型同时保留 schema 原文与共享枚举，方便诊断信息稳定展示，又避免运行时校验反复解析字符串。
/// </summary>
internal sealed class YamlConfigStringFormatConstraint
{
    /// <summary>
    ///     初始化字符串 format 约束模型。
    /// </summary>
    /// <param name="schemaName">schema 中声明的 format 名称。</param>
    /// <param name="kind">归一化后的共享 format 枚举。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="schemaName"/> 为 <see langword="null" /> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="schemaName"/> 为空或仅包含空白字符时抛出。</exception>
    public YamlConfigStringFormatConstraint(
        string schemaName,
        YamlConfigStringFormatKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        SchemaName = schemaName;
        Kind = kind;
    }

    /// <summary>
    ///     获取 schema 中声明的 format 名称。
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     获取归一化后的共享 format 枚举。
    /// </summary>
    public YamlConfigStringFormatKind Kind { get; }
}
