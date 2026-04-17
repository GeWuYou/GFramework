using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Game.SourceGenerators.Diagnostics;

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

    /// <summary>
    ///     schema 字段名无法安全映射为 C# 标识符。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidGeneratedIdentifier = new(
        "GF_ConfigSchema_006",
        "Config schema property name cannot be converted to a valid C# identifier",
        "Property '{1}' in schema file '{0}' uses schema key '{2}', which generates invalid C# identifier '{3}'",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 顶层自定义配置目录元数据无效。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidConfigRelativePathMetadata = new(
        "GF_ConfigSchema_007",
        "Config schema uses invalid custom config path metadata",
        "Schema file '{0}' uses invalid '{1}' metadata: {2}",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 字段的查询索引元数据无效。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidLookupIndexMetadata = new(
        "GF_ConfigSchema_008",
        "Config schema uses invalid lookup index metadata",
        "Property '{1}' in schema file '{0}' uses invalid '{2}' metadata: {3}",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 字段的字符串 format 元数据无效。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidStringFormatMetadata = new(
        "GF_ConfigSchema_009",
        "Config schema uses invalid string format metadata",
        "Property '{1}' in schema file '{0}' uses invalid 'format' metadata: {2}",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 对象节点的 dependentRequired 元数据无效。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidDependentRequiredMetadata = new(
        "GF_ConfigSchema_010",
        "Config schema uses invalid dependentRequired metadata",
        "Property '{1}' in schema file '{0}' uses invalid 'dependentRequired' metadata: {2}",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);

    /// <summary>
    ///     schema 对象节点的 dependentSchemas 元数据无效。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidDependentSchemasMetadata = new(
        "GF_ConfigSchema_011",
        "Config schema uses invalid dependentSchemas metadata",
        "Property '{1}' in schema file '{0}' uses invalid 'dependentSchemas' metadata: {2}",
        SourceGeneratorsConfigCategory,
        DiagnosticSeverity.Error,
        true);
}
