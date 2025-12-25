using Microsoft.CodeAnalysis;

namespace GFramework.Godot.SourceGenerators.logging;

/// <summary>
/// 提供诊断描述符的静态类，用于GFramework日志生成器的编译时检查
/// </summary>
internal static class GodotLoggerDiagnostics
{
    /// <summary>
    /// 诊断描述符：标识使用[GodotLog]特性的类必须声明为partial
    /// </summary>
    /// <remarks>
    /// ID: GFLOG001
    /// 严重性: Error
    /// 分类: GFramework.Godot.Logging
    /// </remarks>
    public static readonly DiagnosticDescriptor MustBePartial =
        new(
            id: "GFLOG001",
            title: "Class must be partial",
            messageFormat: "Class '{0}' must be declared as partial to use [GodotLog]",
            category: "GFramework.Godot.Logging",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


    /// <summary>
    /// 诊断描述符：标识GodotLogAttribute无法在指定类上生成Logger
    /// </summary>
    /// <remarks>
    /// ID: GFW_LOG001
    /// 严重性: Warning
    /// 分类: GFramework.Godot.Logging
    /// </remarks>
    public static readonly DiagnosticDescriptor LogAttributeInvalid = new(
        id: "GFW_LOG001",
        title: "GodotLogAttribute cannot generate Logger",
        messageFormat: "GodotLogAttribute on class '{0}' is ineffective: {1}",
        category: "GFramework.Godot.Logging",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}