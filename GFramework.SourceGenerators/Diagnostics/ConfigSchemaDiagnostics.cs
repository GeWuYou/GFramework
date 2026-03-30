using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.SourceGenerators.Diagnostics;

/// <summary>
///     提供配置 schema 代码生成相关诊断。
/// </summary>
public static class ConfigSchemaDiagnostics
{
    private const string SourceGeneratorsConfigCategory = $"{PathContests.SourceGeneratorsPath}.Config";

    /// <summary>
    ///     schema JSON 无法解析。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidSchemaJson = new(
        "GF_ConfigSchema_001",
        "Config schema JSON is invalid",
        "Schema file '{0}' could not be parsed: {1}",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 顶层必须是 object。
    /// </summary>
    public static readonly DiagnosticDescriptor RootObjectSchemaRequired = new(
        "GF_ConfigSchema_002",
        "Config schema root must describe an object",
        "Schema file '{0}' must declare a root object schema",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 必须声明 id 字段作为主键。
    /// </summary>
    public static readonly DiagnosticDescriptor IdPropertyRequired = new(
        "GF_ConfigSchema_003",
        "Config schema must declare an id property",
        "Schema file '{0}' must declare a required 'id' property for table generation",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 包含暂不支持的字段类型。
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new(
        "GF_ConfigSchema_004",
        "Config schema contains an unsupported property type",
        "Property '{1}' in schema file '{0}' uses unsupported type '{2}'",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 的 id 字段类型不支持作为主键。
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedKeyType = new(
        "GF_ConfigSchema_005",
        "Config schema uses an unsupported key type",
        "Schema file '{0}' uses unsupported id type '{1}'. Supported key types are 'integer' and 'string'.",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);
}