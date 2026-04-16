using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Core.SourceGenerators.Diagnostics;

/// <summary>
///     提供 Context Get 注册可见性分析相关诊断。
/// </summary>
public static class ContextRegistrationDiagnostics
{
    private const string SourceGeneratorsRuleCategory = $"{PathContests.SourceGeneratorsPath}.Rule";

    /// <summary>
    ///     当模型使用点在所属架构中找不到静态可见注册时报告。
    /// </summary>
    public static readonly DiagnosticDescriptor ModelRegistrationMissing = new(
        "GF_ContextRegistration_001",
        "Model usage has no statically discoverable registration",
        "Model '{0}' used by '{1}' is not statically registered in architecture '{2}'",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     当系统使用点在所属架构中找不到静态可见注册时报告。
    /// </summary>
    public static readonly DiagnosticDescriptor SystemRegistrationMissing = new(
        "GF_ContextRegistration_002",
        "System usage has no statically discoverable registration",
        "System '{0}' used by '{1}' is not statically registered in architecture '{2}'",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     当工具使用点在所属架构中找不到静态可见注册时报告。
    /// </summary>
    public static readonly DiagnosticDescriptor UtilityRegistrationMissing = new(
        "GF_ContextRegistration_003",
        "Utility usage has no statically discoverable registration",
        "Utility '{0}' used by '{1}' is not statically registered in architecture '{2}'",
        SourceGeneratorsRuleCategory,
        DiagnosticSeverity.Warning,
        true);
}
