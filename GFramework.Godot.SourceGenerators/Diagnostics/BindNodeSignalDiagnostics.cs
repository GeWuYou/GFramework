// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

/// <summary>
///     BindNodeSignal 生成器相关诊断。
/// </summary>
public static class BindNodeSignalDiagnostics
{
    /// <summary>
    ///     嵌套类型不受支持。
    /// </summary>
    public static readonly DiagnosticDescriptor NestedClassNotSupported =
        new(
            "GF_Godot_BindNodeSignal_001",
            "Nested classes are not supported",
            "Class '{0}' cannot use [BindNodeSignal] inside a nested type",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     static 方法不受支持。
    /// </summary>
    public static readonly DiagnosticDescriptor StaticMethodNotSupported =
        new(
            "GF_Godot_BindNodeSignal_002",
            "Static methods are not supported",
            "Method '{0}' cannot be static when using [BindNodeSignal]",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     节点字段不存在。
    /// </summary>
    public static readonly DiagnosticDescriptor NodeFieldNotFound =
        new(
            "GF_Godot_BindNodeSignal_003",
            "Referenced node field was not found",
            "Method '{0}' references node field '{1}', but no matching field exists on class '{2}'",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     节点字段必须是实例字段。
    /// </summary>
    public static readonly DiagnosticDescriptor NodeFieldMustBeInstanceField =
        new(
            "GF_Godot_BindNodeSignal_004",
            "Referenced node field must be an instance field",
            "Method '{0}' references node field '{1}', but that field must be an instance field",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     字段类型必须继承自 Godot.Node。
    /// </summary>
    public static readonly DiagnosticDescriptor FieldTypeMustDeriveFromNode =
        new(
            "GF_Godot_BindNodeSignal_005",
            "Field type must derive from Godot.Node",
            "Field '{0}' must be a Godot.Node type to use [BindNodeSignal]",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     目标事件不存在。
    /// </summary>
    public static readonly DiagnosticDescriptor SignalNotFound =
        new(
            "GF_Godot_BindNodeSignal_006",
            "Referenced event was not found",
            "Field '{0}' does not contain an event named '{1}'",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     方法签名与事件委托不兼容。
    /// </summary>
    public static readonly DiagnosticDescriptor MethodSignatureNotCompatible =
        new(
            "GF_Godot_BindNodeSignal_007",
            "Method signature is not compatible with the referenced event",
            "Method '{0}' is not compatible with event '{1}' on field '{2}'",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);

    /// <summary>
    ///     现有 _Ready 中未调用生成绑定逻辑。
    /// </summary>
    public static readonly DiagnosticDescriptor ManualReadyHookRequired =
        new(
            "GF_Godot_BindNodeSignal_008",
            "Call generated signal binding from _Ready",
            "Class '{0}' defines _Ready(); call __BindNodeSignals_Generated() there to bind [BindNodeSignal] handlers",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>
    ///     现有 _ExitTree 中未调用生成解绑逻辑。
    /// </summary>
    public static readonly DiagnosticDescriptor ManualExitTreeHookRequired =
        new(
            "GF_Godot_BindNodeSignal_009",
            "Call generated signal unbinding from _ExitTree",
            "Class '{0}' defines _ExitTree(); call __UnbindNodeSignals_Generated() there to unbind [BindNodeSignal] handlers",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Warning,
            true);

    /// <summary>
    ///     BindNodeSignalAttribute 构造参数无效。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidConstructorArgument =
        new(
            "GF_Godot_BindNodeSignal_010",
            "BindNodeSignal attribute arguments are invalid",
            "Method '{0}' uses [BindNodeSignal] with an invalid '{1}' constructor argument; it must be a non-empty string literal",
            PathContests.GodotNamespace,
            DiagnosticSeverity.Error,
            true);
}