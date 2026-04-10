using System.Numerics;
using System.Text.RegularExpressions;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     提供 YAML 配置文件与 JSON Schema 之间的最小运行时校验能力。
///     该校验器与当前配置生成器、VS Code 工具支持的 schema 子集保持一致，
///     并通过递归遍历方式覆盖嵌套对象、对象数组、标量数组与深层 enum / 引用约束。
///     当前共享子集额外支持 <c>multipleOf</c>、<c>uniqueItems</c>、
///     <c>minProperties</c> 与 <c>maxProperties</c>，
///     让数值步进、数组去重和对象属性数量规则在运行时与生成器 / 工具侧保持一致。
/// </summary>
internal static class YamlConfigSchemaValidator
{
    // The runtime intentionally uses the same culture-invariant regex semantics as the
    // JS tooling so grouping and backreferences behave consistently across environments.
    private const RegexOptions SupportedPatternRegexOptions = RegexOptions.CultureInvariant;
    private static readonly Regex ExactDecimalPattern = new(
        @"^(?<sign>[+-]?)(?:(?<integer>\d+)(?:\.(?<fraction>\d*))?|\.(?<fractionOnly>\d+))(?:[eE](?<exponent>[+-]?\d+))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
        ValidateCore(tableName, schema, yamlPath, yamlText, references: null);
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
        var references = new List<YamlConfigReferenceUsage>();
        ValidateCore(tableName, schema, yamlPath, yamlText, references);
        return references;
    }

    /// <summary>
    ///     执行共享的 YAML 结构校验流程，并按需收集跨表引用。
    ///     这样 <see cref="Validate" /> 可以复用同一条校验链路，同时避免为“不关心引用结果”的调用方分配临时列表。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <param name="yamlPath">YAML 文件路径，仅用于诊断信息。</param>
    /// <param name="yamlText">YAML 文本内容。</param>
    /// <param name="references">可选的跨表引用收集器；为 <see langword="null" /> 时只做结构校验。</param>
    /// <exception cref="ArgumentNullException">当参数为空时抛出。</exception>
    /// <exception cref="ConfigLoadException">当 YAML 内容与 schema 不匹配时抛出。</exception>
    private static void ValidateCore(
        string tableName,
        YamlConfigSchema schema,
        string yamlPath,
        string yamlText,
        ICollection<YamlConfigReferenceUsage>? references)
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

        ValidateNode(tableName, yamlPath, string.Empty, yamlStream.Documents[0].RootNode, schema.RootNode, references);
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

        var objectNode = YamlConfigSchemaNode.CreateObject(
            properties,
            requiredProperties,
            ParseObjectConstraints(tableName, schemaPath, propertyPath, element),
            schemaPath);
        return objectNode.WithConstantValue(
            ParseConstantValue(tableName, schemaPath, propertyPath, element, objectNode));
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

        var arrayNode = YamlConfigSchemaNode.CreateArray(
            itemNode,
            ParseArrayConstraints(tableName, schemaPath, propertyPath, element),
            schemaPath);
        return arrayNode.WithConstantValue(
            ParseConstantValue(tableName, schemaPath, propertyPath, element, arrayNode));
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
        var scalarNode = YamlConfigSchemaNode.CreateScalar(
            nodeType,
            referenceTableName,
            ParseEnumValues(tableName, schemaPath, propertyPath, element, nodeType, "enum"),
            ParseScalarConstraints(tableName, schemaPath, propertyPath, element, nodeType),
            schemaPath);
        return scalarNode.WithConstantValue(
            ParseConstantValue(tableName, schemaPath, propertyPath, element, scalarNode));
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
        ICollection<YamlConfigReferenceUsage>? references)
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
        ICollection<YamlConfigReferenceUsage>? references)
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

        if (schemaNode.ObjectConstraints is not null)
        {
            ValidateObjectConstraints(tableName, yamlPath, displayPath, seenProperties.Count, schemaNode);
        }

        ValidateConstantValue(tableName, yamlPath, displayPath, mappingNode, schemaNode);
    }

    /// <summary>
    ///     校验对象节点声明的属性数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">对象字段路径；根对象时为空。</param>
    /// <param name="propertyCount">当前对象实际属性数量。</param>
    /// <param name="schemaNode">对象 schema 节点。</param>
    private static void ValidateObjectConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        int propertyCount,
        YamlConfigSchemaNode schemaNode)
    {
        var constraints = schemaNode.ObjectConstraints;
        if (constraints is null)
        {
            return;
        }

        var subject = string.IsNullOrWhiteSpace(displayPath)
            ? "Root object"
            : $"Property '{displayPath}'";
        var rawValue = propertyCount.ToString(CultureInfo.InvariantCulture);

        if (constraints.MinProperties.HasValue &&
            propertyCount < constraints.MinProperties.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"{subject} in config file '{yamlPath}' must contain at least {constraints.MinProperties.Value.ToString(CultureInfo.InvariantCulture)} properties.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: rawValue,
                detail: $"Minimum property count: {constraints.MinProperties.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (constraints.MaxProperties.HasValue &&
            propertyCount > constraints.MaxProperties.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"{subject} in config file '{yamlPath}' must contain at most {constraints.MaxProperties.Value.ToString(CultureInfo.InvariantCulture)} properties.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: rawValue,
                detail: $"Maximum property count: {constraints.MaxProperties.Value.ToString(CultureInfo.InvariantCulture)}.");
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
        ICollection<YamlConfigReferenceUsage>? references)
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

        ValidateArrayUniqueItemsConstraint(tableName, yamlPath, displayPath, sequenceNode, schemaNode);
        ValidateConstantValue(tableName, yamlPath, displayPath, sequenceNode, schemaNode);
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
        ICollection<YamlConfigReferenceUsage>? references)
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

        ValidateConstantValue(tableName, yamlPath, displayPath, scalarNode, schemaNode);

        if (schemaNode.ReferenceTableName != null &&
            references is not null)
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
                NormalizeKeywordScalarValue(tableName, schemaPath, propertyPath, keywordName, expectedType, item));
        }

        return allowedValues;
    }

    /// <summary>
    ///     解析 <c>const</c>，并把 schema 常量预归一化成与运行时 YAML 相同的稳定比较键。
    ///     这样运行时只需要复用现有递归比较逻辑，而不必在每次加载时重新解释 JSON 常量。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaNode">已解析的 schema 节点。</param>
    /// <returns>常量约束模型；未声明时返回空。</returns>
    private static YamlConfigConstantValue? ParseConstantValue(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaNode schemaNode)
    {
        if (!element.TryGetProperty("const", out var constantElement))
        {
            return null;
        }

        return new YamlConfigConstantValue(
            BuildComparableConstantValue(tableName, schemaPath, propertyPath, "const", constantElement, schemaNode),
            constantElement.GetRawText());
    }

    /// <summary>
    ///     把 schema 中的 <c>const</c> JSON 值转换成与 YAML 运行时一致的比较键。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <param name="element">常量 JSON 值。</param>
    /// <param name="schemaNode">目标 schema 节点。</param>
    /// <returns>可稳定比较的归一化键。</returns>
    private static string BuildComparableConstantValue(
        string tableName,
        string schemaPath,
        string propertyPath,
        string keywordName,
        JsonElement element,
        YamlConfigSchemaNode schemaNode)
    {
        return schemaNode.NodeType switch
        {
            YamlConfigSchemaPropertyType.Object => BuildComparableConstantObjectValue(
                tableName,
                schemaPath,
                propertyPath,
                keywordName,
                element,
                schemaNode),
            YamlConfigSchemaPropertyType.Array => BuildComparableConstantArrayValue(
                tableName,
                schemaPath,
                propertyPath,
                keywordName,
                element,
                schemaNode),
            YamlConfigSchemaPropertyType.Integer => BuildComparableConstantScalarValue(
                tableName,
                schemaPath,
                propertyPath,
                keywordName,
                element,
                schemaNode),
            YamlConfigSchemaPropertyType.Number => BuildComparableConstantScalarValue(
                tableName,
                schemaPath,
                propertyPath,
                keywordName,
                element,
                schemaNode),
            YamlConfigSchemaPropertyType.Boolean => BuildComparableConstantScalarValue(
                tableName,
                schemaPath,
                propertyPath,
                keywordName,
                element,
                schemaNode),
            YamlConfigSchemaPropertyType.String => BuildComparableConstantScalarValue(
                tableName,
                schemaPath,
                propertyPath,
                keywordName,
                element,
                schemaNode),
            _ => throw new InvalidOperationException($"Unsupported schema node type '{schemaNode.NodeType}'.")
        };
    }

    /// <summary>
    ///     构建对象常量的稳定比较键。
    ///     这里同样忽略 JSON 对象字段顺序，避免 schema 文本格式影响常量比较结果。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <param name="element">常量 JSON 值。</param>
    /// <param name="schemaNode">对象 schema 节点。</param>
    /// <returns>对象常量的可比较键。</returns>
    private static string BuildComparableConstantObjectValue(
        string tableName,
        string schemaPath,
        string propertyPath,
        string keywordName,
        JsonElement element,
        YamlConfigSchemaNode schemaNode)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses '{keywordName}', but only object values are compatible with schema type '{GetTypeName(schemaNode.NodeType)}'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var properties = schemaNode.Properties
            ?? throw new InvalidOperationException("Object schema nodes must expose declared properties.");
        var objectEntries = new List<KeyValuePair<string, string>>();
        foreach (var property in element.EnumerateObject())
        {
            if (!properties.TryGetValue(property.Name, out var propertySchema))
            {
                var childPath = CombineSchemaPath(propertyPath, property.Name);
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{propertyPath}' in schema file '{schemaPath}' uses '{keywordName}', but nested property '{childPath}' is not declared in the object schema.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(childPath));
            }

            objectEntries.Add(
                new KeyValuePair<string, string>(
                    property.Name,
                    BuildComparableConstantValue(
                        tableName,
                        schemaPath,
                        CombineSchemaPath(propertyPath, property.Name),
                        keywordName,
                        property.Value,
                        propertySchema)));
        }

        objectEntries.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        return string.Join(
            "|",
            objectEntries.Select(static entry =>
                $"{entry.Key.Length.ToString(CultureInfo.InvariantCulture)}:{entry.Key}={entry.Value.Length.ToString(CultureInfo.InvariantCulture)}:{entry.Value}"));
    }

    /// <summary>
    ///     构建数组常量的稳定比较键。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <param name="element">常量 JSON 值。</param>
    /// <param name="schemaNode">数组 schema 节点。</param>
    /// <returns>数组常量的可比较键。</returns>
    private static string BuildComparableConstantArrayValue(
        string tableName,
        string schemaPath,
        string propertyPath,
        string keywordName,
        JsonElement element,
        YamlConfigSchemaNode schemaNode)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses '{keywordName}', but only array values are compatible with schema type '{GetTypeName(schemaNode.NodeType)}'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (schemaNode.ItemNode is null)
        {
            throw new InvalidOperationException("Array schema nodes must expose their item schema.");
        }

        return "[" +
               string.Join(
                   ",",
                   element.EnumerateArray().Select(
                       (item, index) =>
                       {
                           var comparableValue = BuildComparableConstantValue(
                               tableName,
                               schemaPath,
                               $"{propertyPath}[{index}]",
                               keywordName,
                               item,
                               schemaNode.ItemNode);
                           return
                               $"{comparableValue.Length.ToString(CultureInfo.InvariantCulture)}:{comparableValue}";
                       })) +
               "]";
    }

    /// <summary>
    ///     构建标量常量的稳定比较键。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <param name="element">常量 JSON 值。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    /// <returns>标量常量的可比较键。</returns>
    private static string BuildComparableConstantScalarValue(
        string tableName,
        string schemaPath,
        string propertyPath,
        string keywordName,
        JsonElement element,
        YamlConfigSchemaNode schemaNode)
    {
        var normalizedValue = NormalizeKeywordScalarValue(
            tableName,
            schemaPath,
            propertyPath,
            keywordName,
            schemaNode.NodeType,
            element);
        return
            $"{schemaNode.NodeType}:{normalizedValue.Length.ToString(CultureInfo.InvariantCulture)}:{normalizedValue}";
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
        var multipleOf = TryParseMultipleOfConstraint(tableName, schemaPath, propertyPath, element, nodeType);
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

        var numericConstraints = CreateNumericScalarConstraints(
            minimum,
            maximum,
            exclusiveMinimum,
            exclusiveMaximum,
            multipleOf);
        var stringConstraints = CreateStringScalarConstraints(
            minLength,
            maxLength,
            pattern);

        return numericConstraints is null && stringConstraints is null
            ? null
            : new YamlConfigScalarConstraints(numericConstraints, stringConstraints);
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
        var uniqueItems = TryParseUniqueItemsConstraint(tableName, schemaPath, propertyPath, element);

        if (minItems.HasValue && maxItems.HasValue && minItems.Value > maxItems.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares 'minItems' greater than 'maxItems'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return !minItems.HasValue && !maxItems.HasValue && !uniqueItems
            ? null
            : new YamlConfigArrayConstraints(minItems, maxItems, uniqueItems);
    }

    /// <summary>
    ///     解析对象节点支持的属性数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <returns>对象约束模型；未声明时返回空。</returns>
    private static YamlConfigObjectConstraints? ParseObjectConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element)
    {
        var minProperties = TryParseObjectPropertyCountConstraint(
            tableName,
            schemaPath,
            propertyPath,
            element,
            "minProperties");
        var maxProperties = TryParseObjectPropertyCountConstraint(
            tableName,
            schemaPath,
            propertyPath,
            element,
            "maxProperties");

        if (minProperties.HasValue && maxProperties.HasValue && minProperties.Value > maxProperties.Value)
        {
            var targetDescription = DescribeObjectSchemaTarget(propertyPath);
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{targetDescription} in schema file '{schemaPath}' declares 'minProperties' greater than 'maxProperties'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return !minProperties.HasValue && !maxProperties.HasValue
            ? null
            : new YamlConfigObjectConstraints(minProperties, maxProperties);
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
    ///     读取 <c>multipleOf</c> 约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="nodeType">字段类型。</param>
    /// <returns>步进约束；未声明时返回空。</returns>
    private static double? TryParseMultipleOfConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaPropertyType nodeType)
    {
        var multipleOf = TryParseNumericConstraint(tableName, schemaPath, propertyPath, element, nodeType, "multipleOf");
        if (!multipleOf.HasValue)
        {
            return null;
        }

        if (multipleOf.Value <= 0d)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare 'multipleOf' as a positive finite number.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return multipleOf;
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
    ///     读取对象属性数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <returns>属性数量约束；未声明时返回空。</returns>
    private static int? TryParseObjectPropertyCountConstraint(
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
            var targetDescription = DescribeObjectSchemaTarget(propertyPath);
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{targetDescription} in schema file '{schemaPath}' must declare '{keywordName}' as a non-negative integer.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return constraintValue;
    }

    /// <summary>
    ///     为对象级 schema 关键字构造稳定的诊断主体。
    ///     根对象不会再显示为空字符串属性名，避免坏 schema 诊断出现 <c>Property ''</c> 之类的文本。
    /// </summary>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <returns>用于错误消息的对象主体描述。</returns>
    private static string DescribeObjectSchemaTarget(string propertyPath)
    {
        return string.IsNullOrWhiteSpace(propertyPath)
            ? "Root object"
            : $"Property '{propertyPath}'";
    }

    /// <summary>
    ///     读取数组去重约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <returns>是否启用 <c>uniqueItems</c>。</returns>
    private static bool TryParseUniqueItemsConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element)
    {
        if (!element.TryGetProperty("uniqueItems", out var constraintElement))
        {
            return false;
        }

        if (constraintElement.ValueKind != JsonValueKind.True &&
            constraintElement.ValueKind != JsonValueKind.False)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare 'uniqueItems' as a boolean.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return constraintElement.GetBoolean();
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
                ValidateNumericScalarConstraints(
                    tableName,
                    yamlPath,
                    displayPath,
                    rawValue,
                    normalizedValue,
                    schemaNode,
                    constraints.NumericConstraints);
                return;

            case YamlConfigSchemaPropertyType.String:
                ValidateStringScalarConstraints(
                    tableName,
                    yamlPath,
                    displayPath,
                    rawValue,
                    schemaNode,
                    constraints.StringConstraints);
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
    ///     校验节点值是否满足 <c>const</c> 约束。
    ///     该检查复用与 <c>uniqueItems</c> 相同的稳定比较键，保证对象字段顺序、数字字面量和布尔大小写不会造成伪差异。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径；根节点时为空。</param>
    /// <param name="node">当前 YAML 节点。</param>
    /// <param name="schemaNode">对应的 schema 节点。</param>
    private static void ValidateConstantValue(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode)
    {
        var constantValue = schemaNode.ConstantValue;
        if (constantValue is null)
        {
            return;
        }

        var comparableValue = BuildComparableNodeValue(node, schemaNode);
        if (string.Equals(comparableValue, constantValue.ComparableValue, StringComparison.Ordinal))
        {
            return;
        }

        var subject = string.IsNullOrWhiteSpace(displayPath)
            ? "Root object"
            : $"Property '{displayPath}'";
        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.ConstraintViolation,
            tableName,
            $"{subject} in config file '{yamlPath}' must match constant value {constantValue.DisplayValue}.",
            yamlPath: yamlPath,
            schemaPath: schemaNode.SchemaPathHint,
            displayPath: GetDiagnosticPath(displayPath),
            rawValue: DescribeYamlNodeForDiagnostics(node, schemaNode),
            detail: $"Required constant value: {constantValue.DisplayValue}.");
    }

    /// <summary>
    ///     根据已读取的数值关键字创建数值约束对象。
    ///     该分组让调用方不必再维护一个超过 Sonar 默认阈值的长参数构造函数。
    /// </summary>
    /// <param name="minimum">最小值约束。</param>
    /// <param name="maximum">最大值约束。</param>
    /// <param name="exclusiveMinimum">开区间最小值约束。</param>
    /// <param name="exclusiveMaximum">开区间最大值约束。</param>
    /// <param name="multipleOf">数值步进约束。</param>
    /// <returns>数值约束对象；未声明任何数值约束时返回空。</returns>
    private static YamlConfigNumericConstraints? CreateNumericScalarConstraints(
        double? minimum,
        double? maximum,
        double? exclusiveMinimum,
        double? exclusiveMaximum,
        double? multipleOf)
    {
        return !minimum.HasValue &&
               !maximum.HasValue &&
               !exclusiveMinimum.HasValue &&
               !exclusiveMaximum.HasValue &&
               !multipleOf.HasValue
            ? null
            : new YamlConfigNumericConstraints(
                minimum,
                maximum,
                exclusiveMinimum,
                exclusiveMaximum,
                multipleOf);
    }

    /// <summary>
    ///     根据已读取的字符串关键字创建字符串约束对象。
    ///     正则会在 schema 解析阶段预编译，避免每次校验都重复实例化。
    /// </summary>
    /// <param name="minLength">最小长度约束。</param>
    /// <param name="maxLength">最大长度约束。</param>
    /// <param name="pattern">正则模式约束。</param>
    /// <returns>字符串约束对象；未声明任何字符串约束时返回空。</returns>
    private static YamlConfigStringConstraints? CreateStringScalarConstraints(
        int? minLength,
        int? maxLength,
        string? pattern)
    {
        return !minLength.HasValue &&
               !maxLength.HasValue &&
               pattern is null
            ? null
            : new YamlConfigStringConstraints(
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
    ///     校验数值标量的区间与步进约束。
    ///     该方法把解析失败、闭区间、开区间和步进诊断集中到数值路径，避免主调度方法继续增长。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="rawValue">原始 YAML 标量值。</param>
    /// <param name="normalizedValue">归一化后的比较值。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    /// <param name="constraints">数值约束对象。</param>
    private static void ValidateNumericScalarConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        string rawValue,
        string normalizedValue,
        YamlConfigSchemaNode schemaNode,
        YamlConfigNumericConstraints? constraints)
    {
        if (constraints is null)
        {
            return;
        }

        var numericValue = ParseComparableNumericValue(
            tableName,
            yamlPath,
            displayPath,
            rawValue,
            normalizedValue,
            schemaNode);
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
                detail: $"Minimum allowed value: {constraints.Minimum.Value.ToString(CultureInfo.InvariantCulture)}.");
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
                detail: $"Exclusive minimum allowed value: {constraints.ExclusiveMinimum.Value.ToString(CultureInfo.InvariantCulture)}.");
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
                detail: $"Maximum allowed value: {constraints.Maximum.Value.ToString(CultureInfo.InvariantCulture)}.");
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
                detail: $"Exclusive maximum allowed value: {constraints.ExclusiveMaximum.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (constraints.MultipleOf.HasValue &&
            !IsMultipleOf(normalizedValue, numericValue, constraints.MultipleOf.Value))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must be a multiple of {constraints.MultipleOf.Value.ToString(CultureInfo.InvariantCulture)}, but the current YAML scalar value is '{rawValue}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: rawValue,
                detail: $"Required numeric step: {constraints.MultipleOf.Value.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    /// <summary>
    ///     将归一化后的数值文本还原为双精度值，用于统一后续区间比较。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="rawValue">原始 YAML 标量值。</param>
    /// <param name="normalizedValue">归一化后的比较值。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    /// <returns>可比较的双精度值。</returns>
    private static double ParseComparableNumericValue(
        string tableName,
        string yamlPath,
        string displayPath,
        string rawValue,
        string normalizedValue,
        YamlConfigSchemaNode schemaNode)
    {
        if (double.TryParse(
                normalizedValue,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var numericValue))
        {
            return numericValue;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.UnexpectedFailure,
            tableName,
            $"Property '{displayPath}' in config file '{yamlPath}' could not be normalized into a comparable numeric value.",
            yamlPath: yamlPath,
            schemaPath: schemaNode.SchemaPathHint,
            displayPath: GetDiagnosticPath(displayPath),
            rawValue: rawValue);
    }

    /// <summary>
    ///     校验字符串标量的长度与模式约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="rawValue">原始 YAML 标量值。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    /// <param name="constraints">字符串约束对象。</param>
    private static void ValidateStringScalarConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        string rawValue,
        YamlConfigSchemaNode schemaNode,
        YamlConfigStringConstraints? constraints)
    {
        if (constraints is null)
        {
            return;
        }

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
    ///     校验数组是否满足去重约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="sequenceNode">实际数组节点。</param>
    /// <param name="schemaNode">数组 schema 节点。</param>
    private static void ValidateArrayUniqueItemsConstraint(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlSequenceNode sequenceNode,
        YamlConfigSchemaNode schemaNode)
    {
        var constraints = schemaNode.ArrayConstraints;
        if (constraints is null ||
            !constraints.UniqueItems ||
            schemaNode.ItemNode is null)
        {
            return;
        }

        // The canonical item key uses schema-aware normalization so object key order,
        // scalar quoting, and numeric formatting do not accidentally bypass uniqueItems.
        Dictionary<string, int> seenItems = new(StringComparer.Ordinal);
        for (var itemIndex = 0; itemIndex < sequenceNode.Children.Count; itemIndex++)
        {
            var itemNode = sequenceNode.Children[itemIndex];
            var comparableValue = BuildComparableNodeValue(itemNode, schemaNode.ItemNode);
            if (seenItems.TryGetValue(comparableValue, out var existingIndex))
            {
                var itemPath = $"{displayPath}[{itemIndex}]";
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.ConstraintViolation,
                    tableName,
                    $"Property '{displayPath}' in config file '{yamlPath}' requires unique array items, but item '{itemPath}' duplicates '{displayPath}[{existingIndex}]'.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: itemPath,
                    rawValue: DescribeYamlNodeForDiagnostics(itemNode, schemaNode.ItemNode),
                    detail: "The schema declares uniqueItems = true.");
            }

            seenItems.Add(comparableValue, itemIndex);
        }
    }

    /// <summary>
    ///     将一个已通过结构校验的 YAML 节点归一化为可比较字符串。
    ///     该键同时服务于 <c>uniqueItems</c> 与 <c>const</c>，
    ///     因此要忽略对象字段顺序和字符串引号形式。
    /// </summary>
    /// <param name="node">YAML 节点。</param>
    /// <param name="schemaNode">对应 schema 节点。</param>
    /// <returns>可稳定比较的归一化键。</returns>
    private static string BuildComparableNodeValue(YamlNode node, YamlConfigSchemaNode schemaNode)
    {
        return schemaNode.NodeType switch
        {
            YamlConfigSchemaPropertyType.Object => BuildComparableObjectValue(node, schemaNode),
            YamlConfigSchemaPropertyType.Array => BuildComparableArrayValue(node, schemaNode),
            YamlConfigSchemaPropertyType.Integer => BuildComparableScalarValue(node, schemaNode),
            YamlConfigSchemaPropertyType.Number => BuildComparableScalarValue(node, schemaNode),
            YamlConfigSchemaPropertyType.Boolean => BuildComparableScalarValue(node, schemaNode),
            YamlConfigSchemaPropertyType.String => BuildComparableScalarValue(node, schemaNode),
            _ => throw new InvalidOperationException($"Unsupported schema node type '{schemaNode.NodeType}'.")
        };
    }

    /// <summary>
    ///     构建对象节点的可比较键。
    ///     对象字段会先按属性名排序，避免 YAML 原始字段顺序影响 <c>uniqueItems</c> 的等价关系。
    /// </summary>
    /// <param name="node">YAML 节点。</param>
    /// <param name="schemaNode">对象 schema 节点。</param>
    /// <returns>对象节点的稳定比较键。</returns>
    private static string BuildComparableObjectValue(YamlNode node, YamlConfigSchemaNode schemaNode)
    {
        if (node is not YamlMappingNode mappingNode)
        {
            throw new InvalidOperationException("Validated object nodes must be YAML mappings.");
        }

        var properties = schemaNode.Properties
            ?? throw new InvalidOperationException("Validated object nodes must expose declared properties.");
        var objectEntries = new List<KeyValuePair<string, string>>(mappingNode.Children.Count);
        foreach (var entry in mappingNode.Children)
        {
            if (entry.Key is not YamlScalarNode keyNode ||
                keyNode.Value is null ||
                !properties.TryGetValue(keyNode.Value, out var propertySchema))
            {
                throw new InvalidOperationException("Validated object nodes must use declared scalar property names.");
            }

            objectEntries.Add(
                new KeyValuePair<string, string>(
                    keyNode.Value,
                    BuildComparableNodeValue(entry.Value, propertySchema)));
        }

        objectEntries.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        return string.Join(
            "|",
            objectEntries.Select(static entry =>
                $"{entry.Key.Length.ToString(CultureInfo.InvariantCulture)}:{entry.Key}={entry.Value.Length.ToString(CultureInfo.InvariantCulture)}:{entry.Value}"));
    }

    /// <summary>
    ///     构建数组节点的可比较键。
    ///     数组仍保留元素顺序，因为 <c>uniqueItems</c> 只忽略对象字段顺序，不忽略数组顺序。
    /// </summary>
    /// <param name="node">YAML 节点。</param>
    /// <param name="schemaNode">数组 schema 节点。</param>
    /// <returns>数组节点的稳定比较键。</returns>
    private static string BuildComparableArrayValue(YamlNode node, YamlConfigSchemaNode schemaNode)
    {
        if (node is not YamlSequenceNode sequenceNode ||
            schemaNode.ItemNode is null)
        {
            throw new InvalidOperationException("Validated array nodes must be YAML sequences with item schema.");
        }

        return "[" +
               string.Join(
                   ",",
                   sequenceNode.Children.Select(
                       item =>
                       {
                           var comparableValue = BuildComparableNodeValue(item, schemaNode.ItemNode);
                           return $"{comparableValue.Length.ToString(CultureInfo.InvariantCulture)}:{comparableValue}";
                       })) +
               "]";
    }

    /// <summary>
    ///     构建标量节点的可比较键。
    ///     标量会沿用与 enum / 引用校验一致的归一化规则，避免数字格式和引号形式导致伪差异。
    /// </summary>
    /// <param name="node">YAML 节点。</param>
    /// <param name="schemaNode">标量 schema 节点。</param>
    /// <returns>标量节点的稳定比较键。</returns>
    private static string BuildComparableScalarValue(YamlNode node, YamlConfigSchemaNode schemaNode)
    {
        if (node is not YamlScalarNode scalarNode ||
            scalarNode.Value is null)
        {
            throw new InvalidOperationException("Validated scalar nodes must be YAML scalars.");
        }

        var normalizedScalar = NormalizeScalarValue(schemaNode.NodeType, scalarNode.Value);
        return $"{schemaNode.NodeType}:{normalizedScalar.Length.ToString(CultureInfo.InvariantCulture)}:{normalizedScalar}";
    }

    /// <summary>
    ///     为唯一性诊断提取一个可读的节点摘要。
    /// </summary>
    /// <param name="node">YAML 节点。</param>
    /// <param name="schemaNode">对应 schema 节点。</param>
    /// <returns>诊断摘要。</returns>
    private static string DescribeYamlNodeForDiagnostics(YamlNode node, YamlConfigSchemaNode schemaNode)
    {
        return schemaNode.NodeType switch
        {
            YamlConfigSchemaPropertyType.Object => "{...}",
            YamlConfigSchemaPropertyType.Array => "[...]",
            _ when node is YamlScalarNode scalarNode => scalarNode.Value ?? string.Empty,
            _ => node.GetType().Name
        };
    }

    /// <summary>
    ///     判断数值是否满足 <c>multipleOf</c>。
    ///     优先按十进制字面量做精确整倍数判断，
    ///     以同时避免 0.1 / 0.01 这类十进制步进的伪失败和大数量级非整倍数的伪通过；
    ///     只有当值超出精确十进制路径时才退回双精度容差比较。
    /// </summary>
    /// <param name="normalizedValue">用于数值比较的规范化 YAML 标量文本。</param>
    /// <param name="value">当前值。</param>
    /// <param name="divisor">步进约束。</param>
    /// <returns>是否满足整倍数关系。</returns>
    private static bool IsMultipleOf(string normalizedValue, double value, double divisor)
    {
        if (TryIsExactDecimalMultiple(normalizedValue, divisor, out var exactResult))
        {
            return exactResult;
        }

        var quotient = value / divisor;
        var nearestInteger = Math.Round(quotient);
        var tolerance = 1e-9 * Math.Max(1d, Math.Abs(quotient));
        return Math.Abs(quotient - nearestInteger) <= tolerance;
    }

    /// <summary>
    ///     尝试按十进制字面量精确判断 <c>multipleOf</c>。
    ///     该路径直接对齐 YAML / JSON 中常见的有限十进制写法，
    ///     避免双精度舍入把明显的非整倍数误判为合法。
    /// </summary>
    /// <param name="valueText">规范化后的 YAML 数值文本。</param>
    /// <param name="divisor">Schema 声明的步进约束。</param>
    /// <param name="isMultiple">精确路径下的判断结果。</param>
    /// <returns>是否成功进入精确十进制判断路径。</returns>
    private static bool TryIsExactDecimalMultiple(string valueText, double divisor, out bool isMultiple)
    {
        var divisorText = divisor.ToString("R", CultureInfo.InvariantCulture);
        if (!TryParseExactDecimal(valueText, out var valueSignificand, out var valueScale) ||
            !TryParseExactDecimal(divisorText, out var divisorSignificand, out var divisorScale) ||
            divisorSignificand.IsZero)
        {
            isMultiple = false;
            return false;
        }

        var commonScale = Math.Max(valueScale, divisorScale);
        var scaledValue = ScaleDecimalSignificand(valueSignificand, valueScale, commonScale);
        var scaledDivisor = ScaleDecimalSignificand(divisorSignificand, divisorScale, commonScale);
        isMultiple = scaledValue % scaledDivisor == BigInteger.Zero;
        return true;
    }

    /// <summary>
    ///     将有限十进制或科学计数法文本拆成“整数有效数字 + 十进制位数”形式。
    ///     这样可以把整倍数判断转成同一尺度下的整数取模，避免浮点误差参与计算。
    /// </summary>
    /// <param name="text">待解析的数值文本。</param>
    /// <param name="significand">去掉小数点后的有效数字。</param>
    /// <param name="scale">十进制缩放位数；原值等于 <paramref name="significand" /> / 10^<paramref name="scale" />。</param>
    /// <returns>是否成功解析为有限十进制数。</returns>
    private static bool TryParseExactDecimal(string text, out BigInteger significand, out int scale)
    {
        var match = ExactDecimalPattern.Match(text);
        if (!match.Success)
        {
            significand = BigInteger.Zero;
            scale = 0;
            return false;
        }

        var exponentGroup = match.Groups["exponent"].Value;
        var exponent = 0;
        if (!string.IsNullOrEmpty(exponentGroup) &&
            !int.TryParse(exponentGroup, NumberStyles.Integer, CultureInfo.InvariantCulture, out exponent))
        {
            significand = BigInteger.Zero;
            scale = 0;
            return false;
        }

        var integerDigits = match.Groups["integer"].Value;
        var fractionDigits = match.Groups["fraction"].Success
            ? match.Groups["fraction"].Value
            : match.Groups["fractionOnly"].Value;
        var digits = string.Concat(integerDigits, fractionDigits);
        if (digits.Length == 0)
        {
            digits = "0";
        }

        digits = digits.TrimStart('0');
        if (digits.Length == 0)
        {
            significand = BigInteger.Zero;
            scale = 0;
            return true;
        }

        scale = checked(fractionDigits.Length - exponent);
        if (scale < 0)
        {
            digits = string.Concat(digits, new string('0', -scale));
            scale = 0;
        }

        while (scale > 0 && digits[^1] == '0')
        {
            digits = digits[..^1];
            scale--;
        }

        significand = BigInteger.Parse(digits, CultureInfo.InvariantCulture);
        if (match.Groups["sign"].Value == "-")
        {
            significand = BigInteger.Negate(significand);
        }

        return true;
    }

    /// <summary>
    ///     将十进制有效数字放大到目标尺度，便于在同一量纲下执行整数取模。
    /// </summary>
    /// <param name="significand">原始有效数字。</param>
    /// <param name="currentScale">当前十进制位数。</param>
    /// <param name="targetScale">目标十进制位数。</param>
    /// <returns>放大到目标尺度后的有效数字。</returns>
    private static BigInteger ScaleDecimalSignificand(BigInteger significand, int currentScale, int targetScale)
    {
        if (currentScale == targetScale)
        {
            return significand;
        }

        return significand * BigInteger.Pow(10, targetScale - currentScale);
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
    ///     将 schema 关键字中的标量值归一化到运行时比较字符串。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="keywordName">关键字名称。</param>
    /// <param name="expectedType">期望的标量类型。</param>
    /// <param name="item">当前关键字值节点。</param>
    /// <returns>归一化后的字符串值。</returns>
    private static string NormalizeKeywordScalarValue(
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
    private readonly NodeChildren _children;
    private readonly NodeValidation _validation;

    /// <summary>
    ///     创建对象节点描述。
    /// </summary>
    /// <param name="properties">对象属性集合。</param>
    /// <param name="requiredProperties">对象必填属性集合。</param>
    /// <param name="objectConstraints">对象属性数量约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>对象节点模型。</returns>
    public static YamlConfigSchemaNode CreateObject(
        IReadOnlyDictionary<string, YamlConfigSchemaNode>? properties,
        IReadOnlyCollection<string>? requiredProperties,
        YamlConfigObjectConstraints? objectConstraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Object,
            new NodeChildren(properties, requiredProperties, itemNode: null),
            new NodeValidation(
                referenceTableName: null,
                allowedValues: null,
                constraints: null,
                arrayConstraints: null,
                objectConstraints,
                constantValue: null),
            schemaPathHint);
    }

    /// <summary>
    ///     创建数组节点描述。
    /// </summary>
    /// <param name="itemNode">数组元素节点。</param>
    /// <param name="arrayConstraints">数组元素数量约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>数组节点模型。</returns>
    public static YamlConfigSchemaNode CreateArray(
        YamlConfigSchemaNode itemNode,
        YamlConfigArrayConstraints? arrayConstraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Array,
            new NodeChildren(properties: null, requiredProperties: null, itemNode),
            new NodeValidation(
                referenceTableName: null,
                allowedValues: null,
                constraints: null,
                arrayConstraints,
                objectConstraints: null,
                constantValue: null),
            schemaPathHint);
    }

    /// <summary>
    ///     创建标量节点描述。
    /// </summary>
    /// <param name="nodeType">标量节点类型。</param>
    /// <param name="referenceTableName">目标引用表名称。</param>
    /// <param name="allowedValues">标量允许值集合。</param>
    /// <param name="constraints">标量范围与长度约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>标量节点模型。</returns>
    public static YamlConfigSchemaNode CreateScalar(
        YamlConfigSchemaPropertyType nodeType,
        string? referenceTableName,
        IReadOnlyCollection<string>? allowedValues,
        YamlConfigScalarConstraints? constraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            nodeType,
            NodeChildren.None,
            new NodeValidation(
                referenceTableName,
                allowedValues,
                constraints,
                arrayConstraints: null,
                objectConstraints: null,
                constantValue: null),
            schemaPathHint);
    }

    private YamlConfigSchemaNode(
        YamlConfigSchemaPropertyType nodeType,
        NodeChildren children,
        NodeValidation validation,
        string schemaPathHint)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(schemaPathHint);

        _children = children;
        _validation = validation;
        NodeType = nodeType;
        Properties = children.Properties;
        RequiredProperties = children.RequiredProperties;
        ItemNode = children.ItemNode;
        ReferenceTableName = validation.ReferenceTableName;
        AllowedValues = validation.AllowedValues;
        Constraints = validation.Constraints;
        ArrayConstraints = validation.ArrayConstraints;
        ObjectConstraints = validation.ObjectConstraints;
        ConstantValue = validation.ConstantValue;
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
    ///     获取对象属性数量约束；未声明时返回空。
    /// </summary>
    public YamlConfigObjectConstraints? ObjectConstraints { get; }

    /// <summary>
    ///     获取数组元素数量约束；未声明时返回空。
    /// </summary>
    public YamlConfigArrayConstraints? ArrayConstraints { get; }

    /// <summary>
    ///     获取节点常量约束；未声明 <c>const</c> 时返回空。
    /// </summary>
    public YamlConfigConstantValue? ConstantValue { get; }

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
            _children,
            _validation.WithReferenceTable(referenceTableName),
            SchemaPathHint);
    }

    /// <summary>
    ///     基于当前节点复制一个只替换常量约束的新节点。
    /// </summary>
    /// <param name="constantValue">新的常量约束。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithConstantValue(YamlConfigConstantValue? constantValue)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithConstantValue(constantValue),
            SchemaPathHint);
    }

    private sealed class NodeChildren
    {
        public static NodeChildren None { get; } = new(properties: null, requiredProperties: null, itemNode: null);

        public NodeChildren(
            IReadOnlyDictionary<string, YamlConfigSchemaNode>? properties,
            IReadOnlyCollection<string>? requiredProperties,
            YamlConfigSchemaNode? itemNode)
        {
            Properties = properties;
            RequiredProperties = requiredProperties;
            ItemNode = itemNode;
        }

        public IReadOnlyDictionary<string, YamlConfigSchemaNode>? Properties { get; }

        public IReadOnlyCollection<string>? RequiredProperties { get; }

        public YamlConfigSchemaNode? ItemNode { get; }
    }

    private sealed class NodeValidation
    {
        public static NodeValidation None { get; } = new(
            referenceTableName: null,
            allowedValues: null,
            constraints: null,
            arrayConstraints: null,
            objectConstraints: null,
            constantValue: null);

        public NodeValidation(
            string? referenceTableName,
            IReadOnlyCollection<string>? allowedValues,
            YamlConfigScalarConstraints? constraints,
            YamlConfigArrayConstraints? arrayConstraints,
            YamlConfigObjectConstraints? objectConstraints,
            YamlConfigConstantValue? constantValue)
        {
            ReferenceTableName = referenceTableName;
            AllowedValues = allowedValues;
            Constraints = constraints;
            ArrayConstraints = arrayConstraints;
            ObjectConstraints = objectConstraints;
            ConstantValue = constantValue;
        }

        public string? ReferenceTableName { get; }

        public IReadOnlyCollection<string>? AllowedValues { get; }

        public YamlConfigScalarConstraints? Constraints { get; }

        public YamlConfigArrayConstraints? ArrayConstraints { get; }

        public YamlConfigObjectConstraints? ObjectConstraints { get; }

        public YamlConfigConstantValue? ConstantValue { get; }

        public NodeValidation WithReferenceTable(string referenceTableName)
        {
            return new NodeValidation(referenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue);
        }

        public NodeValidation WithConstantValue(YamlConfigConstantValue? constantValue)
        {
            return new NodeValidation(ReferenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, constantValue);
        }
    }
}

/// <summary>
///     表示一个节点上声明的 <c>const</c> 约束。
///     该模型同时保留稳定比较键与原始 JSON 文本，分别供运行时匹配和诊断输出复用。
/// </summary>
internal sealed class YamlConfigConstantValue
{
    /// <summary>
    ///     初始化常量约束模型。
    /// </summary>
    /// <param name="comparableValue">用于与 YAML 节点比较的稳定键。</param>
    /// <param name="displayValue">用于诊断输出的原始常量文本。</param>
    public YamlConfigConstantValue(string comparableValue, string displayValue)
    {
        ArgumentNullException.ThrowIfNull(comparableValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayValue);

        ComparableValue = comparableValue;
        DisplayValue = displayValue;
    }

    /// <summary>
    ///     获取用于运行时比较的稳定键。
    /// </summary>
    public string ComparableValue { get; }

    /// <summary>
    ///     获取用于诊断输出的原始 JSON 常量文本。
    /// </summary>
    public string DisplayValue { get; }
}

/// <summary>
///     表示一个对象节点上声明的属性数量约束。
///     该模型将对象级约束与数组 / 标量约束拆开保存，避免运行时节点继续暴露无关成员。
/// </summary>
internal sealed class YamlConfigObjectConstraints
{
    /// <summary>
    ///     初始化对象约束模型。
    /// </summary>
    /// <param name="minProperties">最小属性数量约束。</param>
    /// <param name="maxProperties">最大属性数量约束。</param>
    public YamlConfigObjectConstraints(int? minProperties, int? maxProperties)
    {
        MinProperties = minProperties;
        MaxProperties = maxProperties;
    }

    /// <summary>
    ///     获取最小属性数量约束。
    /// </summary>
    public int? MinProperties { get; }

    /// <summary>
    ///     获取最大属性数量约束。
    /// </summary>
    public int? MaxProperties { get; }
}

/// <summary>
///     聚合一个标量节点上声明的数值约束与字符串约束。
///     该包装层保留“标量字段有约束”的统一入口，同时把不同语义的约束分成更小的专用模型。
/// </summary>
internal sealed class YamlConfigScalarConstraints
{
    /// <summary>
    ///     初始化标量约束模型。
    /// </summary>
    /// <param name="numericConstraints">数值约束分组。</param>
    /// <param name="stringConstraints">字符串约束分组。</param>
    public YamlConfigScalarConstraints(
        YamlConfigNumericConstraints? numericConstraints,
        YamlConfigStringConstraints? stringConstraints)
    {
        NumericConstraints = numericConstraints;
        StringConstraints = stringConstraints;
    }

    /// <summary>
    ///     获取数值约束分组。
    /// </summary>
    public YamlConfigNumericConstraints? NumericConstraints { get; }

    /// <summary>
    ///     获取字符串约束分组。
    /// </summary>
    public YamlConfigStringConstraints? StringConstraints { get; }
}

/// <summary>
///     表示标量节点上声明的数值范围与步进约束。
///     该类型只覆盖整数 / 浮点共享的关键字，避免字符串字段继续暴露不相关的成员。
/// </summary>
internal sealed class YamlConfigNumericConstraints
{
    /// <summary>
    ///     初始化数值约束模型。
    /// </summary>
    /// <param name="minimum">最小值约束。</param>
    /// <param name="maximum">最大值约束。</param>
    /// <param name="exclusiveMinimum">开区间最小值约束。</param>
    /// <param name="exclusiveMaximum">开区间最大值约束。</param>
    /// <param name="multipleOf">数值步进约束。</param>
    public YamlConfigNumericConstraints(
        double? minimum,
        double? maximum,
        double? exclusiveMinimum,
        double? exclusiveMaximum,
        double? multipleOf)
    {
        Minimum = minimum;
        Maximum = maximum;
        ExclusiveMinimum = exclusiveMinimum;
        ExclusiveMaximum = exclusiveMaximum;
        MultipleOf = multipleOf;
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
    ///     获取数值步进约束。
    /// </summary>
    public double? MultipleOf { get; }
}

/// <summary>
///     表示标量节点上声明的字符串长度与模式约束。
///     该模型将正则原文与预编译正则绑定保存，保证诊断内容与运行时匹配逻辑保持一致。
/// </summary>
internal sealed class YamlConfigStringConstraints
{
    /// <summary>
    ///     初始化字符串约束模型。
    /// </summary>
    /// <param name="minLength">最小长度约束。</param>
    /// <param name="maxLength">最大长度约束。</param>
    /// <param name="pattern">正则模式约束原文。</param>
    /// <param name="patternRegex">已编译的正则表达式。</param>
    public YamlConfigStringConstraints(
        int? minLength,
        int? maxLength,
        string? pattern,
        Regex? patternRegex)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = pattern;
        PatternRegex = patternRegex;
    }

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
///     表示一个数组节点上声明的元素数量或去重约束。
///     该模型与标量约束拆分保存，避免数组节点继续共享不适用的标量字段。
/// </summary>
internal sealed class YamlConfigArrayConstraints
{
    /// <summary>
    ///     初始化数组约束模型。
    /// </summary>
    /// <param name="minItems">最小元素数量约束。</param>
    /// <param name="maxItems">最大元素数量约束。</param>
    /// <param name="uniqueItems">是否要求数组元素唯一。</param>
    public YamlConfigArrayConstraints(int? minItems, int? maxItems, bool uniqueItems)
    {
        MinItems = minItems;
        MaxItems = maxItems;
        UniqueItems = uniqueItems;
    }

    /// <summary>
    ///     获取最小元素数量约束。
    /// </summary>
    public int? MinItems { get; }

    /// <summary>
    ///     获取最大元素数量约束。
    /// </summary>
    public int? MaxItems { get; }

    /// <summary>
    ///     获取是否要求数组元素唯一。
    /// </summary>
    public bool UniqueItems { get; }
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
