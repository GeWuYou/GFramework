using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.logging;

/// <summary>
///     提供诊断描述符的静态类，用于GFramework日志生成器的编译时检查
/// </summary>
internal static class LoggerDiagnostics
{
    /// <summary>
    ///     定义诊断描述符：要求使用[Log]特性的类必须声明为partial
    /// </summary>
    /// <remarks>
    ///     当类使用[Log]特性但未声明为partial时，编译器将报告此错误
    /// </remarks>
    public static readonly DiagnosticDescriptor MustBePartial =
        new(
            "GFLOG001",
            "Class must be partial",
            "Class '{0}' must be declared partial to use [Log]",
            "GFramework.Logging",
            DiagnosticSeverity.Error,
            true
        );

    /// <summary>
    ///     定义诊断描述符：LogAttribute无法生成Logger的错误情况
    /// </summary>
    public static readonly DiagnosticDescriptor LogAttributeInvalid =
        new(
            "GFW_LOG001",
            "LogAttribute cannot generate Logger",
            "LogAttribute on class '{0}' is ineffective: {1}",
            "GFramework.Godot.Logging",
            DiagnosticSeverity.Warning,
            true);
}