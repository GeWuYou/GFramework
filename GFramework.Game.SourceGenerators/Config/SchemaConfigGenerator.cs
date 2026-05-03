// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Game.SourceGenerators.Diagnostics;

namespace GFramework.Game.SourceGenerators.Config;

/// <summary>
///     根据 AdditionalFiles 中的 JSON schema 生成配置类型和配置表包装。
///     当前实现聚焦 AI-First 配置系统共享的最小 schema 子集，
///     支持嵌套对象、对象数组、标量数组，以及可映射的 default / enum / const / ref-table 元数据。
///     当前共享子集也会把 <c>multipleOf</c>、<c>uniqueItems</c>、
///     <c>contains</c> / <c>minContains</c> / <c>maxContains</c>、
///     <c>minProperties</c>、<c>maxProperties</c>、<c>dependentRequired</c>、
///     <c>dependentSchemas</c>、<c>allOf</c>、object-focused <c>if</c> / <c>then</c> / <c>else</c>
///     与稳定字符串 <c>format</c> 子集写入生成代码文档，
///     让消费者能直接在强类型 API 上看到运行时生效且不改变生成类型形状的约束。
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

    private const string SupportedStringFormatNames =
        "'date', 'date-time', 'duration', 'email', 'time', 'uri', and 'uuid'";

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
        if (!TryReadSchemaText(file, cancellationToken, out var text, out var diagnostic))
        {
            return SchemaParseResult.FromDiagnostic(diagnostic!);
        }

        try
        {
            using var document = JsonDocument.Parse(text!.ToString());
            return ParseSchemaRoot(file.Path, document.RootElement);
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
    ///     Reads an AdditionalFiles schema text while converting all IO failures into generator diagnostics.
    /// </summary>
    /// <param name="file">AdditionalFiles entry supplied by Roslyn.</param>
    /// <param name="cancellationToken">Cancellation token forwarded by the incremental generator pipeline.</param>
    /// <param name="text">Read source text when the file can be loaded.</param>
    /// <param name="diagnostic">Diagnostic describing the read failure.</param>
    /// <returns><see langword="true" /> when schema text was read successfully; otherwise <see langword="false" />.</returns>
    private static bool TryReadSchemaText(
        AdditionalText file,
        CancellationToken cancellationToken,
        out SourceText? text,
        out Diagnostic? diagnostic)
    {
        try
        {
            text = file.GetText(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            text = null;
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidSchemaJson,
                CreateFileLocation(file.Path),
                Path.GetFileName(file.Path),
                exception.Message);
            return false;
        }

        if (text is null)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidSchemaJson,
                CreateFileLocation(file.Path),
                Path.GetFileName(file.Path),
                "File content could not be read.");
            return false;
        }

        diagnostic = null;
        return true;
    }

    /// <summary>
    ///     Parses a JSON schema root after JSON syntax has already been validated.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics and generated metadata.</param>
    /// <param name="root">Parsed root JSON element.</param>
    /// <returns>Parsed schema model or the first schema diagnostic encountered.</returns>
    private static SchemaParseResult ParseSchemaRoot(string filePath, JsonElement root)
    {
        if (!TryValidateSchemaRoot(filePath, root, out var diagnostic))
        {
            return SchemaParseResult.FromDiagnostic(diagnostic!);
        }

        var schemaBaseName = GetSchemaBaseName(filePath);
        if (!TryBuildRootTypeIdentifiers(filePath, schemaBaseName, out var entityName, out var rootClassName, out diagnostic))
        {
            return SchemaParseResult.FromDiagnostic(diagnostic!);
        }

        var rootObject = ParseObjectSpec(filePath, root, "<root>", rootClassName, isRoot: true);
        if (rootObject.Diagnostic is not null)
        {
            return SchemaParseResult.FromDiagnostic(rootObject.Diagnostic);
        }

        var schemaObject = rootObject.Object!;
        var idProperty = FindValidIdProperty(filePath, schemaObject, out diagnostic);
        if (diagnostic is not null)
        {
            return SchemaParseResult.FromDiagnostic(diagnostic);
        }

        var configRelativePath = ResolveConfigRelativePath(filePath, root, schemaBaseName);
        if (configRelativePath.Diagnostic is not null)
        {
            return SchemaParseResult.FromDiagnostic(configRelativePath.Diagnostic);
        }

        return SchemaParseResult.FromSchema(CreateSchemaFileSpec(
            filePath,
            root,
            entityName,
            schemaObject,
            idProperty!,
            schemaBaseName,
            configRelativePath.Path!));
    }

    /// <summary>
    ///     Validates schema-level contracts that must hold before object and property models are built.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="root">Root JSON schema element.</param>
    /// <param name="diagnostic">First validation diagnostic, if any.</param>
    /// <returns><see langword="true" /> when the root can be parsed as a config schema.</returns>
    private static bool TryValidateSchemaRoot(string filePath, JsonElement root, out Diagnostic? diagnostic)
    {
        if (!root.TryGetProperty("type", out var rootTypeElement) ||
            rootTypeElement.ValueKind != JsonValueKind.String ||
            !IsSchemaType(rootTypeElement.GetString() ?? string.Empty, "object"))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.RootObjectSchemaRequired,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath));
            return false;
        }

        return TryValidateStringFormatMetadataRecursively(filePath, "<root>", root, out diagnostic) &&
            TryValidateUnsupportedCombinatorKeywordsRecursively(filePath, "<root>", root, out diagnostic) &&
            TryValidateUnsupportedOpenObjectKeywordsRecursively(filePath, "<root>", root, out diagnostic) &&
            TryValidateDependentRequiredMetadataRecursively(filePath, "<root>", root, out diagnostic) &&
            TryValidateDependentSchemasMetadataRecursively(filePath, "<root>", root, out diagnostic) &&
            TryValidateAllOfMetadataRecursively(filePath, "<root>", root, out diagnostic) &&
            TryValidateConditionalSchemasMetadataRecursively(filePath, "<root>", root, out diagnostic);
    }

    /// <summary>
    ///     Finds and validates the required root <c>id</c> property that becomes the generated table key.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="schemaObject">Parsed root object model.</param>
    /// <param name="diagnostic">Diagnostic explaining why the key is invalid.</param>
    /// <returns>The key property when it satisfies the generator contract; otherwise null.</returns>
    private static SchemaPropertySpec? FindValidIdProperty(
        string filePath,
        SchemaObjectSpec schemaObject,
        out Diagnostic? diagnostic)
    {
        var idProperty = schemaObject.Properties.FirstOrDefault(static property =>
            string.Equals(property.SchemaName, "id", StringComparison.OrdinalIgnoreCase));
        if (idProperty is null || !idProperty.IsRequired)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.IdPropertyRequired,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath));
            return null;
        }

        if (!IsSchemaType(idProperty.TypeSpec.SchemaType, "integer") &&
            !IsSchemaType(idProperty.TypeSpec.SchemaType, "string"))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.UnsupportedKeyType,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                idProperty.TypeSpec.SchemaType);
            return null;
        }

        diagnostic = null;
        return idProperty;
    }

    /// <summary>
    ///     Creates the generator-level schema model from validated root metadata and the parsed object tree.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="root">Root JSON element used for optional title and description metadata.</param>
    /// <param name="entityName">Generated entity name derived from the schema file name.</param>
    /// <param name="schemaObject">Parsed root object model.</param>
    /// <param name="idProperty">Validated required key property.</param>
    /// <param name="schemaBaseName">Normalized schema base name.</param>
    /// <param name="configRelativePath">Resolved config-relative directory path.</param>
    /// <returns>Completed schema file model used by source emission.</returns>
    private static SchemaFileSpec CreateSchemaFileSpec(
        string filePath,
        JsonElement root,
        string entityName,
        SchemaObjectSpec schemaObject,
        SchemaPropertySpec idProperty,
        string schemaBaseName,
        string configRelativePath)
    {
        return new SchemaFileSpec(
            Path.GetFileName(filePath),
            entityName,
            schemaObject.ClassName,
            $"{entityName}Table",
            GeneratedNamespace,
            idProperty.TypeSpec.ClrType.TrimEnd('?'),
            idProperty.PropertyName,
            schemaBaseName,
            configRelativePath,
            GetSchemaRelativePath(filePath),
            TryGetMetadataString(root, "title"),
            TryGetMetadataString(root, "description"),
            schemaObject);
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
        if (!TryParseObjectProperties(
                filePath,
                displayPath,
                isRoot,
                propertiesElement,
                requiredProperties,
                properties,
                out var propertyDiagnostic))
        {
            return ParsedObjectResult.FromDiagnostic(propertyDiagnostic!);
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
    ///     解析对象 schema 的直接子属性，并在进入代码发射前阻止归一化后的属性名冲突落入生成输出。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">当前对象的逻辑路径。</param>
    /// <param name="isRoot">当前对象是否为根对象。</param>
    /// <param name="propertiesElement">对象的 <c>properties</c> JSON 节点。</param>
    /// <param name="requiredProperties">当前对象声明的必填字段集合。</param>
    /// <param name="properties">成功时返回的已解析属性列表。</param>
    /// <param name="diagnostic">解析失败时返回的首个诊断。</param>
    /// <returns>当所有属性都可安全生成时返回 <see langword="true" />。</returns>
    private static bool TryParseObjectProperties(
        string filePath,
        string displayPath,
        bool isRoot,
        JsonElement propertiesElement,
        ISet<string> requiredProperties,
        ICollection<SchemaPropertySpec> properties,
        out Diagnostic? diagnostic)
    {
        var schemaKeyByGeneratedPropertyName = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in propertiesElement.EnumerateObject())
        {
            var propertyDisplayPath = CombinePath(displayPath, property.Name);
            var parsedProperty = ParseProperty(
                filePath,
                property,
                requiredProperties.Contains(property.Name),
                propertyDisplayPath,
                isDirectChildOfRoot: isRoot);
            if (parsedProperty.Diagnostic is not null)
            {
                diagnostic = parsedProperty.Diagnostic;
                return false;
            }

            if (!TryRegisterGeneratedPropertyName(
                    filePath,
                    propertyDisplayPath,
                    property.Name,
                    parsedProperty.Property!.PropertyName,
                    schemaKeyByGeneratedPropertyName,
                    out diagnostic))
            {
                return false;
            }

            properties.Add(parsedProperty.Property!);
        }

        diagnostic = null;
        return true;
    }

    /// <summary>
    ///     解析单个 schema 属性定义。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="property">属性 JSON 节点。</param>
    /// <param name="isRequired">属性是否必填。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="isDirectChildOfRoot">属性是否为根对象下的直接子属性。</param>
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
        if (!TryCreatePropertyParseContext(
                filePath,
                property,
                isRequired,
                displayPath,
                isDirectChildOfRoot,
                schemaType,
                out var context,
                out var diagnostic))
        {
            return ParsedPropertyResult.FromDiagnostic(diagnostic!);
        }

        return CreatePropertyBySchemaType(filePath, property, schemaType, context!);
    }

    /// <summary>
    ///     Collects shared property metadata and validates cross-cutting metadata before type-specific parsing begins.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="property">Schema property JSON node.</param>
    /// <param name="isRequired">Whether the property is required.</param>
    /// <param name="displayPath">Logical schema path.</param>
    /// <param name="isDirectChildOfRoot">Whether the property is a direct child of the schema root.</param>
    /// <param name="schemaType">Validated schema type keyword.</param>
    /// <param name="context">Shared parsing metadata when validation succeeds.</param>
    /// <param name="diagnostic">First metadata validation diagnostic.</param>
    /// <returns><see langword="true" /> when the property can continue to type-specific parsing.</returns>
    private static bool TryCreatePropertyParseContext(
        string filePath,
        JsonProperty property,
        bool isRequired,
        string displayPath,
        bool isDirectChildOfRoot,
        string schemaType,
        out PropertyParseContext? context,
        out Diagnostic? diagnostic)
    {
        context = null;
        var title = TryGetMetadataString(property.Value, "title");
        var description = TryGetMetadataString(property.Value, "description");
        var refTableName = TryGetMetadataString(property.Value, "x-gframework-ref-table");
        if (!TryValidateStringFormatMetadata(
                filePath,
                displayPath,
                property.Value,
                schemaType,
                out var formatDiagnostic))
        {
            diagnostic = formatDiagnostic;
            return false;
        }

        var indexedLookupMetadata = TryGetMetadataBoolean(property.Value, LookupIndexMetadataKey);
        if (indexedLookupMetadata.Diagnostic is not null)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidLookupIndexMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                LookupIndexMetadataKey,
                indexedLookupMetadata.Diagnostic!);
            return false;
        }

        var isIndexedLookup = indexedLookupMetadata.Value ?? false;
        if (!TryBuildPropertyIdentifier(filePath, displayPath, property.Name, out var propertyName, out diagnostic))
        {
            return false;
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
            return false;
        }

        context = new PropertyParseContext(
            displayPath,
            propertyName,
            isRequired,
            title,
            description,
            refTableName,
            isIndexedLookup);
        diagnostic = null;
        return true;
    }

    /// <summary>
    ///     Dispatches schema property parsing by JSON schema type after shared validation and metadata extraction.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="property">Schema property JSON node.</param>
    /// <param name="schemaType">Validated schema type keyword.</param>
    /// <param name="context">Shared parsing metadata collected from the property node.</param>
    /// <returns>Parsed property model or an unsupported-type diagnostic.</returns>
    private static ParsedPropertyResult CreatePropertyBySchemaType(
        string filePath,
        JsonProperty property,
        string schemaType,
        PropertyParseContext context)
    {
        return schemaType switch
        {
            "integer" => CreateScalarPropertyResult(property, context, schemaType, context.IsRequired ? "int" : "int?"),
            "number" => CreateScalarPropertyResult(property, context, schemaType, context.IsRequired ? "double" : "double?"),
            "boolean" => CreateScalarPropertyResult(property, context, schemaType, context.IsRequired ? "bool" : "bool?"),
            "string" => CreateScalarPropertyResult(property, context, schemaType, context.IsRequired ? "string" : "string?"),
            "object" => CreateObjectPropertyResult(filePath, property, context),
            "array" => ParseArrayProperty(filePath, property, context),
            _ => ParsedPropertyResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.UnsupportedPropertyType,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    context.DisplayPath,
                    schemaType)),
        };
    }

    /// <summary>
    ///     Builds a parsed scalar property model while preserving schema metadata used by generated XML docs.
    /// </summary>
    /// <param name="property">Schema property JSON node.</param>
    /// <param name="context">Shared parsing metadata collected from the property node.</param>
    /// <param name="schemaType">Scalar schema type.</param>
    /// <param name="clrType">Generated CLR property type.</param>
    /// <returns>Parsed property result for a scalar schema node.</returns>
    private static ParsedPropertyResult CreateScalarPropertyResult(
        JsonProperty property,
        PropertyParseContext context,
        string schemaType,
        string clrType)
    {
        var initializer = TryBuildScalarInitializer(property.Value, schemaType);
        if (IsSchemaType(schemaType, "string") && initializer is null && context.IsRequired)
        {
            initializer = " = string.Empty;";
        }

        return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
            property.Name,
            context.DisplayPath,
            context.PropertyName,
            context.IsRequired,
            context.Title,
            context.Description,
            context.IsIndexedLookup,
            new SchemaTypeSpec(
                SchemaNodeKind.Scalar,
                schemaType,
                clrType,
                initializer,
                TryBuildEnumDocumentation(property.Value, schemaType),
                TryBuildConstraintDocumentation(property.Value, schemaType),
                context.RefTableName,
                null,
                null)));
    }

    /// <summary>
    ///     Builds a parsed object property model and reports unsupported object reference/index combinations.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="property">Schema property JSON node.</param>
    /// <param name="context">Shared parsing metadata collected from the property node.</param>
    /// <returns>Parsed property result for an object schema node.</returns>
    private static ParsedPropertyResult CreateObjectPropertyResult(
        string filePath,
        JsonProperty property,
        PropertyParseContext context)
    {
        if (context.IsIndexedLookup)
        {
            return ParsedPropertyResult.FromDiagnostic(
                CreateInvalidLookupIndexDiagnostic(filePath, context.DisplayPath, LookupIndexTopLevelScalarOnlyMessage));
        }

        if (!string.IsNullOrWhiteSpace(context.RefTableName))
        {
            return ParsedPropertyResult.FromDiagnostic(
                Diagnostic.Create(
                    ConfigSchemaDiagnostics.UnsupportedPropertyType,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    context.DisplayPath,
                    "object-ref"));
        }

        var objectResult = ParseObjectSpec(filePath, property.Value, context.DisplayPath, $"{context.PropertyName}Config");
        if (objectResult.Diagnostic is not null)
        {
            return ParsedPropertyResult.FromDiagnostic(objectResult.Diagnostic);
        }

        var objectSpec = objectResult.Object!;
        return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
            property.Name,
            context.DisplayPath,
            context.PropertyName,
            context.IsRequired,
            context.Title,
            context.Description,
            false,
            new SchemaTypeSpec(
                SchemaNodeKind.Object,
                "object",
                context.IsRequired ? objectSpec.ClassName : $"{objectSpec.ClassName}?",
                context.IsRequired ? " = new();" : null,
                TryBuildEnumDocumentation(property.Value, "object"),
                null,
                null,
                objectSpec,
                null)));
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
    ///     验证字符串 <c>format</c> 元数据是否属于当前共享支持子集。
    ///     生成器不尝试解释开放格式名，而是直接在编译阶段拒绝三端无法稳定对齐的 schema。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">属性 schema 节点。</param>
    /// <param name="schemaType">当前 schema type。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点的 format 元数据是否有效。</returns>
    private static bool TryValidateStringFormatMetadata(
        string filePath,
        string displayPath,
        JsonElement element,
        string schemaType,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("format", out var formatElement))
        {
            return true;
        }

        if (!string.Equals(schemaType, "string", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidStringFormatMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "Only 'string' properties can declare 'format'.");
            return false;
        }

        if (formatElement.ValueKind != JsonValueKind.String)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidStringFormatMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "The 'format' value must be a string.");
            return false;
        }

        var formatName = formatElement.GetString() ?? string.Empty;
        if (IsSupportedStringFormat(formatName))
        {
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.InvalidStringFormatMetadata,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            $"Unsupported string format '{formatName}'. Supported formats are {SupportedStringFormatNames}.");
        return false;
    }

    /// <summary>
    ///     递归验证 schema 树中的字符串 <c>format</c> 元数据。
    ///     该遍历专门补足根节点、<c>contains</c> 子 schema 等不会完全进入常规属性解析路径的片段，
    ///     避免生成器对同一份 schema 比运行时和工具链更宽松。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树的 format 元数据是否有效。</returns>
    private static bool TryValidateStringFormatMetadataRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, schemaType) =>
            {
                if (string.IsNullOrWhiteSpace(schemaType))
                {
                    return (true, (Diagnostic?)null);
                }

                return TryValidateStringFormatMetadata(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    schemaType,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     递归拒绝当前共享子集尚未支持的组合关键字。
    ///     这里显式拦截 <c>oneOf</c> / <c>anyOf</c>，避免生成器静默接受会改变生成类型形状的 schema。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树是否未声明不支持的组合关键字。</returns>
    private static bool TryValidateUnsupportedCombinatorKeywordsRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, _) =>
            {
                return TryValidateUnsupportedCombinatorKeywords(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     递归拒绝当前共享子集尚未支持的开放对象关键字形状。
    ///     当前对象字段集默认是闭合的，因此这里只接受显式重复该语义的
    ///     <c>additionalProperties: false</c>。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树是否未声明不支持的开放对象关键字形状。</returns>
    private static bool TryValidateUnsupportedOpenObjectKeywordsRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, _) =>
            {
                return TryValidateUnsupportedOpenObjectKeywords(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     验证当前节点是否声明了会改变生成类型形状的未支持组合关键字。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>未声明不支持关键字时返回 <see langword="true" />。</returns>
    private static bool TryValidateUnsupportedCombinatorKeywords(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (TryGetUnsupportedCombinatorKeywordName(element) is not { } keywordName)
        {
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.UnsupportedCombinatorKeyword,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            keywordName,
            "The current config schema subset does not support combinators that can change generated type shape.");
        return false;
    }

    /// <summary>
    ///     验证当前节点是否声明了当前共享子集尚未支持的开放对象关键字形状。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>未声明不支持关键字时返回 <see langword="true" />。</returns>
    private static bool TryValidateUnsupportedOpenObjectKeywords(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("additionalProperties", out var additionalPropertiesElement))
        {
            return true;
        }

        if (additionalPropertiesElement.ValueKind == JsonValueKind.False)
        {
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.UnsupportedOpenObjectKeyword,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            "additionalProperties",
            "The current config schema subset only accepts 'additionalProperties: false' so object fields remain closed and strongly typed.");
        return false;
    }

    /// <summary>
    ///     返回当前节点声明的首个未支持组合关键字。
    /// </summary>
    /// <param name="element">当前 schema 节点。</param>
    /// <returns>命中的关键字名称；未声明时返回空。</returns>
    private static string? TryGetUnsupportedCombinatorKeywordName(JsonElement element)
    {
        return element.TryGetProperty("oneOf", out _) ? "oneOf" :
            element.TryGetProperty("anyOf", out _) ? "anyOf" :
            null;
    }

    /// <summary>
    ///     以统一顺序递归遍历 schema 树，并把每个节点交给调用方提供的校验逻辑。
    ///     该遍历覆盖对象属性、<c>dependentSchemas</c> / <c>allOf</c> /
    ///     <c>if</c> / <c>then</c> / <c>else</c> / <c>not</c> 子 schema、
    ///     数组 <c>items</c> 与 <c>contains</c>，
    ///     避免不同关键字验证器在同一棵 schema 树上各自维护一份容易漂移的递归流程。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="nodeValidator">当前节点的关键字校验回调。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树是否通过指定关键字的校验。</returns>
    private static bool TryTraverseSchemaRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        var schemaType = string.Empty;
        if (element.TryGetProperty("type", out var typeElement) &&
            typeElement.ValueKind == JsonValueKind.String)
        {
            schemaType = typeElement.GetString() ?? string.Empty;
        }

        var nodeValidationResult = nodeValidator(filePath, displayPath, element, schemaType);
        if (!nodeValidationResult.IsValid)
        {
            diagnostic = nodeValidationResult.Diagnostic;
            return false;
        }

        if (IsSchemaType(schemaType, "object") &&
            !TryTraverseObjectChildren(filePath, displayPath, element, nodeValidator, out diagnostic))
        {
            return false;
        }

        if (!TryTraverseSingleSchemaProperty(filePath, $"{displayPath}[not]", element, "not", nodeValidator, out diagnostic))
        {
            return false;
        }

        return !IsSchemaType(schemaType, "array") ||
            TryTraverseArrayChildren(filePath, displayPath, element, nodeValidator, out diagnostic);
    }

    /// <summary>
    ///     Traverses every object-only child schema keyword in the same order as the parser observes properties.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="displayPath">Logical object path.</param>
    /// <param name="element">Object schema node.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">First child validation diagnostic.</param>
    /// <returns><see langword="true" /> when all object child schemas pass validation.</returns>
    private static bool TryTraverseObjectChildren(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        if (!TryTraverseObjectProperties(filePath, displayPath, element, nodeValidator, out diagnostic))
        {
            return false;
        }

        if (!TryTraverseDependentSchemas(filePath, displayPath, element, nodeValidator, out diagnostic))
        {
            return false;
        }

        return TryTraverseAllOfEntries(filePath, displayPath, element, nodeValidator, out diagnostic) &&
            TryTraverseObjectSchemaBranches(filePath, displayPath, element, nodeValidator, out diagnostic);
    }

    /// <summary>
    ///     Traverses nested object properties in declaration order.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="displayPath">Logical object path.</param>
    /// <param name="element">Object schema node.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">First property validation diagnostic.</param>
    /// <returns><see langword="true" /> when every property subtree passes validation.</returns>
    private static bool TryTraverseObjectProperties(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("properties", out var propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        foreach (var property in propertiesElement.EnumerateObject())
        {
            if (!TryTraverseSchemaRecursively(
                    filePath,
                    CombinePath(displayPath, property.Name),
                    property.Value,
                    nodeValidator,
                    out diagnostic))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Traverses object <c>dependentSchemas</c> entries that are themselves schema objects.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="displayPath">Logical object path.</param>
    /// <param name="element">Object schema node.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">First dependent schema validation diagnostic.</param>
    /// <returns><see langword="true" /> when every dependent schema subtree passes validation.</returns>
    private static bool TryTraverseDependentSchemas(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("dependentSchemas", out var dependentSchemasElement) ||
            dependentSchemasElement.ValueKind != JsonValueKind.Object)
        {
            return true;
        }

        foreach (var dependentSchema in dependentSchemasElement.EnumerateObject())
        {
            if (dependentSchema.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryTraverseSchemaRecursively(
                    filePath,
                    $"{displayPath}[dependentSchemas:{dependentSchema.Name}]",
                    dependentSchema.Value,
                    nodeValidator,
                    out diagnostic))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Traverses object <c>allOf</c> entries while preserving their numeric path segments.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="displayPath">Logical object path.</param>
    /// <param name="element">Object schema node.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">First allOf validation diagnostic.</param>
    /// <returns><see langword="true" /> when every object allOf entry passes validation.</returns>
    private static bool TryTraverseAllOfEntries(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("allOf", out var allOfElement) ||
            allOfElement.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        var allOfIndex = 0;
        foreach (var allOfSchema in allOfElement.EnumerateArray())
        {
            if (allOfSchema.ValueKind != JsonValueKind.Object)
            {
                allOfIndex++;
                continue;
            }

            if (!TryTraverseSchemaRecursively(
                    filePath,
                    BuildAllOfEntryPath(displayPath, allOfIndex),
                    allOfSchema,
                    nodeValidator,
                    out diagnostic))
            {
                return false;
            }

            allOfIndex++;
        }

        return true;
    }

    /// <summary>
    ///     Traverses the object-focused conditional schema branches.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="displayPath">Logical object path.</param>
    /// <param name="element">Object schema node.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">First branch validation diagnostic.</param>
    /// <returns><see langword="true" /> when all present conditional branches pass validation.</returns>
    private static bool TryTraverseObjectSchemaBranches(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSingleSchemaProperty(
                filePath,
                BuildConditionalSchemaPath(displayPath, "if"),
                element,
                "if",
                nodeValidator,
                out diagnostic) &&
            TryTraverseSingleSchemaProperty(
                filePath,
                BuildConditionalSchemaPath(displayPath, "then"),
                element,
                "then",
                nodeValidator,
                out diagnostic) &&
            TryTraverseSingleSchemaProperty(
                filePath,
                BuildConditionalSchemaPath(displayPath, "else"),
                element,
                "else",
                nodeValidator,
                out diagnostic);
    }

    /// <summary>
    ///     Traverses a single child schema property when it is present and object-shaped.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="childDisplayPath">Logical path assigned to the child schema.</param>
    /// <param name="element">Parent schema node.</param>
    /// <param name="propertyName">Child schema keyword.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">Child validation diagnostic.</param>
    /// <returns><see langword="true" /> when the child is absent or passes validation.</returns>
    private static bool TryTraverseSingleSchemaProperty(
        string filePath,
        string childDisplayPath,
        JsonElement element,
        string propertyName,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        return !element.TryGetProperty(propertyName, out var childElement) ||
            childElement.ValueKind != JsonValueKind.Object ||
            TryTraverseSchemaRecursively(filePath, childDisplayPath, childElement, nodeValidator, out diagnostic);
    }

    /// <summary>
    ///     Traverses array <c>items</c> and <c>contains</c> child schemas.
    /// </summary>
    /// <param name="filePath">Schema file path.</param>
    /// <param name="displayPath">Logical array path.</param>
    /// <param name="element">Array schema node.</param>
    /// <param name="nodeValidator">Current validation callback.</param>
    /// <param name="diagnostic">First array child validation diagnostic.</param>
    /// <returns><see langword="true" /> when all array child schemas pass validation.</returns>
    private static bool TryTraverseArrayChildren(
        string filePath,
        string displayPath,
        JsonElement element,
        Func<string, string, JsonElement, string, (bool IsValid, Diagnostic? Diagnostic)> nodeValidator,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSingleSchemaProperty(filePath, $"{displayPath}[]", element, "items", nodeValidator, out diagnostic) &&
            TryTraverseSingleSchemaProperty(
                filePath,
                $"{displayPath}[contains]",
                element,
                "contains",
                nodeValidator,
                out diagnostic);
    }

    /// <summary>
    ///     为对象级 <c>allOf</c> 条目生成与运行时一致的逻辑路径。
    /// </summary>
    /// <param name="displayPath">父对象路径。</param>
    /// <param name="allOfIndex">从 0 开始的条目索引。</param>
    /// <returns>格式化后的 allOf 条目路径。</returns>
    private static string BuildAllOfEntryPath(string displayPath, int allOfIndex)
    {
        return $"{displayPath}[allOf[{allOfIndex}]]";
    }

    /// <summary>
    ///     为 object-focused 条件分支生成与运行时一致的逻辑路径。
    /// </summary>
    /// <param name="displayPath">父对象路径。</param>
    /// <param name="keywordName">条件关键字名称。</param>
    /// <returns>格式化后的条件分支路径。</returns>
    private static string BuildConditionalSchemaPath(string displayPath, string keywordName)
    {
        return $"{displayPath}[{keywordName}]";
    }

    /// <summary>
    ///     递归验证 schema 树中的对象级 <c>dependentRequired</c> 元数据。
    ///     该遍历会覆盖根节点、<c>dependentSchemas</c> / <c>not</c> 子 schema、
    ///     数组元素与 <c>contains</c> 子 schema，
    ///     避免生成器在对象字段依赖规则上比运行时和工具侧更宽松。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树的 dependentRequired 元数据是否有效。</returns>
    private static bool TryValidateDependentRequiredMetadataRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, schemaType) =>
            {
                if (!string.Equals(schemaType, "object", StringComparison.Ordinal))
                {
                    return (true, (Diagnostic?)null);
                }

                return TryValidateDependentRequiredMetadata(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     验证单个对象 schema 节点上的 <c>dependentRequired</c> 元数据。
    ///     生成器只接受“当前对象已声明字段之间”的依赖关系，避免强类型文档描述出运行时无法解析的无效键名。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前对象 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前对象上的 dependentRequired 元数据是否有效。</returns>
    private static bool TryValidateDependentRequiredMetadata(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("dependentRequired", out var dependentRequiredElement))
        {
            return true;
        }

        if (dependentRequiredElement.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "The 'dependentRequired' value must be an object.");
            return false;
        }

        if (!TryGetDeclaredProperties(
                filePath,
                displayPath,
                element,
                ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
                "dependentRequired",
                out var declaredProperties,
                out diagnostic))
        {
            return false;
        }

        foreach (var dependency in dependentRequiredElement.EnumerateObject())
        {
            if (!TryValidateDependentRequiredEntry(
                    filePath,
                    displayPath,
                    dependency,
                    declaredProperties,
                    out diagnostic))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     验证单个 <c>dependentRequired</c> 触发项的声明形状。
    ///     该 helper 先锁定 trigger 字段本身是否属于当前对象，再把每个 target 交给更细粒度的 sibling 校验，
    ///     让诊断能够明确区分“触发字段不存在”和“依赖目标非法”两类失败语义。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="dependency">当前 dependentRequired 触发项。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 dependentRequired 触发项是否有效。</returns>
    private static bool TryValidateDependentRequiredEntry(
        string filePath,
        string displayPath,
        JsonProperty dependency,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!declaredProperties.Contains(dependency.Name))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Trigger property '{dependency.Name}' is not declared in the same object schema.");
            return false;
        }

        if (dependency.Value.ValueKind != JsonValueKind.Array)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Property '{dependency.Name}' must declare 'dependentRequired' as an array of sibling property names.");
            return false;
        }

        foreach (var dependencyTarget in dependency.Value.EnumerateArray())
        {
            if (!TryValidateDependentRequiredTarget(
                    filePath,
                    displayPath,
                    dependency.Name,
                    dependencyTarget,
                    declaredProperties,
                    out diagnostic))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     验证单个 <c>dependentRequired</c> target 是否为已声明的 sibling 字段名。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="dependencyName">触发依赖的字段名。</param>
    /// <param name="dependencyTarget">当前 target 元素。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 dependentRequired target 是否有效。</returns>
    private static bool TryValidateDependentRequiredTarget(
        string filePath,
        string displayPath,
        string dependencyName,
        JsonElement dependencyTarget,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (dependencyTarget.ValueKind != JsonValueKind.String)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Property '{dependencyName}' must declare 'dependentRequired' entries as strings.");
            return false;
        }

        var dependencyTargetName = dependencyTarget.GetString();
        if (string.IsNullOrWhiteSpace(dependencyTargetName))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Property '{dependencyName}' cannot declare blank 'dependentRequired' entries.");
            return false;
        }

        var normalizedDependencyTargetName = dependencyTargetName!;
        if (declaredProperties.Contains(normalizedDependencyTargetName))
        {
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.InvalidDependentRequiredMetadata,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            $"Dependent target '{normalizedDependencyTargetName}' is not declared in the same object schema.");
        return false;
    }

    /// <summary>
    ///     验证当前 schema 节点是否以运行时支持的方式声明了 <c>dependentSchemas</c>。
    ///     只有 object 节点允许挂载该关键字；一旦关键字出现，就继续复用对象节点的形状校验，
    ///     保证发布到 XML 文档和运行时的约束解释范围保持一致。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="schemaType">当前节点声明的 schema 类型。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点上的 dependentSchemas 声明是否有效。</returns>
    private static bool TryValidateDependentSchemasDeclaration(
        string filePath,
        string displayPath,
        JsonElement element,
        string? schemaType,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("dependentSchemas", out _))
        {
            return true;
        }

        if (!string.Equals(schemaType, "object", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentSchemasMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "Only object schemas can declare 'dependentSchemas'.");
            return false;
        }

        return TryValidateDependentSchemasMetadata(filePath, displayPath, element, out diagnostic);
    }

    /// <summary>
    ///     递归验证 schema 树中的对象级 <c>dependentSchemas</c> 元数据。
    ///     该遍历会覆盖根节点、<c>not</c>、数组元素、<c>contains</c> 与嵌套 <c>dependentSchemas</c>，
    ///     确保生成器对条件对象子 schema 的接受范围不会比运行时更宽松。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树的 dependentSchemas 元数据是否有效。</returns>
    private static bool TryValidateDependentSchemasMetadataRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, schemaType) =>
            {
                return TryValidateDependentSchemasDeclaration(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    schemaType,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     验证单个对象 schema 节点上的 <c>dependentSchemas</c> 元数据。
    ///     生成器当前只接受“已声明 sibling 字段触发 object 子 schema”的形状，
    ///     避免 XML 文档描述出运行时无法识别的条件 schema。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前对象 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前对象上的 dependentSchemas 元数据是否有效。</returns>
    private static bool TryValidateDependentSchemasMetadata(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("dependentSchemas", out var dependentSchemasElement))
        {
            return true;
        }

        if (dependentSchemasElement.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentSchemasMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "The 'dependentSchemas' value must be an object.");
            return false;
        }

        if (!TryGetDeclaredProperties(
                filePath,
                displayPath,
                element,
                ConfigSchemaDiagnostics.InvalidDependentSchemasMetadata,
                "dependentSchemas",
                out var declaredProperties,
                out diagnostic))
        {
            return false;
        }

        foreach (var dependency in dependentSchemasElement.EnumerateObject())
        {
            if (!TryValidateDependentSchemaEntry(
                    filePath,
                    displayPath,
                    dependency,
                    declaredProperties,
                    out diagnostic))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     验证单个 <c>dependentSchemas</c> 触发项是否保持为当前运行时支持的 object 子 schema 形状。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="dependency">当前 dependentSchemas 触发项。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 dependentSchemas 触发项是否有效。</returns>
    private static bool TryValidateDependentSchemaEntry(
        string filePath,
        string displayPath,
        JsonProperty dependency,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!declaredProperties.Contains(dependency.Name))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentSchemasMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Trigger property '{dependency.Name}' is not declared in the same object schema.");
            return false;
        }

        if (dependency.Value.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentSchemasMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Property '{dependency.Name}' must declare 'dependentSchemas' as an object-valued schema.");
            return false;
        }

        if (!dependency.Value.TryGetProperty("type", out var dependentSchemaTypeElement) ||
            dependentSchemaTypeElement.ValueKind != JsonValueKind.String ||
            !string.Equals(dependentSchemaTypeElement.GetString(), "object", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidDependentSchemasMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Property '{dependency.Name}' must declare an object-typed 'dependentSchemas' schema.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证当前 schema 节点是否以运行时支持的方式声明了 <c>allOf</c>。
    ///     当前共享子集只接受 object 节点上的 object-typed inline schema 数组，
    ///     以便把它们解释成 focused constraint block，而不引入额外的类型合并语义。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="schemaType">当前节点声明的 schema 类型。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点上的 allOf 声明是否有效。</returns>
    private static bool TryValidateAllOfDeclaration(
        string filePath,
        string displayPath,
        JsonElement element,
        string? schemaType,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("allOf", out _))
        {
            return true;
        }

        if (!string.Equals(schemaType, "object", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "Only object schemas can declare 'allOf'.");
            return false;
        }

        return TryValidateAllOfMetadata(filePath, displayPath, element, out diagnostic);
    }

    /// <summary>
    ///     递归验证 schema 树中的对象级 <c>allOf</c> 元数据。
    ///     该遍历会覆盖根节点、<c>not</c>、数组元素、<c>contains</c>、<c>dependentSchemas</c>
    ///     与嵌套 <c>allOf</c>，确保生成器对组合约束的接受范围与运行时保持一致。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树的 allOf 元数据是否有效。</returns>
    private static bool TryValidateAllOfMetadataRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, schemaType) =>
            {
                return TryValidateAllOfDeclaration(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    schemaType,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     验证单个对象 schema 节点上的 <c>allOf</c> 元数据。
    ///     生成器当前只接受 object-typed inline schema 数组，
    ///     避免 XML 文档描述出运行时不会按 focused constraint block 解释的组合形状。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前对象 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前对象上的 allOf 元数据是否有效。</returns>
    private static bool TryValidateAllOfMetadata(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!element.TryGetProperty("allOf", out var allOfElement))
        {
            return true;
        }

        if (allOfElement.ValueKind != JsonValueKind.Array)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "The 'allOf' value must be an array.");
            return false;
        }

        if (!TryGetDeclaredProperties(
                filePath,
                displayPath,
                element,
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                "allOf",
                out var declaredProperties,
                out diagnostic))
        {
            return false;
        }

        var allOfIndex = 0;
        foreach (var allOfSchema in allOfElement.EnumerateArray())
        {
            if (!TryValidateAllOfEntryShape(
                    filePath,
                    displayPath,
                    allOfSchema,
                    allOfIndex,
                    out diagnostic))
            {
                return false;
            }

            if (!TryValidateAllOfEntryTargets(
                    filePath,
                    displayPath,
                    allOfSchema,
                    allOfIndex,
                    declaredProperties,
                    out diagnostic))
            {
                return false;
            }

            allOfIndex++;
        }

        return true;
    }

    /// <summary>
    ///     验证单个 <c>allOf</c> 条目是否维持 object-valued、object-typed 的 focused constraint 形状。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="allOfSchema">当前 allOf 条目。</param>
    /// <param name="allOfIndex">从 0 开始的条目索引。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 allOf 条目形状是否有效。</returns>
    private static bool TryValidateAllOfEntryShape(
        string filePath,
        string displayPath,
        JsonElement allOfSchema,
        int allOfIndex,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (allOfSchema.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Entry #{allOfIndex + 1} in 'allOf' must be an object-valued schema.");
            return false;
        }

        if (!allOfSchema.TryGetProperty("type", out var allOfTypeElement) ||
            allOfTypeElement.ValueKind != JsonValueKind.String ||
            !string.Equals(allOfTypeElement.GetString(), "object", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Entry #{allOfIndex + 1} in 'allOf' must declare an object-typed schema.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证当前 schema 节点是否以运行时支持的方式声明了 object-focused <c>if</c> / <c>then</c> / <c>else</c>。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="schemaType">当前节点声明的 schema 类型。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点上的条件元数据是否有效。</returns>
    private static bool TryValidateConditionalSchemasDeclaration(
        string filePath,
        string displayPath,
        JsonElement element,
        string? schemaType,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        var hasIf = element.TryGetProperty("if", out _);
        var hasThen = element.TryGetProperty("then", out _);
        var hasElse = element.TryGetProperty("else", out _);
        if (!hasIf && !hasThen && !hasElse)
        {
            return true;
        }

        if (!string.Equals(schemaType, "object", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "Only object schemas can declare 'if', 'then', or 'else'.");
            return false;
        }

        return TryValidateConditionalSchemasMetadata(filePath, displayPath, element, out diagnostic);
    }

    /// <summary>
    ///     递归验证 schema 树中的 object-focused <c>if</c> / <c>then</c> / <c>else</c> 元数据。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前节点树的条件元数据是否有效。</returns>
    private static bool TryValidateConditionalSchemasMetadataRecursively(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        return TryTraverseSchemaRecursively(
            filePath,
            displayPath,
            element,
            static (currentFilePath, currentDisplayPath, currentElement, schemaType) =>
            {
                return TryValidateConditionalSchemasDeclaration(
                    currentFilePath,
                    currentDisplayPath,
                    currentElement,
                    schemaType,
                    out var currentDiagnostic)
                    ? (true, (Diagnostic?)null)
                    : (false, currentDiagnostic);
            },
            out diagnostic);
    }

    /// <summary>
    ///     验证单个对象 schema 节点上的 object-focused 条件元数据。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">逻辑字段路径。</param>
    /// <param name="element">当前对象 schema 节点。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前对象上的条件元数据是否有效。</returns>
    private static bool TryValidateConditionalSchemasMetadata(
        string filePath,
        string displayPath,
        JsonElement element,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        var hasIf = element.TryGetProperty("if", out var ifElement);
        var hasThen = element.TryGetProperty("then", out var thenElement);
        var hasElse = element.TryGetProperty("else", out var elseElement);
        if (!TryValidateConditionalSchemaPresence(
                filePath,
                displayPath,
                hasIf,
                hasThen,
                hasElse,
                out diagnostic))
        {
            return false;
        }

        if (!hasIf)
        {
            return true;
        }

        if (!TryGetDeclaredProperties(
                filePath,
                displayPath,
                element,
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                "if/then/else",
                out var declaredProperties,
                out diagnostic))
        {
            return false;
        }

        return TryValidateConditionalSchemaBranches(
            filePath,
            displayPath,
            ifElement,
            hasThen,
            thenElement,
            hasElse,
            elseElement,
            declaredProperties,
            out diagnostic);
    }

    /// <summary>
    ///     验证 object-focused 条件分支集合。
    ///     <c>if</c> 分支始终必检，<c>then</c> / <c>else</c> 仅在声明时校验，
    ///     以保持生成器对分支缺失与分支内容错误的诊断顺序稳定。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="ifElement">if 分支 schema。</param>
    /// <param name="hasThen">是否声明 then。</param>
    /// <param name="thenElement">then 分支 schema。</param>
    /// <param name="hasElse">是否声明 else。</param>
    /// <param name="elseElement">else 分支 schema。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前条件分支集合是否有效。</returns>
    private static bool TryValidateConditionalSchemaBranches(
        string filePath,
        string displayPath,
        JsonElement ifElement,
        bool hasThen,
        JsonElement thenElement,
        bool hasElse,
        JsonElement elseElement,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        if (!TryValidateConditionalSchemaBranch(
                filePath,
                displayPath,
                ifElement,
                "if",
                declaredProperties,
                out diagnostic))
        {
            return false;
        }

        if (hasThen &&
            !TryValidateConditionalSchemaBranch(
                filePath,
                displayPath,
                thenElement,
                "then",
                declaredProperties,
                out diagnostic))
        {
            return false;
        }

        return !hasElse ||
               TryValidateConditionalSchemaBranch(
                   filePath,
                   displayPath,
                   elseElement,
                   "else",
                   declaredProperties,
                   out diagnostic);
    }

    /// <summary>
    ///     验证 object-focused <c>if</c> / <c>then</c> / <c>else</c> 的存在性组合是否合法。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="hasIf">是否声明 if。</param>
    /// <param name="hasThen">是否声明 then。</param>
    /// <param name="hasElse">是否声明 else。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前条件关键字组合是否有效。</returns>
    private static bool TryValidateConditionalSchemaPresence(
        string filePath,
        string displayPath,
        bool hasIf,
        bool hasThen,
        bool hasElse,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!hasIf && !hasThen && !hasElse)
        {
            return true;
        }

        if (!hasIf)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                "Object schemas using 'then' or 'else' must also declare 'if'.");
            return false;
        }

        if (hasThen || hasElse)
        {
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            "Object schemas using 'if' must also declare at least one of 'then' or 'else'.");
        return false;
    }

    private static bool TryGetDeclaredProperties(
        string filePath,
        string displayPath,
        JsonElement element,
        DiagnosticDescriptor descriptor,
        string keywordName,
        out HashSet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        declaredProperties = new HashSet<string>(StringComparer.Ordinal);
        if (!element.TryGetProperty("properties", out var propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                descriptor,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"Object schemas using '{keywordName}' must also declare an object-valued 'properties' map.");
            return false;
        }

        declaredProperties = new HashSet<string>(
            propertiesElement
                .EnumerateObject()
                .Select(static property => property.Name),
            StringComparer.Ordinal);
        return true;
    }

    /// <summary>
    ///     验证单个 object-focused 条件分支的类型与父对象字段引用范围。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="schemaElement">当前条件分支 schema。</param>
    /// <param name="keywordName">条件关键字名称。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前条件分支是否有效。</returns>
    private static bool TryValidateConditionalSchemaBranch(
        string filePath,
        string displayPath,
        JsonElement schemaElement,
        string keywordName,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        var branchPath = BuildConditionalSchemaPath(displayPath, keywordName);
        if (schemaElement.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                branchPath,
                $"The '{keywordName}' value must be an object-valued schema.");
            return false;
        }

        if (!schemaElement.TryGetProperty("type", out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.String ||
            !string.Equals(typeElement.GetString(), "object", StringComparison.Ordinal))
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                branchPath,
                $"The '{keywordName}' schema must declare an object-typed schema.");
            return false;
        }

        return TryValidateObjectFocusedSchemaTargets(
            filePath,
            branchPath,
            keywordName,
            schemaElement,
            declaredProperties,
            out diagnostic);
    }

    /// <summary>
    ///     验证 object-focused 内联 schema 只引用父对象已声明的同级字段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">当前内联 schema 路径。</param>
    /// <param name="entryLabel">用于诊断文本的条目标签。</param>
    /// <param name="schemaElement">当前内联 schema。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前内联 schema 是否有效。</returns>
    private static bool TryValidateObjectFocusedSchemaTargets(
        string filePath,
        string displayPath,
        string entryLabel,
        JsonElement schemaElement,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!TryValidateObjectFocusedSchemaProperties(
                filePath,
                displayPath,
                entryLabel,
                schemaElement,
                declaredProperties,
                out diagnostic))
        {
            return false;
        }

        return TryValidateObjectFocusedSchemaRequiredProperties(
            filePath,
            displayPath,
            entryLabel,
            schemaElement,
            declaredProperties,
            out diagnostic);
    }

    /// <summary>
    ///     验证 object-focused 条件 schema 的 <c>properties</c> 只引用父对象已声明字段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">当前分支逻辑路径。</param>
    /// <param name="entryLabel">分支标签。</param>
    /// <param name="schemaElement">当前分支 schema。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前分支 properties 是否有效。</returns>
    private static bool TryValidateObjectFocusedSchemaProperties(
        string filePath,
        string displayPath,
        string entryLabel,
        JsonElement schemaElement,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!schemaElement.TryGetProperty("properties", out var propertiesElement))
        {
            return true;
        }

        if (propertiesElement.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"The '{entryLabel}' schema must declare 'properties' as an object-valued map.");
            return false;
        }

        foreach (var property in propertiesElement.EnumerateObject())
        {
            if (declaredProperties.Contains(property.Name))
            {
                continue;
            }

            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"The '{entryLabel}' schema declares property '{property.Name}', but that property is not declared in the parent object schema.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证 object-focused 条件 schema 的 <c>required</c> 约束只引用父对象已声明字段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">当前分支逻辑路径。</param>
    /// <param name="entryLabel">分支标签。</param>
    /// <param name="schemaElement">当前分支 schema。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前分支 required 是否有效。</returns>
    private static bool TryValidateObjectFocusedSchemaRequiredProperties(
        string filePath,
        string displayPath,
        string entryLabel,
        JsonElement schemaElement,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!schemaElement.TryGetProperty("required", out var requiredElement))
        {
            return true;
        }

        if (requiredElement.ValueKind != JsonValueKind.Array)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"The '{entryLabel}' schema must declare 'required' as an array of parent property names.");
            return false;
        }

        foreach (var requiredProperty in requiredElement.EnumerateArray())
        {
            if (requiredProperty.ValueKind != JsonValueKind.String)
            {
                diagnostic = Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    displayPath,
                    $"The '{entryLabel}' schema must declare 'required' entries as parent property-name strings.");
                return false;
            }

            var requiredPropertyName = requiredProperty.GetString();
            if (string.IsNullOrWhiteSpace(requiredPropertyName))
            {
                diagnostic = Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    displayPath,
                    $"The '{entryLabel}' schema cannot declare blank property names in 'required'.");
                return false;
            }

            if (declaredProperties.Contains(requiredPropertyName!))
            {
                continue;
            }

            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidConditionalSchemaMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                displayPath,
                $"The '{entryLabel}' schema requires property '{requiredPropertyName}', but that property is not declared in the parent object schema.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证单个 <c>allOf</c> 条目只约束父对象已声明的同级字段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">父对象逻辑路径。</param>
    /// <param name="allOfSchema">当前 allOf 条目。</param>
    /// <param name="allOfIndex">从 0 开始的条目索引。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 allOf 条目是否只引用父对象已声明字段。</returns>
    private static bool TryValidateAllOfEntryTargets(
        string filePath,
        string displayPath,
        JsonElement allOfSchema,
        int allOfIndex,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        var allOfEntryPath = BuildAllOfEntryPath(displayPath, allOfIndex);
        if (!TryValidateAllOfEntryProperties(
                filePath,
                allOfEntryPath,
                allOfSchema,
                allOfIndex,
                declaredProperties,
                out diagnostic))
        {
            return false;
        }

        return TryValidateAllOfEntryRequiredProperties(
            filePath,
            allOfEntryPath,
            allOfSchema,
            allOfIndex,
            declaredProperties,
            out diagnostic);
    }

    /// <summary>
    ///     验证单个 <c>allOf</c> 条目的 <c>properties</c> 映射不会引入父对象未声明字段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="allOfEntryPath">当前 allOf 条目逻辑路径。</param>
    /// <param name="allOfSchema">当前 allOf 条目。</param>
    /// <param name="allOfIndex">从 0 开始的条目索引。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 allOf 条目的 properties 映射是否有效。</returns>
    private static bool TryValidateAllOfEntryProperties(
        string filePath,
        string allOfEntryPath,
        JsonElement allOfSchema,
        int allOfIndex,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!allOfSchema.TryGetProperty("properties", out var allOfPropertiesElement))
        {
            return true;
        }

        if (allOfPropertiesElement.ValueKind != JsonValueKind.Object)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                allOfEntryPath,
                $"Entry #{allOfIndex + 1} in 'allOf' must declare 'properties' as an object-valued map.");
            return false;
        }

        foreach (var property in allOfPropertiesElement.EnumerateObject())
        {
            if (declaredProperties.Contains(property.Name))
            {
                continue;
            }

            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                allOfEntryPath,
                $"Entry #{allOfIndex + 1} in 'allOf' declares property '{property.Name}', but that property is not declared in the parent object schema.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证单个 <c>allOf</c> 条目的 <c>required</c> 约束不会引用父对象未声明字段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="allOfEntryPath">当前 allOf 条目逻辑路径。</param>
    /// <param name="allOfSchema">当前 allOf 条目。</param>
    /// <param name="allOfIndex">从 0 开始的条目索引。</param>
    /// <param name="declaredProperties">父对象已声明属性集合。</param>
    /// <param name="diagnostic">失败时返回的诊断。</param>
    /// <returns>当前 allOf 条目的 required 约束是否有效。</returns>
    private static bool TryValidateAllOfEntryRequiredProperties(
        string filePath,
        string allOfEntryPath,
        JsonElement allOfSchema,
        int allOfIndex,
        ISet<string> declaredProperties,
        out Diagnostic? diagnostic)
    {
        diagnostic = null;
        if (!allOfSchema.TryGetProperty("required", out var requiredElement))
        {
            return true;
        }

        if (requiredElement.ValueKind != JsonValueKind.Array)
        {
            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                allOfEntryPath,
                $"Entry #{allOfIndex + 1} in 'allOf' must declare 'required' as an array of parent property names.");
            return false;
        }

        foreach (var requiredProperty in requiredElement.EnumerateArray())
        {
            if (requiredProperty.ValueKind != JsonValueKind.String)
            {
                diagnostic = Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    allOfEntryPath,
                    $"Entry #{allOfIndex + 1} in 'allOf' must declare 'required' entries as parent property-name strings.");
                return false;
            }

            var requiredPropertyName = requiredProperty.GetString();
            if (string.IsNullOrWhiteSpace(requiredPropertyName))
            {
                diagnostic = Diagnostic.Create(
                    ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                    CreateFileLocation(filePath),
                    Path.GetFileName(filePath),
                    allOfEntryPath,
                    $"Entry #{allOfIndex + 1} in 'allOf' cannot declare blank property names in 'required'.");
                return false;
            }

            var normalizedRequiredPropertyName = requiredPropertyName!;
            if (declaredProperties.Contains(normalizedRequiredPropertyName))
            {
                continue;
            }

            diagnostic = Diagnostic.Create(
                ConfigSchemaDiagnostics.InvalidAllOfMetadata,
                CreateFileLocation(filePath),
                Path.GetFileName(filePath),
                allOfEntryPath,
                $"Entry #{allOfIndex + 1} in 'allOf' requires property '{normalizedRequiredPropertyName}', but that property is not declared in the parent object schema.");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     判断给定 format 名称是否属于当前共享支持子集。
    /// </summary>
    /// <param name="formatName">schema 中声明的 format 名称。</param>
    /// <returns>是否支持该格式。</returns>
    private static bool IsSupportedStringFormat(string formatName)
    {
        return formatName switch
        {
            "date" => true,
            "date-time" => true,
            "duration" => true,
            "email" => true,
            "time" => true,
            "uri" => true,
            "uuid" => true,
            _ => false
        };
    }

    /// <summary>
    ///     使用 schema 关键字的大小写敏感语义比较类型名称。
    /// </summary>
    /// <param name="schemaType">从 JSON schema 读取或推导出的类型名称。</param>
    /// <param name="expectedType">期望匹配的 schema 类型关键字。</param>
    /// <returns>当两个类型名称按 schema 关键字语义完全一致时返回 <see langword="true" />。</returns>
    private static bool IsSchemaType(string schemaType, string expectedType)
    {
        return string.Equals(schemaType, expectedType, StringComparison.Ordinal);
    }

    /// <summary>
    ///     判断类型是否支持 JSON schema 数值范围和倍数约束。
    /// </summary>
    /// <param name="schemaType">从 JSON schema 读取或推导出的类型名称。</param>
    /// <returns>当类型为 <c>integer</c> 或 <c>number</c> 时返回 <see langword="true" />。</returns>
    private static bool IsNumericSchemaType(string schemaType)
    {
        return IsSchemaType(schemaType, "integer") || IsSchemaType(schemaType, "number");
    }

    /// <summary>
    ///     解析数组属性，支持标量数组与对象数组。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="property">属性 JSON 节点。</param>
    /// <param name="context">属性解析共享上下文。</param>
    /// <returns>解析后的属性信息或诊断。</returns>
    private static ParsedPropertyResult ParseArrayProperty(
        string filePath,
        JsonProperty property,
        PropertyParseContext context)
    {
        if (context.IsIndexedLookup)
        {
            return ParsedPropertyResult.FromDiagnostic(
                CreateInvalidLookupIndexDiagnostic(filePath, context.DisplayPath, LookupIndexTopLevelScalarOnlyMessage));
        }

        if (!TryGetArrayItemSchema(filePath, property, context.DisplayPath, out var itemsElement, out var itemType, out var diagnostic))
        {
            return ParsedPropertyResult.FromDiagnostic(diagnostic!);
        }

        if (!TryValidateStringFormatMetadata(filePath, $"{context.DisplayPath}[]", itemsElement, itemType,
                out var formatDiagnostic))
        {
            return ParsedPropertyResult.FromDiagnostic(formatDiagnostic!);
        }

        return itemType switch
        {
            "integer" or "number" or "boolean" or "string" =>
                CreateScalarArrayPropertyResult(property, context, itemsElement, itemType),
            "object" => CreateObjectArrayPropertyResult(filePath, property, context, itemsElement),
            _ => ParsedPropertyResult.FromDiagnostic(CreateUnsupportedArrayItemDiagnostic(
                filePath,
                context.DisplayPath,
                $"array<{itemType}>")),
        };
    }

    /// <summary>
    ///     Reads the <c>items.type</c> declaration required by the supported array schema subset.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="property">Array property JSON node.</param>
    /// <param name="displayPath">Logical schema path.</param>
    /// <param name="itemsElement">Array item schema node when present.</param>
    /// <param name="itemType">Array item schema type when present.</param>
    /// <param name="diagnostic">Diagnostic explaining an invalid declaration.</param>
    /// <returns><see langword="true" /> when the array item schema is supported by the parser.</returns>
    private static bool TryGetArrayItemSchema(
        string filePath,
        JsonProperty property,
        string displayPath,
        out JsonElement itemsElement,
        out string itemType,
        out Diagnostic? diagnostic)
    {
        if (property.Value.TryGetProperty("items", out itemsElement) &&
            itemsElement.ValueKind == JsonValueKind.Object &&
            itemsElement.TryGetProperty("type", out var itemTypeElement) &&
            itemTypeElement.ValueKind == JsonValueKind.String)
        {
            itemType = itemTypeElement.GetString() ?? string.Empty;
            diagnostic = null;
            return true;
        }

        itemType = string.Empty;
        var itemDescription = "array";
        if (itemsElement.ValueKind == JsonValueKind.Object &&
            itemsElement.TryGetProperty("type", out var unsupportedTypeElement) &&
            unsupportedTypeElement.ValueKind == JsonValueKind.String)
        {
            itemDescription = $"array<{unsupportedTypeElement.GetString() ?? string.Empty}>";
        }

        diagnostic = CreateUnsupportedArrayItemDiagnostic(filePath, displayPath, itemDescription);
        return false;
    }

    /// <summary>
    ///     Builds an array property whose items are scalar values.
    /// </summary>
    /// <param name="property">Array property JSON node.</param>
    /// <param name="context">Property parsing context.</param>
    /// <param name="itemsElement">Array item schema node.</param>
    /// <param name="itemType">Array item scalar schema type.</param>
    /// <returns>Parsed array property model.</returns>
    private static ParsedPropertyResult CreateScalarArrayPropertyResult(
        JsonProperty property,
        PropertyParseContext context,
        JsonElement itemsElement,
        string itemType)
    {
        var itemClrType = itemType switch
        {
            "integer" => "int",
            "number" => "double",
            "boolean" => "bool",
            _ => "string"
        };

        return ParsedPropertyResult.FromProperty(new SchemaPropertySpec(
            property.Name,
            context.DisplayPath,
            context.PropertyName,
            context.IsRequired,
            context.Title,
            context.Description,
            false,
            new SchemaTypeSpec(
                SchemaNodeKind.Array,
                "array",
                $"global::System.Collections.Generic.IReadOnlyList<{itemClrType}>",
                TryBuildArrayInitializer(property.Value, itemType, itemClrType) ??
                $" = global::System.Array.Empty<{itemClrType}>();",
                TryBuildEnumDocumentation(property.Value, "array") ??
                TryBuildEnumDocumentation(itemsElement, itemType),
                TryBuildConstraintDocumentation(property.Value, "array"),
                context.RefTableName,
                null,
                new SchemaTypeSpec(
                    SchemaNodeKind.Scalar,
                    itemType,
                    itemClrType,
                    null,
                    TryBuildEnumDocumentation(itemsElement, itemType),
                    TryBuildConstraintDocumentation(itemsElement, itemType),
                    context.RefTableName,
                    null,
                    null))));
    }

    /// <summary>
    ///     Builds an array property whose items are nested object values.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="property">Array property JSON node.</param>
    /// <param name="context">Property parsing context.</param>
    /// <param name="itemsElement">Array item schema node.</param>
    /// <returns>Parsed array property model or an object-array diagnostic.</returns>
    private static ParsedPropertyResult CreateObjectArrayPropertyResult(
        string filePath,
        JsonProperty property,
        PropertyParseContext context,
        JsonElement itemsElement)
    {
        if (!string.IsNullOrWhiteSpace(context.RefTableName))
        {
            return ParsedPropertyResult.FromDiagnostic(
                CreateUnsupportedArrayItemDiagnostic(filePath, context.DisplayPath, "array<object>-ref"));
        }

        var objectResult = ParseObjectSpec(
            filePath,
            itemsElement,
            $"{context.DisplayPath}[]",
            $"{context.PropertyName}ItemConfig");
        if (objectResult.Diagnostic is not null)
        {
            return ParsedPropertyResult.FromDiagnostic(objectResult.Diagnostic);
        }

        return ParsedPropertyResult.FromProperty(CreateObjectArrayPropertySpec(
            property,
            context,
            itemsElement,
            objectResult.Object!));
    }

    /// <summary>
    ///     Creates the nested object array property model after the item object has been parsed.
    /// </summary>
    /// <param name="property">Array property JSON node.</param>
    /// <param name="context">Property parsing context.</param>
    /// <param name="itemsElement">Array item schema node.</param>
    /// <param name="objectSpec">Parsed item object model.</param>
    /// <returns>Completed object array property model.</returns>
    private static SchemaPropertySpec CreateObjectArrayPropertySpec(
        JsonProperty property,
        PropertyParseContext context,
        JsonElement itemsElement,
        SchemaObjectSpec objectSpec)
    {
        return new SchemaPropertySpec(
            property.Name,
            context.DisplayPath,
            context.PropertyName,
            context.IsRequired,
            context.Title,
            context.Description,
            false,
            new SchemaTypeSpec(
                SchemaNodeKind.Array,
                "array",
                $"global::System.Collections.Generic.IReadOnlyList<{objectSpec.ClassName}>",
                $" = global::System.Array.Empty<{objectSpec.ClassName}>();",
                TryBuildEnumDocumentation(property.Value, "array") ??
                TryBuildEnumDocumentation(itemsElement, "object"),
                TryBuildConstraintDocumentation(property.Value, "array"),
                null,
                null,
                new SchemaTypeSpec(
                    SchemaNodeKind.Object,
                    "object",
                    objectSpec.ClassName,
                    null,
                    TryBuildEnumDocumentation(itemsElement, "object"),
                    null,
                    null,
                    objectSpec,
                    null)));
    }

    /// <summary>
    ///     Creates a diagnostic for unsupported or malformed array item declarations.
    /// </summary>
    /// <param name="filePath">Schema file path used for diagnostics.</param>
    /// <param name="displayPath">Logical schema path.</param>
    /// <param name="itemDescription">Unsupported item declaration text.</param>
    /// <returns>Unsupported property type diagnostic.</returns>
    private static Diagnostic CreateUnsupportedArrayItemDiagnostic(
        string filePath,
        string displayPath,
        string itemDescription)
    {
        return Diagnostic.Create(
            ConfigSchemaDiagnostics.UnsupportedPropertyType,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            itemDescription);
    }

    /// <summary>
    ///     生成配置类型源码。
    /// </summary>
    /// <param name="schema">已解析的 schema 模型。</param>
    /// <returns>配置类型源码。</returns>
    private static string GenerateConfigClass(SchemaFileSpec schema)
    {
        var builder = new StringBuilder();
        AppendGeneratedSourceHeader(builder, schema.Namespace);

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
        AppendGeneratedSourceHeader(builder, schema.Namespace);
        AppendGeneratedTableTypeHeader(builder, schema, indexedQueryableProperties);
        AppendGeneratedTableConstructor(builder, schema, indexedQueryableProperties);
        AppendGeneratedTableCoreMembers(builder, schema);
        AppendGeneratedTableLookupMembers(builder, schema, queryableProperties, indexedQueryableProperties);
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
        AppendGeneratedSourceHeader(builder, schema.Namespace);
        AppendBindingsTypeHeader(builder, bindingsClassName, schema.FileName);
        AppendBindingsReferenceMetadataType(builder);
        AppendBindingsMetadataType(builder, schema);
        AppendBindingsMetadataAliases(builder);
        AppendYamlSerializationHelpers(builder, schema);
        AppendBindingsReferencesType(builder, referenceSpecs);
        AppendBindingsRegisterMethod(builder, schema, registerMethodName);
        AppendBindingsGetMethod(builder, schema, getMethodName);
        AppendBindingsTryGetMethod(builder, schema, tryGetMethodName);
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
        AppendGeneratedSourceHeader(builder, GeneratedNamespace);
        AppendGeneratedConfigCatalogType(builder, schemas);
        builder.AppendLine();
        AppendGeneratedConfigRegistrationOptionsType(builder, schemas);
        builder.AppendLine();
        AppendGeneratedConfigRegistrationExtensionsType(builder, schemas);

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    ///     Emits the generated catalog type that exposes schema metadata and filtering helpers.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schemas">Successfully parsed schemas for the current compilation.</param>
    private static void AppendGeneratedConfigCatalogType(StringBuilder builder, IReadOnlyList<SchemaFileSpec> schemas)
    {
        AppendGeneratedConfigCatalogHeader(builder);
        AppendGeneratedConfigTableMetadataType(builder);
        AppendGeneratedConfigTablesProperty(builder, schemas);
        AppendGeneratedConfigResolveAbsolutePathMethod(builder);
        AppendGeneratedConfigTryGetByTableNameMethod(builder, schemas);
        AppendGeneratedConfigGetTablesInConfigDomainMethod(builder);
        AppendGeneratedConfigGetTablesForRegistrationMethod(builder);
        AppendGeneratedConfigMatchesRegistrationOptionsMethod(builder);
        AppendGeneratedConfigAllowListMethod(builder);
        builder.AppendLine("}");
    }

    private static void AppendGeneratedSourceHeader(StringBuilder builder, string namespaceName)
    {
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {namespaceName};");
        builder.AppendLine();
    }

    private static void AppendGeneratedTableTypeHeader(
        StringBuilder builder,
        SchemaFileSpec schema,
        IReadOnlyList<SchemaPropertySpec> indexedQueryableProperties)
    {
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
    }

    private static void AppendGeneratedTableConstructor(
        StringBuilder builder,
        SchemaFileSpec schema,
        IReadOnlyList<SchemaPropertySpec> indexedQueryableProperties)
    {
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
    }

    private static void AppendGeneratedTableCoreMembers(StringBuilder builder, SchemaFileSpec schema)
    {
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
    }

    private static void AppendGeneratedTableLookupMembers(
        StringBuilder builder,
        SchemaFileSpec schema,
        IReadOnlyList<SchemaPropertySpec> queryableProperties,
        IReadOnlyList<SchemaPropertySpec> indexedQueryableProperties)
    {
        if (indexedQueryableProperties.Count > 0)
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
    }

    private static void AppendBindingsTypeHeader(StringBuilder builder, string bindingsClassName, string fileName)
    {
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            $"///     Auto-generated registration and lookup helpers for schema file '{fileName}'.");
        builder.AppendLine(
            "///     The helper centralizes table naming, config directory, schema path, and strongly-typed registry access so consumer projects do not need to duplicate the same conventions.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine($"public static class {bindingsClassName}");
        builder.AppendLine("{");
    }

    private static void AppendBindingsReferenceMetadataType(StringBuilder builder)
    {
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
        AppendBindingsReferenceMetadataConstructorBody(builder);
        AppendBindingsReferenceMetadataProperties(builder);
        builder.AppendLine("    }");
    }

    private static void AppendBindingsReferenceMetadataConstructorBody(StringBuilder builder)
    {
        builder.AppendLine("        {");
        builder.AppendLine(
            "            DisplayPath = displayPath ?? throw new global::System.ArgumentNullException(nameof(displayPath));");
        builder.AppendLine(
            "            ReferencedTableName = referencedTableName ?? throw new global::System.ArgumentNullException(nameof(referencedTableName));");
        builder.AppendLine(
            "            ValueSchemaType = valueSchemaType ?? throw new global::System.ArgumentNullException(nameof(valueSchemaType));");
        builder.AppendLine("            IsCollection = isCollection;");
        builder.AppendLine("        }");
    }

    private static void AppendBindingsReferenceMetadataProperties(StringBuilder builder)
    {
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
    }

    private static void AppendBindingsMetadataType(StringBuilder builder, SchemaFileSpec schema)
    {
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
    }

    private static void AppendBindingsMetadataAliases(StringBuilder builder)
    {
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
    }

    private static void AppendBindingsReferencesType(
        StringBuilder builder,
        IReadOnlyList<GeneratedReferenceSpec> referenceSpecs)
    {
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Exposes generated metadata for schema properties that declare <c>x-gframework-ref-table</c>.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public static class References");
        builder.AppendLine("    {");
        AppendBindingsReferenceMembers(builder, referenceSpecs);
        AppendBindingsReferenceCollectionProperty(builder, referenceSpecs);
        AppendBindingsTryGetByDisplayPathMethod(builder, referenceSpecs);
        builder.AppendLine("    }");
    }

    private static void AppendBindingsReferenceMembers(
        StringBuilder builder,
        IReadOnlyList<GeneratedReferenceSpec> referenceSpecs)
    {
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
    }

    private static void AppendBindingsReferenceCollectionProperty(
        StringBuilder builder,
        IReadOnlyList<GeneratedReferenceSpec> referenceSpecs)
    {
        builder.AppendLine("        /// <summary>");
        builder.AppendLine(
            "        ///     Gets all generated cross-table reference descriptors for the current schema.");
        builder.AppendLine("        /// </summary>");
        if (referenceSpecs.Count == 0)
        {
            builder.AppendLine(
                "        public static global::System.Collections.Generic.IReadOnlyList<ReferenceMetadata> All { get; } = global::System.Array.Empty<ReferenceMetadata>();");
            return;
        }

        builder.AppendLine(
            "        public static global::System.Collections.Generic.IReadOnlyList<ReferenceMetadata> All { get; } = global::System.Array.AsReadOnly(new ReferenceMetadata[]");
        builder.AppendLine("        {");
        foreach (var referenceSpec in referenceSpecs)
        {
            builder.AppendLine($"            {referenceSpec.MemberName},");
        }

        builder.AppendLine("        });");
    }

    private static void AppendBindingsTryGetByDisplayPathMethod(
        StringBuilder builder,
        IReadOnlyList<GeneratedReferenceSpec> referenceSpecs)
    {
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

        if (referenceSpecs.Count == 0)
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
    }

    private static void AppendBindingsRegisterMethod(
        StringBuilder builder,
        SchemaFileSpec schema,
        string registerMethodName)
    {
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
    }

    private static void AppendBindingsGetMethod(
        StringBuilder builder,
        SchemaFileSpec schema,
        string getMethodName)
    {
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
    }

    private static void AppendBindingsTryGetMethod(
        StringBuilder builder,
        SchemaFileSpec schema,
        string tryGetMethodName)
    {
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
    }

    private static void AppendGeneratedConfigCatalogHeader(StringBuilder builder)
    {
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            "///     Provides a project-level catalog for every config table generated from the current consumer project's schemas.");
        builder.AppendLine(
            "///     Use this entry point when you want the C# runtime bootstrap path to register all generated tables without repeating one call per schema.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class GeneratedConfigCatalog");
        builder.AppendLine("{");
    }

    private static void AppendGeneratedConfigTableMetadataType(StringBuilder builder)
    {
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
        AppendGeneratedConfigTableMetadataConstructorBody(builder);
        AppendGeneratedConfigTableMetadataProperties(builder);
        builder.AppendLine("    }");
    }

    private static void AppendGeneratedConfigTableMetadataConstructorBody(StringBuilder builder)
    {
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
    }

    private static void AppendGeneratedConfigTableMetadataProperties(StringBuilder builder)
    {
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
    }

    private static void AppendGeneratedConfigTablesProperty(
        StringBuilder builder,
        IReadOnlyList<SchemaFileSpec> schemas)
    {
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
    }

    private static void AppendGeneratedConfigResolveAbsolutePathMethod(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Resolves one generated relative config path against the caller-supplied config root directory.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"configRootPath\">Absolute or workspace-local config root directory.</param>");
        builder.AppendLine("    /// <param name=\"relativePath\">Generated relative config or schema path.</param>");
        builder.AppendLine("    /// <returns>The combined absolute path.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentException\">When <paramref name=\"configRootPath\"/> is null, empty, or whitespace.</exception>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">When <paramref name=\"relativePath\"/> is null.</exception>");
        builder.AppendLine(
            "    internal static string ResolveAbsolutePath(string configRootPath, string relativePath)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (string.IsNullOrWhiteSpace(configRootPath))");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            throw new global::System.ArgumentException(\"Config root path cannot be null or whitespace.\", nameof(configRootPath));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        if (relativePath is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(relativePath));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        var normalizedRelativePath = relativePath.Replace('/', global::System.IO.Path.DirectorySeparatorChar)");
        builder.AppendLine(
            "            .Replace('\\\\', global::System.IO.Path.DirectorySeparatorChar);");
        builder.AppendLine(
            "        return global::System.IO.Path.Combine(configRootPath, normalizedRelativePath);");
        builder.AppendLine("    }");
    }

    private static void AppendGeneratedConfigTryGetByTableNameMethod(
        StringBuilder builder,
        IReadOnlyList<SchemaFileSpec> schemas)
    {
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
    }

    private static void AppendGeneratedConfigGetTablesInConfigDomainMethod(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Resolves the generated table metadata entries that belong to the specified logical config domain.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"configDomain\">Logical config domain derived from the schema base name.</param>");
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
    }

    private static void AppendGeneratedConfigGetTablesForRegistrationMethod(StringBuilder builder)
    {
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
    }

    private static void AppendGeneratedConfigMatchesRegistrationOptionsMethod(StringBuilder builder)
    {
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
    }

    private static void AppendGeneratedConfigAllowListMethod(StringBuilder builder)
    {
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
    }

    /// <summary>
    ///     Emits the options type consumed by aggregate generated table registration.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schemas">Successfully parsed schemas for the current compilation.</param>
    private static void AppendGeneratedConfigRegistrationOptionsType(
        StringBuilder builder,
        IReadOnlyList<SchemaFileSpec> schemas)
    {
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
        AppendGeneratedConfigComparerOptions(builder, schemas);
        builder.AppendLine("}");
    }

    /// <summary>
    ///     Emits one optional comparer property per generated table for aggregate registration.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schemas">Successfully parsed schemas for the current compilation.</param>
    private static void AppendGeneratedConfigComparerOptions(
        StringBuilder builder,
        IReadOnlyList<SchemaFileSpec> schemas)
    {
        if (schemas.Count > 0)
        {
            builder.AppendLine();
        }

        for (var index = 0; index < schemas.Count; index++)
        {
            var schema = schemas[index];
            var comparerTypeDocumentation = EscapeXmlDocumentation(
                $"global::System.Collections.Generic.IEqualityComparer<{schema.KeyClrType}>?");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine(
                $"    ///     Gets or sets the optional key comparer forwarded to {schema.EntityName}ConfigBindings.Register{schema.EntityName}Table using <c>{comparerTypeDocumentation}</c> when aggregate registration runs.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine(
                $"    public global::System.Collections.Generic.IEqualityComparer<{schema.KeyClrType}>? {schema.EntityName}Comparer {{ get; init; }}");

            if (index < schemas.Count - 1)
            {
                builder.AppendLine();
            }
        }
    }

    /// <summary>
    ///     Emits extension methods that register every generated config table for the current compilation.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schemas">Successfully parsed schemas for the current compilation.</param>
    private static void AppendGeneratedConfigRegistrationExtensionsType(
        StringBuilder builder,
        IReadOnlyList<SchemaFileSpec> schemas)
    {
        builder.AppendLine("/// <summary>");
        builder.AppendLine(
            "///     Provides a single extension method that registers every generated config table discovered in the current consumer project.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static class GeneratedConfigRegistrationExtensions");
        builder.AppendLine("{");
        AppendRegisterAllGeneratedConfigTablesMethod(builder);
        builder.AppendLine();
        AppendRegisterAllGeneratedConfigTablesWithOptionsMethod(builder, schemas);
        builder.AppendLine("}");
    }

    /// <summary>
    ///     Emits the aggregate registration overload that uses default options.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    private static void AppendRegisterAllGeneratedConfigTablesMethod(StringBuilder builder)
    {
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
    }

    /// <summary>
    ///     Emits the aggregate registration overload that honors generated filters and comparer overrides.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schemas">Successfully parsed schemas for the current compilation.</param>
    private static void AppendRegisterAllGeneratedConfigTablesWithOptionsMethod(
        StringBuilder builder,
        IReadOnlyList<SchemaFileSpec> schemas)
    {
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
    }

    /// <summary>
    ///     为生成的绑定类输出 YAML 序列化与 schema 路径辅助。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="schema">生成器级 schema 模型。</param>
    private static void AppendYamlSerializationHelpers(
        StringBuilder builder,
        SchemaFileSpec schema)
    {
        AppendYamlSerializeMethod(builder, schema);
        builder.AppendLine();
        AppendYamlPathMethods(builder);
        builder.AppendLine();
        AppendYamlValidationMethods(builder);
    }

    /// <summary>
    ///     Emits the generated YAML serialization method.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schema">Generator-level schema model.</param>
    private static void AppendYamlSerializeMethod(StringBuilder builder, SchemaFileSpec schema)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Serializes one generated config instance to YAML text using the shared runtime naming convention.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <param name=\"config\">The generated config instance to serialize.</param>");
        builder.AppendLine(
            "    /// <returns>YAML text that preserves the shared camelCase field naming convention.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">Thrown when <paramref name=\"config\"/> is <see langword=\"null\"/>.</exception>");
        builder.AppendLine($"    public static string SerializeToYaml({schema.ClassName} config)");
        builder.AppendLine("    {");
        builder.AppendLine("        return global::GFramework.Game.Config.YamlConfigTextSerializer.Serialize(config);");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     Emits generated helpers that resolve config and schema paths at runtime.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    private static void AppendYamlPathMethods(StringBuilder builder)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Resolves the absolute config directory path by combining the caller-supplied config root with the generated relative directory.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"configRootPath\">Absolute or workspace-local config root directory.</param>");
        builder.AppendLine("    /// <returns>The absolute config directory path for the generated table.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentException\">Thrown when <paramref name=\"configRootPath\"/> is null, empty, or whitespace.</exception>");
        builder.AppendLine("    public static string GetConfigDirectoryPath(string configRootPath)");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        return GeneratedConfigCatalog.ResolveAbsolutePath(configRootPath, Metadata.ConfigRelativePath);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Resolves the absolute schema file path by combining the caller-supplied config root with the generated relative schema path.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"configRootPath\">Absolute or workspace-local config root directory.</param>");
        builder.AppendLine("    /// <returns>The absolute schema file path for the generated table.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentException\">Thrown when <paramref name=\"configRootPath\"/> is null, empty, or whitespace.</exception>");
        builder.AppendLine("    public static string GetSchemaPath(string configRootPath)");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        return GeneratedConfigCatalog.ResolveAbsolutePath(configRootPath, Metadata.SchemaRelativePath);");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     Emits generated synchronous and asynchronous YAML validation methods.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    private static void AppendYamlValidationMethods(StringBuilder builder)
    {
        AppendValidateYamlMethod(builder);
        builder.AppendLine();
        AppendValidateYamlAsyncMethod(builder);
    }

    /// <summary>
    ///     Emits the generated synchronous YAML validation method.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    private static void AppendValidateYamlMethod(StringBuilder builder)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Validates YAML text against the generated schema file located under the supplied config root directory.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"configRootPath\">Absolute or workspace-local config root directory.</param>");
        builder.AppendLine(
            "    /// <param name=\"yamlPath\">Logical or absolute YAML path used for diagnostics.</param>");
        builder.AppendLine("    /// <param name=\"yamlText\">YAML text to validate.</param>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentException\">Thrown when <paramref name=\"configRootPath\"/> is null, empty, or whitespace.</exception>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">Thrown when <paramref name=\"yamlPath\"/> or <paramref name=\"yamlText\"/> is <see langword=\"null\"/>.</exception>");
        builder.AppendLine(
            "    /// <exception cref=\"global::GFramework.Game.Abstractions.Config.ConfigLoadException\">Thrown when the generated schema file cannot be loaded or the YAML text fails schema validation.</exception>");
        builder.AppendLine(
            "    public static void ValidateYaml(string configRootPath, string yamlPath, string yamlText)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::GFramework.Game.Config.YamlConfigTextValidator.Validate(");
        builder.AppendLine("            Metadata.TableName,");
        builder.AppendLine("            GetSchemaPath(configRootPath),");
        builder.AppendLine("            yamlPath,");
        builder.AppendLine("            yamlText);");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     Emits the generated asynchronous YAML validation method.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    private static void AppendValidateYamlAsyncMethod(StringBuilder builder)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Asynchronously validates YAML text against the generated schema file located under the supplied config root directory.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            "    /// <param name=\"configRootPath\">Absolute or workspace-local config root directory.</param>");
        builder.AppendLine(
            "    /// <param name=\"yamlPath\">Logical or absolute YAML path used for diagnostics.</param>");
        builder.AppendLine("    /// <param name=\"yamlText\">YAML text to validate.</param>");
        builder.AppendLine("    /// <param name=\"cancellationToken\">Cancellation token.</param>");
        builder.AppendLine("    /// <returns>A task that represents the asynchronous validation operation.</returns>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentException\">Thrown when <paramref name=\"configRootPath\"/> is null, empty, or whitespace.</exception>");
        builder.AppendLine(
            "    /// <exception cref=\"global::System.ArgumentNullException\">Thrown when <paramref name=\"yamlPath\"/> or <paramref name=\"yamlText\"/> is <see langword=\"null\"/>.</exception>");
        builder.AppendLine(
            "    /// <exception cref=\"global::GFramework.Game.Abstractions.Config.ConfigLoadException\">Thrown when the generated schema file cannot be loaded or the YAML text fails schema validation.</exception>");
        builder.AppendLine(
            "    public static global::System.Threading.Tasks.Task ValidateYamlAsync(");
        builder.AppendLine("        string configRootPath,");
        builder.AppendLine("        string yamlPath,");
        builder.AppendLine("        string yamlText,");
        builder.AppendLine("        global::System.Threading.CancellationToken cancellationToken = default)");
        builder.AppendLine("    {");
        builder.AppendLine("        return global::GFramework.Game.Config.YamlConfigTextValidator.ValidateAsync(");
        builder.AppendLine("            Metadata.TableName,");
        builder.AppendLine("            GetSchemaPath(configRootPath),");
        builder.AppendLine("            yamlPath,");
        builder.AppendLine("            yamlText,");
        builder.AppendLine("            cancellationToken);");
        builder.AppendLine("    }");
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
        AppendSharedLookupIndexBuilderDocumentation(builder, schema);
        AppendSharedLookupIndexBuilderBody(builder, schema);
    }

    /// <summary>
    ///     Emits XML documentation and signature for the shared generated lookup index builder.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schema">Generator-level schema model.</param>
    private static void AppendSharedLookupIndexBuilderDocumentation(
        StringBuilder builder,
        SchemaFileSpec schema)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine(
            "    ///     Materializes a read-only exact-match lookup index from the current table snapshot.");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <typeparam name=\"TProperty\">Indexed property type.</typeparam>");
        builder.AppendLine(
            "    /// <param name=\"keySelector\">Selects the indexed property from one config entry.</param>");
        builder.AppendLine(
            "    /// <returns>A read-only dictionary whose values preserve snapshot iteration order.</returns>");
        builder.AppendLine("    /// <remarks>");
        builder.AppendLine(
            "    ///     The generated index skips runtime null keys even though <typeparamref name=\"TProperty\"/> is constrained to <c>notnull</c>. Malformed YAML payloads can still deserialize missing indexed values to <see langword=\"null\" />, and throwing from this lazy path would permanently poison the cached index for the current table wrapper instance.");
        builder.AppendLine("    /// </remarks>");
        builder.AppendLine(
            $"    private global::System.Collections.Generic.IReadOnlyDictionary<TProperty, global::System.Collections.Generic.IReadOnlyList<{schema.ClassName}>> BuildLookupIndex<TProperty>(");
        builder.AppendLine($"        global::System.Func<{schema.ClassName}, TProperty> keySelector)");
        builder.AppendLine("        where TProperty : notnull");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        var buckets = new global::System.Collections.Generic.Dictionary<TProperty, global::System.Collections.Generic.List<" +
            $"{schema.ClassName}>>();");
    }

    /// <summary>
    ///     Emits the implementation body for the shared generated lookup index builder.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schema">Generator-level schema model.</param>
    private static void AppendSharedLookupIndexBuilderBody(
        StringBuilder builder,
        SchemaFileSpec schema)
    {
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
        builder.AppendLine(
            $"                matches = new global::System.Collections.Generic.List<{schema.ClassName}>();");
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
        AppendFindByPropertyBody(builder, schema, property);
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     Emits the body of a generated <c>FindBy*</c> lookup helper.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schema">Generator-level schema model.</param>
    /// <param name="property">Property model used by the lookup helper.</param>
    private static void AppendFindByPropertyBody(
        StringBuilder builder,
        SchemaFileSpec schema,
        SchemaPropertySpec property)
    {
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
        AppendTryFindFirstByPropertyBody(builder, schema, property);
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     Emits the body of a generated <c>TryFindFirstBy*</c> lookup helper.
    /// </summary>
    /// <param name="builder">Output buffer.</param>
    /// <param name="schema">Generator-level schema model.</param>
    /// <param name="property">Property model used by the lookup helper.</param>
    private static void AppendTryFindFirstByPropertyBody(
        StringBuilder builder,
        SchemaFileSpec schema,
        SchemaPropertySpec property)
    {
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
        AppendObjectTypeHeader(builder, objectSpec, fileName, title, description, isRoot, indent);
        AppendObjectTypeProperties(builder, objectSpec, indentationLevel);
        AppendNestedObjectTypes(builder, objectSpec, fileName, indentationLevel);
        builder.AppendLine($"{indent}}}");
    }

    /// <summary>
    ///     生成单个配置对象类型的 XML 文档和类型声明。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="objectSpec">要生成的对象类型。</param>
    /// <param name="fileName">Schema 文件名。</param>
    /// <param name="title">对象标题元数据。</param>
    /// <param name="description">对象说明元数据。</param>
    /// <param name="isRoot">是否为根配置类型。</param>
    /// <param name="indent">当前缩进。</param>
    private static void AppendObjectTypeHeader(
        StringBuilder builder,
        SchemaObjectSpec objectSpec,
        string fileName,
        string? title,
        string? description,
        bool isRoot,
        string indent)
    {
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
    }

    /// <summary>
    ///     生成配置对象直接拥有的 CLR 属性。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="objectSpec">要生成的对象类型。</param>
    /// <param name="indentationLevel">当前缩进层级。</param>
    private static void AppendObjectTypeProperties(
        StringBuilder builder,
        SchemaObjectSpec objectSpec,
        int indentationLevel)
    {
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
    }

    /// <summary>
    ///     在直接属性之后递归生成嵌套对象类型，保持输出顺序稳定。
    /// </summary>
    /// <param name="builder">输出缓冲区。</param>
    /// <param name="objectSpec">父对象类型。</param>
    /// <param name="fileName">Schema 文件名。</param>
    /// <param name="indentationLevel">父对象缩进层级。</param>
    private static void AppendNestedObjectTypes(
        StringBuilder builder,
        SchemaObjectSpec objectSpec,
        string fileName,
        int indentationLevel)
    {
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
    ///     记录同一对象节点内已分配的生成属性名，并在 schema key 归一化后发生冲突时返回明确诊断。
    ///     该校验会在生成器进入源码发射前阻止重复属性、查询方法与索引成员名落入后续编译阶段。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="displayPath">当前字段的逻辑路径。</param>
    /// <param name="schemaName">当前字段的原始 schema key。</param>
    /// <param name="propertyName">当前字段归一化后的 CLR 属性名。</param>
    /// <param name="schemaKeyByGeneratedPropertyName">同一对象内已分配的属性名与原始 schema key 对照表。</param>
    /// <param name="diagnostic">检测到冲突时返回的诊断。</param>
    /// <returns>当生成属性名在当前对象作用域内唯一时返回 <see langword="true" />。</returns>
    private static bool TryRegisterGeneratedPropertyName(
        string filePath,
        string displayPath,
        string schemaName,
        string propertyName,
        IDictionary<string, string> schemaKeyByGeneratedPropertyName,
        out Diagnostic? diagnostic)
    {
        if (!schemaKeyByGeneratedPropertyName.TryGetValue(propertyName, out var existingSchemaName))
        {
            schemaKeyByGeneratedPropertyName.Add(propertyName, schemaName);
            diagnostic = null;
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.DuplicateGeneratedIdentifier,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            displayPath,
            schemaName,
            propertyName,
            existingSchemaName);
        return false;
    }

    /// <summary>
    ///     将 schema 文件名派生的根实体名验证为生成代码可直接使用的根类型标识符。
    ///     这里与属性名验证保持一致，避免文件名中的前导数字或其他非法字符把根配置类型/表类型生成为无效 C# 标识符。
    /// </summary>
    /// <param name="filePath">Schema 文件路径。</param>
    /// <param name="schemaBaseName">去除扩展名后的 schema 基础名。</param>
    /// <param name="entityName">验证后的实体名。</param>
    /// <param name="rootClassName">验证后的根配置类型名。</param>
    /// <param name="diagnostic">根类型名非法时生成的诊断。</param>
    /// <returns>是否成功生成合法的根类型标识符。</returns>
    private static bool TryBuildRootTypeIdentifiers(
        string filePath,
        string schemaBaseName,
        out string entityName,
        out string rootClassName,
        out Diagnostic? diagnostic)
    {
        entityName = ToPascalCase(schemaBaseName);
        rootClassName = $"{entityName}Config";
        if (SyntaxFacts.IsValidIdentifier(rootClassName))
        {
            diagnostic = null;
            return true;
        }

        diagnostic = Diagnostic.Create(
            ConfigSchemaDiagnostics.InvalidGeneratedIdentifier,
            CreateFileLocation(filePath),
            Path.GetFileName(filePath),
            "<root>",
            schemaBaseName,
            rootClassName);
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
    /// <param name="schemaType">当前 schema 类型。</param>
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
                "array" when item.ValueKind == JsonValueKind.Array => item.GetRawText(),
                "object" when item.ValueKind == JsonValueKind.Object => item.GetRawText(),
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
    ///     将 shared schema 子集中的范围、步进、长度、数组数量 / 去重 / contains、
    ///     对象属性数量 / dependent* / allOf / if-then-else 约束整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <param name="schemaType">标量类型。</param>
    /// <returns>格式化后的约束说明。</returns>
    private static string? TryBuildConstraintDocumentation(JsonElement element, string schemaType)
    {
        var parts = new List<string>();
        AddConstDocumentationPart(element, schemaType, parts);
        AddNumericConstraintDocumentationParts(element, schemaType, parts);
        AddStringConstraintDocumentationParts(element, schemaType, parts);
        AddArrayConstraintDocumentationParts(element, schemaType, parts);
        AddObjectConstraintDocumentationParts(element, schemaType, parts);

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    /// <summary>
    ///     Adds <c>const</c> documentation when the schema value matches the current type.
    /// </summary>
    /// <param name="element">Schema node.</param>
    /// <param name="schemaType">Current schema type.</param>
    /// <param name="parts">Mutable documentation parts.</param>
    private static void AddConstDocumentationPart(JsonElement element, string schemaType, List<string> parts)
    {
        var constDocumentation = TryBuildConstDocumentation(element, schemaType);
        if (constDocumentation is not null)
        {
            parts.Add($"const = {constDocumentation}");
        }
    }

    /// <summary>
    ///     Adds numeric range and step constraints to generated XML documentation.
    /// </summary>
    /// <param name="element">Schema node.</param>
    /// <param name="schemaType">Current schema type.</param>
    /// <param name="parts">Mutable documentation parts.</param>
    private static void AddNumericConstraintDocumentationParts(JsonElement element, string schemaType, List<string> parts)
    {
        if (IsNumericSchemaType(schemaType) &&
            TryGetFiniteNumber(element, "minimum", out var minimum))
        {
            parts.Add($"minimum = {minimum.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsNumericSchemaType(schemaType) &&
            TryGetFiniteNumber(element, "exclusiveMinimum", out var exclusiveMinimum))
        {
            parts.Add($"exclusiveMinimum = {exclusiveMinimum.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsNumericSchemaType(schemaType) &&
            TryGetFiniteNumber(element, "maximum", out var maximum))
        {
            parts.Add($"maximum = {maximum.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsNumericSchemaType(schemaType) &&
            TryGetFiniteNumber(element, "exclusiveMaximum", out var exclusiveMaximum))
        {
            parts.Add($"exclusiveMaximum = {exclusiveMaximum.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsNumericSchemaType(schemaType) &&
            TryGetFiniteNumber(element, "multipleOf", out var multipleOf) &&
            multipleOf > 0d)
        {
            parts.Add($"multipleOf = {multipleOf.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    /// <summary>
    ///     Adds string length, pattern, and stable format constraints to generated XML documentation.
    /// </summary>
    /// <param name="element">Schema node.</param>
    /// <param name="schemaType">Current schema type.</param>
    /// <param name="parts">Mutable documentation parts.</param>
    private static void AddStringConstraintDocumentationParts(JsonElement element, string schemaType, List<string> parts)
    {
        if (IsSchemaType(schemaType, "string") &&
            TryGetNonNegativeInt32(element, "minLength", out var minLength))
        {
            parts.Add($"minLength = {minLength.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "string") &&
            TryGetNonNegativeInt32(element, "maxLength", out var maxLength))
        {
            parts.Add($"maxLength = {maxLength.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "string") &&
            element.TryGetProperty("pattern", out var patternElement) &&
            patternElement.ValueKind == JsonValueKind.String)
        {
            parts.Add($"pattern = '{patternElement.GetString() ?? string.Empty}'");
        }

        if (IsSchemaType(schemaType, "string") &&
            element.TryGetProperty("format", out var formatElement) &&
            formatElement.ValueKind == JsonValueKind.String)
        {
            var formatName = formatElement.GetString() ?? string.Empty;
            if (IsSupportedStringFormat(formatName))
            {
                parts.Add($"format = '{formatName}'");
            }
        }
    }

    /// <summary>
    ///     Adds array count, uniqueness, contains, and negation constraints to generated XML documentation.
    /// </summary>
    /// <param name="element">Schema node.</param>
    /// <param name="schemaType">Current schema type.</param>
    /// <param name="parts">Mutable documentation parts.</param>
    private static void AddArrayConstraintDocumentationParts(JsonElement element, string schemaType, List<string> parts)
    {
        if (IsSchemaType(schemaType, "array") &&
            TryGetNonNegativeInt32(element, "minItems", out var minItems))
        {
            parts.Add($"minItems = {minItems.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "array") &&
            TryGetNonNegativeInt32(element, "maxItems", out var maxItems))
        {
            parts.Add($"maxItems = {maxItems.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "array") &&
            element.TryGetProperty("uniqueItems", out var uniqueItemsElement) &&
            uniqueItemsElement.ValueKind == JsonValueKind.True)
        {
            parts.Add("uniqueItems = true");
        }

        if (IsSchemaType(schemaType, "array"))
        {
            var containsDocumentation = TryBuildContainsDocumentation(element);
            if (containsDocumentation is not null)
            {
                parts.Add($"contains = {containsDocumentation}");
            }
        }

        var notDocumentation = TryBuildNotDocumentation(element);
        if (notDocumentation is not null)
        {
            parts.Add($"not = {notDocumentation}");
        }

        if (IsSchemaType(schemaType, "array") &&
            TryGetNonNegativeInt32(element, "minContains", out var minContains))
        {
            parts.Add($"minContains = {minContains.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "array") &&
            TryGetNonNegativeInt32(element, "maxContains", out var maxContains))
        {
            parts.Add($"maxContains = {maxContains.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    /// <summary>
    ///     Adds object cardinality and composition constraints to generated XML documentation.
    /// </summary>
    /// <param name="element">Schema node.</param>
    /// <param name="schemaType">Current schema type.</param>
    /// <param name="parts">Mutable documentation parts.</param>
    private static void AddObjectConstraintDocumentationParts(JsonElement element, string schemaType, List<string> parts)
    {
        if (IsSchemaType(schemaType, "object") &&
            TryGetNonNegativeInt32(element, "minProperties", out var minProperties))
        {
            parts.Add($"minProperties = {minProperties.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "object") &&
            TryGetNonNegativeInt32(element, "maxProperties", out var maxProperties))
        {
            parts.Add($"maxProperties = {maxProperties.ToString(CultureInfo.InvariantCulture)}");
        }

        if (IsSchemaType(schemaType, "object"))
        {
            var dependentRequiredDocumentation = TryBuildDependentRequiredDocumentation(element);
            if (dependentRequiredDocumentation is not null)
            {
                parts.Add($"dependentRequired = {dependentRequiredDocumentation}");
            }

            var dependentSchemasDocumentation = TryBuildDependentSchemasDocumentation(element);
            if (dependentSchemasDocumentation is not null)
            {
                parts.Add($"dependentSchemas = {dependentSchemasDocumentation}");
            }

            var allOfDocumentation = TryBuildAllOfDocumentation(element);
            if (allOfDocumentation is not null)
            {
                parts.Add($"allOf = {allOfDocumentation}");
            }

            var conditionalDocumentation = TryBuildConditionalDocumentation(element);
            if (conditionalDocumentation is not null)
            {
                parts.Add($"if/then/else = {conditionalDocumentation}");
            }
        }
    }

    /// <summary>
    ///     将对象 <c>dependentRequired</c> 依赖关系整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">对象 schema 节点。</param>
    /// <returns>格式化后的 dependentRequired 说明。</returns>
    private static string? TryBuildDependentRequiredDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("dependentRequired", out var dependentRequiredElement) ||
            dependentRequiredElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var dependency in dependentRequiredElement.EnumerateObject())
        {
            if (dependency.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var targets = dependency.Value
                .EnumerateArray()
                .Where(static item =>
                    item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                .Select(static item => item.GetString()!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (targets.Length == 0)
            {
                continue;
            }

            parts.Add($"{dependency.Name} => [{string.Join(", ", targets)}]");
        }

        return parts.Count > 0
            ? $"{{ {string.Join("; ", parts)} }}"
            : null;
    }

    /// <summary>
    ///     将对象 <c>dependentSchemas</c> 关系整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">对象 schema 节点。</param>
    /// <returns>格式化后的 dependentSchemas 说明。</returns>
    private static string? TryBuildDependentSchemasDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("dependentSchemas", out var dependentSchemasElement) ||
            dependentSchemasElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var dependency in dependentSchemasElement.EnumerateObject())
        {
            if (dependency.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var summary = TryBuildInlineSchemaSummary(dependency.Value, includeRequiredProperties: true);
            if (summary is null)
            {
                continue;
            }

            parts.Add($"{dependency.Name} => {summary}");
        }

        return parts.Count > 0
            ? $"{{ {string.Join("; ", parts)} }}"
            : null;
    }

    /// <summary>
    ///     将对象 <c>allOf</c> 组合约束整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">对象 schema 节点。</param>
    /// <returns>格式化后的 allOf 说明。</returns>
    private static string? TryBuildAllOfDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("allOf", out var allOfElement) ||
            allOfElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var allOfSchema in allOfElement.EnumerateArray())
        {
            if (allOfSchema.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var summary = TryBuildInlineSchemaSummary(allOfSchema, includeRequiredProperties: true);
            if (summary is not null)
            {
                parts.Add(summary);
            }
        }

        return parts.Count > 0
            ? $"[ {string.Join("; ", parts)} ]"
            : null;
    }

    /// <summary>
    ///     将对象 <c>if</c> / <c>then</c> / <c>else</c> 条件约束整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">对象 schema 节点。</param>
    /// <returns>格式化后的条件约束说明。</returns>
    private static string? TryBuildConditionalDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("if", out var ifElement) ||
            ifElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var ifSummary = TryBuildConditionalBranchSummary(ifElement);
        if (ifSummary is null)
        {
            return null;
        }

        var parts = new List<string> { $"if {ifSummary}" };
        if (element.TryGetProperty("then", out var thenElement) &&
            thenElement.ValueKind == JsonValueKind.Object)
        {
            var thenSummary = TryBuildConditionalBranchSummary(thenElement);
            if (thenSummary is not null)
            {
                parts.Add($"then {thenSummary}");
            }
        }

        if (element.TryGetProperty("else", out var elseElement) &&
            elseElement.ValueKind == JsonValueKind.Object)
        {
            var elseSummary = TryBuildConditionalBranchSummary(elseElement);
            if (elseSummary is not null)
            {
                parts.Add($"else {elseSummary}");
            }
        }

        return parts.Count > 1
            ? string.Join("; ", parts)
            : null;
    }

    /// <summary>
    ///     汇总条件分支的对象级约束与子属性约束，避免生成文档只保留笼统的 object 描述。
    /// </summary>
    /// <param name="branchElement">条件分支 schema。</param>
    /// <returns>格式化后的条件分支摘要。</returns>
    private static string? TryBuildConditionalBranchSummary(JsonElement branchElement)
    {
        var branchSummary = TryBuildInlineSchemaSummary(branchElement, includeRequiredProperties: true);
        if (branchSummary is null)
        {
            return null;
        }

        var propertiesSummary = TryBuildInlineObjectPropertiesSummary(branchElement);
        return propertiesSummary is null
            ? branchSummary
            : $"{branchSummary}; properties = {propertiesSummary}";
    }

    /// <summary>
    ///     汇总对象 <c>properties</c> 内每个字段的紧凑约束，补足条件分支文档里的触发条件细节。
    /// </summary>
    /// <param name="schemaElement">对象 schema 节点。</param>
    /// <returns>格式化后的子属性约束摘要。</returns>
    private static string? TryBuildInlineObjectPropertiesSummary(JsonElement schemaElement)
    {
        if (!schemaElement.TryGetProperty("properties", out var propertiesElement) ||
            propertiesElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var property in propertiesElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var propertySummary = TryBuildInlineSchemaSummary(property.Value);
            if (propertySummary is not null)
            {
                parts.Add($"{property.Name}: {propertySummary}");
            }
        }

        return parts.Count == 0
            ? null
            : $"{{ {string.Join("; ", parts)} }}";
    }

    /// <summary>
    ///     将数组 <c>contains</c> 子 schema 整理成 XML 文档可读字符串。
    ///     输出优先保持紧凑，只展示消费者在强类型 API 上最需要看到的匹配摘要。
    /// </summary>
    /// <param name="element">数组 schema 节点。</param>
    /// <returns>格式化后的 contains 说明。</returns>
    private static string? TryBuildContainsDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("contains", out var containsElement) ||
            containsElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TryBuildInlineSchemaSummary(containsElement);
    }

    /// <summary>
    ///     将 <c>not</c> 子 schema 整理成 XML 文档可读字符串。
    /// </summary>
    /// <param name="element">Schema 节点。</param>
    /// <returns>格式化后的 not 说明。</returns>
    private static string? TryBuildNotDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("not", out var notElement) ||
            notElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TryBuildInlineSchemaSummary(notElement);
    }

    /// <summary>
    ///     为内联子 schema 生成紧凑摘要。
    ///     该摘要复用现有 enum / const / 约束文档构造器，避免 contains / not 与主属性文档逐渐漂移。
    /// </summary>
    /// <param name="schemaElement">内联子 schema。</param>
    /// <param name="includeRequiredProperties">
    ///     为对象摘要额外输出 <c>required</c> 信息时返回 <see langword="true" />。
    /// </param>
    /// <returns>格式化后的摘要字符串。</returns>
    private static string? TryBuildInlineSchemaSummary(
        JsonElement schemaElement,
        bool includeRequiredProperties = false)
    {
        if (!schemaElement.TryGetProperty("type", out var typeElement) ||
            typeElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var schemaType = typeElement.GetString();
        if (schemaType is null || string.IsNullOrWhiteSpace(schemaType))
        {
            return null;
        }

        var details = new List<string>();
        if (includeRequiredProperties &&
            IsSchemaType(schemaType, "object"))
        {
            var requiredDocumentation = TryBuildRequiredPropertiesDocumentation(schemaElement);
            if (requiredDocumentation is not null)
            {
                details.Add(requiredDocumentation);
            }
        }

        var enumDocumentation = TryBuildEnumDocumentation(schemaElement, schemaType);
        if (enumDocumentation is not null)
        {
            details.Add($"enum = {enumDocumentation}");
        }

        var constraintDocumentation = TryBuildConstraintDocumentation(schemaElement, schemaType);
        if (constraintDocumentation is not null)
        {
            details.Add(constraintDocumentation);
        }

        var refTable = TryGetMetadataString(schemaElement, "x-gframework-ref-table");
        if (!string.IsNullOrWhiteSpace(refTable))
        {
            details.Add($"ref-table = {refTable}");
        }

        return details.Count == 0
            ? schemaType
            : $"{schemaType} ({string.Join(", ", details)})";
    }

    /// <summary>
    ///     将对象 schema 的 <c>required</c> 字段整理成紧凑说明。
    /// </summary>
    /// <param name="element">对象 schema 节点。</param>
    /// <returns>格式化后的 required 说明。</returns>
    private static string? TryBuildRequiredPropertiesDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("required", out var requiredElement) ||
            requiredElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var requiredProperties = requiredElement
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(static item => item.GetString()!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return requiredProperties.Length == 0
            ? null
            : $"required = [{string.Join(", ", requiredProperties)}]";
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
            "integer" when constElement.ValueKind == JsonValueKind.Number &&
                           constElement.TryGetInt64(out var intValue) =>
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
        return string.Equals(parentPath, "<root>", StringComparison.Ordinal)
            ? propertyName
            : $"{parentPath}.{propertyName}";
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
    ///     Shared state extracted before dispatching one property to a schema-type-specific parser.
    /// </summary>
    /// <param name="DisplayPath">Logical schema path.</param>
    /// <param name="PropertyName">Generated CLR property name.</param>
    /// <param name="IsRequired">Whether the property is required.</param>
    /// <param name="Title">Optional schema title.</param>
    /// <param name="Description">Optional schema description.</param>
    /// <param name="RefTableName">Optional referenced table metadata.</param>
    /// <param name="IsIndexedLookup">Whether the property declares a generated exact-match index.</param>
    private sealed record PropertyParseContext(
        string DisplayPath,
        string PropertyName,
        bool IsRequired,
        string? Title,
        string? Description,
        string? RefTableName,
        bool IsIndexedLookup);

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
