namespace GFramework.Game.Config;

/// <summary>
///     表示单个 YAML 文件中提取出的跨表引用。
///     该模型保留源文件、字段路径和目标表等诊断信息，以便加载器在批量校验失败时给出可定位的错误。
/// </summary>
internal sealed class YamlConfigReferenceUsage
{
    /// <summary>
    ///     初始化一个跨表引用使用记录。
    /// </summary>
    /// <param name="yamlPath">源 YAML 文件路径。</param>
    /// <param name="schemaPath">定义该引用的 schema 文件路径。</param>
    /// <param name="propertyPath">声明引用的字段路径。</param>
    /// <param name="rawValue">YAML 中的原始标量值。</param>
    /// <param name="referencedTableName">目标配置表名称。</param>
    /// <param name="valueType">引用值的 schema 标量类型。</param>
    public YamlConfigReferenceUsage(
        string yamlPath,
        string schemaPath,
        string propertyPath,
        string rawValue,
        string referencedTableName,
        YamlConfigSchemaPropertyType valueType)
    {
        ArgumentNullException.ThrowIfNull(yamlPath);
        ArgumentNullException.ThrowIfNull(schemaPath);
        ArgumentNullException.ThrowIfNull(propertyPath);
        ArgumentNullException.ThrowIfNull(rawValue);
        ArgumentNullException.ThrowIfNull(referencedTableName);

        YamlPath = yamlPath;
        SchemaPath = schemaPath;
        PropertyPath = propertyPath;
        RawValue = rawValue;
        ReferencedTableName = referencedTableName;
        ValueType = valueType;
    }

    /// <summary>
    ///     获取源 YAML 文件路径。
    /// </summary>
    public string YamlPath { get; }

    /// <summary>
    ///     获取定义该引用的 schema 文件路径。
    /// </summary>
    public string SchemaPath { get; }

    /// <summary>
    ///     获取声明引用的字段路径。
    /// </summary>
    public string PropertyPath { get; }

    /// <summary>
    ///     获取 YAML 中的原始标量值。
    /// </summary>
    public string RawValue { get; }

    /// <summary>
    ///     获取目标配置表名称。
    /// </summary>
    public string ReferencedTableName { get; }

    /// <summary>
    ///     获取引用值的 schema 标量类型。
    /// </summary>
    public YamlConfigSchemaPropertyType ValueType { get; }

    /// <summary>
    ///     获取便于诊断显示的字段路径。
    /// </summary>
    public string DisplayPath => PropertyPath;
}
