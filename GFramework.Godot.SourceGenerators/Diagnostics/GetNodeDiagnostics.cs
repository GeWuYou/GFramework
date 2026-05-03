// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;
using Microsoft.CodeAnalysis;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

/// <summary>
///     GetNode 生成器相关诊断。
/// </summary>
public static class GetNodeDiagnostics
{
    /// <summary>
    ///     嵌套类型不受支持。
    /// </summary>
    public static readonly DiagnosticDescriptor NestedClassNotSupported =
        new(
            "GF_Godot_GetNode_001",
            "Nested classes are not supported",
            "Class '{0}' cannot use [GetNode] inside a nested type",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     static 字段不受支持。
    /// </summary>
    public static readonly DiagnosticDescriptor StaticFieldNotSupported =
        new(
            "GF_Godot_GetNode_002",
            "Static fields are not supported",
            "Field '{0}' cannot be static when using [GetNode]",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     readonly 字段不受支持。
    /// </summary>
    public static readonly DiagnosticDescriptor ReadOnlyFieldNotSupported =
        new(
            "GF_Godot_GetNode_003",
            "Readonly fields are not supported",
            "Field '{0}' cannot be readonly when using [GetNode]",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     字段类型必须继承自 Godot.Node。
    /// </summary>
    public static readonly DiagnosticDescriptor FieldTypeMustDeriveFromNode =
        new(
            "GF_Godot_GetNode_004",
            "Field type must derive from Godot.Node",
            "Field '{0}' must be a Godot.Node type to use [GetNode]",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     无法从字段名推导路径。
    /// </summary>
    public static readonly DiagnosticDescriptor CannotInferNodePath =
        new(
            "GF_Godot_GetNode_005",
            "Cannot infer node path",
            "Field '{0}' does not provide a path and its name cannot be converted to a node path",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     现有 _Ready 中未调用生成注入逻辑。
    /// </summary>
    public static readonly DiagnosticDescriptor ManualReadyHookRequired =
        new(
            "GF_Godot_GetNode_006",
            "Call generated injection from _Ready",
            "Class '{0}' defines _Ready(); call __InjectGetNodes_Generated() there or remove _Ready() to use the generated hook",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Warning,
            true);
}