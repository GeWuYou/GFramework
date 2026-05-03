// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.SourceGenerators.Abstractions.Enums;

namespace GFramework.Game.Config;

/// <summary>
///     表示当前运行时 schema 校验器支持的属性类型。
/// </summary>
[GenerateEnumExtensions]
internal enum YamlConfigSchemaPropertyType
{
    /// <summary>
    ///     对象类型。
    /// </summary>
    Object,

    /// <summary>
    ///     整数类型。
    /// </summary>
    Integer,

    /// <summary>
    ///     数值类型。
    /// </summary>
    Number,

    /// <summary>
    ///     布尔类型。
    /// </summary>
    Boolean,

    /// <summary>
    ///     字符串类型。
    /// </summary>
    String,

    /// <summary>
    ///     数组类型。
    /// </summary>
    Array
}
