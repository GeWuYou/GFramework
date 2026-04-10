namespace GFramework.Godot.Config;

/// <summary>
///     描述一个 Godot YAML 配置表在资源目录中的来源信息。
/// </summary>
public sealed class GodotYamlConfigTableSource
{
    /// <summary>
    ///     初始化一个配置表来源描述。
    /// </summary>
    /// <param name="tableName">运行时表名称。</param>
    /// <param name="configRelativePath">相对配置根目录的 YAML 目录。</param>
    /// <param name="schemaRelativePath">相对配置根目录的 schema 文件路径；未启用 schema 时为空。</param>
    public GodotYamlConfigTableSource(
        string tableName,
        string configRelativePath,
        string? schemaRelativePath = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(configRelativePath))
        {
            throw new ArgumentException("Config relative path cannot be null or whitespace.",
                nameof(configRelativePath));
        }

        if (schemaRelativePath != null && string.IsNullOrWhiteSpace(schemaRelativePath))
        {
            throw new ArgumentException(
                "Schema relative path cannot be empty or whitespace when provided.",
                nameof(schemaRelativePath));
        }

        TableName = tableName;
        ConfigRelativePath = configRelativePath;
        SchemaRelativePath = schemaRelativePath;
    }

    /// <summary>
    ///     获取运行时表名称。
    /// </summary>
    public string TableName { get; }

    /// <summary>
    ///     获取相对配置根目录的 YAML 目录路径。
    /// </summary>
    public string ConfigRelativePath { get; }

    /// <summary>
    ///     获取相对配置根目录的 schema 文件路径；未启用 schema 校验时为空。
    /// </summary>
    public string? SchemaRelativePath { get; }
}
