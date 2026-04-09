using System.Text.RegularExpressions;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     提供 YAML 配置文件与 JSON Schema 之间的最小运行时校验能力。
///     该校验器与当前配置生成器、VS Code 工具支持的 schema 子集保持一致，
///     并通过递归遍历方式覆盖嵌套对象、对象数组、标量数组与深层 enum / 引用约束。
/// </summary>
internal static class YamlConfigSchemaValidator
{
    // The runtime intentionally uses the same culture-invariant regex semantics as the
    // JS tooling so grouping and backreferences behave consistently across environments.
    private const RegexOptions SupportedPatternRegexOptions = RegexOptions.CultureInvariant;

    /// <summary>
    ///     从磁盘加载并解析一个 JSON Schema 文件。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析后的 schema 模型。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="schemaPath" /> 为空时抛出。</exception>
    /// <exception cref="ConfigLoadException">当 schema 文件不存在或内容非法时抛出。</exception>
    internal static async Task<YamlConfigSchema> LoadAsync(
        string tableName,
        string schemaPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(schemaPath))
        {
            throw new ArgumentException("Schema path cannot be null or whitespace.", nameof(schemaPath));
        }

        if (!File.Exists(schemaPath))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaFileNotFound,
                tableName,
                $"Schema file '{schemaPath}' was not found.",
                schemaPath: schemaPath);
        }

        string schemaText;
        try
        {
            schemaText = await File.ReadAllTextAsync(schemaPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaReadFailed,
                tableName,
                $"Failed to read schema file '{schemaPath}'.",
                schemaPath: schemaPath,
                innerException: exception);
        }

        try
        {
            using var document = JsonDocument.Parse(schemaText);
            var root = document.RootElement;
            var rootNode = ParseNode(tableName, schemaPath, "<root>", root, isRoot: true);
            if (rootNode.NodeType != YamlConfigSchemaPropertyType.Object)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Schema file '{schemaPath}' must declare a root object schema.",
                    schemaPath: schemaPath);
            }

            var referencedTableNames = new HashSet<string>(StringComparer.Ordinal);
            CollectReferencedTableNames(rootNode, referencedTableNames);

            return new YamlConfigSchema(schemaPath, rootNode, referencedTableNames.ToArray());
        }
        catch (JsonException exception)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaInvalidJson,
                tableName,
                $"Schema file '{schemaPath}' contains invalid JSON.",
                schemaPath: schemaPath,
                innerException: exception);
        }
    }

    /// <summary>
    ///     使用已解析的 schema 校验 YAML 文本。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">YAML 文本内容。</param>
    /// <exception cref="ArgumentNullException">当参数为空时抛出。</exception>
    /// <exception cref="ConfigLoadException">当 YAML 内容与 schema 不匹配时抛出。</exception>
    internal static void Validate(
        string tableName,
        YamlConfigSchema schema,
        string yamlPath,
        string yamlText)
    {
        ValidateAndCollectReferences(tableName, schema, yamlPath, yamlText);
    }

    /// <summary>
    ///     使用已解析的 schema 校验 YAML 文本，并提取声明过的跨表引用。
    ///     该方法让结构校验与引用采集共享同一份 YAML 解析结果，避免加载器重复解析同一文件。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">YAML 文本内容。</param>
    /// <returns>当前 YAML 文件中声明的跨表引用集合。</returns>
    /// <exception cref="ArgumentNullException">当参数为空时抛出。</exception>
    /// <exception cref="ConfigLoadException">当 YAML 内容与 schema 不匹配时抛出。</exception>
    internal static IReadOnlyList<YamlConfigReferenceUsage> ValidateAndCollectReferences(
        string tableName,
        YamlConfigSchema schema,
        string yamlPath,
        string yamlText)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));
        }

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
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.YamlParseFailed,
                tableName,
                $"Config file '{yamlPath}' could not be parsed as YAML before schema validation.",
                yamlPath: yamlPath,
                schemaPath: schema.SchemaPath,
                innerException: exception);
        }

        if (yamlStream.Documents.Count != 1)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.InvalidYamlDocument,
                tableName,
                $"Config file '{yamlPath}' must contain exactly one YAML document.",
                yamlPath: yamlPath,
                schemaPath: schema.SchemaPath);
        }

        var references = new List<YamlConfigReferenceUsage>();
        ValidateNode(tableName, yamlPath, string.Empty, yamlStream.Documents[0].RootNode, schema.RootNode, references);
        return references;
    }

    /// <summary>
    ///     递归解析 schema 节点，使运行时只保留校验真正需要的最小结构信息。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">当前节点的逻辑属性路径。</param>
    /// <param name="element">Schema JSON 节点。</param>
    /// <param name="isRoot">是否为根节点。</param>
    /// <returns>可用于运行时校验的节点模型。</returns>
    private static YamlConfigSchemaNode ParseNode(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        bool isRoot = false)
    {
        if (!element.TryGetProperty("type", out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare a string 'type'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var typeName = typeElement.GetString() ?? string.Empty;
        var referenceTableName = TryGetReferenceTableName(tableName, schemaPath, propertyPath, element);

        switch (typeName)
        {
            case "object":
                EnsureReferenceKeywordIsSupported(tableName, schemaPath, propertyPath,
                    YamlConfigSchemaPropertyType.Object,
                    referenceTableName);
                return ParseObjectNode(tableName, schemaPath, propertyPath, element, isRoot);

            case "array":
                return ParseArrayNode(tableName, schemaPath, propertyPath, element, referenceTableName);

            case "integer":
                return CreateScalarNode(tableName, schemaPath, propertyPath, YamlConfigSchemaPropertyType.Integer,
                    element, referenceTableName);

            case "number":
                return CreateScalarNode(tableName, schemaPath, propertyPath, YamlConfigSchemaPropertyType.Number,
                    element, referenceTableName);

            case "boolean":
                return CreateScalarNode(tableName, schemaPath, propertyPath, YamlConfigSchemaPropertyType.Boolean,
                    element, referenceTableName);

            case "string":
                return CreateScalarNode(tableName, schemaPath, propertyPath, YamlConfigSchemaPropertyType.String,
                    element, referenceTableName);

            default:
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{propertyPath}' in schema file '{schemaPath}' uses unsupported type '{typeName}'.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath),
                    rawValue: typeName);
        }
    }

    /// <summary>
    ///     解析对象节点，保留属性字典与必填集合，以便后续递归校验时逐层定位错误。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象属性路径。</param>
    /// <param name="element">对象 schema 节点。</param>
    /// <param name="isRoot">是否为根节点。</param>
    /// <returns>对象节点模型。</returns>
    private static YamlConfigSchemaNode ParseObjectNode(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        bool isRoot)
    {
        if (!element.TryGetProperty("properties", out var propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object)
        {
            var subject = isRoot ? "root schema" : $"object property '{propertyPath}'";
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"The {subject} in schema file '{schemaPath}' must declare an object-valued 'properties' section.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var requiredProperties = new HashSet<string>(StringComparer.Ordinal);
        if (element.TryGetProperty("required", out var requiredElement) &&
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var requiredPropertyName = item.GetString();
                if (!string.IsNullOrWhiteSpace(requiredPropertyName))
                {
                    requiredProperties.Add(requiredPropertyName);
                }
            }
        }

        var properties = new Dictionary<string, YamlConfigSchemaNode>(StringComparer.Ordinal);
        foreach (var property in propertiesElement.EnumerateObject())
        {
            properties[property.Name] = ParseNode(
                tableName,
                schemaPath,
                CombineSchemaPath(propertyPath, property.Name),
                property.Value);
        }

        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Object,
            properties,
            requiredProperties,
            itemNode: null,
            referenceTableName: null,
            allowedValues: null,
            constraints: null,
            arrayConstraints: null,
            schemaPath);
    }

    /// <summary>
    ///     解析数组节点。
    ///     当前子集支持标量数组和对象数组，不支持数组嵌套数组。
    ///     当数组声明跨表引用时，会把引用语义挂到元素节点上，便于后续逐项校验。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">数组属性路径。</param>
    /// <param name="element">数组 schema 节点。</param>
    /// <param name="referenceTableName">声明在数组节点上的目标引用表。</param>
    /// <returns>数组节点模型。</returns>
    private static YamlConfigSchemaNode ParseArrayNode(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        string? referenceTableName)
    {
        if (!element.TryGetProperty("items", out var itemsElement) ||
            itemsElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Array property '{propertyPath}' in schema file '{schemaPath}' must declare an object-valued 'items' schema.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var itemNode = ParseNode(tableName, schemaPath, $"{propertyPath}[]", itemsElement);
        if (!string.IsNullOrWhiteSpace(referenceTableName))
        {
            if (itemNode.NodeType != YamlConfigSchemaPropertyType.String &&
                itemNode.NodeType != YamlConfigSchemaPropertyType.Integer)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{propertyPath}' in schema file '{schemaPath}' uses 'x-gframework-ref-table', but only string, integer, or arrays of those scalar types can declare cross-table references.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath),
                    referencedTableName: referenceTableName);
            }

            itemNode = itemNode.WithReferenceTable(referenceTableName);
        }

        if (itemNode.NodeType == YamlConfigSchemaPropertyType.Array)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Array property '{propertyPath}' in schema file '{schemaPath}' uses unsupported nested array items.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Array,
            properties: null,
            requiredProperties: null,
            itemNode,
            referenceTableName: null,
            allowedValues: null,
            constraints: null,
            arrayConstraints: ParseArrayConstraints(tableName, schemaPath, propertyPath, element),
            schemaPath);
    }

    /// <summary>
    ///     创建标量节点，并在解析阶段就完成 enum 与引用约束的兼容性检查。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">标量属性路径。</param>
    /// <param name="nodeType">标量类型。</param>
    /// <param name="element">标量 schema 节点。</param>
    /// <param name="referenceTableName">目标引用表名称。</param>
    /// <returns>标量节点模型。</returns>
    private static YamlConfigSchemaNode CreateScalarNode(
        string tableName,
        string schemaPath,
        string propertyPath,
        YamlConfigSchemaPropertyType nodeType,
        JsonElement element,
        string? referenceTableName)
    {
        EnsureReferenceKeywordIsSupported(tableName, schemaPath, propertyPath, nodeType, referenceTableName);
        return new YamlConfigSchemaNode(
            nodeType,
            properties: null,
            requiredProperties: null,
            itemNode: null,
            referenceTableName,
            ParseEnumValues(tableName, schemaPath, propertyPath, element, nodeType, "enum"),
            ParseScalarConstraints(tableName, schemaPath, propertyPath, element, nodeType),
            arrayConstraints: null,
            schemaPath);
    }

    /// <summary>
    ///     递归校验 YAML 节点。
    ///     每层都带上逻辑字段路径，这样深层对象与数组元素的错误也能直接定位。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">当前字段路径；根节点时为空。</param>
    /// <param name="node">实际 YAML 节点。</param>
    /// <param name="schemaNode">对应的 schema 节点。</param>
    /// <param name="references">已收集的跨表引用。</param>
    private static void ValidateNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage> references)
    {
        switch (schemaNode.NodeType)
        {
            case YamlConfigSchemaPropertyType.Object:
                ValidateObjectNode(tableName, yamlPath, displayPath, node, schemaNode, references);
                return;

            case YamlConfigSchemaPropertyType.Array:
                ValidateArrayNode(tableName, yamlPath, displayPath, node, schemaNode, references);
                return;

            case YamlConfigSchemaPropertyType.Integer:
            case YamlConfigSchemaPropertyType.Number:
            case YamlConfigSchemaPropertyType.Boolean:
            case YamlConfigSchemaPropertyType.String:
                ValidateScalarNode(tableName, yamlPath, displayPath, node, schemaNode, references);
                return;

            default:
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.UnexpectedFailure,
                    tableName,
                    $"Schema node '{displayPath}' uses unsupported runtime node type '{schemaNode.NodeType}'.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: GetDiagnosticPath(displayPath),
                    rawValue: schemaNode.NodeType.ToString());
        }
    }

    /// <summary>
    ///     校验对象节点，同时处理重复字段、未知字段和深层必填字段。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">当前对象的逻辑字段路径。</param>
    /// <param name="node">实际 YAML 节点。</param>
    /// <param name="schemaNode">对象 schema 节点。</param>
    /// <param name="references">已收集的跨表引用。</param>
    private static void ValidateObjectNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage> references)
    {
        if (node is not YamlMappingNode mappingNode)
        {
            var subject = displayPath.Length == 0 ? "Root object" : $"Property '{displayPath}'";
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.PropertyTypeMismatch,
                tableName,
                $"{subject} in config file '{yamlPath}' must be an object.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath));
        }

        var seenProperties = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in mappingNode.Children)
        {
            if (entry.Key is not YamlScalarNode keyNode ||
                string.IsNullOrWhiteSpace(keyNode.Value))
            {
                var subject = displayPath.Length == 0 ? "root object" : $"object property '{displayPath}'";
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.InvalidYamlDocument,
                    tableName,
                    $"Config file '{yamlPath}' contains a non-scalar or empty property name inside {subject}.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: GetDiagnosticPath(displayPath));
            }

            var propertyName = keyNode.Value;
            var propertyPath = CombineDisplayPath(displayPath, propertyName);
            if (!seenProperties.Add(propertyName))
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.DuplicateProperty,
                    tableName,
                    $"Config file '{yamlPath}' contains duplicate property '{propertyPath}'.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: propertyPath);
            }

            if (schemaNode.Properties is null ||
                !schemaNode.Properties.TryGetValue(propertyName, out var propertySchema))
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.UnknownProperty,
                    tableName,
                    $"Config file '{yamlPath}' contains unknown property '{propertyPath}' that is not declared in schema '{schemaNode.SchemaPathHint}'.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: propertyPath);
            }

            ValidateNode(tableName, yamlPath, propertyPath, entry.Value, propertySchema, references);
        }

        if (schemaNode.RequiredProperties is null)
        {
            return;
        }

        foreach (var requiredProperty in schemaNode.RequiredProperties)
        {
            if (seenProperties.Contains(requiredProperty))
            {
                continue;
            }

            var requiredPath = CombineDisplayPath(displayPath, requiredProperty);
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.MissingRequiredProperty,
                tableName,
                $"Config file '{yamlPath}' is missing required property '{requiredPath}' defined by schema '{schemaNode.SchemaPathHint}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: requiredPath);
        }
    }

    /// <summary>
    ///     校验数组节点，并递归验证每个元素。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">数组字段路径。</param>
    /// <param name="node">实际 YAML 节点。</param>
    /// <param name="schemaNode">数组 schema 节点。</param>
    /// <param name="references">已收集的跨表引用。</param>
    private static void ValidateArrayNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage> references)
    {
        if (node is not YamlSequenceNode sequenceNode)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.PropertyTypeMismatch,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must be an array.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath));
        }

        if (schemaNode.ItemNode is null)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.UnexpectedFailure,
                tableName,
                $"Schema node '{displayPath}' is missing array item information.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath));
        }

        if (schemaNode.ArrayConstraints is not null)
        {
            ValidateArrayConstraints(tableName, yamlPath, displayPath, sequenceNode.Children.Count, schemaNode);
        }

        for (var itemIndex = 0; itemIndex < sequenceNode.Children.Count; itemIndex++)
        {
            ValidateNode(
                tableName,
                yamlPath,
                $"{displayPath}[{itemIndex}]",
                sequenceNode.Children[itemIndex],
                schemaNode.ItemNode,
                references);
        }
    }

    /// <summary>
    ///     校验标量节点，并在值有效时收集跨表引用。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">标量字段路径。</param>
    /// <param name="node">实际 YAML 节点。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    /// <param name="references">已收集的跨表引用。</param>
    private static void ValidateScalarNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage> references)
    {
        if (node is not YamlScalarNode scalarNode)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.PropertyTypeMismatch,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must be a scalar value of type '{GetTypeName(schemaNode.NodeType)}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath));
        }

        var value = scalarNode.Value;
        if (value is null)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.NullScalarValue,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' cannot be null when schema type is '{GetTypeName(schemaNode.NodeType)}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath));
        }

        var tag = scalarNode.Tag.ToString();
        var isValid = schemaNode.NodeType switch
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

        if (!isValid)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.PropertyTypeMismatch,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must be of type '{GetTypeName(schemaNode.NodeType)}', but the current YAML scalar value is '{value}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: value);
        }

        var normalizedValue = NormalizeScalarValue(schemaNode.NodeType, value);
        if (schemaNode.AllowedValues is { Count: > 0 } &&
            !schemaNode.AllowedValues.Contains(normalizedValue, StringComparer.Ordinal))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.EnumValueNotAllowed,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must be one of [{string.Join(", ", schemaNode.AllowedValues)}], but the current YAML scalar value is '{value}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: value,
                detail: $"Allowed values: {string.Join(", ", schemaNode.AllowedValues)}.");
        }

        if (schemaNode.Constraints is not null)
        {
            ValidateScalarConstraints(tableName, yamlPath, displayPath, value, normalizedValue, schemaNode);
        }

        if (schemaNode.ReferenceTableName != null)
        {
            references.Add(
                new YamlConfigReferenceUsage(
                    yamlPath,
                    schemaNode.SchemaPathHint,
                    displayPath,
                    normalizedValue,
                    schemaNode.ReferenceTableName,
                    schemaNode.NodeType));
        }
    }

    /// <summary>
    ///     解析 enum，并在读取阶段验证枚举值与字段类型的兼容性。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="expectedType">期望的标量类型。</param>
    /// <param name="keywordName">当前读取的关键字名称。</param>
    /// <returns>归一化后的枚举值集合；未声明时返回空。</returns>
    private static IReadOnlyCollection<string>? ParseEnumValues(
        string tableName,
        string schemaPath,
        string propertyPath,
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
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare '{keywordName}' as an array.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var allowedValues = new List<string>();
        foreach (var item in enumElement.EnumerateArray())
        {
            allowedValues.Add(
                NormalizeEnumValue(tableName, schemaPath, propertyPath, keywordName, expectedType, item));
        }

        return allowedValues;
    }

    /// <summary>
    ///     解析标量字段支持的范围、长度与模式约束。
    ///     当前共享子集支持：
    ///     `integer/number` 上的 `minimum/maximum/exclusiveMinimum/exclusiveMaximum`，
    ///     以及 `string` 上的 `minLength/maxLength/pattern`。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="nodeType">标量类型。</param>
    /// <returns>解析后的约束模型；未声明时返回空。</returns>
    private static YamlConfigScalarConstraints? ParseScalarConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaPropertyType nodeType)
    {
        var minimum = TryParseNumericConstraint(tableName, schemaPath, propertyPath, element, nodeType, "minimum");
        var maximum = TryParseNumericConstraint(tableName, schemaPath, propertyPath, element, nodeType, "maximum");
        var exclusiveMinimum =
            TryParseNumericConstraint(tableName, schemaPath, propertyPath, element, nodeType, "exclusiveMinimum");
        var exclusiveMaximum =
            TryParseNumericConstraint(tableName, schemaPath, propertyPath, element, nodeType, "exclusiveMaximum");
        var minLength = TryParseLengthConstraint(tableName, schemaPath, propertyPath, element, nodeType, "minLength");
        var maxLength = TryParseLengthConstraint(tableName, schemaPath, propertyPath, element, nodeType, "maxLength");
        var pattern = TryParsePatternConstraint(tableName, schemaPath, propertyPath, element, nodeType);

        if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares 'minimum' greater than 'maximum'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        ValidateNumericConstraintRange(
            tableName,
            schemaPath,
            propertyPath,
            minimum,
            maximum,
            exclusiveMinimum,
            exclusiveMaximum);

        if (minLength.HasValue && maxLength.HasValue && minLength.Value > maxLength.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares 'minLength' greater than 'maxLength'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (!minimum.HasValue &&
            !maximum.HasValue &&
            !exclusiveMinimum.HasValue &&
            !exclusiveMaximum.HasValue &&
            !minLength.HasValue &&
            !maxLength.HasValue &&
            pattern is null)
        {
            return null;
        }

        return new YamlConfigScalarConstraints(
            minimum,
            maximum,
            exclusiveMinimum,
            exclusiveMaximum,
            minLength,
            maxLength,
            pattern,
            pattern is null
                ? null
                : new Regex(
                    pattern,
                    SupportedPatternRegexOptions));
    }

    /// <summary>
    ///     解析数组节点支持的元素数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">数组字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <returns>数组约束模型；未声明时返回空。</returns>
    private static YamlConfigArrayConstraints? ParseArrayConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element)
    {
        var minItems = TryParseArrayLengthConstraint(tableName, schemaPath, propertyPath, element, "minItems");
        var maxItems = TryParseArrayLengthConstraint(tableName, schemaPath, propertyPath, element, "maxItems");

        if (minItems.HasValue && maxItems.HasValue && minItems.Value > maxItems.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares 'minItems' greater than 'maxItems'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return !minItems.HasValue && !maxItems.HasValue
            ? null
            : new YamlConfigArrayConstraints(minItems, maxItems);
    }

    /// <summary>
    ///     读取数值区间约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="nodeType">字段类型。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <returns>数值约束；未声明时返回空。</returns>
    private static double? TryParseNumericConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaPropertyType nodeType,
        string keywordName)
    {
        if (!element.TryGetProperty(keywordName, out var constraintElement))
        {
            return null;
        }

        if (nodeType != YamlConfigSchemaPropertyType.Integer &&
            nodeType != YamlConfigSchemaPropertyType.Number)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses '{keywordName}', but only 'integer' and 'number' scalar types support numeric range constraints.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (constraintElement.ValueKind != JsonValueKind.Number ||
            !constraintElement.TryGetDouble(out var constraintValue) ||
            double.IsNaN(constraintValue) ||
            double.IsInfinity(constraintValue))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare '{keywordName}' as a finite number.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return constraintValue;
    }

    /// <summary>
    ///     读取字符串长度约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="nodeType">字段类型。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <returns>长度约束；未声明时返回空。</returns>
    private static int? TryParseLengthConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaPropertyType nodeType,
        string keywordName)
    {
        if (!element.TryGetProperty(keywordName, out var constraintElement))
        {
            return null;
        }

        if (nodeType != YamlConfigSchemaPropertyType.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses '{keywordName}', but only 'string' scalar types support length constraints.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (constraintElement.ValueKind != JsonValueKind.Number ||
            !constraintElement.TryGetInt32(out var constraintValue) ||
            constraintValue < 0)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare '{keywordName}' as a non-negative integer.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return constraintValue;
    }

    /// <summary>
    ///     读取字符串正则约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="nodeType">字段类型。</param>
    /// <returns>正则模式；未声明时返回空。</returns>
    private static string? TryParsePatternConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaPropertyType nodeType)
    {
        if (!element.TryGetProperty("pattern", out var patternElement))
        {
            return null;
        }

        if (nodeType != YamlConfigSchemaPropertyType.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses 'pattern', but only 'string' scalar types support regular-expression constraints.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (patternElement.ValueKind != JsonValueKind.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare 'pattern' as a string.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var pattern = patternElement.GetString() ?? string.Empty;
        try
        {
            _ = new Regex(pattern, SupportedPatternRegexOptions);
        }
        catch (ArgumentException exception)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares an invalid 'pattern' regular expression.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath),
                rawValue: pattern,
                innerException: exception);
        }

        return pattern;
    }

    /// <summary>
    ///     读取数组元素数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <returns>数组元素数量约束；未声明时返回空。</returns>
    private static int? TryParseArrayLengthConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        string keywordName)
    {
        if (!element.TryGetProperty(keywordName, out var constraintElement))
        {
            return null;
        }

        if (constraintElement.ValueKind != JsonValueKind.Number ||
            !constraintElement.TryGetInt32(out var constraintValue) ||
            constraintValue < 0)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare '{keywordName}' as a non-negative integer.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return constraintValue;
    }

    /// <summary>
    ///     校验数值上下界组合不会形成空区间。
    ///     这里把闭区间与开区间统一折算为最强边界，避免 schema 进入“无任何合法值”的状态。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="minimum">闭区间最小值。</param>
    /// <param name="maximum">闭区间最大值。</param>
    /// <param name="exclusiveMinimum">开区间最小值。</param>
    /// <param name="exclusiveMaximum">开区间最大值。</param>
    private static void ValidateNumericConstraintRange(
        string tableName,
        string schemaPath,
        string propertyPath,
        double? minimum,
        double? maximum,
        double? exclusiveMinimum,
        double? exclusiveMaximum)
    {
        var hasLowerBound = false;
        var lowerBound = double.MinValue;
        var isLowerBoundExclusive = false;

        if (minimum.HasValue)
        {
            hasLowerBound = true;
            lowerBound = minimum.Value;
        }

        if (exclusiveMinimum.HasValue &&
            (!hasLowerBound ||
             exclusiveMinimum.Value > lowerBound ||
             (exclusiveMinimum.Value.Equals(lowerBound) && !isLowerBoundExclusive)))
        {
            hasLowerBound = true;
            lowerBound = exclusiveMinimum.Value;
            isLowerBoundExclusive = true;
        }

        var hasUpperBound = false;
        var upperBound = double.MaxValue;
        var isUpperBoundExclusive = false;

        if (maximum.HasValue)
        {
            hasUpperBound = true;
            upperBound = maximum.Value;
        }

        if (exclusiveMaximum.HasValue &&
            (!hasUpperBound ||
             exclusiveMaximum.Value < upperBound ||
             (exclusiveMaximum.Value.Equals(upperBound) && !isUpperBoundExclusive)))
        {
            hasUpperBound = true;
            upperBound = exclusiveMaximum.Value;
            isUpperBoundExclusive = true;
        }

        if (!hasLowerBound || !hasUpperBound)
        {
            return;
        }

        if (lowerBound > upperBound ||
            (lowerBound.Equals(upperBound) && (isLowerBoundExclusive || isUpperBoundExclusive)))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares numeric constraints that do not leave any valid value range.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }
    }

    /// <summary>
    ///     校验标量值是否满足范围与长度约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="rawValue">原始 YAML 标量值。</param>
    /// <param name="normalizedValue">归一化后的比较值。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    private static void ValidateScalarConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        string rawValue,
        string normalizedValue,
        YamlConfigSchemaNode schemaNode)
    {
        var constraints = schemaNode.Constraints;
        if (constraints is null)
        {
            return;
        }

        switch (schemaNode.NodeType)
        {
            case YamlConfigSchemaPropertyType.Integer:
            case YamlConfigSchemaPropertyType.Number:
                if (!double.TryParse(
                        normalizedValue,
                        NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out var numericValue))
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.UnexpectedFailure,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' could not be normalized into a comparable numeric value.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue);
                }

                if (constraints.Minimum.HasValue && numericValue < constraints.Minimum.Value)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must be greater than or equal to {constraints.Minimum.Value.ToString(CultureInfo.InvariantCulture)}, but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail:
                        $"Minimum allowed value: {constraints.Minimum.Value.ToString(CultureInfo.InvariantCulture)}.");
                }

                if (constraints.ExclusiveMinimum.HasValue && numericValue <= constraints.ExclusiveMinimum.Value)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must be greater than {constraints.ExclusiveMinimum.Value.ToString(CultureInfo.InvariantCulture)}, but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail:
                        $"Exclusive minimum allowed value: {constraints.ExclusiveMinimum.Value.ToString(CultureInfo.InvariantCulture)}.");
                }

                if (constraints.Maximum.HasValue && numericValue > constraints.Maximum.Value)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must be less than or equal to {constraints.Maximum.Value.ToString(CultureInfo.InvariantCulture)}, but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail:
                        $"Maximum allowed value: {constraints.Maximum.Value.ToString(CultureInfo.InvariantCulture)}.");
                }

                if (constraints.ExclusiveMaximum.HasValue && numericValue >= constraints.ExclusiveMaximum.Value)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must be less than {constraints.ExclusiveMaximum.Value.ToString(CultureInfo.InvariantCulture)}, but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail:
                        $"Exclusive maximum allowed value: {constraints.ExclusiveMaximum.Value.ToString(CultureInfo.InvariantCulture)}.");
                }

                return;

            case YamlConfigSchemaPropertyType.String:
                var stringLength = rawValue.Length;

                if (constraints.MinLength.HasValue && stringLength < constraints.MinLength.Value)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must be at least {constraints.MinLength.Value} characters long, but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail: $"Minimum length: {constraints.MinLength.Value}.");
                }

                if (constraints.MaxLength.HasValue && stringLength > constraints.MaxLength.Value)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must be at most {constraints.MaxLength.Value} characters long, but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail: $"Maximum length: {constraints.MaxLength.Value}.");
                }

                if (constraints.PatternRegex is not null &&
                    !constraints.PatternRegex.IsMatch(rawValue))
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.ConstraintViolation,
                        tableName,
                        $"Property '{displayPath}' in config file '{yamlPath}' must match regular expression '{constraints.Pattern}', but the current YAML scalar value is '{rawValue}'.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: GetDiagnosticPath(displayPath),
                        rawValue: rawValue,
                        detail: $"Expected pattern: {constraints.Pattern}.");
                }

                return;

            default:
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.UnexpectedFailure,
                    tableName,
                    $"Property '{displayPath}' in config file '{yamlPath}' resolved unsupported constraint host type '{schemaNode.NodeType}'.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: GetDiagnosticPath(displayPath),
                    rawValue: schemaNode.NodeType.ToString());
        }
    }

    /// <summary>
    ///     校验数组值是否满足元素数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="itemCount">当前数组元素数量。</param>
    /// <param name="schemaNode">数组 schema 节点。</param>
    private static void ValidateArrayConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        int itemCount,
        YamlConfigSchemaNode schemaNode)
    {
        var constraints = schemaNode.ArrayConstraints;
        if (constraints is null)
        {
            return;
        }

        if (constraints.MinItems.HasValue && itemCount < constraints.MinItems.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must contain at least {constraints.MinItems.Value} items, but the current YAML sequence contains {itemCount}.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: itemCount.ToString(CultureInfo.InvariantCulture),
                detail: $"Minimum item count: {constraints.MinItems.Value}.");
        }

        if (constraints.MaxItems.HasValue && itemCount > constraints.MaxItems.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must contain at most {constraints.MaxItems.Value} items, but the current YAML sequence contains {itemCount}.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: itemCount.ToString(CultureInfo.InvariantCulture),
                detail: $"Maximum item count: {constraints.MaxItems.Value}.");
        }
    }

    /// <summary>
    ///     解析跨表引用目标表名称。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <returns>目标表名称；未声明时返回空。</returns>
    private static string? TryGetReferenceTableName(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element)
    {
        if (!element.TryGetProperty("x-gframework-ref-table", out var referenceTableElement))
        {
            return null;
        }

        if (referenceTableElement.ValueKind != JsonValueKind.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare a string 'x-gframework-ref-table' value.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var referenceTableName = referenceTableElement.GetString();
        if (string.IsNullOrWhiteSpace(referenceTableName))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare a non-empty 'x-gframework-ref-table' value.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return referenceTableName;
    }

    /// <summary>
    ///     验证哪些 schema 类型允许声明跨表引用。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="propertyType">字段类型。</param>
    /// <param name="referenceTableName">目标表名称。</param>
    private static void EnsureReferenceKeywordIsSupported(
        string tableName,
        string schemaPath,
        string propertyPath,
        YamlConfigSchemaPropertyType propertyType,
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

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"Property '{propertyPath}' in schema file '{schemaPath}' uses 'x-gframework-ref-table', but only string, integer, or arrays of those scalar types can declare cross-table references.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(propertyPath),
            referencedTableName: referenceTableName);
    }

    /// <summary>
    ///     递归收集 schema 中声明的目标表名称。
    /// </summary>
    /// <param name="node">当前 schema 节点。</param>
    /// <param name="referencedTableNames">输出集合。</param>
    private static void CollectReferencedTableNames(
        YamlConfigSchemaNode node,
        ISet<string> referencedTableNames)
    {
        if (node.ReferenceTableName != null)
        {
            referencedTableNames.Add(node.ReferenceTableName);
        }

        if (node.Properties is not null)
        {
            foreach (var property in node.Properties.Values)
            {
                CollectReferencedTableNames(property, referencedTableNames);
            }
        }

        if (node.ItemNode is not null)
        {
            CollectReferencedTableNames(node.ItemNode, referencedTableNames);
        }
    }

    /// <summary>
    ///     将 schema 中的 enum 单值归一化到运行时比较字符串。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <param name="expectedType">期望的标量类型。</param>
    /// <param name="item">当前枚举值节点。</param>
    /// <returns>归一化后的字符串值。</returns>
    private static string NormalizeEnumValue(
        string tableName,
        string schemaPath,
        string propertyPath,
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
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' contains a '{keywordName}' value that is incompatible with schema type '{GetTypeName(expectedType)}'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }
    }

    /// <summary>
    ///     将内部路径转换为适合放入诊断对象的可选字段路径。
    /// </summary>
    /// <param name="path">内部使用的属性路径。</param>
    /// <returns>可用于诊断的路径；根节点时返回空。</returns>
    private static string? GetDiagnosticPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) || string.Equals(path, "<root>", StringComparison.Ordinal)
            ? null
            : path;
    }

    /// <summary>
    ///     将 YAML 标量值规范化成运行时比较格式。
    /// </summary>
    /// <param name="expectedType">期望的标量类型。</param>
    /// <param name="value">原始字符串值。</param>
    /// <returns>归一化后的字符串。</returns>
    private static string NormalizeScalarValue(YamlConfigSchemaPropertyType expectedType, string value)
    {
        return expectedType switch
        {
            YamlConfigSchemaPropertyType.String => value,
            YamlConfigSchemaPropertyType.Integer when long.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var integerValue) =>
                integerValue.ToString(CultureInfo.InvariantCulture),
            YamlConfigSchemaPropertyType.Number when double.TryParse(
                    value,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var numberValue) =>
                numberValue.ToString(CultureInfo.InvariantCulture),
            YamlConfigSchemaPropertyType.Boolean when bool.TryParse(value, out var booleanValue) =>
                booleanValue.ToString().ToLowerInvariant(),
            YamlConfigSchemaPropertyType.Integer =>
                throw new InvalidOperationException($"Value '{value}' cannot be normalized as integer."),
            YamlConfigSchemaPropertyType.Number =>
                throw new InvalidOperationException($"Value '{value}' cannot be normalized as number."),
            YamlConfigSchemaPropertyType.Boolean =>
                throw new InvalidOperationException($"Value '{value}' cannot be normalized as boolean."),
            _ =>
                throw new InvalidOperationException(
                    $"Schema node type '{expectedType}' cannot be normalized as a scalar value.")
        };
    }

    /// <summary>
    ///     获取 schema 类型的可读名称，用于错误信息。
    /// </summary>
    /// <param name="type">Schema 节点类型。</param>
    /// <returns>可读类型名。</returns>
    private static string GetTypeName(YamlConfigSchemaPropertyType type)
    {
        return type switch
        {
            YamlConfigSchemaPropertyType.Integer => "integer",
            YamlConfigSchemaPropertyType.Number => "number",
            YamlConfigSchemaPropertyType.Boolean => "boolean",
            YamlConfigSchemaPropertyType.String => "string",
            YamlConfigSchemaPropertyType.Array => "array",
            YamlConfigSchemaPropertyType.Object => "object",
            _ => type.ToString()
        };
    }

    /// <summary>
    ///     组合 schema 中的逻辑路径，便于诊断时指出深层字段。
    /// </summary>
    /// <param name="parentPath">父级路径。</param>
    /// <param name="propertyName">当前属性名。</param>
    /// <returns>组合后的路径。</returns>
    private static string CombineSchemaPath(string parentPath, string propertyName)
    {
        return parentPath == "<root>" ? propertyName : $"{parentPath}.{propertyName}";
    }

    /// <summary>
    ///     组合 YAML 诊断展示路径。
    /// </summary>
    /// <param name="parentPath">父级路径。</param>
    /// <param name="propertyName">当前属性名。</param>
    /// <returns>组合后的路径。</returns>
    private static string CombineDisplayPath(string parentPath, string propertyName)
    {
        return string.IsNullOrWhiteSpace(parentPath) ? propertyName : $"{parentPath}.{propertyName}";
    }

    /// <summary>
    ///     判断当前标量是否应按字符串处理。
    ///     这里显式排除 YAML 的数字、布尔和 null 标签，避免未加引号的值被当成字符串混入运行时。
    /// </summary>
    /// <param name="tag">YAML 标量标签。</param>
    /// <returns>是否为字符串标量。</returns>
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
///     该模型保留根节点与引用依赖集合，避免运行时引入完整 schema 引擎。
/// </summary>
internal sealed class YamlConfigSchema
{
    /// <summary>
    ///     初始化一个可用于运行时校验的 schema 模型。
    /// </summary>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="rootNode">根节点模型。</param>
    /// <param name="referencedTableNames">Schema 声明的目标引用表名称集合。</param>
    public YamlConfigSchema(
        string schemaPath,
        YamlConfigSchemaNode rootNode,
        IReadOnlyCollection<string> referencedTableNames)
    {
        ArgumentNullException.ThrowIfNull(schemaPath);
        ArgumentNullException.ThrowIfNull(rootNode);
        ArgumentNullException.ThrowIfNull(referencedTableNames);

        SchemaPath = schemaPath;
        RootNode = rootNode;
        ReferencedTableNames = referencedTableNames;
    }

    /// <summary>
    ///     获取 schema 文件路径。
    /// </summary>
    public string SchemaPath { get; }

    /// <summary>
    ///     获取根节点模型。
    /// </summary>
    public YamlConfigSchemaNode RootNode { get; }

    /// <summary>
    ///     获取 schema 声明的目标引用表名称集合。
    ///     该信息用于热重载时推导受影响的依赖表闭包。
    /// </summary>
    public IReadOnlyCollection<string> ReferencedTableNames { get; }
}

/// <summary>
///     表示单个 schema 节点的最小运行时描述。
///     同一个模型同时覆盖对象、数组和标量，便于递归校验逻辑只依赖一种树结构。
/// </summary>
internal sealed class YamlConfigSchemaNode
{
    /// <summary>
    ///     初始化一个 schema 节点描述。
    /// </summary>
    /// <param name="nodeType">节点类型。</param>
    /// <param name="properties">对象属性集合。</param>
    /// <param name="requiredProperties">对象必填属性集合。</param>
    /// <param name="itemNode">数组元素节点。</param>
    /// <param name="referenceTableName">目标引用表名称。</param>
    /// <param name="allowedValues">标量允许值集合。</param>
    /// <param name="constraints">标量范围与长度约束。</param>
    /// <param name="arrayConstraints">数组元素数量约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    public YamlConfigSchemaNode(
        YamlConfigSchemaPropertyType nodeType,
        IReadOnlyDictionary<string, YamlConfigSchemaNode>? properties,
        IReadOnlyCollection<string>? requiredProperties,
        YamlConfigSchemaNode? itemNode,
        string? referenceTableName,
        IReadOnlyCollection<string>? allowedValues,
        YamlConfigScalarConstraints? constraints,
        YamlConfigArrayConstraints? arrayConstraints,
        string schemaPathHint)
    {
        NodeType = nodeType;
        Properties = properties;
        RequiredProperties = requiredProperties;
        ItemNode = itemNode;
        ReferenceTableName = referenceTableName;
        AllowedValues = allowedValues;
        Constraints = constraints;
        ArrayConstraints = arrayConstraints;
        SchemaPathHint = schemaPathHint;
    }

    /// <summary>
    ///     获取节点类型。
    /// </summary>
    public YamlConfigSchemaPropertyType NodeType { get; }

    /// <summary>
    ///     获取对象属性集合；非对象节点时返回空。
    /// </summary>
    public IReadOnlyDictionary<string, YamlConfigSchemaNode>? Properties { get; }

    /// <summary>
    ///     获取对象必填属性集合；非对象节点时返回空。
    /// </summary>
    public IReadOnlyCollection<string>? RequiredProperties { get; }

    /// <summary>
    ///     获取数组元素节点；非数组节点时返回空。
    /// </summary>
    public YamlConfigSchemaNode? ItemNode { get; }

    /// <summary>
    ///     获取目标引用表名称；未声明跨表引用时返回空。
    /// </summary>
    public string? ReferenceTableName { get; }

    /// <summary>
    ///     获取标量允许值集合；未声明 enum 时返回空。
    /// </summary>
    public IReadOnlyCollection<string>? AllowedValues { get; }

    /// <summary>
    ///     获取标量范围与长度约束；未声明时返回空。
    /// </summary>
    public YamlConfigScalarConstraints? Constraints { get; }

    /// <summary>
    ///     获取数组元素数量约束；未声明时返回空。
    /// </summary>
    public YamlConfigArrayConstraints? ArrayConstraints { get; }

    /// <summary>
    ///     获取用于诊断显示的 schema 路径提示。
    ///     当前节点本身不记录独立路径，因此对象校验会回退到所属根 schema 路径。
    /// </summary>
    public string SchemaPathHint { get; }

    /// <summary>
    ///     基于当前节点复制一个只替换引用表名称的新节点。
    ///     该方法用于把数组级别的 ref-table 语义挂接到元素节点上。
    /// </summary>
    /// <param name="referenceTableName">新的目标引用表名称。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithReferenceTable(string referenceTableName)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            Properties,
            RequiredProperties,
            ItemNode,
            referenceTableName,
            AllowedValues,
            Constraints,
            ArrayConstraints,
            SchemaPathHint);
    }
}

/// <summary>
///     表示一个标量节点上声明的数值范围或字符串长度约束。
///     该模型让运行时、热重载和跨文件诊断都能复用同一份最小约束信息。
/// </summary>
internal sealed class YamlConfigScalarConstraints
{
    /// <summary>
    ///     初始化标量约束模型。
    /// </summary>
    /// <param name="minimum">最小值约束。</param>
    /// <param name="maximum">最大值约束。</param>
    /// <param name="exclusiveMinimum">开区间最小值约束。</param>
    /// <param name="exclusiveMaximum">开区间最大值约束。</param>
    /// <param name="minLength">最小长度约束。</param>
    /// <param name="maxLength">最大长度约束。</param>
    /// <param name="pattern">正则模式约束。</param>
    /// <param name="patternRegex">已编译的正则表达式。</param>
    public YamlConfigScalarConstraints(
        double? minimum,
        double? maximum,
        double? exclusiveMinimum,
        double? exclusiveMaximum,
        int? minLength,
        int? maxLength,
        string? pattern,
        Regex? patternRegex)
    {
        Minimum = minimum;
        Maximum = maximum;
        ExclusiveMinimum = exclusiveMinimum;
        ExclusiveMaximum = exclusiveMaximum;
        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = pattern;
        PatternRegex = patternRegex;
    }

    /// <summary>
    ///     获取最小值约束。
    /// </summary>
    public double? Minimum { get; }

    /// <summary>
    ///     获取最大值约束。
    /// </summary>
    public double? Maximum { get; }

    /// <summary>
    ///     获取开区间最小值约束。
    /// </summary>
    public double? ExclusiveMinimum { get; }

    /// <summary>
    ///     获取开区间最大值约束。
    /// </summary>
    public double? ExclusiveMaximum { get; }

    /// <summary>
    ///     获取最小长度约束。
    /// </summary>
    public int? MinLength { get; }

    /// <summary>
    ///     获取最大长度约束。
    /// </summary>
    public int? MaxLength { get; }

    /// <summary>
    ///     获取正则模式约束原文。
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    ///     获取已编译的正则表达式。
    /// </summary>
    public Regex? PatternRegex { get; }
}

/// <summary>
///     表示一个数组节点上声明的元素数量约束。
///     该模型与标量约束拆分保存，避免数组节点继续共享不适用的标量字段。
/// </summary>
internal sealed class YamlConfigArrayConstraints
{
    /// <summary>
    ///     初始化数组约束模型。
    /// </summary>
    /// <param name="minItems">最小元素数量约束。</param>
    /// <param name="maxItems">最大元素数量约束。</param>
    public YamlConfigArrayConstraints(int? minItems, int? maxItems)
    {
        MinItems = minItems;
        MaxItems = maxItems;
    }

    /// <summary>
    ///     获取最小元素数量约束。
    /// </summary>
    public int? MinItems { get; }

    /// <summary>
    ///     获取最大元素数量约束。
    /// </summary>
    public int? MaxItems { get; }
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

/// <summary>
///     表示当前运行时 schema 校验器支持的属性类型。
/// </summary>
internal enum YamlConfigSchemaPropertyType
{
    /// <summary>
    ///     对象类型。
    /// </summary>
    Object,

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
