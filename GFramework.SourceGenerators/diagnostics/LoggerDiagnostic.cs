using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.diagnostics;

/// <summary>
///     提供诊断描述符的静态类，用于GFramework日志生成器的编译时检查
/// </summary>
internal static class LoggerDiagnostics
{
    /// <summary>
    ///     定义诊断描述符：LogAttribute无法生成Logger的错误情况
    /// </summary>
    public static readonly DiagnosticDescriptor LogAttributeInvalid =
        new(
            "GF_Logging_001",
            "LogAttribute cannot generate Logger",
            "LogAttribute on class '{0}' is ineffective: {1}",
            "GFramework.Godot.logging",
            DiagnosticSeverity.Warning,
            true);
}