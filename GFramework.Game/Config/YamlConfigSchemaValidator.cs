using System.Numerics;
using System.Text.RegularExpressions;
using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     提供 YAML 配置文件与 JSON Schema 之间的最小运行时校验能力。
///     该校验器与当前配置生成器、VS Code 工具支持的 schema 子集保持一致，
///     并通过递归遍历方式覆盖嵌套对象、对象数组、标量数组与深层 enum / 引用约束。
///     当前共享子集额外支持 <c>multipleOf</c>、<c>uniqueItems</c>、
///     <c>contains</c> / <c>minContains</c> / <c>maxContains</c>、
///     <c>minProperties</c>、<c>maxProperties</c>、<c>dependentRequired</c>、
///     <c>dependentSchemas</c>
///     与稳定字符串 <c>format</c> 子集，让数值步进、数组去重、数组匹配计数、
///     对象属性数量、对象内字段依赖以及条件对象子 schema 在运行时与生成器 / 工具侧保持一致。
/// </summary>
internal static class YamlConfigSchemaValidator
{
    // The runtime intentionally uses the same culture-invariant regex semantics as the
    // JS tooling so grouping and backreferences behave consistently across environments.
    private const RegexOptions SupportedPatternRegexOptions = RegexOptions.CultureInvariant;
    private const string SupportedStringFormatNames = "'date', 'date-time', 'duration', 'email', 'time', 'uri', 'uuid'";

    private static readonly Regex ExactDecimalPattern = new(
        @"^(?<sign>[+-]?)(?:(?<integer>\d+)(?:\.(?<fraction>\d*))?|\.(?<fractionOnly>\d+))(?:[eE](?<exponent>[+-]?\d+))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SupportedEmailFormatRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SupportedDateFormatRegex = new(
        @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SupportedDateTimeFormatRegex = new(
        @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(?<fraction>\.\d+)?(?<offset>Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SupportedDurationFormatRegex = new(
        @"^P(?:(?<days>\d+)D)?(?:T(?:(?<hours>\d+)H)?(?:(?<minutes>\d+)M)?(?:(?<seconds>\d+(?:\.\d+)?)S)?)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SupportedTimeFormatRegex = new(
        @"^(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(?<fraction>\.\d+)?(?<offset>Z|[+-]\d{2}:\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SupportedUriSchemeRegex = new(
        @"^[A-Za-z][A-Za-z0-9+\.-]*:",
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

        return ParseLoadedSchema(tableName, schemaPath, schemaText);
    }

    /// <summary>
    ///     从磁盘同步加载并解析一个 JSON Schema 文件。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <returns>解析后的 schema 模型。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="tableName" /> 为空时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="schemaPath" /> 为空时抛出。</exception>
    /// <exception cref="ConfigLoadException">当 schema 文件不存在或内容非法时抛出。</exception>
    internal static YamlConfigSchema Load(
        string tableName,
        string schemaPath)
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
            schemaText = File.ReadAllText(schemaPath);
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

        return ParseLoadedSchema(tableName, schemaPath, schemaText);
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
    ///     解析已读取到内存中的 schema 文本，并构造运行时最小模型。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径，仅用于诊断信息。</param>
    /// <param name="schemaText">Schema 文本内容。</param>
    /// <returns>解析后的 schema 模型。</returns>
    private static YamlConfigSchema ParseLoadedSchema(
        string tableName,
        string schemaPath,
        string schemaText)
    {
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
            // Preserve a deterministic dependency order so hot-reload bookkeeping and tests
            // do not depend on HashSet enumeration details.
            var orderedReferencedTableNames = referencedTableNames
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();

            return new YamlConfigSchema(schemaPath, rootNode, orderedReferencedTableNames);
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

        var parsedNode = typeName switch
        {
            "object" => ParseObjectSchemaNode(
                tableName,
                schemaPath,
                propertyPath,
                element,
                referenceTableName,
                isRoot),
            "array" => ParseArrayNode(tableName, schemaPath, propertyPath, element, referenceTableName),
            "integer" => CreateScalarNode(
                tableName,
                schemaPath,
                propertyPath,
                YamlConfigSchemaPropertyType.Integer,
                element,
                referenceTableName),
            "number" => CreateScalarNode(
                tableName,
                schemaPath,
                propertyPath,
                YamlConfigSchemaPropertyType.Number,
                element,
                referenceTableName),
            "boolean" => CreateScalarNode(
                tableName,
                schemaPath,
                propertyPath,
                YamlConfigSchemaPropertyType.Boolean,
                element,
                referenceTableName),
            "string" => CreateScalarNode(
                tableName,
                schemaPath,
                propertyPath,
                YamlConfigSchemaPropertyType.String,
                element,
                referenceTableName),
            _ => throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses unsupported type '{typeName}'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath),
                rawValue: typeName)
        };
        return parsedNode.WithNegatedSchemaNode(ParseNegatedSchemaNode(tableName, schemaPath, propertyPath, element));
    }

    /// <summary>
    ///     解析对象类型 schema，并在进入对象节点解析前先校验 ref-table 是否兼容。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象属性路径。</param>
    /// <param name="element">对象 schema 节点。</param>
    /// <param name="referenceTableName">声明在当前节点上的目标引用表。</param>
    /// <param name="isRoot">是否为根节点。</param>
    /// <returns>对象节点模型。</returns>
    private static YamlConfigSchemaNode ParseObjectSchemaNode(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        string? referenceTableName,
        bool isRoot)
    {
        EnsureReferenceKeywordIsSupported(
            tableName,
            schemaPath,
            propertyPath,
            YamlConfigSchemaPropertyType.Object,
            referenceTableName);
        return ParseObjectNode(tableName, schemaPath, propertyPath, element, isRoot);
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
            ParseObjectConstraints(tableName, schemaPath, propertyPath, element, properties),
            schemaPath);
        objectNode = objectNode.WithAllowedValues(
            ParseEnumValues(tableName, schemaPath, propertyPath, element, objectNode, "enum"));
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
            allowedValues: null,
            ParseArrayConstraints(tableName, schemaPath, propertyPath, element),
            schemaPath);
        arrayNode = arrayNode.WithAllowedValues(
            ParseEnumValues(tableName, schemaPath, propertyPath, element, arrayNode, "enum"));
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
            allowedValues: null,
            ParseScalarConstraints(tableName, schemaPath, propertyPath, element, nodeType),
            schemaPath);
        scalarNode = scalarNode.WithAllowedValues(
            ParseEnumValues(tableName, schemaPath, propertyPath, element, scalarNode, "enum"));
        return scalarNode.WithConstantValue(
            ParseConstantValue(tableName, schemaPath, propertyPath, element, scalarNode));
    }

    /// <summary>
    ///     解析节点上的 <c>not</c> 约束。
    ///     该子 schema 继续复用同一套节点解析逻辑，保证 Runtime / Generator / Tooling
    ///     对深层结构与格式白名单的解释保持一致。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">当前节点路径。</param>
    /// <param name="element">Schema JSON 节点。</param>
    /// <returns>解析后的 negated schema；未声明时返回空。</returns>
    private static YamlConfigSchemaNode? ParseNegatedSchemaNode(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element)
    {
        if (!element.TryGetProperty("not", out var notElement))
        {
            return null;
        }

        if (notElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare 'not' as an object-valued schema.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return ParseNode(
            tableName,
            schemaPath,
            BuildNestedSchemaPath(propertyPath, "not"),
            notElement);
    }

    /// <summary>
    ///     为 <c>contains</c> / <c>not</c> / <c>dependentSchemas</c> 这类内联子 schema 构建稳定的诊断路径。
    /// </summary>
    /// <param name="propertyPath">当前节点路径。</param>
    /// <param name="suffix">内联子 schema 后缀。</param>
    /// <returns>带内联后缀的 schema 路径。</returns>
    private static string BuildNestedSchemaPath(string propertyPath, string suffix)
    {
        return string.IsNullOrWhiteSpace(propertyPath)
            ? $"[{suffix}]"
            : $"{propertyPath}[{suffix}]";
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
    /// <param name="allowUnknownObjectProperties">
    ///     是否允许对象节点出现当前 schema 子树未声明的额外字段。
    ///     该开关仅用于 <c>contains</c> 试匹配，让对象子 schema 可以按“声明属性子集匹配”工作；
    ///     正常加载主链路仍保持未知字段即失败的严格语义。
    /// </param>
    private static void ValidateNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage>? references,
        bool allowUnknownObjectProperties = false)
    {
        switch (schemaNode.NodeType)
        {
            case YamlConfigSchemaPropertyType.Object:
                ValidateObjectNode(
                    tableName,
                    yamlPath,
                    displayPath,
                    node,
                    schemaNode,
                    references,
                    allowUnknownObjectProperties);
                return;

            case YamlConfigSchemaPropertyType.Array:
                ValidateArrayNode(
                    tableName,
                    yamlPath,
                    displayPath,
                    node,
                    schemaNode,
                    references,
                    allowUnknownObjectProperties);
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
    /// <param name="allowUnknownObjectProperties">
    ///     是否允许当前对象包含 schema 子树未声明的额外字段。
    /// </param>
    private static void ValidateObjectNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage>? references,
        bool allowUnknownObjectProperties)
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
                if (allowUnknownObjectProperties)
                {
                    continue;
                }

                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.UnknownProperty,
                    tableName,
                    $"Config file '{yamlPath}' contains unknown property '{propertyPath}' that is not declared in schema '{schemaNode.SchemaPathHint}'.",
                    yamlPath: yamlPath,
                    schemaPath: schemaNode.SchemaPathHint,
                    displayPath: propertyPath);
            }

            ValidateNode(
                tableName,
                yamlPath,
                propertyPath,
                entry.Value,
                propertySchema,
                references,
                allowUnknownObjectProperties);
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

        ValidateObjectConstraints(
            tableName,
            yamlPath,
            displayPath,
            mappingNode,
            seenProperties,
            schemaNode,
            references);

        ValidateAllowedValues(tableName, yamlPath, displayPath, mappingNode, schemaNode);
        ValidateConstantValue(tableName, yamlPath, displayPath, mappingNode, schemaNode);
        ValidateNegatedSchemaConstraint(tableName, yamlPath, displayPath, mappingNode, schemaNode);
    }

    /// <summary>
    ///     校验对象节点声明的数量约束与条件对象约束。
    ///     该阶段除了检查 <c>minProperties</c> / <c>maxProperties</c>，还会复用同一份 sibling 集合处理
    ///     <c>dependentRequired</c>，并在 <c>dependentSchemas</c> 命中时以 focused constraint block 语义
    ///     对整个 <paramref name="mappingNode" /> 做额外试匹配。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">对象字段路径；根对象时为空。</param>
    /// <param name="mappingNode">当前 YAML 对象节点；用于让条件子 schema 在完整对象视图上做匹配。</param>
    /// <param name="seenProperties">当前对象已出现的属性集合。</param>
    /// <param name="schemaNode">对象 schema 节点。</param>
    /// <param name="references">
    ///     可选的跨表引用收集器；当 <c>dependentSchemas</c> 命中且匹配成功时，只会回写该条件分支新增的引用。
    /// </param>
    private static void ValidateObjectConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlMappingNode mappingNode,
        HashSet<string> seenProperties,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage>? references)
    {
        var constraints = schemaNode.ObjectConstraints;
        if (constraints is null)
        {
            return;
        }

        var propertyCount = seenProperties.Count;
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
                detail:
                $"Minimum property count: {constraints.MinProperties.Value.ToString(CultureInfo.InvariantCulture)}.");
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
                detail:
                $"Maximum property count: {constraints.MaxProperties.Value.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (constraints.DependentRequired is not null &&
            constraints.DependentRequired.Count > 0)
        {
            // Reuse the collected sibling-name set so the main validation path and
            // the contains/not matcher both interpret object dependencies identically.
            foreach (var dependency in constraints.DependentRequired)
            {
                if (!seenProperties.Contains(dependency.Key))
                {
                    continue;
                }

                var triggerPath = CombineDisplayPath(displayPath, dependency.Key);
                foreach (var dependentProperty in dependency.Value)
                {
                    if (seenProperties.Contains(dependentProperty))
                    {
                        continue;
                    }

                    var requiredPath = CombineDisplayPath(displayPath, dependentProperty);
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.MissingRequiredProperty,
                        tableName,
                        $"Property '{requiredPath}' in config file '{yamlPath}' is required when sibling property '{triggerPath}' is present.",
                        yamlPath: yamlPath,
                        schemaPath: schemaNode.SchemaPathHint,
                        displayPath: requiredPath,
                        detail:
                        $"Dependent requirement: when '{triggerPath}' exists, '{requiredPath}' must also be declared.");
                }
            }
        }

        if (constraints.DependentSchemas is null ||
            constraints.DependentSchemas.Count == 0)
        {
            return;
        }

        foreach (var dependency in constraints.DependentSchemas)
        {
            if (!seenProperties.Contains(dependency.Key))
            {
                continue;
            }

            var triggerPath = CombineDisplayPath(displayPath, dependency.Key);
            // dependentSchemas acts as an additional conditional constraint block on the
            // current object. Keep undeclared sibling fields outside the dependent sub-schema
            // from blocking the match so schema authors can express focused follow-up rules.
            // The trial matcher merges only new reference usages back into the outer collector,
            // so re-checking the same scalar via a conditional sub-schema does not duplicate
            // cross-table validation work later in the loader pipeline.
            if (TryMatchSchemaNode(
                    tableName,
                    yamlPath,
                    displayPath,
                    mappingNode,
                    dependency.Value,
                    references,
                    allowUnknownObjectProperties: true))
            {
                continue;
            }

            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"{subject} in config file '{yamlPath}' must satisfy the 'dependentSchemas' schema triggered by sibling property '{triggerPath}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                detail:
                $"Dependent schema: when '{triggerPath}' exists, the current object must satisfy the corresponding inline schema.");
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
    /// <param name="allowUnknownObjectProperties">
    ///     是否允许数组元素内的对象节点包含 schema 子树未声明的额外字段。
    /// </param>
    private static void ValidateArrayNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage>? references,
        bool allowUnknownObjectProperties)
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
                references,
                allowUnknownObjectProperties);
        }

        ValidateArrayUniqueItemsConstraint(tableName, yamlPath, displayPath, sequenceNode, schemaNode);
        ValidateArrayContainsConstraints(tableName, yamlPath, displayPath, sequenceNode, schemaNode, references);
        ValidateAllowedValues(tableName, yamlPath, displayPath, sequenceNode, schemaNode);
        ValidateConstantValue(tableName, yamlPath, displayPath, sequenceNode, schemaNode);
        ValidateNegatedSchemaConstraint(tableName, yamlPath, displayPath, sequenceNode, schemaNode);
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
        ValidateAllowedValues(tableName, yamlPath, displayPath, scalarNode, schemaNode);

        if (schemaNode.Constraints is not null)
        {
            ValidateScalarConstraints(tableName, yamlPath, displayPath, value, normalizedValue, schemaNode);
        }

        ValidateConstantValue(tableName, yamlPath, displayPath, scalarNode, schemaNode);
        ValidateNegatedSchemaConstraint(tableName, yamlPath, displayPath, scalarNode, schemaNode);

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
    ///     解析 <c>enum</c>，并在读取阶段将每个候选值预归一化成与运行时 YAML 相同的稳定比较键。
    ///     这样对象、数组和标量节点都可以复用同一套匹配逻辑，而不必在每次加载时重新解释 schema 字面量。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaNode">当前节点的已解析 schema 模型。</param>
    /// <param name="keywordName">当前读取的关键字名称。</param>
    /// <returns>归一化后的允许值集合；未声明时返回空。</returns>
    private static IReadOnlyCollection<YamlConfigAllowedValue>? ParseEnumValues(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaNode schemaNode,
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

        var allowedValues = new List<YamlConfigAllowedValue>();
        foreach (var item in enumElement.EnumerateArray())
        {
            allowedValues.Add(
                new YamlConfigAllowedValue(
                    BuildComparableConstantValue(tableName, schemaPath, propertyPath, keywordName, item, schemaNode),
                    BuildEnumDisplayValue(item)));
        }

        return allowedValues;
    }

    /// <summary>
    ///     读取一个 <c>enum</c> 候选值的展示文本。
    ///     这里直接保留原始 JSON 字面量，避免字符串引号、对象字段顺序或数组结构在诊断中被二次格式化后丢失上下文。
    /// </summary>
    /// <param name="element">Schema 中的单个枚举值。</param>
    /// <returns>适合放入诊断消息的展示文本。</returns>
    private static string BuildEnumDisplayValue(JsonElement element)
    {
        return element.GetRawText();
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
                   element.EnumerateArray().Select((item, index) =>
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
    ///     以及 `string` 上的 `minLength/maxLength/pattern/format`。
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
        var formatConstraint = TryParseFormatConstraint(tableName, schemaPath, propertyPath, element, nodeType);

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
            pattern,
            formatConstraint);

        return numericConstraints is null && stringConstraints is null
            ? null
            : new YamlConfigScalarConstraints(numericConstraints, stringConstraints);
    }

    /// <summary>
    ///     解析数组节点支持的元素数量、去重与 <c>contains</c> 匹配数量约束。
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
        var containsConstraints = ParseArrayContainsConstraints(tableName, schemaPath, propertyPath, element);

        if (minItems.HasValue && maxItems.HasValue && minItems.Value > maxItems.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares 'minItems' greater than 'maxItems'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return !minItems.HasValue && !maxItems.HasValue && !uniqueItems && containsConstraints is null
            ? null
            : new YamlConfigArrayConstraints(minItems, maxItems, uniqueItems, containsConstraints);
    }

    /// <summary>
    ///     解析数组节点声明的 <c>contains</c> 约束及其匹配数量边界。
    ///     运行时会把 <c>contains</c> 解析成独立的 schema 子树，后续逐项复用同一套递归校验逻辑判断“是否匹配”。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">数组字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <returns>数组 contains 约束模型；未声明时返回空。</returns>
    private static YamlConfigArrayContainsConstraints? ParseArrayContainsConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element)
    {
        var minContains = TryParseArrayLengthConstraint(tableName, schemaPath, propertyPath, element, "minContains");
        var maxContains = TryParseArrayLengthConstraint(tableName, schemaPath, propertyPath, element, "maxContains");
        if (!element.TryGetProperty("contains", out var containsElement))
        {
            if (minContains.HasValue || maxContains.HasValue)
            {
                var keywordName = minContains.HasValue ? "minContains" : "maxContains";
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{propertyPath}' in schema file '{schemaPath}' declares '{keywordName}' without a companion 'contains' schema.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath));
            }

            return null;
        }

        if (containsElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare 'contains' as an object-valued schema.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var containsNode = ParseNode(tableName, schemaPath, $"{propertyPath}[contains]", containsElement);
        if (containsNode.NodeType == YamlConfigSchemaPropertyType.Array)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses unsupported nested array 'contains' schemas.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var effectiveMinContains = minContains ?? 1;
        if (maxContains.HasValue && effectiveMinContains > maxContains.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' declares 'minContains' greater than 'maxContains'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        return new YamlConfigArrayContainsConstraints(containsNode, minContains, maxContains);
    }

    /// <summary>
    ///     解析对象节点支持的属性数量约束。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="properties">当前对象已声明的属性集合。</param>
    /// <returns>对象约束模型；未声明时返回空。</returns>
    private static YamlConfigObjectConstraints? ParseObjectConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
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
        var dependentRequired = ParseDependentRequiredConstraints(tableName, schemaPath, propertyPath, element, properties);
        var dependentSchemas = ParseDependentSchemasConstraints(tableName, schemaPath, propertyPath, element, properties);

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

        return !minProperties.HasValue && !maxProperties.HasValue && dependentRequired is null && dependentSchemas is null
            ? null
            : new YamlConfigObjectConstraints(minProperties, maxProperties, dependentRequired, dependentSchemas);
    }

    /// <summary>
    ///     解析对象节点声明的 <c>dependentRequired</c> 依赖关系。
    ///     该关键字只表达“当触发字段出现时，还必须同时声明哪些同级字段”，
    ///     因此这里会把触发字段与依赖字段都限制在当前对象已声明的属性集合内，
    ///     避免运行时与工具链对无效键名各自做隐式容错。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="properties">当前对象已声明的属性集合。</param>
    /// <returns>归一化后的依赖关系表；未声明或只有空依赖时返回空。</returns>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? ParseDependentRequiredConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (!element.TryGetProperty("dependentRequired", out var dependentRequiredElement))
        {
            return null;
        }

        if (dependentRequiredElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare 'dependentRequired' as an object.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var dependentRequired = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var dependency in dependentRequiredElement.EnumerateObject())
        {
            if (!properties.ContainsKey(dependency.Name))
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' declares 'dependentRequired' for undeclared property '{dependency.Name}'.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath));
            }

            if (dependency.Value.ValueKind != JsonValueKind.Array)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{dependency.Name}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'dependentRequired' as an array of sibling property names.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath));
            }

            var dependencyTargets = new List<string>();
            var seenDependencyTargets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var dependencyTarget in dependency.Value.EnumerateArray())
            {
                if (dependencyTarget.ValueKind != JsonValueKind.String)
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.SchemaUnsupported,
                        tableName,
                        $"Property '{dependency.Name}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'dependentRequired' entries as strings.",
                        schemaPath: schemaPath,
                        displayPath: GetDiagnosticPath(propertyPath));
                }

                var dependencyTargetName = dependencyTarget.GetString();
                if (string.IsNullOrWhiteSpace(dependencyTargetName))
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.SchemaUnsupported,
                        tableName,
                        $"Property '{dependency.Name}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' cannot declare blank 'dependentRequired' entries.",
                        schemaPath: schemaPath,
                        displayPath: GetDiagnosticPath(propertyPath));
                }

                if (!properties.ContainsKey(dependencyTargetName))
                {
                    throw ConfigLoadExceptionFactory.Create(
                        ConfigLoadFailureKind.SchemaUnsupported,
                        tableName,
                        $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' declares 'dependentRequired' target '{dependencyTargetName}' that is not declared in the same object schema.",
                        schemaPath: schemaPath,
                        displayPath: GetDiagnosticPath(propertyPath));
                }

                if (seenDependencyTargets.Add(dependencyTargetName))
                {
                    dependencyTargets.Add(dependencyTargetName);
                }
            }

            if (dependencyTargets.Count > 0)
            {
                dependentRequired[dependency.Name] = dependencyTargets;
            }
        }

        return dependentRequired.Count == 0
            ? null
            : dependentRequired;
    }

    /// <summary>
    ///     解析对象节点声明的 <c>dependentSchemas</c> 条件 schema。
    ///     当前实现把它作为“当触发字段出现时，当前对象还必须额外满足一段内联 schema”来解释，
    ///     因此触发字段仍限制在当前对象已声明的属性内，而具体约束则继续复用现有递归节点解析逻辑。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="properties">当前对象已声明的属性集合。</param>
    /// <returns>归一化后的触发字段到条件 schema 的映射；未声明时返回空。</returns>
    private static IReadOnlyDictionary<string, YamlConfigSchemaNode>? ParseDependentSchemasConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (!element.TryGetProperty("dependentSchemas", out var dependentSchemasElement))
        {
            return null;
        }

        if (dependentSchemasElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare 'dependentSchemas' as an object.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var dependentSchemas = new Dictionary<string, YamlConfigSchemaNode>(StringComparer.Ordinal);
        foreach (var dependency in dependentSchemasElement.EnumerateObject())
        {
            if (!properties.ContainsKey(dependency.Name))
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' declares 'dependentSchemas' for undeclared property '{dependency.Name}'.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath));
            }

            if (dependency.Value.ValueKind != JsonValueKind.Object)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{dependency.Name}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'dependentSchemas' as an object-valued schema.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath));
            }

            var dependencySchemaPath = BuildNestedSchemaPath(propertyPath, $"dependentSchemas:{dependency.Name}");
            var dependencySchemaNode = ParseNode(
                tableName,
                schemaPath,
                dependencySchemaPath,
                dependency.Value);
            if (dependencySchemaNode.NodeType != YamlConfigSchemaPropertyType.Object)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Property '{dependency.Name}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare an object-typed 'dependentSchemas' schema.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(dependencySchemaPath));
            }

            dependentSchemas[dependency.Name] = dependencySchemaNode;
        }

        return dependentSchemas.Count == 0
            ? null
            : dependentSchemas;
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
        var multipleOf =
            TryParseNumericConstraint(tableName, schemaPath, propertyPath, element, nodeType, "multipleOf");
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
    ///     读取字符串 format 约束。
    ///     运行时只接受当前三端共享并已验证收益的稳定子集，避免把开放格式名误当成“全部支持”。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="nodeType">字段类型。</param>
    /// <returns>归一化后的 format 约束；未声明时返回空。</returns>
    private static YamlConfigStringFormatConstraint? TryParseFormatConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        YamlConfigSchemaPropertyType nodeType)
    {
        if (!element.TryGetProperty("format", out var formatElement))
        {
            return null;
        }

        if (nodeType != YamlConfigSchemaPropertyType.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' uses 'format', but only 'string' scalar types support string formats.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (formatElement.ValueKind != JsonValueKind.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{propertyPath}' in schema file '{schemaPath}' must declare 'format' as a string.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var formatName = formatElement.GetString() ?? string.Empty;
        if (TryMapSupportedStringFormat(formatName, out var kind))
        {
            return new YamlConfigStringFormatConstraint(formatName, kind);
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"Property '{propertyPath}' in schema file '{schemaPath}' declares unsupported string format '{formatName}'. Supported formats are {SupportedStringFormatNames}.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(propertyPath),
            rawValue: formatName);
    }

    /// <summary>
    ///     将 schema 原文中的 format 名称映射为运行时共享枚举。
    ///     映射阶段统一使用大小写敏感比较，避免同一 schema 在不同环境下被“宽松容错”解释成不同语义。
    /// </summary>
    /// <param name="formatName">schema 原始 format 名称。</param>
    /// <param name="kind">解析成功时输出归一化枚举。</param>
    /// <returns>当前格式是否属于共享支持子集。</returns>
    private static bool TryMapSupportedStringFormat(
        string formatName,
        out YamlConfigStringFormatKind kind)
    {
        switch (formatName)
        {
            case "date":
                kind = YamlConfigStringFormatKind.Date;
                return true;

            case "date-time":
                kind = YamlConfigStringFormatKind.DateTime;
                return true;

            case "duration":
                kind = YamlConfigStringFormatKind.Duration;
                return true;

            case "email":
                kind = YamlConfigStringFormatKind.Email;
                return true;

            case "time":
                kind = YamlConfigStringFormatKind.Time;
                return true;

            case "uri":
                kind = YamlConfigStringFormatKind.Uri;
                return true;

            case "uuid":
                kind = YamlConfigStringFormatKind.Uuid;
                return true;

            default:
                kind = default;
                return false;
        }
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
    ///     为插入句中位置的对象级 schema 关键字构造稳定描述。
    ///     这里只调整语法前缀大小写，不改变真实字段路径，避免诊断消息把 schema 作者声明的大小写一起改写。
    /// </summary>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <returns>可直接拼接到句中介词后的对象主体描述。</returns>
    private static string DescribeObjectSchemaTargetInClause(string propertyPath)
    {
        return string.IsNullOrWhiteSpace(propertyPath)
            ? "root object"
            : $"property '{propertyPath}'";
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
    ///     校验节点值是否命中声明的 <c>enum</c> 集合。
    ///     该实现与 <c>const</c> 共享同一套可比较键，保证对象字段顺序、数组顺序和数值归一化语义稳定一致。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径；根节点时为空。</param>
    /// <param name="node">当前 YAML 节点。</param>
    /// <param name="schemaNode">对应的 schema 节点。</param>
    private static void ValidateAllowedValues(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode)
    {
        var allowedValues = schemaNode.AllowedValues;
        if (allowedValues is null || allowedValues.Count == 0)
        {
            return;
        }

        var comparableValue = BuildComparableNodeValue(node, schemaNode);
        if (allowedValues.Any(allowedValue =>
                string.Equals(allowedValue.ComparableValue, comparableValue, StringComparison.Ordinal)))
        {
            return;
        }

        var subject = string.IsNullOrWhiteSpace(displayPath)
            ? "Root object"
            : $"Property '{displayPath}'";
        var displayValues = string.Join(", ", allowedValues.Select(static allowedValue => allowedValue.DisplayValue));
        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.EnumValueNotAllowed,
            tableName,
            $"{subject} in config file '{yamlPath}' must be one of [{displayValues}].",
            yamlPath: yamlPath,
            schemaPath: schemaNode.SchemaPathHint,
            displayPath: GetDiagnosticPath(displayPath),
            rawValue: DescribeYamlNodeForDiagnostics(node, schemaNode),
            detail: $"Allowed values: {displayValues}.");
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
    ///     正则会在 schema 解析阶段预编译，字符串 format 也会先归一化成共享枚举，
    ///     避免每次校验都重新解释 schema 原文。
    /// </summary>
    /// <param name="minLength">最小长度约束。</param>
    /// <param name="maxLength">最大长度约束。</param>
    /// <param name="pattern">正则模式约束。</param>
    /// <param name="formatConstraint">字符串 format 约束。</param>
    /// <returns>字符串约束对象；未声明任何字符串约束时返回空。</returns>
    private static YamlConfigStringConstraints? CreateStringScalarConstraints(
        int? minLength,
        int? maxLength,
        string? pattern,
        YamlConfigStringFormatConstraint? formatConstraint)
    {
        return !minLength.HasValue &&
               !maxLength.HasValue &&
               pattern is null &&
               formatConstraint is null
            ? null
            : new YamlConfigStringConstraints(
                minLength,
                maxLength,
                pattern,
                pattern is null
                    ? null
                    : new Regex(
                        pattern,
                        SupportedPatternRegexOptions),
                formatConstraint);
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
                detail:
                $"Exclusive maximum allowed value: {constraints.ExclusiveMaximum.Value.ToString(CultureInfo.InvariantCulture)}.");
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
                detail:
                $"Required numeric step: {constraints.MultipleOf.Value.ToString(CultureInfo.InvariantCulture)}.");
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
    ///     校验字符串标量的长度、模式与 format 约束。
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

        if (constraints.FormatConstraint is not null &&
            !MatchesSupportedStringFormat(rawValue, constraints.FormatConstraint.Kind))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must satisfy string format '{constraints.FormatConstraint.SchemaName}', but the current YAML scalar value is '{rawValue}'.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: rawValue,
                detail: $"Expected string format: {constraints.FormatConstraint.SchemaName}.");
        }
    }

    /// <summary>
    ///     判断一个字符串是否满足当前共享支持的 format 子集。
    ///     这里只接受 Runtime / Generator / Tooling 三端都能稳定解释的格式，
    ///     避免把 JSON Schema 的开放格式名直接当成“全部支持”。
    /// </summary>
    /// <param name="value">待校验的字符串值。</param>
    /// <param name="formatKind">共享 format 枚举。</param>
    /// <returns>当前字符串是否满足指定 format。</returns>
    private static bool MatchesSupportedStringFormat(
        string value,
        YamlConfigStringFormatKind formatKind)
    {
        ArgumentNullException.ThrowIfNull(value);

        return formatKind switch
        {
            YamlConfigStringFormatKind.Date => MatchesSupportedDateFormat(value),
            YamlConfigStringFormatKind.DateTime => MatchesSupportedDateTimeFormat(value),
            YamlConfigStringFormatKind.Duration => MatchesSupportedDurationFormat(value),
            YamlConfigStringFormatKind.Email => SupportedEmailFormatRegex.IsMatch(value),
            YamlConfigStringFormatKind.Time => MatchesSupportedTimeFormat(value),
            YamlConfigStringFormatKind.Uri => MatchesSupportedUriFormat(value),
            YamlConfigStringFormatKind.Uuid => Guid.TryParseExact(value, "D", out _),
            _ => false
        };
    }

    /// <summary>
    ///     判断字符串是否满足共享支持的 <c>date</c> 格式。
    ///     这里固定采用 <c>yyyy-MM-dd</c>，避免文化区域或宽松解析导致工具与运行时漂移。
    /// </summary>
    /// <param name="value">待校验的字符串值。</param>
    /// <returns>当前值是否是合法日期。</returns>
    private static bool MatchesSupportedDateFormat(string value)
    {
        return SupportedDateFormatRegex.IsMatch(value) &&
               DateOnly.TryParseExact(
                   value,
                   "yyyy-MM-dd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out _);
    }

    /// <summary>
    ///     判断字符串是否满足共享支持的 <c>date-time</c> 格式。
    ///     该格式固定要求显式时区偏移，避免消费者误把本地时区文本当成跨进程稳定配置。
    /// </summary>
    /// <param name="value">待校验的字符串值。</param>
    /// <returns>当前值是否是合法时间戳。</returns>
    private static bool MatchesSupportedDateTimeFormat(string value)
    {
        if (!SupportedDateTimeFormatRegex.IsMatch(value))
        {
            return false;
        }

        return DateTimeOffset.TryParseExact(
            value,
            ["yyyy'-'MM'-'dd'T'HH':'mm':'ssK", "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    /// <summary>
    ///     判断字符串是否满足共享支持的 <c>duration</c> 格式。
    ///     当前共享子集只接受 day-time duration：可声明 <c>D/H/M/S</c>，秒允许小数，
    ///     但拒绝 <c>Y</c> / <c>M(月)</c> / <c>W</c> 等依赖日历语义的部分，
    ///     避免不同宿主对“一个月/一年到底多长”出现解释漂移。
    /// </summary>
    /// <param name="value">待校验的字符串值。</param>
    /// <returns>当前值是否是合法持续时间文本。</returns>
    private static bool MatchesSupportedDurationFormat(string value)
    {
        var match = SupportedDurationFormatRegex.Match(value);
        if (!match.Success)
        {
            return false;
        }

        var hasDayComponent = match.Groups["days"].Success;
        var hasHourComponent = match.Groups["hours"].Success;
        var hasMinuteComponent = match.Groups["minutes"].Success;
        var hasSecondComponent = match.Groups["seconds"].Success;
        var hasAnyComponent = hasDayComponent || hasHourComponent || hasMinuteComponent || hasSecondComponent;
        if (!hasAnyComponent)
        {
            return false;
        }

        var hasTimeSection = value.Contains('T', StringComparison.Ordinal);
        if (hasTimeSection &&
            !hasHourComponent &&
            !hasMinuteComponent &&
            !hasSecondComponent)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     判断字符串是否满足共享支持的 <c>time</c> 格式。
    ///     该格式固定要求显式时区偏移，并只接受 24 小时制的合法时分秒文本，
    ///     避免不同宿主对“time-only + offset”解析默认日期或时区时产生漂移。
    /// </summary>
    /// <param name="value">待校验的字符串值。</param>
    /// <returns>当前值是否是合法时间文本。</returns>
    private static bool MatchesSupportedTimeFormat(string value)
    {
        var match = SupportedTimeFormatRegex.Match(value);
        if (!match.Success)
        {
            return false;
        }

        var hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture);
        var second = int.Parse(match.Groups["second"].Value, CultureInfo.InvariantCulture);
        if (hour > 23 || minute > 59 || second > 59)
        {
            return false;
        }

        var offset = match.Groups["offset"].Value;
        if (offset == "Z")
        {
            return true;
        }

        var offsetHour = int.Parse(offset.AsSpan(1, 2), CultureInfo.InvariantCulture);
        var offsetMinute = int.Parse(offset.AsSpan(4, 2), CultureInfo.InvariantCulture);
        return offsetHour <= 23 && offsetMinute <= 59;
    }

    /// <summary>
    ///     判断字符串是否满足共享支持的 <c>uri</c> 格式。
    ///     这里要求输入显式包含 scheme，避免把普通路径意外解释成平台相关的绝对 URI。
    /// </summary>
    /// <param name="value">待校验的字符串值。</param>
    /// <returns>当前值是否是合法绝对 URI。</returns>
    private static bool MatchesSupportedUriFormat(string value)
    {
        return SupportedUriSchemeRegex.IsMatch(value) &&
               Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.IsAbsoluteUri;
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
    ///     校验数组是否满足 <c>contains</c> 声明的匹配数量边界。
    ///     该实现会对每个数组项复用同一套递归校验逻辑做“非抛出式匹配”，避免 contains 与主校验链各自维护不同的 schema 解释规则。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">字段路径。</param>
    /// <param name="sequenceNode">实际数组节点。</param>
    /// <param name="schemaNode">数组 schema 节点。</param>
    /// <param name="references">匹配成功的 <c>contains</c> 子树所声明的跨表引用收集器。</param>
    private static void ValidateArrayContainsConstraints(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlSequenceNode sequenceNode,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage>? references)
    {
        var containsConstraints = schemaNode.ArrayConstraints?.ContainsConstraints;
        if (containsConstraints is null)
        {
            return;
        }

        var matchingCount = CountMatchingContainsItems(
            tableName,
            yamlPath,
            displayPath,
            sequenceNode,
            containsConstraints.ContainsNode,
            references);
        var rawValue = matchingCount.ToString(CultureInfo.InvariantCulture);
        var requiredMinContains = containsConstraints.MinContains ?? 1;
        if (matchingCount < requiredMinContains)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must contain at least {requiredMinContains} items matching the 'contains' schema, but the current YAML sequence contains {matchingCount}.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: rawValue,
                detail: $"Minimum matching contains count: {requiredMinContains}.");
        }

        if (containsConstraints.MaxContains.HasValue &&
            matchingCount > containsConstraints.MaxContains.Value)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.ConstraintViolation,
                tableName,
                $"Property '{displayPath}' in config file '{yamlPath}' must contain at most {containsConstraints.MaxContains.Value} items matching the 'contains' schema, but the current YAML sequence contains {matchingCount}.",
                yamlPath: yamlPath,
                schemaPath: schemaNode.SchemaPathHint,
                displayPath: GetDiagnosticPath(displayPath),
                rawValue: rawValue,
                detail: $"Maximum matching contains count: {containsConstraints.MaxContains.Value}.");
        }
    }

    /// <summary>
    ///     统计当前数组中有多少元素满足 <c>contains</c> 子 schema。
    ///     非预期内部错误会继续抛出，只有正常的 schema 不匹配才会被当成“当前元素不计数”。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">数组字段路径。</param>
    /// <param name="sequenceNode">实际数组节点。</param>
    /// <param name="containsNode">contains 子 schema。</param>
    /// <param name="references">匹配成功元素的可选跨表引用收集器。</param>
    /// <returns>匹配 <c>contains</c> 子 schema 的元素数量。</returns>
    private static int CountMatchingContainsItems(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlSequenceNode sequenceNode,
        YamlConfigSchemaNode containsNode,
        ICollection<YamlConfigReferenceUsage>? references)
    {
        var matchingCount = 0;
        for (var itemIndex = 0; itemIndex < sequenceNode.Children.Count; itemIndex++)
        {
            if (TryMatchSchemaNode(
                    tableName,
                    yamlPath,
                    $"{displayPath}[{itemIndex}]",
                    sequenceNode.Children[itemIndex],
                    containsNode,
                    references,
                    allowUnknownObjectProperties: true))
            {
                matchingCount++;
            }
        }

        return matchingCount;
    }

    /// <summary>
    ///     判断当前 YAML 节点是否满足给定 schema 子树。
    ///     contains / not 都通过该路径复用主校验逻辑，因此普通约束失败会返回 <see langword="false" />，
    ///     但内部意外状态仍会继续抛出。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">当前节点路径。</param>
    /// <param name="node">实际 YAML 节点。</param>
    /// <param name="schemaNode">要试匹配的 schema 子树。</param>
    /// <param name="references">当前节点匹配成功后要写回的可选跨表引用收集器。</param>
    /// <param name="allowUnknownObjectProperties">对象试匹配时是否允许额外字段。</param>
    /// <returns>当前节点是否匹配指定 schema 子树。</returns>
    private static bool TryMatchSchemaNode(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode,
        ICollection<YamlConfigReferenceUsage>? references,
        bool allowUnknownObjectProperties)
    {
        // 约束试匹配不能把失败路径的引用泄漏给外层，但匹配成功的分支仍需要把引用写回，
        // 这样 contains / not 等内联 schema 才能与主校验链复用同一套递归解释规则。
        List<YamlConfigReferenceUsage>? matchedReferences = references is null ? null : new();

        try
        {
            ValidateNode(
                tableName,
                yamlPath,
                displayPath,
                node,
                schemaNode,
                matchedReferences,
                allowUnknownObjectProperties);

            if (references is not null &&
                matchedReferences is not null)
            {
                AddUniqueReferenceUsages(references, matchedReferences);
            }

            return true;
        }
        catch (ConfigLoadException exception) when (exception.Diagnostic.FailureKind !=
                                                    ConfigLoadFailureKind.UnexpectedFailure)
        {
            return false;
        }
    }

    /// <summary>
    ///     将试匹配分支采集到的引用回写到外层集合，并按结构化标识去重。
    /// </summary>
    /// <param name="references">外层引用集合。</param>
    /// <param name="matchedReferences">当前成功匹配分支采集到的引用。</param>
    private static void AddUniqueReferenceUsages(
        ICollection<YamlConfigReferenceUsage> references,
        IEnumerable<YamlConfigReferenceUsage> matchedReferences)
    {
        foreach (var referenceUsage in matchedReferences)
        {
            if (!ContainsReferenceUsage(references, referenceUsage))
            {
                references.Add(referenceUsage);
            }
        }
    }

    /// <summary>
    ///     判断外层引用集合中是否已经存在同一条引用使用记录。
    /// </summary>
    /// <param name="references">要检查的引用集合。</param>
    /// <param name="candidate">当前待合并的引用记录。</param>
    /// <returns>当集合中已存在语义相同的记录时返回 <see langword="true" />。</returns>
    private static bool ContainsReferenceUsage(
        IEnumerable<YamlConfigReferenceUsage> references,
        YamlConfigReferenceUsage candidate)
    {
        foreach (var referenceUsage in references)
        {
            if (string.Equals(referenceUsage.YamlPath, candidate.YamlPath, StringComparison.Ordinal) &&
                string.Equals(referenceUsage.SchemaPath, candidate.SchemaPath, StringComparison.Ordinal) &&
                string.Equals(referenceUsage.PropertyPath, candidate.PropertyPath, StringComparison.Ordinal) &&
                string.Equals(referenceUsage.RawValue, candidate.RawValue, StringComparison.Ordinal) &&
                string.Equals(referenceUsage.ReferencedTableName, candidate.ReferencedTableName, StringComparison.Ordinal) &&
                referenceUsage.ValueType == candidate.ValueType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     校验节点是否命中了 <c>not</c> 声明的禁用 schema。
    ///     与 contains 不同，not 会沿用主校验链的严格对象语义，避免把“声明属性子集”误当成完整命中。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="yamlPath">YAML 文件路径。</param>
    /// <param name="displayPath">当前字段路径。</param>
    /// <param name="node">当前 YAML 节点。</param>
    /// <param name="schemaNode">当前 schema 节点。</param>
    private static void ValidateNegatedSchemaConstraint(
        string tableName,
        string yamlPath,
        string displayPath,
        YamlNode node,
        YamlConfigSchemaNode schemaNode)
    {
        if (schemaNode.NegatedSchemaNode is null ||
            !TryMatchSchemaNode(
                tableName,
                yamlPath,
                displayPath,
                node,
                schemaNode.NegatedSchemaNode,
                references: null,
                allowUnknownObjectProperties: false))
        {
            return;
        }

        var subject = string.IsNullOrWhiteSpace(displayPath)
            ? "Root object"
            : $"Property '{displayPath}'";
        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.ConstraintViolation,
            tableName,
            $"{subject} in config file '{yamlPath}' must not match the 'not' schema.",
            yamlPath: yamlPath,
            schemaPath: schemaNode.SchemaPathHint,
            displayPath: GetDiagnosticPath(displayPath),
            rawValue: DescribeYamlNodeForDiagnostics(node, schemaNode.NegatedSchemaNode),
            detail: "The current YAML value matches the forbidden 'not' schema.");
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
                         ?? throw new InvalidOperationException(
                             "Validated object nodes must expose declared properties.");
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
                   sequenceNode.Children.Select(item =>
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
        return
            $"{schemaNode.NodeType}:{normalizedScalar.Length.ToString(CultureInfo.InvariantCulture)}:{normalizedScalar}";
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

        var containsNode = node.ArrayConstraints?.ContainsConstraints?.ContainsNode;
        if (containsNode is not null)
        {
            CollectReferencedTableNames(containsNode, referencedTableNames);
        }

        var dependentSchemas = node.ObjectConstraints?.DependentSchemas;
        if (dependentSchemas is not null)
        {
            foreach (var dependentSchemaNode in dependentSchemas.Values)
            {
                CollectReferencedTableNames(dependentSchemaNode, referencedTableNames);
            }
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
        NegatedSchemaNode = validation.NegatedSchemaNode;
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
    ///     获取节点允许值集合；未声明 <c>enum</c> 时返回空。
    /// </summary>
    public IReadOnlyCollection<YamlConfigAllowedValue>? AllowedValues { get; }

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
    ///     获取节点声明的 <c>not</c> 子 schema；未声明时返回空。
    /// </summary>
    public YamlConfigSchemaNode? NegatedSchemaNode { get; }

    /// <summary>
    ///     获取用于诊断显示的 schema 路径提示。
    ///     当前节点本身不记录独立路径，因此对象校验会回退到所属根 schema 路径。
    /// </summary>
    public string SchemaPathHint { get; }

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
                constantValue: null,
                negatedSchemaNode: null),
            schemaPathHint);
    }

    /// <summary>
    ///     创建数组节点描述。
    /// </summary>
    /// <param name="itemNode">数组元素节点。</param>
    /// <param name="allowedValues">数组节点允许值集合。</param>
    /// <param name="arrayConstraints">数组元素数量约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>数组节点模型。</returns>
    public static YamlConfigSchemaNode CreateArray(
        YamlConfigSchemaNode itemNode,
        IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues,
        YamlConfigArrayConstraints? arrayConstraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Array,
            new NodeChildren(properties: null, requiredProperties: null, itemNode),
            new NodeValidation(
                referenceTableName: null,
                allowedValues,
                constraints: null,
                arrayConstraints,
                objectConstraints: null,
                constantValue: null,
                negatedSchemaNode: null),
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
        IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues,
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
                constantValue: null,
                negatedSchemaNode: null),
            schemaPathHint);
    }

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
    ///     基于当前节点复制一个只替换 <c>enum</c> 允许值集合的新节点。
    /// </summary>
    /// <param name="allowedValues">新的允许值集合。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithAllowedValues(IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithAllowedValues(allowedValues),
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

    /// <summary>
    ///     基于当前节点复制一个只替换 <c>not</c> 子 schema 的新节点。
    /// </summary>
    /// <param name="negatedSchemaNode">新的 negated schema。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithNegatedSchemaNode(YamlConfigSchemaNode? negatedSchemaNode)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithNegatedSchemaNode(negatedSchemaNode),
            SchemaPathHint);
    }

    private sealed class NodeChildren
    {
        public NodeChildren(
            IReadOnlyDictionary<string, YamlConfigSchemaNode>? properties,
            IReadOnlyCollection<string>? requiredProperties,
            YamlConfigSchemaNode? itemNode)
        {
            Properties = properties;
            RequiredProperties = requiredProperties;
            ItemNode = itemNode;
        }

        public static NodeChildren None { get; } = new(properties: null, requiredProperties: null, itemNode: null);

        public IReadOnlyDictionary<string, YamlConfigSchemaNode>? Properties { get; }

        public IReadOnlyCollection<string>? RequiredProperties { get; }

        public YamlConfigSchemaNode? ItemNode { get; }
    }

    private sealed class NodeValidation
    {
        public NodeValidation(
            string? referenceTableName,
            IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues,
            YamlConfigScalarConstraints? constraints,
            YamlConfigArrayConstraints? arrayConstraints,
            YamlConfigObjectConstraints? objectConstraints,
            YamlConfigConstantValue? constantValue,
            YamlConfigSchemaNode? negatedSchemaNode)
        {
            ReferenceTableName = referenceTableName;
            AllowedValues = allowedValues;
            Constraints = constraints;
            ArrayConstraints = arrayConstraints;
            ObjectConstraints = objectConstraints;
            ConstantValue = constantValue;
            NegatedSchemaNode = negatedSchemaNode;
        }

        public static NodeValidation None { get; } = new(
            referenceTableName: null,
            allowedValues: null,
            constraints: null,
            arrayConstraints: null,
            objectConstraints: null,
            constantValue: null,
            negatedSchemaNode: null);

        public string? ReferenceTableName { get; }

        public IReadOnlyCollection<YamlConfigAllowedValue>? AllowedValues { get; }

        public YamlConfigScalarConstraints? Constraints { get; }

        public YamlConfigArrayConstraints? ArrayConstraints { get; }

        public YamlConfigObjectConstraints? ObjectConstraints { get; }

        public YamlConfigConstantValue? ConstantValue { get; }

        public YamlConfigSchemaNode? NegatedSchemaNode { get; }

        public NodeValidation WithReferenceTable(string referenceTableName)
        {
            return new NodeValidation(referenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue, NegatedSchemaNode);
        }

        public NodeValidation WithAllowedValues(IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues)
        {
            return new NodeValidation(ReferenceTableName, allowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue, NegatedSchemaNode);
        }

        public NodeValidation WithConstantValue(YamlConfigConstantValue? constantValue)
        {
            return new NodeValidation(ReferenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, constantValue, NegatedSchemaNode);
        }

        public NodeValidation WithNegatedSchemaNode(YamlConfigSchemaNode? negatedSchemaNode)
        {
            return new NodeValidation(ReferenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue, negatedSchemaNode);
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
///     表示一个节点上声明的单个 <c>enum</c> 候选值。
///     该模型同时保留稳定比较键与原始 JSON 文本，分别供运行时匹配和诊断输出复用。
/// </summary>
internal sealed class YamlConfigAllowedValue
{
    /// <summary>
    ///     初始化一个枚举候选值模型。
    /// </summary>
    /// <param name="comparableValue">用于与 YAML 节点比较的稳定键。</param>
    /// <param name="displayValue">用于诊断输出的原始 JSON 文本。</param>
    public YamlConfigAllowedValue(string comparableValue, string displayValue)
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
    ///     获取用于诊断输出的原始 JSON 文本。
    /// </summary>
    public string DisplayValue { get; }
}

/// <summary>
///     表示一个对象节点上声明的属性数量约束、字段依赖约束与条件子 schema。
///     该模型将对象级约束与数组 / 标量约束拆开保存，避免运行时节点继续暴露无关成员。
/// </summary>
internal sealed class YamlConfigObjectConstraints
{
    /// <summary>
    ///     初始化对象约束模型。
    /// </summary>
    /// <param name="minProperties">最小属性数量约束。</param>
    /// <param name="maxProperties">最大属性数量约束。</param>
    /// <param name="dependentRequired">对象内字段依赖约束。</param>
    /// <param name="dependentSchemas">对象内条件 schema 约束。</param>
    public YamlConfigObjectConstraints(
        int? minProperties,
        int? maxProperties,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? dependentRequired,
        IReadOnlyDictionary<string, YamlConfigSchemaNode>? dependentSchemas)
    {
        MinProperties = minProperties;
        MaxProperties = maxProperties;
        DependentRequired = dependentRequired;
        DependentSchemas = dependentSchemas;
    }

    /// <summary>
    ///     获取最小属性数量约束。
    /// </summary>
    public int? MinProperties { get; }

    /// <summary>
    ///     获取最大属性数量约束。
    /// </summary>
    public int? MaxProperties { get; }

    /// <summary>
    ///     获取对象内字段依赖约束。
    ///     键表示“触发字段”，值表示“触发字段出现后还必须存在的同级字段集合”。
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? DependentRequired { get; }

    /// <summary>
    ///     获取对象内条件 schema 约束。
    ///     键表示“触发字段”，值表示“触发字段出现后当前对象还必须满足的额外 schema 子树”。
    /// </summary>
    public IReadOnlyDictionary<string, YamlConfigSchemaNode>? DependentSchemas { get; }
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
///     表示标量节点上声明的字符串长度、模式与 format 约束。
///     该模型将正则原文、预编译正则和共享 format 枚举绑定保存，
///     保证诊断内容与运行时匹配逻辑保持一致。
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
    /// <param name="formatConstraint">字符串 format 约束。</param>
    public YamlConfigStringConstraints(
        int? minLength,
        int? maxLength,
        string? pattern,
        Regex? patternRegex,
        YamlConfigStringFormatConstraint? formatConstraint)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        Pattern = pattern;
        PatternRegex = patternRegex;
        FormatConstraint = formatConstraint;
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

    /// <summary>
    ///     获取字符串 format 约束。
    /// </summary>
    public YamlConfigStringFormatConstraint? FormatConstraint { get; }
}

/// <summary>
///     表示一个已归一化的字符串 format 约束。
///     该模型同时保留 schema 原文与共享枚举，方便诊断信息稳定展示，又避免运行时校验反复解析字符串。
/// </summary>
internal sealed class YamlConfigStringFormatConstraint
{
    /// <summary>
    ///     初始化字符串 format 约束模型。
    /// </summary>
    /// <param name="schemaName">schema 中声明的 format 名称。</param>
    /// <param name="kind">归一化后的共享 format 枚举。</param>
    public YamlConfigStringFormatConstraint(
        string schemaName,
        YamlConfigStringFormatKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        SchemaName = schemaName;
        Kind = kind;
    }

    /// <summary>
    ///     获取 schema 中声明的 format 名称。
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    ///     获取归一化后的共享 format 枚举。
    /// </summary>
    public YamlConfigStringFormatKind Kind { get; }
}

/// <summary>
///     表示当前 Runtime / Generator / Tooling 共享支持的字符串 format 子集。
/// </summary>
internal enum YamlConfigStringFormatKind
{
    /// <summary>
    ///     表示 <c>yyyy-MM-dd</c> 形式的日期。
    /// </summary>
    Date,

    /// <summary>
    ///     表示带显式时区偏移的 RFC 3339 日期时间。
    /// </summary>
    DateTime,

    /// <summary>
    ///     表示 day-time duration 形式的持续时间。
    /// </summary>
    Duration,

    /// <summary>
    ///     表示基础电子邮件地址格式。
    /// </summary>
    Email,

    /// <summary>
    ///     表示带显式时区偏移的 RFC 3339 时间。
    /// </summary>
    Time,

    /// <summary>
    ///     表示绝对 URI。
    /// </summary>
    Uri,

    /// <summary>
    ///     表示连字符分隔的 UUID 文本。
    /// </summary>
    Uuid
}

/// <summary>
///     表示一个数组节点上声明的元素数量、去重与 contains 匹配计数约束。
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
    /// <param name="containsConstraints">数组 contains 约束；未声明时为空。</param>
    public YamlConfigArrayConstraints(
        int? minItems,
        int? maxItems,
        bool uniqueItems,
        YamlConfigArrayContainsConstraints? containsConstraints)
    {
        MinItems = minItems;
        MaxItems = maxItems;
        UniqueItems = uniqueItems;
        ContainsConstraints = containsConstraints;
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

    /// <summary>
    ///     获取数组 contains 约束；未声明时返回空。
    /// </summary>
    public YamlConfigArrayContainsConstraints? ContainsConstraints { get; }
}

/// <summary>
///     表示数组节点声明的 <c>contains</c> 匹配约束。
///     该模型把 contains 子 schema 与匹配数量边界聚合在一起，避免数组节点再额外散落多组相关成员。
/// </summary>
internal sealed class YamlConfigArrayContainsConstraints
{
    /// <summary>
    ///     初始化数组 contains 约束模型。
    /// </summary>
    /// <param name="containsNode">contains 子 schema。</param>
    /// <param name="minContains">最小匹配数量；为 <see langword="null" /> 时按 JSON Schema 语义默认 1。</param>
    /// <param name="maxContains">最大匹配数量。</param>
    public YamlConfigArrayContainsConstraints(
        YamlConfigSchemaNode containsNode,
        int? minContains,
        int? maxContains)
    {
        ArgumentNullException.ThrowIfNull(containsNode);

        ContainsNode = containsNode;
        MinContains = minContains;
        MaxContains = maxContains;
    }

    /// <summary>
    ///     获取 contains 子 schema。
    /// </summary>
    public YamlConfigSchemaNode ContainsNode { get; }

    /// <summary>
    ///     获取最小匹配数量；未显式声明时返回空，由调用方按默认值 1 解释。
    /// </summary>
    public int? MinContains { get; }

    /// <summary>
    ///     获取最大匹配数量。
    /// </summary>
    public int? MaxContains { get; }
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
