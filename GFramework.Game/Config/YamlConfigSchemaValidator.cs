using System.Globalization;
using System.IO;
using System.Text.Json;

namespace GFramework.Game.Config;

/// <summary>
///     提供 YAML 配置文件与 JSON Schema 之间的最小运行时校验能力。
///     该校验器与当前配置生成器支持的 schema 子集保持一致，
///     以便在配置进入运行时注册表之前就拒绝缺失字段、未知字段和基础类型错误。
/// </summary>
internal static class YamlConfigSchemaValidator
{
    /// <summary>
    ///     从磁盘加载并解析一个 JSON Schema 文件。
    /// </summary>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析后的 schema 模型。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="schemaPath" /> 为空时抛出。</exception>
    /// <exception cref="FileNotFoundException">当 schema 文件不存在时抛出。</exception>
    /// <exception cref="InvalidOperationException">当 schema 内容不符合当前运行时支持的子集时抛出。</exception>
    internal static async Task<YamlConfigSchema> LoadAsync(
        string schemaPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            throw new ArgumentException("Schema path cannot be null or whitespace.", nameof(schemaPath));
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema file '{schemaPath}' was not found.", schemaPath);
        }

        string schemaText;
        try
        {
            schemaText = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to read schema file '{schemaPath}'.", exception);
        }

        try
        {
            using var document = JsonDocument.Parse(schemaText);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeElement) ||
                !string.Equals(typeElement.GetString(), "object", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Schema file '{schemaPath}' must declare a root object schema.");
            }

            if (!root.TryGetProperty("properties", out var propertiesElement) ||
                propertiesElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Schema file '{schemaPath}' must declare an object-valued 'properties' section.");
            }

            var requiredProperties = new HashSet<string>(StringComparer.Ordinal);
            if (root.TryGetProperty("required", out var requiredElement) &&
                requiredElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in requiredElement.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var propertyName = item.GetString();
                    if (!string.IsNullOrWhiteSpace(propertyName))
                    {
                        requiredProperties.Add(propertyName);
                    }
                }
            }

            var properties = new Dictionary<string, YamlConfigSchemaProperty>(StringComparer.Ordinal);
            foreach (var property in propertiesElement.EnumerateObject())
            {
                cancellationToken.ThrowIfCancellationRequested();
                properties.Add(property.Name, ParseProperty(schemaPath, property));
            }

            return new YamlConfigSchema(schemaPath, properties, requiredProperties);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Schema file '{schemaPath}' contains invalid JSON.", exception);
        }
    }

    /// <summary>
    ///     使用已解析的 schema 校验 YAML 文本。
    /// </summary>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">YAML 文本内容。</param>
    /// <exception cref="ArgumentNullException">当参数为空时抛出。</exception>
    /// <exception cref="InvalidOperationException">当 YAML 内容与 schema 不匹配时抛出。</exception>
    internal static void Validate(
        YamlConfigSchema schema,
        string yamlPath,
        string yamlText)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(yamlPath);
        ArgumentNullException.ThrowIfNull(yamlText);

        YamlStream yamlStream = new();
        try
        {
            using var reader = new StringReader(yamlText);
            yamlStream.Load(reader);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Config file '{yamlPath}' could not be parsed as YAML before schema validation.",
                exception);
        }

        if (yamlStream.Documents.Count != 1 ||
            yamlStream.Documents[0].RootNode is not YamlMappingNode rootMapping)
        {
            throw new InvalidOperationException(
                $"Config file '{yamlPath}' must contain a single root mapping object.");
        }

        var seenProperties = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in rootMapping.Children)
        {
            if (entry.Key is not YamlScalarNode keyNode ||
                string.IsNullOrWhiteSpace(keyNode.Value))
            {
                throw new InvalidOperationException(
                    $"Config file '{yamlPath}' contains a non-scalar or empty top-level property name.");
            }

            var propertyName = keyNode.Value;
            if (!seenProperties.Add(propertyName))
            {
                throw new InvalidOperationException(
                    $"Config file '{yamlPath}' contains duplicate property '{propertyName}'.");
            }

            if (!schema.Properties.TryGetValue(propertyName, out var property))
            {
                throw new InvalidOperationException(
                    $"Config file '{yamlPath}' contains unknown property '{propertyName}' that is not declared in schema '{schema.SchemaPath}'.");
            }

            ValidateNode(yamlPath, propertyName, entry.Value, property);
        }

        foreach (var requiredProperty in schema.RequiredProperties)
        {
            if (!seenProperties.Contains(requiredProperty))
            {
                throw new InvalidOperationException(
                    $"Config file '{yamlPath}' is missing required property '{requiredProperty}' defined by schema '{schema.SchemaPath}'.");
            }
        }
    }

    private static YamlConfigSchemaProperty ParseProperty(string schemaPath, JsonProperty property)
    {
        if (!property.Value.TryGetProperty("type", out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"Property '{property.Name}' in schema file '{schemaPath}' must declare a string 'type'.");
        }

        var typeName = typeElement.GetString() ?? string.Empty;
        var propertyType = typeName switch
        {
            "integer" => YamlConfigSchemaPropertyType.Integer,
            "number" => YamlConfigSchemaPropertyType.Number,
            "boolean" => YamlConfigSchemaPropertyType.Boolean,
            "string" => YamlConfigSchemaPropertyType.String,
            "array" => YamlConfigSchemaPropertyType.Array,
            _ => throw new InvalidOperationException(
                $"Property '{property.Name}' in schema file '{schemaPath}' uses unsupported type '{typeName}'.")
        };

        if (propertyType != YamlConfigSchemaPropertyType.Array)
        {
            return new YamlConfigSchemaProperty(property.Name, propertyType, null);
        }

        if (!property.Value.TryGetProperty("items", out var itemsElement) ||
            itemsElement.ValueKind != JsonValueKind.Object ||
            !itemsElement.TryGetProperty("type", out var itemTypeElement) ||
            itemTypeElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"Array property '{property.Name}' in schema file '{schemaPath}' must declare an item type.");
        }

        var itemTypeName = itemTypeElement.GetString() ?? string.Empty;
        var itemType = itemTypeName switch
        {
            "integer" => YamlConfigSchemaPropertyType.Integer,
            "number" => YamlConfigSchemaPropertyType.Number,
            "boolean" => YamlConfigSchemaPropertyType.Boolean,
            "string" => YamlConfigSchemaPropertyType.String,
            _ => throw new InvalidOperationException(
                $"Array property '{property.Name}' in schema file '{schemaPath}' uses unsupported item type '{itemTypeName}'.")
        };

        return new YamlConfigSchemaProperty(property.Name, propertyType, itemType);
    }

    private static void ValidateNode(
        string yamlPath,
        string propertyName,
        YamlNode node,
        YamlConfigSchemaProperty property)
    {
        if (property.PropertyType == YamlConfigSchemaPropertyType.Array)
        {
            if (node is not YamlSequenceNode sequenceNode)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' in config file '{yamlPath}' must be an array.");
            }

            foreach (var item in sequenceNode.Children)
            {
                ValidateScalarNode(yamlPath, propertyName, item, property.ItemType!.Value, isArrayItem: true);
            }

            return;
        }

        ValidateScalarNode(yamlPath, propertyName, node, property.PropertyType, isArrayItem: false);
    }

    private static void ValidateScalarNode(
        string yamlPath,
        string propertyName,
        YamlNode node,
        YamlConfigSchemaPropertyType expectedType,
        bool isArrayItem)
    {
        if (node is not YamlScalarNode scalarNode)
        {
            var subject = isArrayItem
                ? $"Array item in property '{propertyName}'"
                : $"Property '{propertyName}'";
            throw new InvalidOperationException(
                $"{subject} in config file '{yamlPath}' must be a scalar value of type '{GetTypeName(expectedType)}'.");
        }

        var value = scalarNode.Value;
        if (value is null)
        {
            var subject = isArrayItem
                ? $"Array item in property '{propertyName}'"
                : $"Property '{propertyName}'";
            throw new InvalidOperationException(
                $"{subject} in config file '{yamlPath}' cannot be null when schema type is '{GetTypeName(expectedType)}'.");
        }

        var tag = scalarNode.Tag.ToString();
        var isValid = expectedType switch
        {
            YamlConfigSchemaPropertyType.String => IsStringScalar(tag),
            YamlConfigSchemaPropertyType.Integer => long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _),
            YamlConfigSchemaPropertyType.Number => double.TryParse(
                value,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out _),
            YamlConfigSchemaPropertyType.Boolean => bool.TryParse(value, out _),
            _ => false
        };

        if (isValid)
        {
            return;
        }

        var subjectName = isArrayItem
            ? $"Array item in property '{propertyName}'"
            : $"Property '{propertyName}'";
        throw new InvalidOperationException(
            $"{subjectName} in config file '{yamlPath}' must be of type '{GetTypeName(expectedType)}', but the current YAML scalar value is '{value}'.");
    }

    private static string GetTypeName(YamlConfigSchemaPropertyType type)
    {
        return type switch
        {
            YamlConfigSchemaPropertyType.Integer => "integer",
            YamlConfigSchemaPropertyType.Number => "number",
            YamlConfigSchemaPropertyType.Boolean => "boolean",
            YamlConfigSchemaPropertyType.String => "string",
            YamlConfigSchemaPropertyType.Array => "array",
            _ => type.ToString()
        };
    }

    private static bool IsStringScalar(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return true;
        }

        return !string.Equals(tag, "tag:yaml.org,2002:int", StringComparison.Ordinal) &&
               !string.Equals(tag, "tag:yaml.org,2002:float", StringComparison.Ordinal) &&
               !string.Equals(tag, "tag:yaml.org,2002:bool", StringComparison.Ordinal) &&
               !string.Equals(tag, "tag:yaml.org,2002:null", StringComparison.Ordinal);
    }
}

/// <summary>
///     表示已解析并可用于运行时校验的 JSON Schema。
///     该模型只保留当前运行时加载器真正需要的最小信息，以避免在游戏运行时引入完整 schema 引擎。
/// </summary>
internal sealed class YamlConfigSchema
{
    /// <summary>
    ///     初始化一个可用于运行时校验的 schema 模型。
    /// </summary>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="properties">Schema 属性定义。</param>
    /// <param name="requiredProperties">必填属性集合。</param>
    public YamlConfigSchema(
        string schemaPath,
        IReadOnlyDictionary<string, YamlConfigSchemaProperty> properties,
        IReadOnlyCollection<string> requiredProperties)
    {
        ArgumentNullException.ThrowIfNull(schemaPath);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(requiredProperties);

        SchemaPath = schemaPath;
        Properties = properties;
        RequiredProperties = requiredProperties;
    }

    /// <summary>
    ///     获取 schema 文件路径。
    /// </summary>
    public string SchemaPath { get; }

    /// <summary>
    ///     获取按属性名索引的 schema 属性定义。
    /// </summary>
    public IReadOnlyDictionary<string, YamlConfigSchemaProperty> Properties { get; }

    /// <summary>
    ///     获取 schema 声明的必填属性集合。
    /// </summary>
    public IReadOnlyCollection<string> RequiredProperties { get; }
}

/// <summary>
///     表示单个 schema 属性的最小运行时描述。
/// </summary>
internal sealed class YamlConfigSchemaProperty
{
    /// <summary>
    ///     初始化一个 schema 属性描述。
    /// </summary>
    /// <param name="name">属性名称。</param>
    /// <param name="propertyType">属性类型。</param>
    /// <param name="itemType">数组元素类型；仅当属性类型为数组时有效。</param>
    public YamlConfigSchemaProperty(
        string name,
        YamlConfigSchemaPropertyType propertyType,
        YamlConfigSchemaPropertyType? itemType)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        PropertyType = propertyType;
        ItemType = itemType;
    }

    /// <summary>
    ///     获取属性名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     获取属性类型。
    /// </summary>
    public YamlConfigSchemaPropertyType PropertyType { get; }

    /// <summary>
    ///     获取数组元素类型；非数组属性时返回空。
    /// </summary>
    public YamlConfigSchemaPropertyType? ItemType { get; }
}

/// <summary>
///     表示当前运行时 schema 校验器支持的属性类型。
/// </summary>
internal enum YamlConfigSchemaPropertyType
{
    /// <summary>
    ///     整数类型。
    /// </summary>
    Integer,

    /// <summary>
    ///     数值类型。
    /// </summary>
    Number,

    /// <summary>
    ///     布尔类型。
    /// </summary>
    Boolean,

    /// <summary>
    ///     字符串类型。
    /// </summary>
    String,

    /// <summary>
    ///     数组类型。
    /// </summary>
    Array
}