// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

#nullable enable
namespace GFramework.Godot.SourceGenerators.Abstractions;

/// <summary>
///     节点路径的查找模式。
/// </summary>
public enum NodeLookupMode
{
    /// <summary>
    ///     自动推断。未显式设置路径时默认按唯一名查找。
    /// </summary>
    Auto = 0,

    /// <summary>
    ///     按唯一名查找，对应 Godot 的 %Name 语法。
    /// </summary>
    UniqueName = 1,

    /// <summary>
    ///     按相对路径查找。
    /// </summary>
    RelativePath = 2,

    /// <summary>
    ///     按绝对路径查找。
    /// </summary>
    AbsolutePath = 3
}