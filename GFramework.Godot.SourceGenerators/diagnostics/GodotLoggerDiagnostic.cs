using Microsoft.CodeAnalysis;

namespace GFramework.Godot.SourceGenerators.diagnostics;

/// <summary>
///     提供诊断描述符的静态类，用于GFramework日志生成器的编译时检查
/// </summary>
internal static class GodotLoggerDiagnostics
{
    /// <summary>
    ///     诊断描述符：标识GodotLogAttribute无法在指定类上生成Logger
    /// </summary>
    /// <remarks>
    ///     ID: GFW_LOG001
    ///     严重性: Warning
    ///     分类: GFramework.Godot.Logging
    /// </remarks>
    public static readonly DiagnosticDescriptor LogAttributeInvalid = new(
        "GF_Godot_Logging_001",
        "GodotLogAttribute cannot generate Logger",
        "GodotLogAttribute on class '{0}' is ineffective: {1}",
        "GFramework.Godot.Logging",
        DiagnosticSeverity.Warning,
        true);
}