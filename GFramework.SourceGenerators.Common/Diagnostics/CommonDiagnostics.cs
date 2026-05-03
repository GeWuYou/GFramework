// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.SourceGenerators.Common.Diagnostics;

/// <summary>
///     提供通用诊断描述符的静态类
/// </summary>
public static class CommonDiagnostics
{
    /// <summary>
    ///     定义类必须为partial的诊断描述符
    /// </summary>
    /// <remarks>
    ///     诊断ID: GF001
    ///     诊断消息: "Class '{0}' must be declared partial for code generation"
    ///     分类: GFramework.Common
    ///     严重性: Error
    ///     是否启用: true
    /// </remarks>
    public static readonly DiagnosticDescriptor ClassMustBePartial =
        new(
            "GF_Common_Class_001",
            "Class must be partial",
            "Class '{0}' must be declared partial for code generation",
            "GFramework.Common",
            DiagnosticSeverity.Error,
            true
        );

    /// <summary>
    ///     定义生成方法名与用户代码冲突的诊断描述符。
    /// </summary>
    /// <remarks>
    ///     该诊断用于保护生成器保留的方法名，避免用户代码手动声明了相同零参数方法时出现重复成员错误，
    ///     并使多个生成器可以复用同一条一致的冲突报告规则。
    /// </remarks>
    public static readonly DiagnosticDescriptor GeneratedMethodNameConflict =
        new(
            "GF_Common_Class_002",
            "Generated method name conflicts with an existing member",
            "Class '{0}' already defines method '{1}()', which conflicts with generated code",
            "GFramework.Common",
            DiagnosticSeverity.Error,
            true
        );

    /// <summary>
    ///     定义源代码生成器跟踪信息的诊断描述符
    /// </summary>
    /// <remarks>
    ///     诊断ID: GF_Common_Trace_001
    ///     诊断消息: "{0}"
    ///     分类: GFramework.Trace
    ///     严重性: Info
    ///     是否启用: true
    /// </remarks>
    public static readonly DiagnosticDescriptor GeneratorTrace =
        new(
            "GF_Common_Trace_001",
            "Source generator trace",
            "{0}",
            "GFramework.Trace",
            DiagnosticSeverity.Info,
            true
        );

    /// <summary>
    ///     源代码生成器跟踪信息
    /// </summary>
    public static void Trace(SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            GeneratorTrace,
            Location.None,
            message));
    }
}