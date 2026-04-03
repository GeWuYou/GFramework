using GFramework.SourceGenerators.Diagnostics;

namespace GFramework.SourceGenerators.Config;

/// <summary>
///     根据 AdditionalFiles 中的 JSON schema 生成配置类型和配置表包装。
///     当前实现聚焦 AI-First 配置系统共享的最小 schema 子集，
///     支持嵌套对象、对象数组、标量数组，以及可映射的 default / enum / ref-table 元数据。
/// </summary>
[Generator]
public sealed class SchemaConfigGenerator : IIncrementalGenerator
{
    private const string GeneratedNamespace = "GFramework.Game.Config.Generated";

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
            var schema = new SchemaFileSpec(
                Path.GetFileName(file.Path),
                entityName,
                schemaObject.ClassName,
                $"{entityName}Table",
                GeneratedNamespace,
                idProperty.TypeSpec.ClrType.TrimEnd('?'),
                idProperty.PropertyName,
                schemaBaseName,
                schemaBaseName,
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

        var requiredProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                CombinePath(displayPath, property.Name));
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
        string displayPath)
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
        var propertyName = ToPascalCase(property.Name);

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
                    description, refTableName);

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
        string? refTableName)
    {
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
                    new SchemaTypeSpec(
                        SchemaNodeKind.Array,
                        "array",
                        $"global::System.Collections.Generic.IReadOnlyList<{itemClrType}>",
                        TryBuildArrayInitializer(property.Value, itemType, itemClrType) ??
                        $" = global::System.Array.Empty<{itemClrType}>();",
                        TryBuildEnumDocumentation(itemsElement, itemType),
                        null,
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
                    new SchemaTypeSpec(
                        SchemaNodeKind.Array,
                        "array",
                        $"global::System.Collections.Generic.IReadOnlyList<{objectSpec.ClassName}>",
                        $" = global::System.Array.Empty<{objectSpec.ClassName}>();",
                        null,
                        null,
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
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Creates a generated table wrapper around the runtime config table instance.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"inner\">The runtime config table instance.</param>");
        builder.AppendLine(
            $"    public {schema.TableName}(global::GFramework.Game.Abstractions.Config.IConfigTable<{schema.KeyClrType}, {schema.ClassName}> inner)");
        builder.AppendLine("    {");
        builder.AppendLine("        _inner = inner ?? throw new global::System.ArgumentNullException(nameof(inner));");
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
        builder.AppendLine("    ///     Gets the runtime registration name of the generated config table.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            $"    public const string TableName = {SymbolDisplay.FormatLiteral(schema.TableRegistrationName, true)};");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Gets the config directory path expected by the generated registration helper.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            $"    public const string ConfigRelativePath = {SymbolDisplay.FormatLiteral(schema.ConfigRelativePath, true)};");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     Gets the schema file path expected by the generated registration helper.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            $"    public const string SchemaRelativePath = {SymbolDisplay.FormatLiteral(schema.SchemaRelativePath, true)};");
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
        builder.AppendLine("            TableName,");
        builder.AppendLine("            ConfigRelativePath,");
        builder.AppendLine("            SchemaRelativePath,");
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
            $"        return new {schema.TableName}(registry.GetTable<{schema.KeyClrType}, {schema.ClassName}>(TableName));");
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
            $"        if (registry.TryGetTable<{schema.KeyClrType}, {schema.ClassName}>(TableName, out var innerTable) && innerTable is not null)");
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
    ///     将 shared schema 子集中的范围与长度约束整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaType">标量类型。</param>
    /// <returns>格式化后的约束说明。</returns>
    private static string? TryBuildConstraintDocumentation(JsonElement element, string schemaType)
    {
        var parts = new List<string>();

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "minimum", out var minimum))
        {
            parts.Add($"minimum = {minimum.ToString(CultureInfo.InvariantCulture)}");
        }

        if ((schemaType == "integer" || schemaType == "number") &&
            TryGetFiniteNumber(element, "maximum", out var maximum))
        {
            parts.Add($"maximum = {maximum.ToString(CultureInfo.InvariantCulture)}");
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

        return parts.Count > 0 ? string.Join(", ", parts) : null;
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
    /// <param name="Properties">对象属性集合。</param>
    private sealed record SchemaObjectSpec(
        string DisplayPath,
        string ClassName,
        string? Title,
        string? Description,
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
    /// <param name="TypeSpec">字段类型模型。</param>
    private sealed record SchemaPropertySpec(
        string SchemaName,
        string DisplayPath,
        string PropertyName,
        bool IsRequired,
        string? Title,
        string? Description,
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