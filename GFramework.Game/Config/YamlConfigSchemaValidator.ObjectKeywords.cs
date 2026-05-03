// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.Abstractions.Config;

namespace GFramework.Game.Config;

/// <summary>
///     承载对象级 schema 关键字的解析与元数据校验逻辑。
///     该 partial 将 <c>minProperties</c>、<c>maxProperties</c>、
///     <c>dependentRequired</c>、<c>dependentSchemas</c>、<c>allOf</c>
///     与 object-focused <c>if</c> / <c>then</c> / <c>else</c>
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
        var conditionalSchemas = ParseConditionalSchemasConstraints(tableName, schemaPath, propertyPath, element, properties);

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
               allOfSchemas is null && conditionalSchemas is null
            ? null
            : new YamlConfigObjectConstraints(
                minProperties,
                maxProperties,
                dependentRequired,
                dependentSchemas,
                allOfSchemas,
                conditionalSchemas);
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
            var dependencyTargets = ParseDependentRequiredConstraint(
                tableName,
                schemaPath,
                propertyPath,
                dependency,
                properties);
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
    ///     解析单个 <c>dependentRequired</c> 触发字段的依赖目标列表。
    ///     触发字段和目标字段必须都来自父对象已声明属性；重复目标会被去重以保持运行时约束稳定。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="dependency">当前触发字段声明。</param>
    /// <param name="properties">当前对象已声明的属性集合。</param>
    /// <returns>去重后的依赖目标列表。</returns>
    private static IReadOnlyList<string> ParseDependentRequiredConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonProperty dependency,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
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
            var dependencyTargetName = ParseDependentRequiredTargetName(
                tableName,
                schemaPath,
                propertyPath,
                dependency.Name,
                dependencyTarget,
                properties);
            if (seenDependencyTargets.Add(dependencyTargetName))
            {
                dependencyTargets.Add(dependencyTargetName);
            }
        }

        return dependencyTargets;
    }

    /// <summary>
    ///     读取并校验 <c>dependentRequired</c> 的单个目标字段名。
    ///     目标必须是非空字符串并已在同一个对象 schema 中声明，避免依赖关系指向不可满足字段。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="dependencyName">当前触发字段名称。</param>
    /// <param name="dependencyTarget">当前依赖目标节点。</param>
    /// <param name="properties">当前对象已声明的属性集合。</param>
    /// <returns>已校验的依赖目标字段名。</returns>
    private static string ParseDependentRequiredTargetName(
        string tableName,
        string schemaPath,
        string propertyPath,
        string dependencyName,
        JsonElement dependencyTarget,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (dependencyTarget.ValueKind != JsonValueKind.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{dependencyName}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'dependentRequired' entries as strings.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        var dependencyTargetName = dependencyTarget.GetString();
        if (string.IsNullOrWhiteSpace(dependencyTargetName))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"Property '{dependencyName}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' cannot declare blank 'dependentRequired' entries.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (properties.ContainsKey(dependencyTargetName))
        {
            return dependencyTargetName;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' declares 'dependentRequired' target '{dependencyTargetName}' that is not declared in the same object schema.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(propertyPath));
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
            dependentSchemas[dependency.Name] = ParseDependentSchemaConstraint(
                tableName,
                schemaPath,
                propertyPath,
                dependency,
                properties);
        }

        return dependentSchemas.Count == 0
            ? null
            : dependentSchemas;
    }

    /// <summary>
    ///     解析单个 <c>dependentSchemas</c> 触发字段关联的 object-typed schema。
    ///     触发字段必须属于当前对象，关联 schema 继续通过通用节点解析流程获得完整约束模型。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="dependency">当前触发字段声明。</param>
    /// <param name="properties">当前对象已声明的属性集合。</param>
    /// <returns>解析后的 object-typed 条件 schema。</returns>
    private static YamlConfigSchemaNode ParseDependentSchemaConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonProperty dependency,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
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
        if (dependencySchemaNode.NodeType == YamlConfigSchemaPropertyType.Object)
        {
            return dependencySchemaNode;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"Property '{dependency.Name}' in {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare an object-typed 'dependentSchemas' schema.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(dependencySchemaPath));
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
            var allOfSchemaNode = ParseAllOfSchemaConstraint(
                tableName,
                schemaPath,
                propertyPath,
                allOfSchemaElement,
                allOfIndex,
                properties);
            allOfSchemas.Add(allOfSchemaNode);
            allOfIndex++;
        }

        return allOfSchemas.Count == 0
            ? null
            : allOfSchemas;
    }

    /// <summary>
    ///     解析 <c>allOf</c> 中的单个 object-focused schema 条目。
    ///     每个条目只允许约束父对象已声明的字段，并且必须保持 object-typed 语义。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="allOfSchemaElement">当前 allOf 条目。</param>
    /// <param name="allOfIndex">当前 allOf 条目的零基索引。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    /// <returns>解析后的 object-typed schema。</returns>
    private static YamlConfigSchemaNode ParseAllOfSchemaConstraint(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement allOfSchemaElement,
        int allOfIndex,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
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

        var allOfEntryLabel = $"Entry #{(allOfIndex + 1).ToString(CultureInfo.InvariantCulture)} in 'allOf'";
        var allOfSchemaPath = BuildNestedSchemaPath(propertyPath, $"allOf[{allOfIndex.ToString(CultureInfo.InvariantCulture)}]");
        ValidateInlineObjectSchemaTargetsAgainstParentObject(
            tableName,
            schemaPath,
            propertyPath,
            allOfSchemaPath,
            allOfEntryLabel,
            allOfSchemaElement,
            properties);

        var allOfSchemaNode = ParseNode(
            tableName,
            schemaPath,
            allOfSchemaPath,
            allOfSchemaElement);
        if (allOfSchemaNode.NodeType == YamlConfigSchemaPropertyType.Object)
        {
            return allOfSchemaNode;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"{allOfEntryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare an object-typed schema.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(allOfSchemaPath));
    }

    /// <summary>
    ///     解析对象节点声明的 object-focused <c>if</c> / <c>then</c> / <c>else</c> 条件约束。
    ///     当前共享子集要求三段内联 schema 都保持 object-typed focused block 语义，
    ///     既允许根据 sibling 值切换约束分支，又避免把条件 schema 扩展成新的生成类型形状。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="element">Schema 节点。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    /// <returns>归一化后的条件约束；未声明时返回空。</returns>
    private static YamlConfigConditionalSchemas? ParseConditionalSchemasConstraints(
        string tableName,
        string schemaPath,
        string propertyPath,
        JsonElement element,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        var hasIf = element.TryGetProperty("if", out var ifElement);
        var hasThen = element.TryGetProperty("then", out var thenElement);
        var hasElse = element.TryGetProperty("else", out var elseElement);
        if (!hasIf && !hasThen && !hasElse)
        {
            return null;
        }

        ValidateConditionalSchemaKeywordPresence(tableName, schemaPath, propertyPath, hasIf, hasThen, hasElse);

        var ifSchemaPath = BuildNestedSchemaPath(propertyPath, "if");
        var ifSchemaNode = ParseConditionalObjectSchema(
            tableName,
            schemaPath,
            propertyPath,
            ifSchemaPath,
            "if",
            ifElement,
            properties);
        var thenSchemaNode = ParseOptionalConditionalObjectSchema(
            tableName,
            schemaPath,
            propertyPath,
            "then",
            hasThen,
            thenElement,
            properties);
        var elseSchemaNode = ParseOptionalConditionalObjectSchema(
            tableName,
            schemaPath,
            propertyPath,
            "else",
            hasElse,
            elseElement,
            properties);

        return new YamlConfigConditionalSchemas(ifSchemaNode, thenSchemaNode, elseSchemaNode);
    }

    /// <summary>
    ///     校验 object-focused 条件关键字的组合关系。
    ///     <c>then</c> 与 <c>else</c> 只能跟随 <c>if</c>，而单独的 <c>if</c> 没有可执行分支。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="hasIf">是否声明 if。</param>
    /// <param name="hasThen">是否声明 then。</param>
    /// <param name="hasElse">是否声明 else。</param>
    private static void ValidateConditionalSchemaKeywordPresence(
        string tableName,
        string schemaPath,
        string propertyPath,
        bool hasIf,
        bool hasThen,
        bool hasElse)
    {
        if (!hasIf)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare 'if' when using 'then' or 'else'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(propertyPath));
        }

        if (hasThen || hasElse)
        {
            return;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare at least one of 'then' or 'else' when using 'if'.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(propertyPath));
    }

    /// <summary>
    ///     解析可选的 <c>then</c> 或 <c>else</c> 条件分支。
    ///     未声明的分支保留为空，声明的分支必须通过 object-focused schema 校验。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">对象字段路径。</param>
    /// <param name="keywordName">条件关键字名称。</param>
    /// <param name="hasKeyword">是否声明该条件关键字。</param>
    /// <param name="keywordElement">条件关键字对应的 schema 节点。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    /// <returns>解析后的条件分支；未声明时返回空。</returns>
    private static YamlConfigSchemaNode? ParseOptionalConditionalObjectSchema(
        string tableName,
        string schemaPath,
        string propertyPath,
        string keywordName,
        bool hasKeyword,
        JsonElement keywordElement,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        return hasKeyword
            ? ParseConditionalObjectSchema(
                tableName,
                schemaPath,
                propertyPath,
                BuildNestedSchemaPath(propertyPath, keywordName),
                keywordName,
                keywordElement,
                properties)
            : null;
    }

    /// <summary>
    ///     解析单个条件分支的 object-focused 内联 schema。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="conditionalSchemaPath">当前条件分支路径。</param>
    /// <param name="keywordName">条件关键字名称。</param>
    /// <param name="conditionalSchemaElement">当前条件分支 schema。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    /// <returns>解析后的 object-typed schema。</returns>
    private static YamlConfigSchemaNode ParseConditionalObjectSchema(
        string tableName,
        string schemaPath,
        string propertyPath,
        string conditionalSchemaPath,
        string keywordName,
        JsonElement conditionalSchemaElement,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (conditionalSchemaElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{DescribeObjectSchemaTarget(conditionalSchemaPath)} in schema file '{schemaPath}' must declare '{keywordName}' as an object-valued schema.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(conditionalSchemaPath));
        }

        ValidateInlineObjectSchemaTargetsAgainstParentObject(
            tableName,
            schemaPath,
            propertyPath,
            conditionalSchemaPath,
            $"'{keywordName}'",
            conditionalSchemaElement,
            properties);

        var conditionalSchemaNode = ParseNode(
            tableName,
            schemaPath,
            conditionalSchemaPath,
            conditionalSchemaElement);
        if (conditionalSchemaNode.NodeType == YamlConfigSchemaPropertyType.Object)
        {
            return conditionalSchemaNode;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"{DescribeObjectSchemaTarget(propertyPath)} in schema file '{schemaPath}' must declare an object-typed '{keywordName}' schema.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(conditionalSchemaPath));
    }

    /// <summary>
    ///     验证 object-focused 内联 schema 只约束父对象已经声明过的同级字段。
    ///     当前 shared subset 不会把 focused block 内字段并回父对象形状，因此这里会提前拒绝
    ///     “在 focused block 里引入父对象未声明字段”的不可满足 schema。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="inlineSchemaPath">当前内联 schema 路径。</param>
    /// <param name="entryLabel">用于诊断文本的条目标签。</param>
    /// <param name="inlineSchemaElement">当前内联 schema。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    private static void ValidateInlineObjectSchemaTargetsAgainstParentObject(
        string tableName,
        string schemaPath,
        string propertyPath,
        string inlineSchemaPath,
        string entryLabel,
        JsonElement inlineSchemaElement,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        ValidateInlineObjectSchemaPropertiesAgainstParentObject(
            tableName,
            schemaPath,
            propertyPath,
            inlineSchemaPath,
            entryLabel,
            inlineSchemaElement,
            properties);

        ValidateInlineRequiredPropertiesAgainstParentObject(
            tableName,
            schemaPath,
            propertyPath,
            inlineSchemaPath,
            entryLabel,
            inlineSchemaElement,
            properties);
    }

    /// <summary>
    ///     校验 object-focused 内联 schema 的 <c>properties</c> 只引用父对象字段。
    ///     focused block 不负责声明新字段，所以任何父对象未声明字段都会在 schema 加载时被拒绝。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="inlineSchemaPath">当前内联 schema 路径。</param>
    /// <param name="entryLabel">用于诊断文本的条目标签。</param>
    /// <param name="inlineSchemaElement">当前内联 schema。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    private static void ValidateInlineObjectSchemaPropertiesAgainstParentObject(
        string tableName,
        string schemaPath,
        string propertyPath,
        string inlineSchemaPath,
        string entryLabel,
        JsonElement inlineSchemaElement,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (!inlineSchemaElement.TryGetProperty("properties", out var inlinePropertiesElement))
        {
            return;
        }

        if (inlinePropertiesElement.ValueKind != JsonValueKind.Object)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{entryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'properties' as an object-valued map.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(inlineSchemaPath));
        }

        foreach (var property in inlinePropertiesElement.EnumerateObject())
        {
            if (properties.ContainsKey(property.Name))
            {
                continue;
            }

            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{entryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' declares property '{property.Name}', but that property is not declared in the parent object schema.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(inlineSchemaPath));
        }
    }

    /// <summary>
    ///     校验 object-focused 内联 schema 的 <c>required</c> 只引用父对象字段。
    ///     该校验在加载期暴露不可满足的条件块，而不是等到运行时才发现无效字段名。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="inlineSchemaPath">当前内联 schema 路径。</param>
    /// <param name="entryLabel">用于诊断文本的条目标签。</param>
    /// <param name="inlineSchemaElement">当前内联 schema。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    private static void ValidateInlineRequiredPropertiesAgainstParentObject(
        string tableName,
        string schemaPath,
        string propertyPath,
        string inlineSchemaPath,
        string entryLabel,
        JsonElement inlineSchemaElement,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (!inlineSchemaElement.TryGetProperty("required", out var inlineRequiredElement))
        {
            return;
        }

        if (inlineRequiredElement.ValueKind != JsonValueKind.Array)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{entryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'required' as an array of property names.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(inlineSchemaPath));
        }

        foreach (var requiredProperty in inlineRequiredElement.EnumerateArray())
        {
            ValidateInlineRequiredPropertyAgainstParentObject(
                tableName,
                schemaPath,
                propertyPath,
                inlineSchemaPath,
                entryLabel,
                requiredProperty,
                properties);
        }
    }

    /// <summary>
    ///     校验 object-focused 内联 schema 的单个 <c>required</c> 字段名。
    ///     字段名必须是非空字符串并且属于父对象声明范围，保持条件块与父对象形状一致。
    /// </summary>
    /// <param name="tableName">所属配置表名称。</param>
    /// <param name="schemaPath">Schema 文件路径。</param>
    /// <param name="propertyPath">父对象路径。</param>
    /// <param name="inlineSchemaPath">当前内联 schema 路径。</param>
    /// <param name="entryLabel">用于诊断文本的条目标签。</param>
    /// <param name="requiredProperty">当前 required 条目。</param>
    /// <param name="properties">父对象已声明的属性集合。</param>
    private static void ValidateInlineRequiredPropertyAgainstParentObject(
        string tableName,
        string schemaPath,
        string propertyPath,
        string inlineSchemaPath,
        string entryLabel,
        JsonElement requiredProperty,
        IReadOnlyDictionary<string, YamlConfigSchemaNode> properties)
    {
        if (requiredProperty.ValueKind != JsonValueKind.String)
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{entryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' must declare 'required' entries as property-name strings.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(inlineSchemaPath));
        }

        var requiredPropertyName = requiredProperty.GetString();
        if (string.IsNullOrWhiteSpace(requiredPropertyName))
        {
            throw ConfigLoadExceptionFactory.Create(
                ConfigLoadFailureKind.SchemaUnsupported,
                tableName,
                $"{entryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' cannot declare blank property names in 'required'.",
                schemaPath: schemaPath,
                displayPath: GetDiagnosticPath(inlineSchemaPath));
        }

        if (properties.ContainsKey(requiredPropertyName))
        {
            return;
        }

        throw ConfigLoadExceptionFactory.Create(
            ConfigLoadFailureKind.SchemaUnsupported,
            tableName,
            $"{entryLabel} for {DescribeObjectSchemaTargetInClause(propertyPath)} of schema file '{schemaPath}' requires property '{requiredPropertyName}', but that property is not declared in the parent object schema.",
            schemaPath: schemaPath,
            displayPath: GetDiagnosticPath(inlineSchemaPath));
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
