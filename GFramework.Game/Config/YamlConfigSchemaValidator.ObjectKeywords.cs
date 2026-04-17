using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     承载对象级 schema 关键字的解析与元数据校验逻辑。
///     该 partial 将 <c>minProperties</c>、<c>maxProperties</c>、
///     <c>dependentRequired</c>、<c>dependentSchemas</c> 与 <c>allOf</c>
///     从主校验文件中拆出，降低超大文件继续堆叠对象关键字时的维护成本。
/// </summary>
internal static partial class YamlConfigSchemaValidator
{
    /// <summary>
    ///     解析对象节点支持的属性数量约束与对象关键字约束。
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
        var allOfSchemas = ParseAllOfConstraints(tableName, schemaPath, propertyPath, element, properties);

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

        return !minProperties.HasValue && !maxProperties.HasValue && dependentRequired is null && dependentSchemas is null &&
               allOfSchemas is null
            ? null
            : new YamlConfigObjectConstraints(minProperties, maxProperties, dependentRequired, dependentSchemas, allOfSchemas);
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
    ///     解析对象节点声明的 <c>allOf</c> 组合约束。
    ///     当前实现仅接受 object-typed 内联 schema，并把每个条目当成 focused constraint block
    ///     叠加到当前对象上，而不是参与属性合并或改变生成类型形状。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    /// <returns>归一化后的 allOf schema 列表；未声明或为空时返回空。</returns>
    private static IReadOnlyList<YamlConfigSchemaNode>? ParseAllOfConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (!element.TryGetProperty("allOf", out var allOfElement))
        {
            return null;
        }

        if (allOfElement.ValueKind != JsonValueKind.Array)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare 'allOf' as an array of object-valued schemas.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var allOfSchemas = new List<YamlConfigSchemaNode>();
        var allOfIndex = 0;
        foreach (var allOfSchemaElement in allOfElement.EnumerateArray())
        {
            if (allOfSchemaElement.ValueKind != JsonValueKind.Object)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare 'allOf' entries as object-valued schemas.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(propertyPath));
            }

            var allOfSchemaPath = BuildNestedSchemaPath(propertyPath, $"allOf[{allOfIndex.ToString(CultureInfo.InvariantCulture)}]");
            ValidateAllOfSchemaTargetsAgainstParentObject(
                tableName,
                schemaPath,
                propertyPath,
                allOfSchemaPath,
                allOfIndex + 1,
                allOfSchemaElement,
                properties);
            var allOfSchemaNode = ParseNode(
                tableName,
                schemaPath,
                allOfSchemaPath,
                allOfSchemaElement);
            if (allOfSchemaNode.NodeType != YamlConfigSchemaPropertyType.Object)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Entry #{(allOfIndex + 1).ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare an object-typed schema.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(allOfSchemaPath));
            }

            allOfSchemas.Add(allOfSchemaNode);
            allOfIndex++;
        }

        return allOfSchemas.Count == 0
            ? null
            : allOfSchemas;
    }

    /// <summary>
    ///     验证 <c>allOf</c> 条目只约束父对象已经声明过的同级字段。
    ///     当前 object-focused 语义不会把条目里的属性并回父对象形状，因此这里要提前拒绝
    ///     “在 focused block 里引入父对象未声明字段”的不可满足 schema。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="allOfSchemaPath">当前 allOf 条目路径。</param>
    /// <param name="allOfEntryNumber">从 1 开始的 allOf 条目编号。</param>
    /// <param name="allOfSchemaElement">当前 allOf 条目。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    private static void ValidateAllOfSchemaTargetsAgainstParentObject(
        string tableName,
        string schemaPath,
        string propertyPath,
        string allOfSchemaPath,
        int allOfEntryNumber,
        JsonElement allOfSchemaElement,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (allOfSchemaElement.TryGetProperty("properties", out var allOfPropertiesElement))
        {
            if (allOfPropertiesElement.ValueKind != JsonValueKind.Object)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Entry #{allOfEntryNumber.ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'properties' as an object-valued map.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(allOfSchemaPath));
            }

            foreach (var property in allOfPropertiesElement.EnumerateObject())
            {
                if (properties.ContainsKey(property.Name))
                {
                    continue;
                }

                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Entry #{allOfEntryNumber.ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' declares property '{property.Name}', but that property is not declared in the parent object schema.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(allOfSchemaPath));
            }
        }

        if (!allOfSchemaElement.TryGetProperty("required", out var allOfRequiredElement))
        {
            return;
        }

        if (allOfRequiredElement.ValueKind != JsonValueKind.Array)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Entry #{allOfEntryNumber.ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'required' as an array of property names.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(allOfSchemaPath));
        }

        foreach (var requiredProperty in allOfRequiredElement.EnumerateArray())
        {
            if (requiredProperty.ValueKind != JsonValueKind.String)
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Entry #{allOfEntryNumber.ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'required' entries as property-name strings.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(allOfSchemaPath));
            }

            var requiredPropertyName = requiredProperty.GetString();
            if (string.IsNullOrWhiteSpace(requiredPropertyName))
            {
                throw ConfigLoadExceptionFactory.Create(
                    ConfigLoadFailureKind.SchemaUnsupported,
                    tableName,
                    $"Entry #{allOfEntryNumber.ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' cannot declare blank property names in 'required'.",
                    schemaPath: schemaPath,
                    displayPath: GetDiagnosticPath(allOfSchemaPath));
            }

            if (properties.ContainsKey(requiredPropertyName))
            {
                continue;
            }

            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Entry #{allOfEntryNumber.ToString(CultureInfo.InvariantCulture)} in 'allOf' for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' requires property '{requiredPropertyName}', but that property is not declared in the parent object schema.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(allOfSchemaPath));
        }
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
}
