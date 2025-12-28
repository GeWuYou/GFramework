using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.diagnostics;

/// <summary>
///     提供与上下文感知相关的诊断规则定义
/// </summary>
public static class ContextAwareDiagnostic
{
    /// <summary>
    ///     定义类必须实现IContextAware接口的诊断规则
    /// </summary>
    /// <remarks>
    ///     诊断ID: GF_Rule_001
    ///     诊断类别: GFramework.SourceGenerators.rule
    ///     严重级别: 错误
    ///     启用状态: true
    ///     消息格式: "Class '{0}' must implement IContextAware"
    /// </remarks>
    public static readonly DiagnosticDescriptor ClassMustImplementIContextAware = new(
        "GF_Rule_001",
        "Class must implement IContextAware",
        "Class '{0}' must implement IContextAware",
        "GFramework.SourceGenerators.rule",
        DiagnosticSeverity.Error,
        true
    );
}