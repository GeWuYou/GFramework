// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Godot.SourceGenerators.Diagnostics;

/// <summary>
///     定义行为类自动生成器使用的诊断描述符。
/// </summary>
/// <remarks>
///     这些规则覆盖 <c>AutoScene</c> 与 <c>AutoUiPage</c> 等行为生成器的常见使用约束，
///     以便在生成被跳过前向调用方报告明确的失败原因。
/// </remarks>
internal static class AutoBehaviorDiagnostics
{
    private const string Category = $"{PathContests.GodotNamespace}.SourceGenerators.Behavior";

    /// <summary>
    ///     报告行为生成器不支持在嵌套类型上运行。
    /// </summary>
    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        "GF_AutoBehavior_001",
        "Auto behavior generators do not support nested classes",
        "Generator '{0}' does not support nested class '{1}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告目标类型没有继承生成器要求的 Godot 基类。
    /// </summary>
    public static readonly DiagnosticDescriptor MissingBaseType = new(
        "GF_AutoBehavior_002",
        "Auto behavior generators require a compatible base type",
        "Type '{0}' must inherit from '{1}' to use '{2}'",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告 UI 页面声明中使用了不存在的 <c>UiLayer</c> 名称。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidUiLayerName = new(
        "GF_AutoBehavior_003",
        "Unknown UiLayer name",
        "Ui layer '{0}' on '{1}' does not exist on GFramework.Game.Abstractions.Enums.UiLayer",
        Category,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     报告行为生成器特性参数不满足约定签名，导致生成器无法推导所需元数据。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAttributeArguments = new(
        "GF_AutoBehavior_004",
        "Auto behavior attribute arguments are invalid",
        "Attribute '{0}' on '{1}' must provide {2}",
        Category,
        DiagnosticSeverity.Error,
        true);
}
