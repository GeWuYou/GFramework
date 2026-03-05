using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.diagnostics;

/// <summary>
/// Priority 特性相关的诊断信息
/// </summary>
internal static class PriorityDiagnostic
{
    private const string Category = "GFramework.Priority";

    /// <summary>
    /// GF_Priority_001: Priority 特性只能应用于类
    /// </summary>
    public static readonly DiagnosticDescriptor OnlyApplyToClass = new(
        id: "GF_Priority_001",
        title: "Priority 特性只能应用于类",
        messageFormat: "Priority 特性只能应用于类，不能应用于 '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Priority 特性设计用于类级别的优先级标记，不支持其他类型。"
    );

    /// <summary>
    /// GF_Priority_002: 类已手动实现 IPrioritized 接口
    /// </summary>
    public static readonly DiagnosticDescriptor AlreadyImplemented = new(
        id: "GF_Priority_002",
        title: "类已实现 IPrioritized 接口",
        messageFormat: "类 '{0}' 已手动实现 IPrioritized 接口，将跳过自动生成",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "当类已经手动实现 IPrioritized 接口时，源生成器将跳过代码生成以避免冲突。"
    );

    /// <summary>
    /// GF_Priority_003: 类必须声明为 partial
    /// </summary>
    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "GF_Priority_003",
        title: "类必须声明为 partial",
        messageFormat: "类 '{0}' 使用了 Priority 特性，必须声明为 partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "源生成器需要在 partial 类中生成 IPrioritized 接口实现。"
    );

    /// <summary>
    /// GF_Priority_004: Priority 值缺失或无效
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidValue = new(
        id: "GF_Priority_004",
        title: "Priority 值无效",
        messageFormat: "Priority 特性的值无效或缺失",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Priority 特性必须提供一个有效的整数值。"
    );
}