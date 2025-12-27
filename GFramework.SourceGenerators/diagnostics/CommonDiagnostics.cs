using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.diagnostics;

/// <summary>
/// 提供通用诊断描述符的静态类
/// </summary>
public static class CommonDiagnostics
{
    /// <summary>
    /// 定义类必须为partial的诊断描述符
    /// </summary>
    /// <remarks>
    /// 诊断ID: GF001
    /// 诊断消息: "Class '{0}' must be declared partial for code generation"
    /// 分类: GFramework.Common
    /// 严重性: Error
    /// 是否启用: true
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
}