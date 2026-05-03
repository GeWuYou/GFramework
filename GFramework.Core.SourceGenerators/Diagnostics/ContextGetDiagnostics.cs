// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Core.SourceGenerators.Diagnostics;

/// <summary>
///     提供 Context Get 注入生成器相关诊断。
/// </summary>
public static class ContextGetDiagnostics
{
    private const string SourceGeneratorsRuleCategory = $"{PathContests.SourceGeneratorsPath}.Rule";

    /// <summary>
    ///     不支持在嵌套类中生成注入代码。
    /// </summary>
    public static readonly DiagnosticDescriptor NestedClassNotSupported = new(
        "GF_ContextGet_001",
        "Context Get injection does not support nested classes",
        "Class '{0}' cannot use context Get injection inside a nested type",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     带注入语义的字段不能是静态字段。
    /// </summary>
    public static readonly DiagnosticDescriptor StaticFieldNotSupported = new(
        "GF_ContextGet_002",
        "Static field is not supported for context Get injection",
        "Field '{0}' cannot be static when using generated context Get injection",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     带注入语义的字段不能是只读字段。
    /// </summary>
    public static readonly DiagnosticDescriptor ReadOnlyFieldNotSupported = new(
        "GF_ContextGet_003",
        "Readonly field is not supported for context Get injection",
        "Field '{0}' cannot be readonly when using generated context Get injection",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     使用 <c>[GetAll]</c> 时，静态字段会被跳过且不会生成注入赋值。
    /// </summary>
    public static readonly DiagnosticDescriptor GetAllStaticFieldSkipped = new(
        "GF_ContextGet_007",
        "Static field will be skipped by [GetAll] context Get injection",
        "Field '{0}' is static and will be skipped by [GetAll] context Get injection generation",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     使用 <c>[GetAll]</c> 时，只读字段会被跳过且不会生成注入赋值。
    /// </summary>
    public static readonly DiagnosticDescriptor GetAllReadOnlyFieldSkipped = new(
        "GF_ContextGet_008",
        "Readonly field will be skipped by [GetAll] context Get injection",
        "Field '{0}' is readonly and will be skipped by [GetAll] context Get injection generation",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     字段类型与注入特性不匹配。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidBindingType = new(
        "GF_ContextGet_004",
        "Field type is not valid for the selected context Get attribute",
        "Field '{0}' type '{1}' is not valid for [{2}]",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     使用 Context Get 注入的类型必须是上下文感知类型。
    /// </summary>
    public static readonly DiagnosticDescriptor ContextAwareTypeRequired = new(
        "GF_ContextGet_005",
        "Context-aware type is required",
        "Class '{0}' must be context-aware to use generated context Get injection",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     一个字段不允许同时声明多个 Context Get 特性。
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleBindingAttributesNotSupported = new(
        "GF_ContextGet_006",
        "Multiple context Get attributes are not supported on the same field",
        "Field '{0}' cannot declare multiple generated context Get attributes",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Error,
        true);
}
