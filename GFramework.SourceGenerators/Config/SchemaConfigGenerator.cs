using GFramework.SourceGenerators.Diagnostics;

namespace GFramework.SourceGenerators.Config;

/// <summary>
///     根据 AdditionalFiles 中的 JSON schema 生成配置类型和配置表包装。
///     当前实现聚焦 AI-First 配置系统共享的最小 schema 子集，
///     支持嵌套对象、对象数组、标量数组，以及可映射的 default / enum / const / ref-table 元数据。
///     当前共享子集也会把 <c>multipleOf</c>、<c>uniqueItems</c>、
///     <c>minProperties</c> 与 <c>maxProperties</c> 写入生成代码文档，
///     让消费者能直接在强类型 API 上看到运行时生效的约束。
/// </summary>
[Generator]
public sealed class SchemaConfigGenerator : IIncrementalGenerator
{
    private const string ConfigPathMetadataKey = "x-gframework-config-path";
    private const string LookupIndexMetadataKey = "x-gframework-index";
    private const string GeneratedNamespace = "GFramework.Game.Config.Generated";
    private const string LookupIndexTopLevelScalarOnlyMessage =
        "Only top-level required non-key scalar properties can declare a generated lookup index.";
    private const string LookupIndexRequiresRequiredScalarMessage =
        "Generated lookup indexes currently require a required scalar property so dictionary keys remain non-null.";
    private const string LookupIndexPrimaryKeyMessage =
        "The primary key already has Get/TryGet lookup semantics and should not declare a generated lookup index.";
    private const string LookupIndexReferencePropertyMessage =
        "Reference properties are excluded from generated lookup indexes because they already carry cross-table semantics.";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var schemaFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) => ParseSchema(file, cancellationToken));

        context.RegisterSourceOutput(schemaFiles, static (productionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }

            if (result.Schema is null)
            {
                return;
            }

            productionContext.AddSource(
                $"{result.Schema.ClassName}.g.cs",
                SourceText.From(GenerateConfigClass(result.Schema), Encoding.UTF8));
            productionContext.AddSource(
                $"{result.Schema.TableName}.g.cs",
                SourceText.From(GenerateTableClass(result.Schema), Encoding.UTF8));
            productionContext.AddSource(
                $"{result.Schema.EntityName}ConfigBindings.g.cs",
                SourceText.From(GenerateBindingsClass(result.Schema), Encoding.UTF8));
        });

        var collectedSchemas = schemaFiles.Collect();
        context.RegisterSourceOutput(collectedSchemas, static (productionContext, results) =>
        {
            var schemas = results
                .Where(static result => result.Schema is not null)
                .Select(static result => result.Schema!)
                .OrderBy(static schema => schema.TableRegistrationName, StringComparer.Ordinal)
                .ToArray();

            if (schemas.Length == 0)
            {
                return;
            }

            productionContext.AddSource(
                "GeneratedConfigCatalog.g.cs",
                SourceText.From(GenerateCatalogClass(schemas), Encoding.UTF8));
        });
    }

    /// <summary>
    ///     解析单个 schema 文件。
    /// </summary>
    /// <param name="file">AdditionalFiles 中的 schema 文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析结果，包含 schema 模型或诊断。</returns>
    private static SchemaParseResult ParseSchema(
        AdditionalText file,
        CancellationToken cancellationToken)
    {
        SourceText? text;
        try
        {
            text = file.GetText(cancellationToken);
        }
        catch (Exception exception)
        {
            return SchemaParseResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidSchemaJson,
                    CreateFileLocation(file.Path),
                    Path.GetFileName(file.Path),
                    exception.Message));
        }

        if (text is null)
        {
            return SchemaParseResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidSchemaJson,
                    CreateFileLocation(file.Path),
                    Path.GetFileName(file.Path),
                    "File content could not be read."));
        }

        try
        {
            using var document = JsonDocument.Parse(text.ToString());
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var rootTypeElement) ||
                !string.Equals(rootTypeElement.GetString(), "object", StringComparison.Ordinal))
            {
                return SchemaParseResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.RootObjectSchemaRequired,
                        CreateFileLocation(file.Path),
                        Path.GetFileName(file.Path)));
            }

            var entityName = ToPascalCase(GetSchemaBaseName(file.Path));
            var rootObject = ParseObjectSpec(
                file.Path,
                root,
                "<root>",
                $"{entityName}Config",
                isRoot: true);
            if (rootObject.Diagnostic is not null)
            {
                return SchemaParseResult.FromDiagnostic(rootObject.Diagnostic);
            }

            var schemaObject = rootObject.Object!;
            var idProperty = schemaObject.Properties.FirstOrDefault(static property =>
                string.Equals(property.SchemaName, "id", StringComparison.OrdinalIgnoreCase));
            if (idProperty is null || !idProperty.IsRequired)
            {
                return SchemaParseResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.IdPropertyRequired,
                        CreateFileLocation(file.Path),
                        Path.GetFileName(file.Path)));
            }

            if (idProperty.TypeSpec.SchemaType != "integer" &&
                idProperty.TypeSpec.SchemaType != "string")
            {
                return SchemaParseResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.UnsupportedKeyType,
                        CreateFileLocation(file.Path),
                        Path.GetFileName(file.Path),
                        idProperty.TypeSpec.SchemaType));
            }

            var schemaBaseName = GetSchemaBaseName(file.Path);
            var configRelativePath = ResolveConfigRelativePath(file.Path, root, schemaBaseName);
            if (configRelativePath.Diagnostic is not null)
            {
                return SchemaParseResult.FromDiagnostic(configRelativePath.Diagnostic);
            }

            var schema = new SchemaFileSpec(
                Path.GetFileName(file.Path),
                entityName,
                schemaObject.ClassName,
                $"{entityName}Table",
                GeneratedNamespace,
                idProperty.TypeSpec.ClrType.TrimEnd('?'),
                idProperty.PropertyName,
                schemaBaseName,
                configRelativePath.Path!,
                GetSchemaRelativePath(file.Path),
                TryGetMetadataString(root, "title"),
                TryGetMetadataString(root, "description"),
                schemaObject);

            return SchemaParseResult.FromSchema(schema);
        }
        catch (JsonException exception)
        {
            return SchemaParseResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidSchemaJson,
                    CreateFileLocation(file.Path),
                    Path.GetFileName(file.Path),
                    exception.Message));
        }
    }

    /// <summary>
    ///     解析对象 schema，并递归构建子属性模型。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="element">对象 schema 节点。</param>
    /// <param name="displayPath">当前对象的逻辑字段路径。</param>
    /// <param name="className">要生成的 CLR 类型名。</param>
    /// <param name="isRoot">是否为根对象。</param>
    /// <returns>对象模型或诊断。</returns>
    private static ParsedObjectResult ParseObjectSpec(
        string filePath,
        JsonElement element,
        string displayPath,
        string className,
        bool isRoot = false)
    {
        if (!element.TryGetProperty("properties", out var propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return ParsedObjectResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.RootObjectSchemaRequired,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath)));
        }

        var requiredProperties = new HashSet<string>(StringComparer.Ordinal);
        if (element.TryGetProperty("required", out var requiredElement) &&
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        requiredProperties.Add(value!);
                    }
                }
            }
        }

        var properties = new List<SchemaPropertySpec>();
        foreach (var property in propertiesElement.EnumerateObject())
        {
            var parsedProperty = ParseProperty(
                filePath,
                property,
                requiredProperties.Contains(property.Name),
                CombinePath(displayPath, property.Name),
                isDirectChildOfRoot: isRoot);
            if (parsedProperty.Diagnostic is not null)
            {
                return ParsedObjectResult.FromDiagnostic(parsedProperty.Diagnostic);
            }

            properties.Add(parsedProperty.Property!);
        }

        return ParsedObjectResult.FromObject(new SchemaObjectSpec(
            displayPath,
            className,
            TryGetMetadataString(element, "title"),
            TryGetMetadataString(element, "description"),
            TryBuildConstraintDocumentation(element, "object"),
            properties));
    }

    /// <summary>
    ///     解析单个 schema 属性定义。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="property">属性 JSON 节点。</param>
    /// <param name="isRequired">属性是否必填。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <returns>解析后的属性信息或诊断。</returns>
    private static ParsedPropertyResult ParseProperty(
        string filePath,
        JsonProperty property,
        bool isRequired,
        string displayPath,
        bool isDirectChildOfRoot)
    {
        if (!property.Value.TryGetProperty("type", out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.String)
        {
            return ParsedPropertyResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.UnsupportedPropertyType,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    displayPath,
                    "<missing>"));
        }

        var schemaType = typeElement.GetString() ?? string.Empty;
        var title = TryGetMetadataString(property.Value, "title");
        var description = TryGetMetadataString(property.Value, "description");
        var refTableName = TryGetMetadataString(property.Value, "x-gframework-ref-table");
        var indexedLookupMetadata = TryGetMetadataBoolean(property.Value, LookupIndexMetadataKey);
        if (indexedLookupMetadata.Diagnostic is not null)
        {
            return ParsedPropertyResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidLookupIndexMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    displayPath,
                    LookupIndexMetadataKey,
                    indexedLookupMetadata.Diagnostic!));
        }

        var isIndexedLookup = indexedLookupMetadata.Value ?? false;
        if (!TryBuildPropertyIdentifier(filePath, displayPath, property.Name, out var propertyName, out var diagnostic))
        {
            return ParsedPropertyResult.FromDiagnostic(diagnostic!);
        }

        if (isIndexedLookup &&
            !TryValidateIndexedLookupEligibility(
                filePath,
                property.Name,
                displayPath,
                isDirectChildOfRoot,
                isRequired,
                refTableName,
                out diagnostic))
        {
            return ParsedPropertyResult.FromDiagnostic(diagnostic!);
        }

        switch (schemaType)
        {
            case "integer":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    isIndexedLookup,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Scalar,
                        "integer",
                        isRequired ? "int" : "int?",
                        TryBuildScalarInitializer(property.Value, "integer"),
                        TryBuildEnumDocumentation(property.Value, "integer"),
                        TryBuildConstraintDocumentation(property.Value, "integer"),
                        refTableName,
                        null,
                        null)));

            case "number":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    isIndexedLookup,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Scalar,
                        "number",
                        isRequired ? "double" : "double?",
                        TryBuildScalarInitializer(property.Value, "number"),
                        TryBuildEnumDocumentation(property.Value, "number"),
                        TryBuildConstraintDocumentation(property.Value, "number"),
                        refTableName,
                        null,
                        null)));

            case "boolean":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    isIndexedLookup,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Scalar,
                        "boolean",
                        isRequired ? "bool" : "bool?",
                        TryBuildScalarInitializer(property.Value, "boolean"),
                        TryBuildEnumDocumentation(property.Value, "boolean"),
                        TryBuildConstraintDocumentation(property.Value, "boolean"),
                        refTableName,
                        null,
                        null)));

            case "string":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    isIndexedLookup,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Scalar,
                        "string",
                        isRequired ? "string" : "string?",
                        TryBuildScalarInitializer(property.Value, "string") ??
                        (isRequired ? " = string.Empty;" : null),
                        TryBuildEnumDocumentation(property.Value, "string"),
                        TryBuildConstraintDocumentation(property.Value, "string"),
                        refTableName,
                        null,
                        null)));

            case "object":
                if (isIndexedLookup)
                {
                    return ParsedPropertyResult.FromDiagnostic(
                        CreateInvalidLookupIndexDiagnostic(filePath, displayPath, LookupIndexTopLevelScalarOnlyMessage));
                }

                if (!string.IsNullOrWhiteSpace(refTableName))
                {
                    return ParsedPropertyResult.FromDiagnostic(
                        Diagnostic.Create(
                            ConfigSchemaDiagnostics.UnsupportedPropertyType,
                            CreateFileLocation(filePath),
                            Path.GetFileName(filePath),
                            displayPath,
                            "object-ref"));
                }

                var objectResult = ParseObjectSpec(
                    filePath,
                    property.Value,
                    displayPath,
                    $"{propertyName}Config");
                if (objectResult.Diagnostic is not null)
                {
                    return ParsedPropertyResult.FromDiagnostic(objectResult.Diagnostic);
                }

                var objectSpec = objectResult.Object!;
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    false,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Object,
                        "object",
                        isRequired ? objectSpec.ClassName : $"{objectSpec.ClassName}?",
                        isRequired ? " = new();" : null,
                        null,
                        null,
                        null,
                        objectSpec,
                        null)));

            case "array":
                return ParseArrayProperty(filePath, property, isRequired, displayPath, propertyName, title,
                    description, refTableName, isIndexedLookup);

            default:
                return ParsedPropertyResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.UnsupportedPropertyType,
                        CreateFileLocation(filePath),
                        Path.GetFileName(filePath),
                        displayPath,
                        schemaType));
        }
    }

    /// <summary>
    ///     验证字段是否满足生成只读精确匹配索引的前提。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="schemaName">Schema 原始字段名。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="isDirectChildOfRoot">字段是否直接从属于 schema 根对象。</param>
    /// <param name="isRequired">字段是否必填。</param>
    /// <param name="refTableName">可选的引用表名。</param>
    /// <param name="diagnostic">不满足条件时输出的诊断。</param>
    /// <returns>当前字段是否允许声明只读索引。</returns>
    private static bool TryValidateIndexedLookupEligibility(
        string filePath,
        string schemaName,
        string displayPath,
        bool isDirectChildOfRoot,
        bool isRequired,
        string? refTableName,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!isDirectChildOfRoot)
        {
            diagnostic = CreateInvalidLookupIndexDiagnostic(
                filePath,
                displayPath,
                LookupIndexTopLevelScalarOnlyMessage);
            return false;
        }

        if (!isRequired)
        {
            diagnostic = CreateInvalidLookupIndexDiagnostic(
                filePath,
                displayPath,
                LookupIndexRequiresRequiredScalarMessage);
            return false;
        }

        if (string.Equals(schemaName, "id", StringComparison.OrdinalIgnoreCase))
        {
            diagnostic = CreateInvalidLookupIndexDiagnostic(
                filePath,
                displayPath,
                LookupIndexPrimaryKeyMessage);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(refTableName))
        {
            diagnostic = CreateInvalidLookupIndexDiagnostic(
                filePath,
                displayPath,
                LookupIndexReferencePropertyMessage);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     解析数组属性，支持标量数组与对象数组。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="property">属性 JSON 节点。</param>
    /// <param name="isRequired">属性是否必填。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="propertyName">CLR 属性名。</param>
    /// <param name="title">标题元数据。</param>
    /// <param name="description">说明元数据。</param>
    /// <param name="refTableName">目标引用表名称。</param>
    /// <returns>解析后的属性信息或诊断。</returns>
    private static ParsedPropertyResult ParseArrayProperty(
        string filePath,
        JsonProperty property,
        bool isRequired,
        string displayPath,
        string propertyName,
        string? title,
        string? description,
        string? refTableName,
        bool isIndexedLookup)
    {
        if (isIndexedLookup)
        {
            return ParsedPropertyResult.FromDiagnostic(
                CreateInvalidLookupIndexDiagnostic(filePath, displayPath, LookupIndexTopLevelScalarOnlyMessage));
        }

        if (!property.Value.TryGetProperty("items", out var itemsElement) ||
            itemsElement.ValueKind != JsonValueKind.Object ||
            !itemsElement.TryGetProperty("type", out var itemTypeElement) ||
            itemTypeElement.ValueKind != JsonValueKind.String)
        {
            return ParsedPropertyResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.UnsupportedPropertyType,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    displayPath,
                    "array"));
        }

        var itemType = itemTypeElement.GetString() ?? string.Empty;
        switch (itemType)
        {
            case "integer":
            case "number":
            case "boolean":
            case "string":
                var itemClrType = itemType switch
                {
                    "integer" => "int",
                    "number" => "double",
                    "boolean" => "bool",
                    _ => "string"
                };

                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    false,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Array,
                        "array",
                        $"global::System.Collections.Generic.IReadOnlyList<{itemClrType}>",
                        TryBuildArrayInitializer(property.Value, itemType, itemClrType) ??
                        $" = global::System.Array.Empty<{itemClrType}>();",
                        TryBuildEnumDocumentation(itemsElement, itemType),
                        TryBuildConstraintDocumentation(property.Value, "array"),
                        refTableName,
                        null,
                        new SchemaTypeSpec(
                            SchemaNodeKind.Scalar,
                            itemType,
                            itemClrType,
                            null,
                            TryBuildEnumDocumentation(itemsElement, itemType),
                            TryBuildConstraintDocumentation(itemsElement, itemType),
                            refTableName,
                            null,
                            null))));

            case "object":
                if (!string.IsNullOrWhiteSpace(refTableName))
                {
                    return ParsedPropertyResult.FromDiagnostic(
                        Diagnostic.Create(
                            ConfigSchemaDiagnostics.UnsupportedPropertyType,
                            CreateFileLocation(filePath),
                            Path.GetFileName(filePath),
                            displayPath,
                            "array<object>-ref"));
                }

                var objectResult = ParseObjectSpec(
                    filePath,
                    itemsElement,
                    $"{displayPath}[]",
                    $"{propertyName}ItemConfig");
                if (objectResult.Diagnostic is not null)
                {
                    return ParsedPropertyResult.FromDiagnostic(objectResult.Diagnostic);
                }

                var objectSpec = objectResult.Object!;
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    displayPath,
                    propertyName,
                    isRequired,
                    title,
                    description,
                    false,
                    new SchemaTypeSpec(
                        SchemaNodeKind.Array,
                        "array",
                        $"global::System.Collections.Generic.IReadOnlyList<{objectSpec.ClassName}>",
                        $" = global::System.Array.Empty<{objectSpec.ClassName}>();",
                        null,
                        TryBuildConstraintDocumentation(property.Value, "array"),
                        null,
                        null,
                        new SchemaTypeSpec(
                            SchemaNodeKind.Object,
                            "object",
                            objectSpec.ClassName,
                            null,
                            null,
                            null,
                            null,
                            objectSpec,
                            null))));

            default:
                return ParsedPropertyResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.UnsupportedPropertyType,
                        CreateFileLocation(filePath),
                        Path.GetFileName(filePath),
                        displayPath,
                        $"array<{itemType}>"));
        }
    }

    /// <summary>
    ///     生成配置类型源码。
    /// </summary>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <returns>配置类型源码。</returns>
    private static string GenerateConfigClass(SchemaFileSpec schema)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {schema.Namespace};");
        builder.AppendLine();

        AppendObjectType(builder, schema.RootObject, schema.FileName, schema.Title, schema.Description, isRoot: true,
            indentationLevel: 0);
        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     生成表包装源码。
    /// </summary>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <returns>配置表包装源码。</returns>
    private static string GenerateTableClass(SchemaFileSpec schema)
    {
        var builder = new StringBuilder();
        var queryableProperties = CollectQueryableProperties(schema).ToArray();
        var indexedQueryableProperties = queryableProperties
            .Where(static property => property.IsIndexedLookup)
            .ToArray();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {schema.Namespace};");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            $"///     Auto-generated table wrapper for schema file '{schema.FileName}'.");
        builder.AppendLine(
            "///     The wrapper keeps generated call sites strongly typed while delegating actual storage to the runtime config table implementation.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine(
            $"public sealed partial class {schema.TableName} : global::GFramework.Game.Abstractions.Config.IConfigTable<{schema.KeyClrType}, {schema.ClassName}>");
        builder.AppendLine("{");
        builder.AppendLine(
            $"    private readonly global::GFramework.Game.Abstractions.Config.IConfigTable<{schema.KeyClrType}, {schema.ClassName}> _inner;");
        foreach (var property in indexedQueryableProperties)
        {
            builder.AppendLine(
                $"    private readonly global::System.Lazy<global::System.Collections.Generic.IReadOnlyDictionary<{property.TypeSpec.ClrType}, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>>> _{ToCamelCase(property.PropertyName)}Index;");
        }

        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Creates a generated table wrapper around the runtime config table instance.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"inner\">The runtime config table instance.</param>");
        builder.AppendLine(
            $"    public {schema.TableName}(global::GFramework.Game.Abstractions.Config.IConfigTable<{schema.KeyClrType}, {schema.ClassName}> inner)");
        builder.AppendLine("    {");
        builder.AppendLine("        _inner = inner ?? throw new global::System.ArgumentNullException(nameof(inner));");
        foreach (var property in indexedQueryableProperties)
        {
            builder.AppendLine(
                $"        _{ToCamelCase(property.PropertyName)}Index = new global::System.Lazy<global::System.Collections.Generic.IReadOnlyDictionary<{property.TypeSpec.ClrType}, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>>>(");
            builder.AppendLine($"            Build{property.PropertyName}Index,");
            builder.AppendLine("            global::System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);");
        }

        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public global::System.Type KeyType => _inner.KeyType;");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public global::System.Type ValueType => _inner.ValueType;");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine("    public int Count => _inner.Count;");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine($"    public {schema.ClassName} Get({schema.KeyClrType} key)");
        builder.AppendLine("    {");
        builder.AppendLine("        return _inner.Get(key);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine($"    public bool TryGet({schema.KeyClrType} key, out {schema.ClassName}? value)");
        builder.AppendLine("    {");
        builder.AppendLine("        return _inner.TryGet(key, out value);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine($"    public bool ContainsKey({schema.KeyClrType} key)");
        builder.AppendLine("    {");
        builder.AppendLine("        return _inner.ContainsKey(key);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <inheritdoc />");
        builder.AppendLine(
            $"    public global::System.Collections.Generic.IReadOnlyCollection<{schema.ClassName}> All()");
        builder.AppendLine("    {");
        builder.AppendLine("        return _inner.All();");
        builder.AppendLine("    }");

        if (indexedQueryableProperties.Length > 0)
        {
            foreach (var property in indexedQueryableProperties)
            {
                builder.AppendLine();
                AppendIndexedLookupBuilderMethod(builder, schema, property);
            }

            builder.AppendLine();
            AppendSharedLookupIndexBuilderMethod(builder, schema);
        }

        foreach (var property in queryableProperties)
        {
            builder.AppendLine();
            AppendFindByPropertyMethod(builder, schema, property);
            builder.AppendLine();
            AppendTryFindFirstByPropertyMethod(builder, schema, property);
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     生成运行时注册与访问辅助源码。
    ///     该辅助类型把 schema 命名约定、配置目录和 schema 相对路径固化为生成代码，
    ///     让消费端无需重复手写字符串常量和主键提取逻辑。
    /// </summary>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <returns>辅助类型源码。</returns>
    private static string GenerateBindingsClass(SchemaFileSpec schema)
    {
        var registerMethodName = $"Register{schema.EntityName}Table";
        var getMethodName = $"Get{schema.EntityName}Table";
        var tryGetMethodName = $"TryGet{schema.EntityName}Table";
        var bindingsClassName = $"{schema.EntityName}ConfigBindings";
        var referenceSpecs = CollectReferenceSpecs(schema.RootObject).ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {schema.Namespace};");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            $"///     Auto-generated registration and lookup helpers for schema file '{schema.FileName}'.");
        builder.AppendLine(
            "///     The helper centralizes table naming, config directory, schema path, and strongly-typed registry access so consumer projects do not need to duplicate the same conventions.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine($"public static class {bindingsClassName}");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Describes one schema property that declares <c>x-gframework-ref-table</c> metadata.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public readonly struct ReferenceMetadata");
        builder.AppendLine("    {");
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Initializes one generated cross-table reference descriptor.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        /// <param name=\"displayPath\">Schema property path.</param>");
        builder.AppendLine("        /// <param name=\"referencedTableName\">Referenced runtime table name.</param>");
        builder.AppendLine(
            "        /// <param name=\"valueSchemaType\">Schema scalar type used by the reference value.</param>");
        builder.AppendLine(
            "        /// <param name=\"isCollection\">Whether the property stores multiple reference keys.</param>");
        builder.AppendLine("        public ReferenceMetadata(");
        builder.AppendLine("            string displayPath,");
        builder.AppendLine("            string referencedTableName,");
        builder.AppendLine("            string valueSchemaType,");
        builder.AppendLine("            bool isCollection)");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            DisplayPath = displayPath ?? throw new global::System.ArgumentNullException(nameof(displayPath));");
        builder.AppendLine(
            "            ReferencedTableName = referencedTableName ?? throw new global::System.ArgumentNullException(nameof(referencedTableName));");
        builder.AppendLine(
            "            ValueSchemaType = valueSchemaType ?? throw new global::System.ArgumentNullException(nameof(valueSchemaType));");
        builder.AppendLine("            IsCollection = isCollection;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets the schema property path such as <c>dropItems</c> or <c>phases[].monsterId</c>.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string DisplayPath { get; }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Gets the runtime registration name of the referenced config table.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string ReferencedTableName { get; }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Gets the schema scalar type used by the referenced key value.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string ValueSchemaType { get; }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets a value indicating whether the property stores multiple reference keys.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public bool IsCollection { get; }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Groups the schema-derived metadata constants so consumer code can reuse one stable entry point.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static class Metadata");
        builder.AppendLine("    {");
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets the logical config domain derived from the schema base name. The current runtime convention keeps this value aligned with the generated table name.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine(
            $"        public const string ConfigDomain = {SymbolDisplay.FormatLiteral(schema.TableRegistrationName, true)};");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Gets the runtime registration name of the generated config table.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine(
            $"        public const string TableName = {SymbolDisplay.FormatLiteral(schema.TableRegistrationName, true)};");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets the config directory path expected by the generated registration helper.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine(
            $"        public const string ConfigRelativePath = {SymbolDisplay.FormatLiteral(schema.ConfigRelativePath, true)};");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Gets the schema file path expected by the generated registration helper.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine(
            $"        public const string SchemaRelativePath = {SymbolDisplay.FormatLiteral(schema.SchemaRelativePath, true)};");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Gets the logical config domain derived from the schema base name. The current runtime convention keeps this value aligned with the generated table name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public const string ConfigDomain = Metadata.ConfigDomain;");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Gets the runtime registration name of the generated config table.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public const string TableName = Metadata.TableName;");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Gets the config directory path expected by the generated registration helper.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public const string ConfigRelativePath = Metadata.ConfigRelativePath;");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Gets the schema file path expected by the generated registration helper.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public const string SchemaRelativePath = Metadata.SchemaRelativePath;");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Exposes generated metadata for schema properties that declare <c>x-gframework-ref-table</c>.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static class References");
        builder.AppendLine("    {");

        foreach (var referenceSpec in referenceSpecs)
        {
            builder.AppendLine("        /// <summary>");
            builder.AppendLine(
                $"        ///     Gets generated reference metadata for schema property path '{EscapeXmlDocumentation(referenceSpec.DisplayPath)}'.");
            builder.AppendLine("        /// </summary>");
            builder.AppendLine(
                $"        public static readonly ReferenceMetadata {referenceSpec.MemberName} = new(");
            builder.AppendLine(
                $"            {SymbolDisplay.FormatLiteral(referenceSpec.DisplayPath, true)},");
            builder.AppendLine(
                $"            {SymbolDisplay.FormatLiteral(referenceSpec.ReferencedTableName, true)},");
            builder.AppendLine(
                $"            {SymbolDisplay.FormatLiteral(referenceSpec.ValueSchemaType, true)},");
            builder.AppendLine(
                $"            {(referenceSpec.IsCollection ? "true" : "false")});");
            builder.AppendLine();
        }

        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets all generated cross-table reference descriptors for the current schema.");
        builder.AppendLine("        /// </summary>");
        if (referenceSpecs.Length == 0)
        {
            builder.AppendLine(
                "        public static global::System.Collections.Generic.IReadOnlyList<ReferenceMetadata> All { get; } = global::System.Array.Empty<ReferenceMetadata>();");
        }
        else
        {
            builder.AppendLine(
                "        public static global::System.Collections.Generic.IReadOnlyList<ReferenceMetadata> All { get; } = global::System.Array.AsReadOnly(new ReferenceMetadata[]");
            builder.AppendLine("        {");
            foreach (var referenceSpec in referenceSpecs)
            {
                builder.AppendLine($"            {referenceSpec.MemberName},");
            }

            builder.AppendLine("        });");
        }

        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Tries to resolve generated reference metadata by schema property path.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        /// <param name=\"displayPath\">Schema property path.</param>");
        builder.AppendLine(
            "        /// <param name=\"metadata\">Resolved generated reference metadata when the path is known; otherwise the default value.</param>");
        builder.AppendLine(
            "        /// <returns>True when the schema property path has generated cross-table metadata; otherwise false.</returns>");
        builder.AppendLine(
            "        public static bool TryGetByDisplayPath(string displayPath, out ReferenceMetadata metadata)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (displayPath is null)");
        builder.AppendLine("            {");
        builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(displayPath));");
        builder.AppendLine("            }");
        builder.AppendLine();

        if (referenceSpecs.Length == 0)
        {
            builder.AppendLine("            metadata = default;");
            builder.AppendLine("            return false;");
        }
        else
        {
            foreach (var referenceSpec in referenceSpecs)
            {
                builder.AppendLine(
                    $"            if (string.Equals(displayPath, {SymbolDisplay.FormatLiteral(referenceSpec.DisplayPath, true)}, global::System.StringComparison.Ordinal))");
                builder.AppendLine("            {");
                builder.AppendLine($"                metadata = {referenceSpec.MemberName};");
                builder.AppendLine("                return true;");
                builder.AppendLine("            }");
            }

            builder.AppendLine();
            builder.AppendLine("            metadata = default;");
            builder.AppendLine("            return false;");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Registers the generated config table using the schema-derived runtime conventions.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"loader\">The target YAML config loader.</param>");
        builder.AppendLine(
            "    /// <param name=\"comparer\">Optional key comparer for the generated table registration.</param>");
        builder.AppendLine("    /// <returns>The same loader instance so registration can keep chaining.</returns>");
        builder.AppendLine(
            $"    public static global::GFramework.Game.Config.YamlConfigLoader {registerMethodName}(");
        builder.AppendLine("        this global::GFramework.Game.Config.YamlConfigLoader loader,");
        builder.AppendLine(
            $"        global::System.Collections.Generic.IEqualityComparer<{schema.KeyClrType}>? comparer = null)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (loader is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(loader));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            $"        return loader.RegisterTable<{schema.KeyClrType}, {schema.ClassName}>(");
        builder.AppendLine("            Metadata.TableName,");
        builder.AppendLine("            Metadata.ConfigRelativePath,");
        builder.AppendLine("            Metadata.SchemaRelativePath,");
        builder.AppendLine($"            static config => config.{schema.KeyPropertyName},");
        builder.AppendLine("            comparer);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Gets the generated config table wrapper from the registry.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"registry\">The source config registry.</param>");
        builder.AppendLine("    /// <returns>The generated strongly-typed table wrapper.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">When <paramref name=\"registry\"/> is null.</exception>");
        builder.AppendLine(
            $"    public static {schema.TableName} {getMethodName}(this global::GFramework.Game.Abstractions.Config.IConfigRegistry registry)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (registry is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(registry));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            $"        return new {schema.TableName}(registry.GetTable<{schema.KeyClrType}, {schema.ClassName}>(Metadata.TableName));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Tries to get the generated config table wrapper from the registry.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"registry\">The source config registry.</param>");
        builder.AppendLine(
            "    /// <param name=\"table\">The generated strongly-typed table wrapper when lookup succeeds; otherwise null.</param>");
        builder.AppendLine(
            "    /// <returns>True when the generated table is registered and type-compatible; otherwise false.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">When <paramref name=\"registry\"/> is null.</exception>");
        builder.AppendLine(
            $"    public static bool {tryGetMethodName}(this global::GFramework.Game.Abstractions.Config.IConfigRegistry registry, out {schema.TableName}? table)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (registry is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(registry));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            $"        if (registry.TryGetTable<{schema.KeyClrType}, {schema.ClassName}>(Metadata.TableName, out var innerTable) && innerTable is not null)");
        builder.AppendLine("        {");
        builder.AppendLine($"            table = new {schema.TableName}(innerTable);");
        builder.AppendLine("            return true;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        table = null;");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     生成项目级聚合辅助源码。
    ///     该辅助把当前消费者项目内所有有效 schema 汇总为一个统一入口，
    ///     以便运行时快速完成批量注册并在需要时枚举已生成的配置域元数据。
    /// </summary>
    /// <param name="schemas">当前编译中成功解析的 schema 集合。</param>
    /// <returns>聚合辅助源码。</returns>
    private static string GenerateCatalogClass(IReadOnlyList<SchemaFileSpec> schemas)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {GeneratedNamespace};");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            "///     Provides a project-level catalog for every config table generated from the current consumer project's schemas.");
        builder.AppendLine(
            "///     Use this entry point when you want the C# runtime bootstrap path to register all generated tables without repeating one call per schema.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class GeneratedConfigCatalog");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Describes one generated config table so bootstrap code can enumerate generated domains without re-parsing schema files at runtime.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public readonly struct TableMetadata");
        builder.AppendLine("    {");
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Initializes one generated table metadata entry.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine(
            "        /// <param name=\"configDomain\">Logical config domain derived from the schema base name.</param>");
        builder.AppendLine("        /// <param name=\"tableName\">Runtime registration name.</param>");
        builder.AppendLine("        /// <param name=\"configRelativePath\">Relative YAML directory path.</param>");
        builder.AppendLine("        /// <param name=\"schemaRelativePath\">Relative schema file path.</param>");
        builder.AppendLine("        public TableMetadata(");
        builder.AppendLine("            string configDomain,");
        builder.AppendLine("            string tableName,");
        builder.AppendLine("            string configRelativePath,");
        builder.AppendLine("            string schemaRelativePath)");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            ConfigDomain = configDomain ?? throw new global::System.ArgumentNullException(nameof(configDomain));");
        builder.AppendLine(
            "            TableName = tableName ?? throw new global::System.ArgumentNullException(nameof(tableName));");
        builder.AppendLine(
            "            ConfigRelativePath = configRelativePath ?? throw new global::System.ArgumentNullException(nameof(configRelativePath));");
        builder.AppendLine(
            "            SchemaRelativePath = schemaRelativePath ?? throw new global::System.ArgumentNullException(nameof(schemaRelativePath));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Gets the logical config domain derived from the schema base name.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string ConfigDomain { get; }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets the runtime registration name used by <see cref=\"global::GFramework.Game.Config.YamlConfigLoader\" />.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string TableName { get; }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets the relative directory that stores YAML files for the generated config table.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string ConfigRelativePath { get; }");
        builder.AppendLine();
        builder.AppendLine("        /// <summary>");
        builder.AppendLine("        ///     Gets the relative schema file path collected by the source generator.");
        builder.AppendLine("        /// </summary>");
        builder.AppendLine("        public string SchemaRelativePath { get; }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Gets metadata for every generated config table in the current consumer project.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    public static global::System.Collections.Generic.IReadOnlyList<TableMetadata> Tables { get; } = global::System.Array.AsReadOnly(new TableMetadata[]");
        builder.AppendLine("    {");

        foreach (var schema in schemas)
        {
            builder.AppendLine("        new(");
            builder.AppendLine($"            {schema.EntityName}ConfigBindings.Metadata.ConfigDomain,");
            builder.AppendLine($"            {schema.EntityName}ConfigBindings.Metadata.TableName,");
            builder.AppendLine($"            {schema.EntityName}ConfigBindings.Metadata.ConfigRelativePath,");
            builder.AppendLine($"            {schema.EntityName}ConfigBindings.Metadata.SchemaRelativePath),");
        }

        builder.AppendLine("    });");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Tries to resolve generated table metadata by runtime registration name.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"tableName\">Runtime registration name.</param>");
        builder.AppendLine(
            "    /// <param name=\"metadata\">Resolved generated table metadata when the registration name exists; otherwise the default value.</param>");
        builder.AppendLine(
            "    /// <returns><see langword=\"true\" /> when the registration name belongs to a generated config table; otherwise <see langword=\"false\" />.</returns>");
        builder.AppendLine("    public static bool TryGetByTableName(string tableName, out TableMetadata metadata)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (tableName is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(tableName));");
        builder.AppendLine("        }");
        builder.AppendLine();

        for (var index = 0; index < schemas.Count; index++)
        {
            var schema = schemas[index];
            builder.AppendLine(
                $"        if (string.Equals(tableName, {schema.EntityName}ConfigBindings.Metadata.TableName, global::System.StringComparison.Ordinal))");
            builder.AppendLine("        {");
            builder.AppendLine(
                $"            metadata = Tables[{index.ToString(CultureInfo.InvariantCulture)}];");
            builder.AppendLine("            return true;");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        metadata = default;");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Resolves the generated table metadata entries that belong to the specified logical config domain.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"configDomain\">Logical config domain derived from the schema base name.</param>");
        builder.AppendLine(
            "    /// <returns>A deterministic metadata snapshot for the requested config domain, or an empty list when no generated table belongs to that domain.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">When <paramref name=\"configDomain\"/> is null.</exception>");
        builder.AppendLine(
            "    public static global::System.Collections.Generic.IReadOnlyList<TableMetadata> GetTablesInConfigDomain(string configDomain)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (configDomain is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(configDomain));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var matchedTables = new global::System.Collections.Generic.List<TableMetadata>();");
        builder.AppendLine("        foreach (var metadata in Tables)");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            if (string.Equals(metadata.ConfigDomain, configDomain, global::System.StringComparison.Ordinal))");
        builder.AppendLine("            {");
        builder.AppendLine("                matchedTables.Add(metadata);");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        return matchedTables.Count == 0 ? global::System.Array.Empty<TableMetadata>() : matchedTables.ToArray();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Resolves the generated table metadata entries that aggregate registration would currently include under the supplied filters.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"options\">Optional aggregate registration filters and comparer overrides. When null, every generated table remains eligible.</param>");
        builder.AppendLine(
            "    /// <returns>A deterministic metadata snapshot in the same order used by <see cref=\"GeneratedConfigRegistrationExtensions.RegisterAllGeneratedConfigTables(global::GFramework.Game.Config.YamlConfigLoader, GeneratedConfigRegistrationOptions?)\" />.</returns>");
        builder.AppendLine(
            "    public static global::System.Collections.Generic.IReadOnlyList<TableMetadata> GetTablesForRegistration(GeneratedConfigRegistrationOptions? options = null)");
        builder.AppendLine("    {");
        builder.AppendLine("        var matchedTables = new global::System.Collections.Generic.List<TableMetadata>();");
        builder.AppendLine("        foreach (var metadata in Tables)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (MatchesRegistrationOptions(metadata, options))");
        builder.AppendLine("            {");
        builder.AppendLine("                matchedTables.Add(metadata);");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        return matchedTables.Count == 0 ? global::System.Array.Empty<TableMetadata>() : matchedTables.ToArray();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Evaluates whether one generated table metadata entry remains eligible under the supplied aggregate registration filters.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"metadata\">Generated table metadata under evaluation.</param>");
        builder.AppendLine(
            "    /// <param name=\"options\">Optional aggregate registration filters and comparer overrides. When null, the metadata entry is always eligible.</param>");
        builder.AppendLine(
            "    /// <returns><see langword=\"true\" /> when the generated table would be included by aggregate registration; otherwise <see langword=\"false\" />.</returns>");
        builder.AppendLine("    public static bool MatchesRegistrationOptions(");
        builder.AppendLine("        TableMetadata metadata,");
        builder.AppendLine("        GeneratedConfigRegistrationOptions? options)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (options is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return true;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        // Apply cheap generated allow-lists before invoking the optional caller predicate so startup diagnostics stay aligned with real registration.");
        builder.AppendLine(
            "        if (!MatchesOptionalAllowList(options.IncludedConfigDomains, metadata.ConfigDomain))");
        builder.AppendLine("        {");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (!MatchesOptionalAllowList(options.IncludedTableNames, metadata.TableName))");
        builder.AppendLine("        {");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return options.TableFilter?.Invoke(metadata) ?? true;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Treats a null or empty allow-list as an unrestricted match, and otherwise performs ordinal string comparison against the generated metadata value.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"allowedValues\">Optional caller-supplied allow-list.</param>");
        builder.AppendLine("    /// <param name=\"candidate\">Generated metadata value being evaluated.</param>");
        builder.AppendLine(
            "    /// <returns><see langword=\"true\" /> when the value should remain eligible for registration; otherwise <see langword=\"false\" />.</returns>");
        builder.AppendLine("    private static bool MatchesOptionalAllowList(");
        builder.AppendLine("        global::System.Collections.Generic.IReadOnlyCollection<string>? allowedValues,");
        builder.AppendLine("        string candidate)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (allowedValues is null || allowedValues.Count == 0)");
        builder.AppendLine("        {");
        builder.AppendLine("            return true;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        foreach (var allowedValue in allowedValues)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (allowedValue is not null &&");
        builder.AppendLine(
            "                string.Equals(allowedValue, candidate, global::System.StringComparison.Ordinal))");
        builder.AppendLine("            {");
        builder.AppendLine("                return true;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            "///     Captures optional per-table registration overrides for the generated aggregate registration entry point.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public sealed class GeneratedConfigRegistrationOptions");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Gets or sets the optional allow-list of generated config domains that aggregate registration should include. When null or empty, every generated domain remains eligible.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    public global::System.Collections.Generic.IReadOnlyCollection<string>? IncludedConfigDomains { get; init; }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Gets or sets the optional allow-list of runtime table names that aggregate registration should include. When null or empty, every generated table remains eligible.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    public global::System.Collections.Generic.IReadOnlyCollection<string>? IncludedTableNames { get; init; }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Gets or sets the optional predicate that can reject individual generated table metadata entries after allow-list filtering has passed.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    public global::System.Predicate<GeneratedConfigCatalog.TableMetadata>? TableFilter { get; init; }");

        if (schemas.Count > 0)
        {
            builder.AppendLine();
        }

        for (var index = 0; index < schemas.Count; index++)
        {
            var schema = schemas[index];
            builder.AppendLine("    /// <summary>");
            builder.AppendLine(
                $"    ///     Gets or sets the optional key comparer forwarded to {schema.EntityName}ConfigBindings.Register{schema.EntityName}Table(global::GFramework.Game.Config.YamlConfigLoader, global::System.Collections.Generic.IEqualityComparer<{schema.KeyClrType}>?) when aggregate registration runs.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine(
                $"    public global::System.Collections.Generic.IEqualityComparer<{schema.KeyClrType}>? {schema.EntityName}Comparer {{ get; init; }}");

            if (index < schemas.Count - 1)
            {
                builder.AppendLine();
            }
        }

        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            "///     Provides a single extension method that registers every generated config table discovered in the current consumer project.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class GeneratedConfigRegistrationExtensions");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Registers all generated config tables using schema-derived conventions so bootstrap code can stay one-line even as schemas grow.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"loader\">Target YAML config loader.</param>");
        builder.AppendLine(
            "    /// <returns>The same loader instance after all generated table registrations have been applied.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">When <paramref name=\"loader\"/> is null.</exception>");
        builder.AppendLine(
            "    public static global::GFramework.Game.Config.YamlConfigLoader RegisterAllGeneratedConfigTables(");
        builder.AppendLine("        this global::GFramework.Game.Config.YamlConfigLoader loader)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (loader is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(loader));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        return RegisterAllGeneratedConfigTables(loader, options: null);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Registers all generated config tables while preserving optional per-table overrides such as custom key comparers.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"loader\">Target YAML config loader.</param>");
        builder.AppendLine(
            "    /// <param name=\"options\">Optional per-table overrides for aggregate registration; when null, all tables use their default comparer behavior.</param>");
        builder.AppendLine(
            "    /// <returns>The same loader instance after all generated table registrations have been applied.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">When <paramref name=\"loader\"/> is null.</exception>");
        builder.AppendLine(
            "    public static global::GFramework.Game.Config.YamlConfigLoader RegisterAllGeneratedConfigTables(");
        builder.AppendLine("        this global::GFramework.Game.Config.YamlConfigLoader loader,");
        builder.AppendLine("        GeneratedConfigRegistrationOptions? options)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (loader is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(loader));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var effectiveOptions = options ?? new GeneratedConfigRegistrationOptions();");
        builder.AppendLine();

        for (var index = 0; index < schemas.Count; index++)
        {
            var schema = schemas[index];
            builder.AppendLine(
                $"        if (GeneratedConfigCatalog.MatchesRegistrationOptions(GeneratedConfigCatalog.Tables[{index.ToString(CultureInfo.InvariantCulture)}], effectiveOptions))");
            builder.AppendLine("        {");
            builder.AppendLine(
                $"            loader.Register{schema.EntityName}Table(effectiveOptions.{schema.EntityName}Comparer);");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        builder.AppendLine("        return loader;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     收集 schema 中声明的跨表引用元数据，并为生成代码分配稳定成员名。
    /// </summary>
    /// <param name="rootObject">根对象模型。</param>
    /// <returns>生成期引用元数据集合。</returns>
    private static IEnumerable<GeneratedReferenceSpec> CollectReferenceSpecs(SchemaObjectSpec rootObject)
    {
        var nextSuffixByBaseMemberName = new Dictionary<string, int>(StringComparer.Ordinal);
        var allocatedMemberNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var referenceSeed in EnumerateReferenceSeeds(rootObject.Properties))
        {
            var baseMemberName = BuildReferenceMemberName(referenceSeed.DisplayPath);
            var memberName = baseMemberName;
            if (!allocatedMemberNames.Add(memberName))
            {
                // Track globally allocated member names because a suffixed duplicate from one path can collide
                // with the unsuffixed base name produced by a later, different path.
                var duplicateCount = nextSuffixByBaseMemberName.TryGetValue(baseMemberName, out var nextSuffix)
                    ? nextSuffix + 1
                    : 1;

                memberName = $"{baseMemberName}{duplicateCount.ToString(CultureInfo.InvariantCulture)}";
                while (!allocatedMemberNames.Add(memberName))
                {
                    duplicateCount++;
                    memberName = $"{baseMemberName}{duplicateCount.ToString(CultureInfo.InvariantCulture)}";
                }

                nextSuffixByBaseMemberName[baseMemberName] = duplicateCount;
            }
            else
            {
                nextSuffixByBaseMemberName[baseMemberName] = 0;
            }

            yield return new GeneratedReferenceSpec(
                memberName,
                referenceSeed.DisplayPath,
                referenceSeed.ReferencedTableName,
                referenceSeed.ValueSchemaType,
                referenceSeed.IsCollection);
        }
    }

    /// <summary>
    ///     收集适合生成轻量查询辅助的根级标量字段。
    ///     当前实现故意限定在顶层非主键标量字段，避免把嵌套结构、数组或引用语义提前固化为运行时契约。
    /// </summary>
    /// <param name="schema">生成器级 schema 模型。</param>
    /// <returns>可生成查询辅助的属性集合。</returns>
    private static IEnumerable<SchemaPropertySpec> CollectQueryableProperties(SchemaFileSpec schema)
    {
        foreach (var property in schema.RootObject.Properties)
        {
            if (property.TypeSpec.Kind != SchemaNodeKind.Scalar)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(property.TypeSpec.RefTableName))
            {
                continue;
            }

            if (string.Equals(property.PropertyName, schema.KeyPropertyName, StringComparison.Ordinal))
            {
                continue;
            }

            yield return property;
        }
    }

    /// <summary>
    ///     为单个索引字段生成延迟构建器。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="schema">生成器级 schema 模型。</param>
    /// <param name="property">声明了索引元数据的字段。</param>
    private static void AppendIndexedLookupBuilderMethod(
        StringBuilder builder,
        SchemaFileSpec schema,
        SchemaPropertySpec property)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            $"    ///     Builds the exact-match lookup index declared for property '{EscapeXmlDocumentation(property.DisplayPath)}'.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            $"    /// <returns>A read-only lookup index keyed by <c>{EscapeXmlDocumentation(property.PropertyName)}</c>.</returns>");
        builder.AppendLine(
            $"    private global::System.Collections.Generic.IReadOnlyDictionary<{property.TypeSpec.ClrType}, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>> Build{property.PropertyName}Index()");
        builder.AppendLine("    {");
        builder.AppendLine(
            $"        return BuildLookupIndex(static config => config.{property.PropertyName});");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     为当前生成表输出共享索引构建逻辑。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="schema">生成器级 schema 模型。</param>
    private static void AppendSharedLookupIndexBuilderMethod(
        StringBuilder builder,
        SchemaFileSpec schema)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Materializes a read-only exact-match lookup index from the current table snapshot.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <typeparam name=\"TProperty\">Indexed property type.</typeparam>");
        builder.AppendLine("    /// <param name=\"keySelector\">Selects the indexed property from one config entry.</param>");
        builder.AppendLine("    /// <returns>A read-only dictionary whose values preserve snapshot iteration order.</returns>");
        builder.AppendLine("    /// <remarks>");
        builder.AppendLine(
            "    ///     The generated index skips runtime null keys even though <typeparamref name=\"TProperty\"/> is constrained to <c>notnull</c>. Malformed YAML payloads can still deserialize missing indexed values to <see langword=\"null\" />, and throwing from this lazy path would permanently poison the cached index for the current table wrapper instance.");
        builder.AppendLine("    /// </remarks>");
        builder.AppendLine(
            $"    private global::System.Collections.Generic.IReadOnlyDictionary<TProperty, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>> BuildLookupIndex<TProperty>(");
        builder.AppendLine($"        global::System.Func<{schema.ClassName}, TProperty> keySelector)");
        builder.AppendLine("        where TProperty : notnull");
        builder.AppendLine("    {");
        builder.AppendLine("        var buckets = new global::System.Collections.Generic.Dictionary<TProperty, global::System.Collections.Generic.List<" +
                           $"{schema.ClassName}>>();");
        builder.AppendLine();
        builder.AppendLine(
            "        // Capture the current table snapshot once so indexed lookups stay deterministic for this wrapper instance.");
        builder.AppendLine("        foreach (var candidate in All())");
        builder.AppendLine("        {");
        builder.AppendLine("            var key = keySelector(candidate);");
        builder.AppendLine("            if (key is null)");
        builder.AppendLine("            {");
        builder.AppendLine(
            "                // Skip malformed runtime data so the lazy lookup cache remains usable for valid keys.");
        builder.AppendLine(
            "                // Throwing here would permanently poison the cached index for this wrapper instance.");
        builder.AppendLine("                continue;");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            if (!buckets.TryGetValue(key, out var matches))");
        builder.AppendLine("            {");
        builder.AppendLine($"                matches = new global::System.Collections.Generic.List<{schema.ClassName}>();");
        builder.AppendLine("                buckets.Add(key, matches);");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            matches.Add(candidate);");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            $"        var materialized = new global::System.Collections.Generic.Dictionary<TProperty, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>>(buckets.Count, buckets.Comparer);");
        builder.AppendLine("        foreach (var pair in buckets)");
        builder.AppendLine("        {");
        builder.AppendLine(
            $"            materialized.Add(pair.Key, pair.Value.AsReadOnly());");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            $"        return new global::System.Collections.ObjectModel.ReadOnlyDictionary<TProperty, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>>(materialized);");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     生成按字段匹配全部结果的轻量查询辅助。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="schema">生成器级 schema 模型。</param>
    /// <param name="property">要生成查询辅助的字段模型。</param>
    private static void AppendFindByPropertyMethod(
        StringBuilder builder,
        SchemaFileSpec schema,
        SchemaPropertySpec property)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            $"    ///     Finds all config entries whose property '{EscapeXmlDocumentation(property.DisplayPath)}' equals the supplied value.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"value\">The property value to match.</param>");
        builder.AppendLine("    /// <returns>A read-only snapshot containing every matching config entry.</returns>");
        builder.AppendLine("    /// <remarks>");
        if (property.IsIndexedLookup)
        {
            builder.AppendLine(
                "    ///     This property declares <c>x-gframework-index</c>, so the generated helper resolves matches through a lazily materialized read-only lookup index built from the current table snapshot.");
        }
        else
        {
            builder.AppendLine(
                "    ///     The generated helper performs a deterministic linear scan over <see cref=\"All\"/> so it stays compatible with runtime hot reload and does not require secondary index infrastructure.");
        }

        builder.AppendLine("    /// </remarks>");
        builder.AppendLine(
            $"    public global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}> FindBy{property.PropertyName}({property.TypeSpec.ClrType} value)");
        builder.AppendLine("    {");
        if (property.IsIndexedLookup)
        {
            if (RequiresIndexedLookupNullGuard(property.TypeSpec))
            {
                builder.AppendLine("        if (value is null)");
                builder.AppendLine("        {");
                builder.AppendLine($"            return global::System.Array.Empty<{schema.ClassName}>();");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine(
                $"        if (_{ToCamelCase(property.PropertyName)}Index.Value.TryGetValue(value, out var matches))");
            builder.AppendLine("        {");
            builder.AppendLine("            return matches;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        return global::System.Array.Empty<{schema.ClassName}>();");
        }
        else
        {
            builder.AppendLine(
                $"        var matches = new global::System.Collections.Generic.List<{schema.ClassName}>();");
            builder.AppendLine();
            builder.AppendLine(
                "        // Scan the current table snapshot on demand so generated helpers stay aligned with reloadable runtime data.");
            builder.AppendLine("        foreach (var candidate in All())");
            builder.AppendLine("        {");
            builder.AppendLine(
                $"            if (global::System.Collections.Generic.EqualityComparer<{property.TypeSpec.ClrType}>.Default.Equals(candidate.{property.PropertyName}, value))");
            builder.AppendLine("            {");
            builder.AppendLine("                matches.Add(candidate);");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine(
                $"        return matches.Count == 0 ? global::System.Array.Empty<{schema.ClassName}>() : matches.AsReadOnly();");
        }

        builder.AppendLine("    }");
    }

    /// <summary>
    ///     生成按字段匹配首个结果的轻量查询辅助。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="schema">生成器级 schema 模型。</param>
    /// <param name="property">要生成查询辅助的字段模型。</param>
    private static void AppendTryFindFirstByPropertyMethod(
        StringBuilder builder,
        SchemaFileSpec schema,
        SchemaPropertySpec property)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            $"    ///     Tries to find the first config entry whose property '{EscapeXmlDocumentation(property.DisplayPath)}' equals the supplied value.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"value\">The property value to match.</param>");
        builder.AppendLine(
            "    /// <param name=\"result\">The first matching config entry when lookup succeeds; otherwise <see langword=\"null\" />.</param>");
        builder.AppendLine(
            "    /// <returns><see langword=\"true\" /> when a matching config entry is found; otherwise <see langword=\"false\" />.</returns>");
        builder.AppendLine("    /// <remarks>");
        if (property.IsIndexedLookup)
        {
            builder.AppendLine(
                "    ///     This property declares <c>x-gframework-index</c>, so the generated helper returns the first element from the lazily materialized exact-match bucket.");
        }
        else
        {
            builder.AppendLine(
                "    ///     The generated helper walks the same snapshot exposed by <see cref=\"All\"/> and returns the first match in iteration order.");
        }

        builder.AppendLine("    /// </remarks>");
        builder.AppendLine(
            $"    public bool TryFindFirstBy{property.PropertyName}({property.TypeSpec.ClrType} value, out {schema.ClassName}? result)");
        builder.AppendLine("    {");
        if (property.IsIndexedLookup)
        {
            if (RequiresIndexedLookupNullGuard(property.TypeSpec))
            {
                builder.AppendLine("        if (value is null)");
                builder.AppendLine("        {");
                builder.AppendLine("            result = null;");
                builder.AppendLine("            return false;");
                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine(
                $"        if (_{ToCamelCase(property.PropertyName)}Index.Value.TryGetValue(value, out var matches) && matches.Count > 0)");
            builder.AppendLine("        {");
            builder.AppendLine("            result = matches[0];");
            builder.AppendLine("            return true;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        result = null;");
            builder.AppendLine("        return false;");
        }
        else
        {
            builder.AppendLine(
                "        // Keep the search path allocation-free for the first-match case by exiting as soon as one entry matches.");
            builder.AppendLine("        foreach (var candidate in All())");
            builder.AppendLine("        {");
            builder.AppendLine(
                $"            if (global::System.Collections.Generic.EqualityComparer<{property.TypeSpec.ClrType}>.Default.Equals(candidate.{property.PropertyName}, value))");
            builder.AppendLine("            {");
            builder.AppendLine("                result = candidate;");
            builder.AppendLine("                return true;");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        result = null;");
            builder.AppendLine("        return false;");
        }

        builder.AppendLine("    }");
    }

    /// <summary>
    ///     递归枚举对象树中所有带 ref-table 元数据的字段。
    /// </summary>
    /// <param name="properties">对象属性集合。</param>
    /// <returns>原始引用字段信息。</returns>
    private static IEnumerable<GeneratedReferenceSeed> EnumerateReferenceSeeds(
        IEnumerable<SchemaPropertySpec> properties)
    {
        foreach (var property in properties)
        {
            if (!string.IsNullOrWhiteSpace(property.TypeSpec.RefTableName))
            {
                yield return new GeneratedReferenceSeed(
                    property.DisplayPath,
                    property.TypeSpec.RefTableName!,
                    property.TypeSpec.Kind == SchemaNodeKind.Array
                        ? property.TypeSpec.ItemTypeSpec?.SchemaType ?? property.TypeSpec.SchemaType
                        : property.TypeSpec.SchemaType,
                    property.TypeSpec.Kind == SchemaNodeKind.Array);
            }

            if (property.TypeSpec.NestedObject is not null)
            {
                foreach (var nestedReference in EnumerateReferenceSeeds(property.TypeSpec.NestedObject.Properties))
                {
                    yield return nestedReference;
                }
            }

            if (property.TypeSpec.ItemTypeSpec?.NestedObject is not null)
            {
                foreach (var nestedReference in EnumerateReferenceSeeds(property.TypeSpec.ItemTypeSpec.NestedObject
                             .Properties))
                {
                    yield return nestedReference;
                }
            }
        }
    }

    /// <summary>
    ///     递归生成配置对象类型。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="objectSpec">要生成的对象类型。</param>
    /// <param name="fileName">Schema 文件名。</param>
    /// <param name="title">对象标题元数据。</param>
    /// <param name="description">对象说明元数据。</param>
    /// <param name="isRoot">是否为根配置类型。</param>
    /// <param name="indentationLevel">缩进层级。</param>
    private static void AppendObjectType(
        StringBuilder builder,
        SchemaObjectSpec objectSpec,
        string fileName,
        string? title,
        string? description,
        bool isRoot,
        int indentationLevel)
    {
        var indent = new string(' ', indentationLevel * 4);
        builder.AppendLine($"{indent}/// <summary>");
        if (isRoot)
        {
            builder.AppendLine(
                $"{indent}///     Auto-generated config type for schema file '{fileName}'.");
            builder.AppendLine(
                $"{indent}///     {EscapeXmlDocumentation(description ?? title ?? "This type is generated from JSON schema so runtime loading and editor tooling can share the same contract.")}");
        }
        else
        {
            builder.AppendLine(
                $"{indent}///     Auto-generated nested config type for schema property path '{EscapeXmlDocumentation(objectSpec.DisplayPath)}'.");
            builder.AppendLine(
                $"{indent}///     {EscapeXmlDocumentation(description ?? title ?? "This nested type is generated so object-valued schema fields remain strongly typed in consumer code.")}");
        }

        builder.AppendLine($"{indent}/// </summary>");
        if (!string.IsNullOrWhiteSpace(objectSpec.ConstraintDocumentation))
        {
            builder.AppendLine($"{indent}/// <remarks>");
            builder.AppendLine(
                $"{indent}///     Constraints: {EscapeXmlDocumentation(objectSpec.ConstraintDocumentation!)}.");
            builder.AppendLine($"{indent}/// </remarks>");
        }

        builder.AppendLine($"{indent}public sealed partial class {objectSpec.ClassName}");
        builder.AppendLine($"{indent}{{");

        for (var index = 0; index < objectSpec.Properties.Count; index++)
        {
            var property = objectSpec.Properties[index];
            AppendPropertyDocumentation(builder, property, indentationLevel + 1);

            var propertyIndent = new string(' ', (indentationLevel + 1) * 4);
            builder.Append(
                $"{propertyIndent}public {property.TypeSpec.ClrType} {property.PropertyName} {{ get; set; }}");
            if (!string.IsNullOrEmpty(property.TypeSpec.Initializer))
            {
                builder.Append(property.TypeSpec.Initializer);
            }

            builder.AppendLine();
            builder.AppendLine();
        }

        var nestedTypes = CollectNestedTypes(objectSpec.Properties).ToArray();
        for (var index = 0; index < nestedTypes.Length; index++)
        {
            var nestedType = nestedTypes[index];
            AppendObjectType(
                builder,
                nestedType,
                fileName,
                nestedType.Title,
                nestedType.Description,
                isRoot: false,
                indentationLevel: indentationLevel + 1);

            if (index < nestedTypes.Length - 1)
            {
                builder.AppendLine();
            }
        }

        builder.AppendLine($"{indent}}}");
    }

    /// <summary>
    ///     枚举一个对象直接拥有的嵌套类型。
    /// </summary>
    /// <param name="properties">对象属性集合。</param>
    /// <returns>嵌套对象类型序列。</returns>
    private static IEnumerable<SchemaObjectSpec> CollectNestedTypes(IEnumerable<SchemaPropertySpec> properties)
    {
        foreach (var property in properties)
        {
            if (property.TypeSpec.Kind == SchemaNodeKind.Object && property.TypeSpec.NestedObject is not null)
            {
                yield return property.TypeSpec.NestedObject;
                continue;
            }

            if (property.TypeSpec.Kind == SchemaNodeKind.Array &&
                property.TypeSpec.ItemTypeSpec?.Kind == SchemaNodeKind.Object &&
                property.TypeSpec.ItemTypeSpec.NestedObject is not null)
            {
                yield return property.TypeSpec.ItemTypeSpec.NestedObject;
            }
        }
    }

    /// <summary>
    ///     为生成属性输出 XML 文档。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="property">属性模型。</param>
    /// <param name="indentationLevel">缩进层级。</param>
    private static void AppendPropertyDocumentation(
        StringBuilder builder,
        SchemaPropertySpec property,
        int indentationLevel)
    {
        var indent = new string(' ', indentationLevel * 4);
        builder.AppendLine($"{indent}/// <summary>");
        builder.AppendLine(
            $"{indent}///     {EscapeXmlDocumentation(property.Description ?? property.Title ?? $"Gets or sets the value mapped from schema property path '{property.DisplayPath}'.")}");
        builder.AppendLine($"{indent}/// </summary>");
        builder.AppendLine($"{indent}/// <remarks>");
        builder.AppendLine(
            $"{indent}///     Schema property path: '{EscapeXmlDocumentation(property.DisplayPath)}'.");

        if (!string.IsNullOrWhiteSpace(property.Title))
        {
            builder.AppendLine(
                $"{indent}///     Display title: '{EscapeXmlDocumentation(property.Title!)}'.");
        }

        if (!string.IsNullOrWhiteSpace(property.TypeSpec.EnumDocumentation))
        {
            builder.AppendLine(
                $"{indent}///     Allowed values: {EscapeXmlDocumentation(property.TypeSpec.EnumDocumentation!)}.");
        }

        if (!string.IsNullOrWhiteSpace(property.TypeSpec.ConstraintDocumentation))
        {
            builder.AppendLine(
                $"{indent}///     Constraints: {EscapeXmlDocumentation(property.TypeSpec.ConstraintDocumentation!)}.");
        }

        if (!string.IsNullOrWhiteSpace(property.TypeSpec.RefTableName))
        {
            builder.AppendLine(
                $"{indent}///     References config table: '{EscapeXmlDocumentation(property.TypeSpec.RefTableName!)}'.");
        }

        var itemConstraintDocumentation = property.TypeSpec.ItemTypeSpec?.ConstraintDocumentation;
        if (property.TypeSpec.Kind == SchemaNodeKind.Array &&
            !string.IsNullOrWhiteSpace(itemConstraintDocumentation))
        {
            builder.AppendLine(
                $"{indent}///     Item constraints: {EscapeXmlDocumentation(itemConstraintDocumentation!)}.");
        }

        if (!string.IsNullOrWhiteSpace(property.TypeSpec.Initializer))
        {
            builder.AppendLine(
                $"{indent}///     Generated default initializer: {EscapeXmlDocumentation(property.TypeSpec.Initializer!.Trim())}");
        }

        builder.AppendLine($"{indent}/// </remarks>");
    }

    /// <summary>
    ///     将 schema 字段名转换并验证为生成代码可直接使用的属性标识符。
    ///     生成器会在这里拒绝无法映射为合法 C# 标识符的外部输入，避免生成源码后才在编译阶段失败。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="schemaName">Schema 原始字段名。</param>
    /// <param name="propertyName">生成后的属性名。</param>
    /// <param name="diagnostic">字段名非法时生成的诊断。</param>
    /// <returns>是否成功生成合法属性标识符。</returns>
    private static bool TryBuildPropertyIdentifier(
        string filePath,
        string displayPath,
        string schemaName,
        out string propertyName,
        out Diagnostic? diagnostic)
    {
        propertyName = ToPascalCase(schemaName);
        if (SyntaxFacts.IsValidIdentifier(propertyName))
        {
            diagnostic = null;
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.InvalidGeneratedIdentifier,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            schemaName,
            propertyName);
        return false;
    }

    /// <summary>
    ///     从 schema 文件路径提取实体基础名。
    /// </summary>
    /// <param name="path">Schema 文件路径。</param>
    /// <returns>去掉扩展名和 <c>.schema</c> 后缀的实体基础名。</returns>
    private static string GetSchemaBaseName(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
        {
            return fileName.Substring(0, fileName.Length - ".schema.json".Length);
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    /// <summary>
    ///     解析生成注册辅助时要使用的 schema 相对路径。
    ///     生成器优先保留 `schemas/` 目录以下的相对路径，以便消费端默认约定和 MSBuild AdditionalFiles 约定保持一致。
    /// </summary>
    /// <param name="path">Schema 文件路径。</param>
    /// <returns>用于运行时注册的 schema 相对路径。</returns>
    private static string GetSchemaRelativePath(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        const string rootMarker = "schemas/";
        const string nestedMarker = "/schemas/";

        if (normalizedPath.StartsWith(rootMarker, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath;
        }

        var nestedMarkerIndex = normalizedPath.LastIndexOf(nestedMarker, StringComparison.OrdinalIgnoreCase);
        if (nestedMarkerIndex >= 0)
        {
            return normalizedPath.Substring(nestedMarkerIndex + 1);
        }

        return $"schemas/{Path.GetFileName(path)}";
    }

    /// <summary>
    ///     将 schema 名称转换为 PascalCase 标识符。
    /// </summary>
    /// <param name="value">原始名称。</param>
    /// <returns>PascalCase 标识符。</returns>
    private static string ToPascalCase(string value)
    {
        var tokens = value
            .Split(new[] { '-', '_', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static token =>
                char.ToUpperInvariant(token[0]) + token.Substring(1))
            .ToArray();

        return tokens.Length == 0 ? "Config" : string.Concat(tokens);
    }

    /// <summary>
    ///     将 PascalCase 标识符转换为 camelCase 字段名。
    /// </summary>
    /// <param name="value">PascalCase 标识符。</param>
    /// <returns>适合作为私有字段名的 camelCase 标识符。</returns>
    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            return char.ToLowerInvariant(value[0]).ToString();
        }

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    ///     将 schema 字段路径转换为可用于生成引用元数据成员的 PascalCase 标识符。
    /// </summary>
    /// <param name="displayPath">Schema 字段路径。</param>
    /// <returns>稳定的成员名。</returns>
    private static string BuildReferenceMemberName(string displayPath)
    {
        var segments = displayPath.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();

        foreach (var segment in segments)
        {
            var normalizedSegment = segment
                .Replace("[]", "Items")
                .Replace("[", " ")
                .Replace("]", " ");
            builder.Append(ToPascalCase(normalizedSegment));
        }

        return builder.Length == 0 ? "Reference" : builder.ToString();
    }

    /// <summary>
    ///     为 AdditionalFiles 诊断创建文件位置。
    /// </summary>
    /// <param name="path">文件路径。</param>
    /// <returns>指向文件开头的位置。</returns>
    private static Location CreateFileLocation(string path)
    {
        return Location.Create(
            path,
            TextSpan.FromBounds(0, 0),
            new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
    }

    /// <summary>
    ///     读取字符串元数据。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="propertyName">元数据字段名。</param>
    /// <returns>非空字符串值；不存在时返回空。</returns>
    private static string? TryGetMetadataString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var metadataElement) ||
            metadataElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = metadataElement.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    ///     创建统一格式的无效查询索引元数据诊断。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="reason">具体失败原因。</param>
    /// <returns>稳定的查询索引诊断。</returns>
    private static Diagnostic CreateInvalidLookupIndexDiagnostic(
        string filePath,
        string displayPath,
        string reason)
    {
        return Diagnostic.Create(
            ConfigSchemaDiagnostics.InvalidLookupIndexMetadata,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            LookupIndexMetadataKey,
            reason);
    }

    /// <summary>
    ///     读取布尔元数据。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="propertyName">元数据字段名。</param>
    /// <returns>布尔元数据值；不存在时返回空。</returns>
    private static (bool? Value, string? Diagnostic) TryGetMetadataBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var metadataElement))
        {
            return (null, null);
        }

        if (metadataElement.ValueKind != JsonValueKind.True &&
            metadataElement.ValueKind != JsonValueKind.False)
        {
            return (null, $"Expected a JSON boolean but found '{metadataElement.ValueKind}'.");
        }

        return (metadataElement.GetBoolean(), null);
    }

    /// <summary>
    ///     判断某个已支持的索引标量映射是否需要在查询辅助中生成空值守卫。
    ///     这里必须显式枚举所有已支持的 schema 标量类型，避免未来新增引用类型标量时静默漏掉空检查。
    /// </summary>
    /// <param name="typeSpec">生成字段的标量类型模型。</param>
    /// <returns>需要在生成的索引查询辅助中保护 <see langword="null" /> 参数时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    /// <exception cref="InvalidOperationException">当前受支持的标量映射未被完整分类时抛出。</exception>
    private static bool RequiresIndexedLookupNullGuard(SchemaTypeSpec typeSpec)
    {
        return typeSpec.SchemaType switch
        {
            "integer" => false,
            "number" => false,
            "boolean" => false,
            "string" => true,
            _ => throw new InvalidOperationException(
                $"Indexed lookup null-guard classification does not cover schema scalar type '{typeSpec.SchemaType}' mapped to '{typeSpec.ClrType}'.")
        };
    }

    /// <summary>
    ///     解析 schema 顶层配置目录元数据。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="element">Schema 顶层节点。</param>
    /// <param name="defaultRelativePath">默认配置目录。</param>
    /// <returns>最终使用的配置目录或诊断。</returns>
    private static (string? Path, Diagnostic? Diagnostic) ResolveConfigRelativePath(
        string filePath,
        JsonElement element,
        string defaultRelativePath)
    {
        if (!element.TryGetProperty(ConfigPathMetadataKey, out var configPathElement))
        {
            return (defaultRelativePath, null);
        }

        if (configPathElement.ValueKind != JsonValueKind.String)
        {
            return (
                null,
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidConfigRelativePathMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    ConfigPathMetadataKey,
                    $"Expected a JSON string but found '{configPathElement.ValueKind}'."));
        }

        var configuredPath = configPathElement.GetString();
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return (
                null,
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidConfigRelativePathMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    ConfigPathMetadataKey,
                    "Path cannot be null, empty, or whitespace."));
        }

        var normalizedPath = NormalizeConfigRelativePath(configuredPath!);
        if (normalizedPath is null)
        {
            return (
                null,
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidConfigRelativePathMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    ConfigPathMetadataKey,
                    "Path must be relative and cannot contain '..' segments."));
        }

        return (normalizedPath, null);
    }

    /// <summary>
    ///     标准化配置目录元数据，统一斜杠并拒绝逃逸配置根目录的写法。
    /// </summary>
    /// <param name="configuredPath">Schema 中声明的相对目录。</param>
    /// <returns>标准化后的相对目录；无效时返回空。</returns>
    private static string? NormalizeConfigRelativePath(string configuredPath)
    {
        var normalizedPath = configuredPath.Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath) ||
            normalizedPath.StartsWith("/", StringComparison.Ordinal) ||
            Path.IsPathRooted(normalizedPath))
        {
            return null;
        }

        var normalizedSegments = new List<string>();
        foreach (var segment in normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(segment, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(segment, "..", StringComparison.Ordinal))
            {
                return null;
            }

            normalizedSegments.Add(segment);
        }

        return normalizedSegments.Count == 0 ? null : string.Join("/", normalizedSegments);
    }

    /// <summary>
    ///     为标量字段构建可直接生成到属性上的默认值初始化器。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaType">标量类型。</param>
    /// <returns>初始化器源码；不兼容时返回空。</returns>
    private static string? TryBuildScalarInitializer(JsonElement element, string schemaType)
    {
        if (!element.TryGetProperty("default", out var defaultElement))
        {
            return null;
        }

        return schemaType switch
        {
            "integer" when defaultElement.ValueKind == JsonValueKind.Number &&
                           defaultElement.TryGetInt64(out var intValue) =>
                $" = {intValue.ToString(CultureInfo.InvariantCulture)};",
            "number" when defaultElement.ValueKind == JsonValueKind.Number =>
                $" = {defaultElement.GetDouble().ToString(CultureInfo.InvariantCulture)};",
            "boolean" when defaultElement.ValueKind == JsonValueKind.True => " = true;",
            "boolean" when defaultElement.ValueKind == JsonValueKind.False => " = false;",
            "string" when defaultElement.ValueKind == JsonValueKind.String =>
                $" = {SymbolDisplay.FormatLiteral(defaultElement.GetString() ?? string.Empty, true)};",
            _ => null
        };
    }

    /// <summary>
    ///     为标量数组构建默认值初始化器。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="itemType">元素类型。</param>
    /// <param name="itemClrType">元素 CLR 类型。</param>
    /// <returns>初始化器源码；不兼容时返回空。</returns>
    private static string? TryBuildArrayInitializer(JsonElement element, string itemType, string itemClrType)
    {
        if (!element.TryGetProperty("default", out var defaultElement) ||
            defaultElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var items = new List<string>();
        foreach (var item in defaultElement.EnumerateArray())
        {
            var literal = itemType switch
            {
                "integer" when item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var intValue) =>
                    intValue.ToString(CultureInfo.InvariantCulture),
                "number" when item.ValueKind == JsonValueKind.Number =>
                    item.GetDouble().ToString(CultureInfo.InvariantCulture),
                "boolean" when item.ValueKind == JsonValueKind.True => "true",
                "boolean" when item.ValueKind == JsonValueKind.False => "false",
                "string" when item.ValueKind == JsonValueKind.String =>
                    SymbolDisplay.FormatLiteral(item.GetString() ?? string.Empty, true),
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(literal))
            {
                return null;
            }

            items.Add(literal);
        }

        return $" = new {itemClrType}[] {{ {string.Join(", ", items)} }};";
    }

    /// <summary>
    ///     将 enum 值整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaType">标量类型。</param>
    /// <returns>格式化后的枚举说明。</returns>
    private static string? TryBuildEnumDocumentation(JsonElement element, string schemaType)
    {
        if (!element.TryGetProperty("enum", out var enumElement) ||
            enumElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();
        foreach (var item in enumElement.EnumerateArray())
        {
            var displayValue = schemaType switch
            {
                "integer" when item.ValueKind == JsonValueKind.Number && item.TryGetInt64(out var intValue) =>
                    intValue.ToString(CultureInfo.InvariantCulture),
                "number" when item.ValueKind == JsonValueKind.Number =>
                    item.GetDouble().ToString(CultureInfo.InvariantCulture),
                "boolean" when item.ValueKind == JsonValueKind.True => "true",
                "boolean" when item.ValueKind == JsonValueKind.False => "false",
                "string" when item.ValueKind == JsonValueKind.String => item.GetString(),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(displayValue))
            {
                values.Add(displayValue!);
            }
        }

        return values.Count > 0 ? string.Join(", ", values) : null;
    }

    /// <summary>
    ///     将 shared schema 子集中的范围、步进、长度、数组数量 / 去重与对象属性数量约束整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaType">标量类型。</param>
    /// <returns>格式化后的约束说明。</returns>
    private static string? TryBuildConstraintDocumentation(JsonElement element, string schemaType)
    {
        var parts = new List<string>();

        var constDocumentation = TryBuildConstDocumentation(element, schemaType);
        if (constDocumentation is not null)
        {
            parts.Add($"const = {constDocumentation}");
        }

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "minimum", out var minimum))
        {
            parts.Add($"minimum = {minimum.ToString(CultureInfo.InvariantCulture)}");
        }

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "exclusiveMinimum", out var exclusiveMinimum))
        {
            parts.Add($"exclusiveMinimum = {exclusiveMinimum.ToString(CultureInfo.InvariantCulture)}");
        }

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "maximum", out var maximum))
        {
            parts.Add($"maximum = {maximum.ToString(CultureInfo.InvariantCulture)}");
        }

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "exclusiveMaximum", out var exclusiveMaximum))
        {
            parts.Add($"exclusiveMaximum = {exclusiveMaximum.ToString(CultureInfo.InvariantCulture)}");
        }

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "multipleOf", out var multipleOf) &&
            multipleOf > 0d)
        {
            parts.Add($"multipleOf = {multipleOf.ToString(CultureInfo.InvariantCulture)}");
        }

        if (schemaType == "string" &&
            TryGetNonNegativeInt32(element, "minLength", out var minLength))
        {
            parts.Add($"minLength = {minLength.ToString(CultureInfo.InvariantCulture)}");
        }

        if (schemaType == "string" &&
            TryGetNonNegativeInt32(element, "maxLength", out var maxLength))
        {
            parts.Add($"maxLength = {maxLength.ToString(CultureInfo.InvariantCulture)}");
        }

        if (schemaType == "string" &&
            element.TryGetProperty("pattern", out var patternElement) &&
            patternElement.ValueKind == JsonValueKind.String)
        {
            parts.Add($"pattern = '{patternElement.GetString() ?? string.Empty}'");
        }

        if (schemaType == "array" &&
            TryGetNonNegativeInt32(element, "minItems", out var minItems))
        {
            parts.Add($"minItems = {minItems.ToString(CultureInfo.InvariantCulture)}");
        }

        if (schemaType == "array" &&
            TryGetNonNegativeInt32(element, "maxItems", out var maxItems))
        {
            parts.Add($"maxItems = {maxItems.ToString(CultureInfo.InvariantCulture)}");
        }

        if (schemaType == "array" &&
            element.TryGetProperty("uniqueItems", out var uniqueItemsElement) &&
            uniqueItemsElement.ValueKind == JsonValueKind.True)
        {
            parts.Add("uniqueItems = true");
        }

        if (schemaType == "object" &&
            TryGetNonNegativeInt32(element, "minProperties", out var minProperties))
        {
            parts.Add($"minProperties = {minProperties.ToString(CultureInfo.InvariantCulture)}");
        }

        if (schemaType == "object" &&
            TryGetNonNegativeInt32(element, "maxProperties", out var maxProperties))
        {
            parts.Add($"maxProperties = {maxProperties.ToString(CultureInfo.InvariantCulture)}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    /// <summary>
    ///     将 const 值整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaType">当前 schema 类型。</param>
    /// <returns>格式化后的常量说明。</returns>
    private static string? TryBuildConstDocumentation(JsonElement element, string schemaType)
    {
        if (!element.TryGetProperty("const", out var constElement))
        {
            return null;
        }

        return schemaType switch
        {
            "integer" when constElement.ValueKind == JsonValueKind.Number && constElement.TryGetInt64(out var intValue) =>
                intValue.ToString(CultureInfo.InvariantCulture),
            "number" when constElement.ValueKind == JsonValueKind.Number =>
                constElement.GetDouble().ToString(CultureInfo.InvariantCulture),
            "boolean" when constElement.ValueKind == JsonValueKind.True => "true",
            "boolean" when constElement.ValueKind == JsonValueKind.False => "false",
            // Preserve the exact JSON literal so empty strings and other string-shaped constants
            // remain unambiguous in generated XML documentation.
            "string" when constElement.ValueKind == JsonValueKind.String => constElement.GetRawText(),
            "array" when constElement.ValueKind == JsonValueKind.Array => constElement.GetRawText(),
            "object" when constElement.ValueKind == JsonValueKind.Object => constElement.GetRawText(),
            _ => null
        };
    }

    /// <summary>
    ///     读取有限数值元数据。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="propertyName">元数据名称。</param>
    /// <param name="value">读取到的数值。</param>
    /// <returns>是否读取成功。</returns>
    private static bool TryGetFiniteNumber(
        JsonElement element,
        string propertyName,
        out double value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var metadataElement) &&
               metadataElement.ValueKind == JsonValueKind.Number &&
               metadataElement.TryGetDouble(out value) &&
               !double.IsNaN(value) &&
               !double.IsInfinity(value);
    }

    /// <summary>
    ///     读取非负整数元数据。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="propertyName">元数据名称。</param>
    /// <param name="value">读取到的整数值。</param>
    /// <returns>是否读取成功。</returns>
    private static bool TryGetNonNegativeInt32(
        JsonElement element,
        string propertyName,
        out int value)
    {
        value = default;
        return element.TryGetProperty(propertyName, out var metadataElement) &&
               metadataElement.ValueKind == JsonValueKind.Number &&
               metadataElement.TryGetInt32(out value) &&
               value >= 0;
    }

    /// <summary>
    ///     组合逻辑字段路径。
    /// </summary>
    /// <param name="parentPath">父路径。</param>
    /// <param name="propertyName">当前属性名。</param>
    /// <returns>组合后的路径。</returns>
    private static string CombinePath(string parentPath, string propertyName)
    {
        return parentPath == "<root>" ? propertyName : $"{parentPath}.{propertyName}";
    }

    /// <summary>
    ///     转义 XML 文档文本。
    /// </summary>
    /// <param name="value">原始字符串。</param>
    /// <returns>已转义的字符串。</returns>
    private static string EscapeXmlDocumentation(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    ///     解析结果包装。
    /// </summary>
    /// <param name="Schema">解析出的 schema。</param>
    /// <param name="Diagnostics">生成过程中收集的诊断。</param>
    private sealed record SchemaParseResult(
        SchemaFileSpec? Schema,
        IReadOnlyList<Diagnostic> Diagnostics)
    {
        public static SchemaParseResult FromSchema(SchemaFileSpec schema)
        {
            return new SchemaParseResult(schema, Array.Empty<Diagnostic>());
        }

        public static SchemaParseResult FromDiagnostic(Diagnostic diagnostic)
        {
            return new SchemaParseResult(null, new[] { diagnostic });
        }
    }

    /// <summary>
    ///     对象解析结果包装。
    /// </summary>
    /// <param name="Object">解析出的对象类型。</param>
    /// <param name="Diagnostic">错误诊断。</param>
    private sealed record ParsedObjectResult(
        SchemaObjectSpec? Object,
        Diagnostic? Diagnostic)
    {
        public static ParsedObjectResult FromObject(SchemaObjectSpec schemaObject)
        {
            return new ParsedObjectResult(schemaObject, null);
        }

        public static ParsedObjectResult FromDiagnostic(Diagnostic diagnostic)
        {
            return new ParsedObjectResult(null, diagnostic);
        }
    }

    /// <summary>
    ///     生成器级 schema 模型。
    /// </summary>
    /// <param name="FileName">Schema 文件名。</param>
    /// <param name="EntityName">实体名基础标识。</param>
    /// <param name="ClassName">根配置类型名。</param>
    /// <param name="TableName">配置表包装类型名。</param>
    /// <param name="Namespace">目标命名空间。</param>
    /// <param name="KeyClrType">主键 CLR 类型。</param>
    /// <param name="KeyPropertyName">生成配置类型中的主键属性名。</param>
    /// <param name="TableRegistrationName">运行时注册名。</param>
    /// <param name="ConfigRelativePath">配置目录相对路径。</param>
    /// <param name="SchemaRelativePath">Schema 文件相对路径。</param>
    /// <param name="Title">根标题元数据。</param>
    /// <param name="Description">根描述元数据。</param>
    /// <param name="RootObject">根对象模型。</param>
    private sealed record SchemaFileSpec(
        string FileName,
        string EntityName,
        string ClassName,
        string TableName,
        string Namespace,
        string KeyClrType,
        string KeyPropertyName,
        string TableRegistrationName,
        string ConfigRelativePath,
        string SchemaRelativePath,
        string? Title,
        string? Description,
        SchemaObjectSpec RootObject);

    /// <summary>
    ///     生成器内部的对象类型模型。
    /// </summary>
    /// <param name="DisplayPath">对象字段路径。</param>
    /// <param name="ClassName">要生成的 CLR 类型名。</param>
    /// <param name="Title">对象标题元数据。</param>
    /// <param name="Description">对象描述元数据。</param>
    /// <param name="ConstraintDocumentation">对象约束说明。</param>
    /// <param name="Properties">对象属性集合。</param>
    private sealed record SchemaObjectSpec(
        string DisplayPath,
        string ClassName,
        string? Title,
        string? Description,
        string? ConstraintDocumentation,
        IReadOnlyList<SchemaPropertySpec> Properties);

    /// <summary>
    ///     单个配置属性模型。
    /// </summary>
    /// <param name="SchemaName">Schema 原始字段名。</param>
    /// <param name="DisplayPath">逻辑字段路径。</param>
    /// <param name="PropertyName">CLR 属性名。</param>
    /// <param name="IsRequired">是否必填。</param>
    /// <param name="Title">字段标题元数据。</param>
    /// <param name="Description">字段描述元数据。</param>
    /// <param name="IsIndexedLookup">是否声明生成只读精确匹配索引。</param>
    /// <param name="TypeSpec">字段类型模型。</param>
    private sealed record SchemaPropertySpec(
        string SchemaName,
        string DisplayPath,
        string PropertyName,
        bool IsRequired,
        string? Title,
        string? Description,
        bool IsIndexedLookup,
        SchemaTypeSpec TypeSpec);

    /// <summary>
    ///     类型模型，覆盖标量、对象和数组。
    /// </summary>
    /// <param name="Kind">节点种类。</param>
    /// <param name="SchemaType">Schema 类型名。</param>
    /// <param name="ClrType">CLR 类型名。</param>
    /// <param name="Initializer">属性初始化器。</param>
    /// <param name="EnumDocumentation">枚举文档说明。</param>
    /// <param name="ConstraintDocumentation">范围或长度约束说明。</param>
    /// <param name="RefTableName">目标引用表名称。</param>
    /// <param name="NestedObject">对象节点对应的嵌套类型。</param>
    /// <param name="ItemTypeSpec">数组元素类型模型。</param>
    private sealed record SchemaTypeSpec(
        SchemaNodeKind Kind,
        string SchemaType,
        string ClrType,
        string? Initializer,
        string? EnumDocumentation,
        string? ConstraintDocumentation,
        string? RefTableName,
        SchemaObjectSpec? NestedObject,
        SchemaTypeSpec? ItemTypeSpec);

    /// <summary>
    ///     生成代码前的跨表引用字段种子信息。
    /// </summary>
    /// <param name="DisplayPath">Schema 字段路径。</param>
    /// <param name="ReferencedTableName">目标表名称。</param>
    /// <param name="ValueSchemaType">引用值的标量 schema 类型。</param>
    /// <param name="IsCollection">是否为数组引用。</param>
    private sealed record GeneratedReferenceSeed(
        string DisplayPath,
        string ReferencedTableName,
        string ValueSchemaType,
        bool IsCollection);

    /// <summary>
    ///     已分配稳定成员名的生成期跨表引用信息。
    /// </summary>
    /// <param name="MemberName">生成到绑定类中的成员名。</param>
    /// <param name="DisplayPath">Schema 字段路径。</param>
    /// <param name="ReferencedTableName">目标表名称。</param>
    /// <param name="ValueSchemaType">引用值的标量 schema 类型。</param>
    /// <param name="IsCollection">是否为数组引用。</param>
    private sealed record GeneratedReferenceSpec(
        string MemberName,
        string DisplayPath,
        string ReferencedTableName,
        string ValueSchemaType,
        bool IsCollection);

    /// <summary>
    ///     属性解析结果包装。
    /// </summary>
    /// <param name="Property">解析出的属性模型。</param>
    /// <param name="Diagnostic">错误诊断。</param>
    private sealed record ParsedPropertyResult(
        SchemaPropertySpec? Property,
        Diagnostic? Diagnostic)
    {
        public static ParsedPropertyResult FromProperty(SchemaPropertySpec property)
        {
            return new ParsedPropertyResult(property, null);
        }

        public static ParsedPropertyResult FromDiagnostic(Diagnostic diagnostic)
        {
            return new ParsedPropertyResult(null, diagnostic);
        }
    }

    /// <summary>
    ///     类型节点种类。
    /// </summary>
    private enum SchemaNodeKind
    {
        Scalar,
        Object,
        Array
    }
}
