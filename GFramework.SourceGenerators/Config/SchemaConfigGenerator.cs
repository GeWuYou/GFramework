using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using GFramework.SourceGenerators.Diagnostics;

namespace GFramework.SourceGenerators.Config;

/// <summary>
///     根据 AdditionalFiles 中的 JSON schema 生成配置类型和配置表包装。
///     当前实现聚焦 Runtime MVP 需要的最小能力：单 schema 对应单配置类型，并约定使用必填的 id 字段作为表主键。
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

            if (!root.TryGetProperty("properties", out var propertiesElement) ||
                propertiesElement.ValueKind != JsonValueKind.Object)
            {
                return SchemaParseResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.RootObjectSchemaRequired,
                        CreateFileLocation(file.Path),
                        Path.GetFileName(file.Path)));
            }

            var requiredProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("required", out var requiredElement) &&
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
                var parsedProperty = ParseProperty(file.Path, property, requiredProperties.Contains(property.Name));
                if (parsedProperty.Diagnostic is not null)
                {
                    return SchemaParseResult.FromDiagnostic(parsedProperty.Diagnostic);
                }

                properties.Add(parsedProperty.Property!);
            }

            var idProperty = properties.FirstOrDefault(static property =>
                string.Equals(property.SchemaName, "id", StringComparison.OrdinalIgnoreCase));
            if (idProperty is null || !idProperty.IsRequired)
            {
                return SchemaParseResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.IdPropertyRequired,
                        CreateFileLocation(file.Path),
                        Path.GetFileName(file.Path)));
            }

            if (!string.Equals(idProperty.SchemaType, "integer", StringComparison.Ordinal) &&
                !string.Equals(idProperty.SchemaType, "string", StringComparison.Ordinal))
            {
                return SchemaParseResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.UnsupportedKeyType,
                        CreateFileLocation(file.Path),
                        Path.GetFileName(file.Path),
                        idProperty.SchemaType));
            }

            var entityName = ToPascalCase(GetSchemaBaseName(file.Path));
            var schema = new SchemaFileSpec(
                Path.GetFileName(file.Path),
                entityName,
                $"{entityName}Config",
                $"{entityName}Table",
                GeneratedNamespace,
                idProperty.ClrType,
                TryGetMetadataString(root, "title"),
                TryGetMetadataString(root, "description"),
                properties);

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
    ///     解析单个 schema 属性定义。
    /// </summary>
    /// <param name="filePath">schema 文件路径。</param>
    /// <param name="property">属性 JSON 节点。</param>
    /// <param name="isRequired">属性是否必填。</param>
    /// <returns>解析后的属性信息或诊断。</returns>
    private static ParsedPropertyResult ParseProperty(
        string filePath,
        JsonProperty property,
        bool isRequired)
    {
        if (!property.Value.TryGetProperty("type", out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.String)
        {
            return ParsedPropertyResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.UnsupportedPropertyType,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    property.Name,
                    "<missing>"));
        }

        var schemaType = typeElement.GetString() ?? string.Empty;
        var title = TryGetMetadataString(property.Value, "title");
        var description = TryGetMetadataString(property.Value, "description");
        var refTableName = TryGetMetadataString(property.Value, "x-gframework-ref-table");

        switch (schemaType)
        {
            case "integer":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    ToPascalCase(property.Name),
                    "integer",
                    isRequired ? "int" : "int?",
                    isRequired,
                    TryBuildScalarInitializer(property.Value, "integer"),
                    title,
                    description,
                    TryBuildEnumDocumentation(property.Value, "integer"),
                    refTableName));

            case "number":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    ToPascalCase(property.Name),
                    "number",
                    isRequired ? "double" : "double?",
                    isRequired,
                    TryBuildScalarInitializer(property.Value, "number"),
                    title,
                    description,
                    TryBuildEnumDocumentation(property.Value, "number"),
                    refTableName));

            case "boolean":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    ToPascalCase(property.Name),
                    "boolean",
                    isRequired ? "bool" : "bool?",
                    isRequired,
                    TryBuildScalarInitializer(property.Value, "boolean"),
                    title,
                    description,
                    TryBuildEnumDocumentation(property.Value, "boolean"),
                    refTableName));

            case "string":
                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    ToPascalCase(property.Name),
                    "string",
                    isRequired ? "string" : "string?",
                    isRequired,
                    TryBuildScalarInitializer(property.Value, "string") ??
                    (isRequired ? " = string.Empty;" : null),
                    title,
                    description,
                    TryBuildEnumDocumentation(property.Value, "string"),
                    refTableName));

            case "array":
                if (!property.Value.TryGetProperty("items", out var itemsElement) ||
                    !itemsElement.TryGetProperty("type", out var itemTypeElement) ||
                    itemTypeElement.ValueKind != JsonValueKind.String)
                {
                    return ParsedPropertyResult.FromDiagnostic(
                        Diagnostic.Create(
                            ConfigSchemaDiagnostics.UnsupportedPropertyType,
                            CreateFileLocation(filePath),
                            Path.GetFileName(filePath),
                            property.Name,
                            "array"));
                }

                var itemType = itemTypeElement.GetString() ?? string.Empty;
                var itemClrType = itemType switch
                {
                    "integer" => "int",
                    "number" => "double",
                    "boolean" => "bool",
                    "string" => "string",
                    _ => string.Empty
                };

                if (string.IsNullOrEmpty(itemClrType))
                {
                    return ParsedPropertyResult.FromDiagnostic(
                        Diagnostic.Create(
                            ConfigSchemaDiagnostics.UnsupportedPropertyType,
                            CreateFileLocation(filePath),
                            Path.GetFileName(filePath),
                            property.Name,
                            $"array<{itemType}>"));
                }

                return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
                    property.Name,
                    ToPascalCase(property.Name),
                    "array",
                    $"global::System.Collections.Generic.IReadOnlyList<{itemClrType}>",
                    isRequired,
                    TryBuildArrayInitializer(property.Value, itemType, itemClrType) ??
                    " = global::System.Array.Empty<" + itemClrType + ">();",
                    title,
                    description,
                    TryBuildEnumDocumentation(itemsElement, itemType),
                    refTableName));

            default:
                return ParsedPropertyResult.FromDiagnostic(
                    Diagnostic.Create(
                        ConfigSchemaDiagnostics.UnsupportedPropertyType,
                        CreateFileLocation(filePath),
                        Path.GetFileName(filePath),
                        property.Name,
                        schemaType));
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
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            $"///     Auto-generated config type for schema file '{schema.FileName}'.");
        builder.AppendLine(
            $"///     {EscapeXmlDocumentation(schema.Description ?? schema.Title ?? "This type is generated from JSON schema so runtime loading and editor tooling can share the same contract.")}");
        builder.AppendLine("/// </summary>");
        builder.AppendLine($"public sealed partial class {schema.ClassName}");
        builder.AppendLine("{");

        foreach (var property in schema.Properties)
        {
            AppendPropertyDocumentation(builder, property);
            builder.Append($"    public {property.ClrType} {property.PropertyName} {{ get; set; }}");
            if (!string.IsNullOrEmpty(property.Initializer))
            {
                builder.Append(property.Initializer);
            }

            builder.AppendLine();
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     生成配置表包装源码。
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
    ///     从 schema 文件路径提取实体基础名。
    /// </summary>
    /// <param name="path">schema 文件路径。</param>
    /// <returns>去掉扩展名和 `.schema` 后缀的实体基础名。</returns>
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

    private static void AppendPropertyDocumentation(StringBuilder builder, SchemaPropertySpec property)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            $"    ///     {EscapeXmlDocumentation(property.Description ?? property.Title ?? $"Gets or sets the value mapped from schema property '{property.SchemaName}'.")}");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <remarks>");
        builder.AppendLine(
            $"    ///     Schema property: '{EscapeXmlDocumentation(property.SchemaName)}'.");

        if (!string.IsNullOrWhiteSpace(property.Title))
        {
            builder.AppendLine(
                $"    ///     Display title: '{EscapeXmlDocumentation(property.Title!)}'.");
        }

        if (!string.IsNullOrWhiteSpace(property.EnumDocumentation))
        {
            builder.AppendLine(
                $"    ///     Allowed values: {EscapeXmlDocumentation(property.EnumDocumentation!)}.");
        }

        if (!string.IsNullOrWhiteSpace(property.ReferenceTableName))
        {
            builder.AppendLine(
                $"    ///     References config table: '{EscapeXmlDocumentation(property.ReferenceTableName!)}'.");
        }

        if (!string.IsNullOrWhiteSpace(property.Initializer))
        {
            builder.AppendLine(
                $"    ///     Generated default initializer: {EscapeXmlDocumentation(property.Initializer!.Trim())}");
        }

        builder.AppendLine("    /// </remarks>");
    }

    private static string EscapeXmlDocumentation(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    /// <summary>
    ///     表示单个 schema 文件的解析结果。
    /// </summary>
    /// <param name="Schema">成功解析出的 schema 模型。</param>
    /// <param name="Diagnostics">解析阶段产生的诊断。</param>
    private sealed record SchemaParseResult(
        SchemaFileSpec? Schema,
        ImmutableArray<Diagnostic> Diagnostics)
    {
        /// <summary>
        ///     从成功解析的 schema 模型创建结果。
        /// </summary>
        public static SchemaParseResult FromSchema(SchemaFileSpec schema)
        {
            return new SchemaParseResult(schema, ImmutableArray<Diagnostic>.Empty);
        }

        /// <summary>
        ///     从单个诊断创建结果。
        /// </summary>
        public static SchemaParseResult FromDiagnostic(Diagnostic diagnostic)
        {
            return new SchemaParseResult(null, ImmutableArray.Create(diagnostic));
        }
    }

    /// <summary>
    ///     表示已解析的 schema 文件模型。
    /// </summary>
    private sealed record SchemaFileSpec(
        string FileName,
        string EntityName,
        string ClassName,
        string TableName,
        string Namespace,
        string KeyClrType,
        string? Title,
        string? Description,
        IReadOnlyList<SchemaPropertySpec> Properties);

    /// <summary>
    ///     表示已解析的 schema 属性。
    /// </summary>
    private sealed record SchemaPropertySpec(
        string SchemaName,
        string PropertyName,
        string SchemaType,
        string ClrType,
        bool IsRequired,
        string? Initializer,
        string? Title,
        string? Description,
        string? EnumDocumentation,
        string? ReferenceTableName);

    /// <summary>
    ///     表示单个属性的解析结果。
    /// </summary>
    private sealed record ParsedPropertyResult(
        SchemaPropertySpec? Property,
        Diagnostic? Diagnostic)
    {
        /// <summary>
        ///     从属性模型创建成功结果。
        /// </summary>
        public static ParsedPropertyResult FromProperty(SchemaPropertySpec property)
        {
            return new ParsedPropertyResult(property, null);
        }

        /// <summary>
        ///     从诊断创建失败结果。
        /// </summary>
        public static ParsedPropertyResult FromDiagnostic(Diagnostic diagnostic)
        {
            return new ParsedPropertyResult(null, diagnostic);
        }
    }
}