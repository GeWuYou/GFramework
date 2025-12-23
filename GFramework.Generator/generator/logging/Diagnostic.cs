using Microsoft.CodeAnalysis;

namespace GFramework.Generator.generator.logging;

/// <summary>
/// 提供诊断描述符的静态类，用于GFramework日志生成器的编译时检查
/// </summary>
internal static class Diagnostics
{

    /// <summary>
    /// 定义诊断描述符：要求使用[Log]特性的类必须声明为partial
    /// </summary>
    /// <remarks>
    /// 当类使用[Log]特性但未声明为partial时，编译器将报告此错误
    /// </remarks>
    public static readonly DiagnosticDescriptor MustBePartial =
        new(
            id: "GFLOG001",
            title: "Class must be partial",
            messageFormat: "Class '{0}' must be declared partial to use [Log]",
            category: "GFramework.Logging",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    
    /// <summary>
    /// 定义诊断描述符：LogAttribute无法生成Logger的错误情况
    /// </summary>
    public static readonly DiagnosticDescriptor LogAttributeInvalid =
        new(
            id: "GFW_LOG001",
            title: "LogAttribute 无法生成 Logger",
            messageFormat: "类 '{0}' 上的 LogAttribute 无法生效：{1}",
            category: "GFramework.Logging",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

}
