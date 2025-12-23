using Microsoft.CodeAnalysis;

namespace GFramework.Generator.generator.logging;

/// <summary>
/// 提供诊断描述符的静态类，用于GFramework日志生成器的编译时检查
/// </summary>
internal static class Diagnostics
{
    /// <summary>
    /// 定义一个诊断描述符，用于检查使用[Log]特性的类是否声明为partial
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
}
