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

            var referencedTableNames = properties.Values
                .Select(static property => property.ReferenceTableName)
                .Where(static tableName => !string.IsNullOrWhiteSpace(tableName))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return new YamlConfigSchema(schemaPath, properties, requiredProperties, referencedTableNames);
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
        ValidateAndCollectReferences(schema, yamlPath, yamlText);
    }

    /// <summary>
    ///     使用已解析的 schema 校验 YAML 文本，并提取声明过的跨表引用。
    ///     该方法让结构校验与引用采集共享同一份 YAML 解析结果，避免加载器重复解析同一文件。
    /// </summary>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">YAML 文本内容。</param>
    /// <returns>当前 YAML 文件中声明的跨表引用集合。</returns>
    /// <exception cref="ArgumentNullException">当参数为空时抛出。</exception>
    /// <exception cref="InvalidOperationException">当 YAML 内容与 schema 不匹配时抛出。</exception>
    internal static IReadOnlyList<YamlConfigReferenceUsage> ValidateAndCollectReferences(
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

        var references = new List<YamlConfigReferenceUsage>();
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

            ValidateNode(yamlPath, propertyName, entry.Value, property, references);
        }

        foreach (var requiredProperty in schema.RequiredProperties)
        {
            if (!seenProperties.Contains(requiredProperty))
            {
                throw new InvalidOperationException(
                    $"Config file '{yamlPath}' is missing required property '{requiredProperty}' defined by schema '{schema.SchemaPath}'.");
            }
        }

        return references;
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

        string? referenceTableName = null;
        if (property.Value.TryGetProperty("x-gframework-ref-table", out var referenceTableElement))
        {
            if (referenceTableElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    $"Property '{property.Name}' in schema file '{schemaPath}' must declare a string 'x-gframework-ref-table' value.");
            }

            referenceTableName = referenceTableElement.GetString();
            if (string.IsNullOrWhiteSpace(referenceTableName))
            {
                throw new InvalidOperationException(
                    $"Property '{property.Name}' in schema file '{schemaPath}' must declare a non-empty 'x-gframework-ref-table' value.");
            }
        }

        if (propertyType != YamlConfigSchemaPropertyType.Array)
        {
            EnsureReferenceKeywordIsSupported(schemaPath, property.Name, propertyType, null, referenceTableName);
            return new YamlConfigSchemaProperty(
                property.Name,
                propertyType,
                null,
                referenceTableName,
                ParseEnumValues(schemaPath, property.Name, property.Value, propertyType, "enum"),
                null);
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

        EnsureReferenceKeywordIsSupported(schemaPath, property.Name, propertyType, itemType, referenceTableName);
        return new YamlConfigSchemaProperty(
            property.Name,
            propertyType,
            itemType,
            referenceTableName,
            null,
            ParseEnumValues(schemaPath, property.Name, itemsElement, itemType, "items.enum"));
    }

    private static void EnsureReferenceKeywordIsSupported(
        string schemaPath,
        string propertyName,
        YamlConfigSchemaPropertyType propertyType,
        YamlConfigSchemaPropertyType? itemType,
        string? referenceTableName)
    {
        if (referenceTableName == null)
        {
            return;
        }

        if (propertyType == YamlConfigSchemaPropertyType.String ||
            propertyType == YamlConfigSchemaPropertyType.Integer)
        {
            return;
        }

        if (propertyType == YamlConfigSchemaPropertyType.Array &&
            (itemType == YamlConfigSchemaPropertyType.String || itemType == YamlConfigSchemaPropertyType.Integer))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' in schema file '{schemaPath}' uses 'x-gframework-ref-table', but only string, integer, or arrays of those scalar types can declare cross-table references.");
    }

    private static void ValidateNode(
        string yamlPath,
        string propertyName,
        YamlNode node,
        YamlConfigSchemaProperty property,
        ICollection<YamlConfigReferenceUsage> references)
    {
        if (property.PropertyType == YamlConfigSchemaPropertyType.Array)
        {
            if (node is not YamlSequenceNode sequenceNode)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' in config file '{yamlPath}' must be an array.");
            }

            for (var itemIndex = 0; itemIndex < sequenceNode.Children.Count; itemIndex++)
            {
                ValidateScalarNode(
                    yamlPath,
                    propertyName,
                    sequenceNode.Children[itemIndex],
                    property.ItemType!.Value,
                    property.ReferenceTableName,
                    property.ItemAllowedValues,
                    references,
                    isArrayItem: true,
                    itemIndex);
            }

            return;
        }

        ValidateScalarNode(
            yamlPath,
            propertyName,
            node,
            property.PropertyType,
            property.ReferenceTableName,
            property.AllowedValues,
            references,
            isArrayItem: false,
            itemIndex: null);
    }

    private static void ValidateScalarNode(
        string yamlPath,
        string propertyName,
        YamlNode node,
        YamlConfigSchemaPropertyType expectedType,
        string? referenceTableName,
        IReadOnlyCollection<string>? allowedValues,
        ICollection<YamlConfigReferenceUsage> references,
        bool isArrayItem,
        int? itemIndex)
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
            var normalizedValue = NormalizeScalarValue(expectedType, value);
            if (allowedValues != null &&
                allowedValues.Count > 0 &&
                !allowedValues.Contains(normalizedValue, StringComparer.Ordinal))
            {
                var enumSubject = isArrayItem
                    ? $"Array item in property '{propertyName}'"
                    : $"Property '{propertyName}'";
                throw new InvalidOperationException(
                    $"{enumSubject} in config file '{yamlPath}' must be one of [{string.Join(", ", allowedValues)}], but the current YAML scalar value is '{value}'.");
            }

            if (referenceTableName != null)
            {
                references.Add(
                    new YamlConfigReferenceUsage(
                        yamlPath,
                        propertyName,
                        itemIndex,
                        normalizedValue,
                        referenceTableName,
                        expectedType));
            }

            return;
        }

        var subjectName = isArrayItem
            ? $"Array item in property '{propertyName}'"
            : $"Property '{propertyName}'";
        throw new InvalidOperationException(
            $"{subjectName} in config file '{yamlPath}' must be of type '{GetTypeName(expectedType)}', but the current YAML scalar value is '{value}'.");
    }

    private static IReadOnlyCollection<string>? ParseEnumValues(
        string schemaPath,
        string propertyName,
        JsonElement element,
        YamlConfigSchemaPropertyType expectedType,
        string keywordName)
    {
        if (!element.TryGetProperty("enum", out var enumElement))
        {
            return null;
        }

        if (enumElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' in schema file '{schemaPath}' must declare '{keywordName}' as an array.");
        }

        var allowedValues = new List<string>();
        foreach (var item in enumElement.EnumerateArray())
        {
            allowedValues.Add(NormalizeEnumValue(schemaPath, propertyName, keywordName, expectedType, item));
        }

        return allowedValues;
    }

    private static string NormalizeEnumValue(
        string schemaPath,
        string propertyName,
        string keywordName,
        YamlConfigSchemaPropertyType expectedType,
        JsonElement item)
    {
        try
        {
            return expectedType switch
            {
                YamlConfigSchemaPropertyType.String when item.ValueKind == JsonValueKind.String =>
                    item.GetString() ?? string.Empty,
                YamlConfigSchemaPropertyType.Integer when item.ValueKind == JsonValueKind.Number =>
                    item.GetInt64().ToString(CultureInfo.InvariantCulture),
                YamlConfigSchemaPropertyType.Number when item.ValueKind == JsonValueKind.Number =>
                    item.GetDouble().ToString(CultureInfo.InvariantCulture),
                YamlConfigSchemaPropertyType.Boolean when item.ValueKind == JsonValueKind.True =>
                    bool.TrueString.ToLowerInvariant(),
                YamlConfigSchemaPropertyType.Boolean when item.ValueKind == JsonValueKind.False =>
                    bool.FalseString.ToLowerInvariant(),
                _ => throw new InvalidOperationException()
            };
        }
        catch
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' in schema file '{schemaPath}' contains a '{keywordName}' value that is incompatible with schema type '{GetTypeName(expectedType)}'.");
        }
    }

    private static string NormalizeScalarValue(YamlConfigSchemaPropertyType expectedType, string value)
    {
        return expectedType switch
        {
            YamlConfigSchemaPropertyType.String => value,
            YamlConfigSchemaPropertyType.Integer => long.Parse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            YamlConfigSchemaPropertyType.Number => double.Parse(
                value,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            YamlConfigSchemaPropertyType.Boolean => bool.Parse(value).ToString().ToLowerInvariant(),
            _ => value
        };
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
    /// <param name="referencedTableNames">Schema 声明的目标引用表名称集合。</param>
    public YamlConfigSchema(
        string schemaPath,
        IReadOnlyDictionary<string, YamlConfigSchemaProperty> properties,
        IReadOnlyCollection<string> requiredProperties,
        IReadOnlyCollection<string> referencedTableNames)
    {
        ArgumentNullException.ThrowIfNull(schemaPath);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(requiredProperties);
        ArgumentNullException.ThrowIfNull(referencedTableNames);

        SchemaPath = schemaPath;
        Properties = properties;
        RequiredProperties = requiredProperties;
        ReferencedTableNames = referencedTableNames;
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

    /// <summary>
    ///     获取 schema 声明的目标引用表名称集合。
    ///     该信息用于热重载时推导受影响的依赖表闭包。
    /// </summary>
    public IReadOnlyCollection<string> ReferencedTableNames { get; }
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
    /// <param name="referenceTableName">目标引用表名称；未声明跨表引用时为空。</param>
    /// <param name="allowedValues">标量允许值集合；未声明 enum 时为空。</param>
    /// <param name="itemAllowedValues">数组元素允许值集合；未声明 items.enum 时为空。</param>
    public YamlConfigSchemaProperty(
        string name,
        YamlConfigSchemaPropertyType propertyType,
        YamlConfigSchemaPropertyType? itemType,
        string? referenceTableName,
        IReadOnlyCollection<string>? allowedValues,
        IReadOnlyCollection<string>? itemAllowedValues)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        PropertyType = propertyType;
        ItemType = itemType;
        ReferenceTableName = referenceTableName;
        AllowedValues = allowedValues;
        ItemAllowedValues = itemAllowedValues;
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

    /// <summary>
    ///     获取目标引用表名称；未声明跨表引用时返回空。
    /// </summary>
    public string? ReferenceTableName { get; }

    /// <summary>
    ///     获取标量允许值集合；未声明 enum 时返回空。
    /// </summary>
    public IReadOnlyCollection<string>? AllowedValues { get; }

    /// <summary>
    ///     获取数组元素允许值集合；未声明 items.enum 时返回空。
    /// </summary>
    public IReadOnlyCollection<string>? ItemAllowedValues { get; }
}

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
    /// <param name="propertyName">声明引用的属性名。</param>
    /// <param name="itemIndex">数组元素索引；标量属性时为空。</param>
    /// <param name="rawValue">YAML 中的原始标量值。</param>
    /// <param name="referencedTableName">目标配置表名称。</param>
    /// <param name="valueType">引用值的 schema 标量类型。</param>
    public YamlConfigReferenceUsage(
        string yamlPath,
        string propertyName,
        int? itemIndex,
        string rawValue,
        string referencedTableName,
        YamlConfigSchemaPropertyType valueType)
    {
        ArgumentNullException.ThrowIfNull(yamlPath);
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(rawValue);
        ArgumentNullException.ThrowIfNull(referencedTableName);

        YamlPath = yamlPath;
        PropertyName = propertyName;
        ItemIndex = itemIndex;
        RawValue = rawValue;
        ReferencedTableName = referencedTableName;
        ValueType = valueType;
    }

    /// <summary>
    ///     获取源 YAML 文件路径。
    /// </summary>
    public string YamlPath { get; }

    /// <summary>
    ///     获取声明引用的属性名。
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    ///     获取数组元素索引；标量属性时返回空。
    /// </summary>
    public int? ItemIndex { get; }

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
    public string DisplayPath => ItemIndex.HasValue ? $"{PropertyName}[{ItemIndex.Value}]" : PropertyName;
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