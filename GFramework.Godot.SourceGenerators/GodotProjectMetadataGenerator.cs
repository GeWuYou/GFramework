// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.Godot.SourceGenerators;

/// <summary>
///     读取 <c>project.godot</c> 项目元数据，并生成 AutoLoad 与 Input Action 的强类型访问入口。
/// </summary>
/// <remarks>
///     该生成器把 Godot 项目层面的事实模型暴露为稳定的编译期 API：
///     <list type="bullet">
///         <item>
///             <description>从 <c>[autoload]</c> 段生成统一访问入口，并在可唯一解析到 C# 节点类型时生成强类型属性。</description>
///         </item>
///         <item>
///             <description>从 <c>[input]</c> 段生成输入动作常量，避免手写魔法字符串。</description>
///         </item>
///     </list>
///     对于类型映射冲突或标识符冲突，该生成器会优先给出诊断并退化为可工作的稳定输出，而不是静默生成不确定代码。
/// </remarks>
[Generator]
public sealed class GodotProjectMetadataGenerator : IIncrementalGenerator
{
    private const string ProjectFileName = "project.godot";
    private const string GeneratedNamespace = $"{PathContests.GodotNamespace}.Generated";

    private const string AutoLoadAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.AutoLoadAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var projectFiles = context.AdditionalTextsProvider
            .Where(static file =>
                string.Equals(Path.GetFileName(file.Path), ProjectFileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) => ParseProjectFile(file, cancellationToken))
            .Collect();

        var typeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static (syntaxContext, _) => TransformTypeCandidate(syntaxContext))
            .Where(static candidate => candidate is not null)
            .Collect();

        var generationInput = context.CompilationProvider.Combine(projectFiles).Combine(typeCandidates);
        context.RegisterSourceOutput(generationInput, static (productionContext, input) =>
            Execute(productionContext, input.Left.Left, input.Left.Right, input.Right));
    }

    private static GodotTypeCandidate? TransformTypeCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        return new GodotTypeCandidate(classDeclaration, typeSymbol);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ProjectMetadataParseResult> projectFileResults,
        ImmutableArray<GodotTypeCandidate?> typeCandidates)
    {
        if (projectFileResults.IsDefaultOrEmpty)
            return;

        var projectResult = projectFileResults
            .OrderBy(static result => result.FilePath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        foreach (var diagnostic in projectResult.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        var godotNodeSymbol = compilation.GetTypeByMetadataName("Godot.Node");
        if (godotNodeSymbol is null)
            return;

        var autoLoadAttributeSymbol = compilation.GetTypeByMetadataName(AutoLoadAttributeMetadataName);
        var concreteCandidates = typeCandidates
            .Where(static candidate => candidate is not null)
            .Select(static candidate => candidate!)
            .ToArray();

        var typedMappings = BuildTypedMappings(
            context,
            projectResult,
            concreteCandidates,
            autoLoadAttributeSymbol,
            godotNodeSymbol);

        if (projectResult.AutoLoads.Length > 0)
        {
            var autoLoadMembers = CreateAutoLoadMembers(context, projectResult, typedMappings);
            context.AddSource(
                "GFramework_Godot_Generated_AutoLoads.g.cs",
                SourceText.From(GenerateAutoLoadSource(autoLoadMembers), Encoding.UTF8));
        }

        if (projectResult.InputActions.Length > 0)
        {
            var inputActionMembers = CreateInputActionMembers(context, projectResult);
            context.AddSource(
                "GFramework_Godot_Generated_InputActions.g.cs",
                SourceText.From(GenerateInputActionsSource(inputActionMembers), Encoding.UTF8));
        }
    }

    private static Dictionary<string, INamedTypeSymbol> BuildTypedMappings(
        SourceProductionContext context,
        ProjectMetadataParseResult projectResult,
        IReadOnlyList<GodotTypeCandidate> typeCandidates,
        INamedTypeSymbol? autoLoadAttributeSymbol,
        INamedTypeSymbol godotNodeSymbol)
    {
        var projectAutoLoadNames = new HashSet<string>(
            projectResult.AutoLoads.Select(static entry => entry.Name),
            StringComparer.Ordinal);

        var explicitMappings = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
        var implicitCandidates = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
        CollectMappingCandidates(
            context,
            typeCandidates,
            autoLoadAttributeSymbol,
            godotNodeSymbol,
            projectAutoLoadNames,
            explicitMappings,
            implicitCandidates);

        return ResolveTypedMappings(context, projectAutoLoadNames, explicitMappings, implicitCandidates);
    }

    private static void CollectMappingCandidates(
        SourceProductionContext context,
        IReadOnlyList<GodotTypeCandidate> typeCandidates,
        INamedTypeSymbol? autoLoadAttributeSymbol,
        INamedTypeSymbol godotNodeSymbol,
        ISet<string> projectAutoLoadNames,
        IDictionary<string, List<INamedTypeSymbol>> explicitMappings,
        IDictionary<string, List<INamedTypeSymbol>> implicitCandidates)
    {
        foreach (var candidate in typeCandidates)
        {
            var typeSymbol = candidate.TypeSymbol;
            var derivesFromNode = typeSymbol.IsAssignableTo(godotNodeSymbol);

            if (derivesFromNode)
            {
                if (!implicitCandidates.TryGetValue(typeSymbol.Name, out var implicitList))
                {
                    implicitList = new List<INamedTypeSymbol>();
                    implicitCandidates.Add(typeSymbol.Name, implicitList);
                }

                implicitList.Add(typeSymbol);
            }

            if (autoLoadAttributeSymbol is null)
                continue;

            var autoLoadAttribute = typeSymbol.GetAttributes()
                .FirstOrDefault(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, autoLoadAttributeSymbol));

            if (autoLoadAttribute is null)
                continue;

            if (!derivesFromNode)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GodotProjectDiagnostics.AutoLoadTypeMustDeriveFromNode,
                    candidate.ClassDeclaration.Identifier.GetLocation(),
                    typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                continue;
            }

            if (!TryGetAutoLoadName(autoLoadAttribute, out var autoLoadName))
                continue;

            if (!projectAutoLoadNames.Contains(autoLoadName))
                continue;

            if (!explicitMappings.TryGetValue(autoLoadName, out var explicitList))
            {
                explicitList = new List<INamedTypeSymbol>();
                explicitMappings.Add(autoLoadName, explicitList);
            }

            explicitList.Add(typeSymbol);
        }
    }

    private static Dictionary<string, INamedTypeSymbol> ResolveTypedMappings(
        SourceProductionContext context,
        IEnumerable<string> projectAutoLoadNames,
        IReadOnlyDictionary<string, List<INamedTypeSymbol>> explicitMappings,
        IReadOnlyDictionary<string, List<INamedTypeSymbol>> implicitCandidates)
    {
        var resolvedMappings = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);

        foreach (var projectAutoLoadName in projectAutoLoadNames.OrderBy(static name => name, StringComparer.Ordinal))
        {
            // 显式 [AutoLoad] 映射优先于按类型名推断，因为它代表了用户给出的稳定契约。
            if (explicitMappings.TryGetValue(projectAutoLoadName, out var explicitList))
            {
                var distinctExplicitTypes = DistinctTypeSymbols(explicitList);

                if (distinctExplicitTypes.Length == 1)
                {
                    resolvedMappings.Add(projectAutoLoadName, distinctExplicitTypes[0]);
                }
                else if (distinctExplicitTypes.Length > 1)
                {
                    ReportDuplicateAutoLoadMapping(context, projectAutoLoadName, distinctExplicitTypes);
                }

                continue;
            }

            if (!implicitCandidates.TryGetValue(projectAutoLoadName, out var implicitList))
                continue;

            var distinctImplicitTypes = DistinctTypeSymbols(implicitList);

            if (distinctImplicitTypes.Length == 1)
            {
                resolvedMappings.Add(projectAutoLoadName, distinctImplicitTypes[0]);
            }
            else if (distinctImplicitTypes.Length > 1)
            {
                // 隐式推断只在唯一命中时才安全；出现同名候选时改为诊断并退化成 Godot.Node。
                ReportDuplicateAutoLoadMapping(context, projectAutoLoadName, distinctImplicitTypes);
            }
        }

        return resolvedMappings;
    }

    private static bool TryGetAutoLoadName(AttributeData attribute, out string autoLoadName)
    {
        autoLoadName = string.Empty;

        if (attribute.ConstructorArguments.Length != 1 ||
            attribute.ConstructorArguments[0].Value is not string rawName ||
            string.IsNullOrWhiteSpace(rawName))
        {
            return false;
        }

        autoLoadName = rawName;
        return true;
    }

    private static void ReportDuplicateAutoLoadMapping(
        SourceProductionContext context,
        string autoLoadName,
        IEnumerable<INamedTypeSymbol> duplicateTypes)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            GodotProjectDiagnostics.DuplicateAutoLoadMapping,
            Location.None,
            autoLoadName,
            string.Join(
                ", ",
                duplicateTypes.Select(static type =>
                    type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))));
    }

    private static IReadOnlyList<GeneratedAutoLoadMember> CreateAutoLoadMembers(
        SourceProductionContext context,
        ProjectMetadataParseResult projectResult,
        IReadOnlyDictionary<string, INamedTypeSymbol> typedMappings)
    {
        var identifierCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var members = new List<GeneratedAutoLoadMember>(projectResult.AutoLoads.Length);

        foreach (var entry in projectResult.AutoLoads)
        {
            var baseIdentifier = SanitizeIdentifier(entry.Name, "AutoLoad");
            var identifier = ResolveUniqueIdentifier(
                context,
                identifierCounts,
                entry.Name,
                baseIdentifier,
                GodotProjectDiagnostics.AutoLoadIdentifierCollision);

            var typeName = typedMappings.TryGetValue(entry.Name, out var typeSymbol)
                ? typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : "global::Godot.Node";

            members.Add(new GeneratedAutoLoadMember(entry.Name, identifier, typeName, entry.ResourcePath));
        }

        return members;
    }

    private static IReadOnlyList<GeneratedInputActionMember> CreateInputActionMembers(
        SourceProductionContext context,
        ProjectMetadataParseResult projectResult)
    {
        var identifierCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var members = new List<GeneratedInputActionMember>(projectResult.InputActions.Length);

        foreach (var actionName in projectResult.InputActions)
        {
            var baseIdentifier = SanitizeIdentifier(actionName, "Action");
            var identifier = ResolveUniqueIdentifier(
                context,
                identifierCounts,
                actionName,
                baseIdentifier,
                GodotProjectDiagnostics.InputActionIdentifierCollision);

            members.Add(new GeneratedInputActionMember(actionName, identifier));
        }

        return members;
    }

    private static string ResolveUniqueIdentifier(
        SourceProductionContext context,
        IDictionary<string, int> identifierCounts,
        string originalName,
        string baseIdentifier,
        DiagnosticDescriptor collisionDiagnostic)
    {
        if (!identifierCounts.TryGetValue(baseIdentifier, out var count))
        {
            identifierCounts.Add(baseIdentifier, 1);
            return baseIdentifier;
        }

        count++;
        identifierCounts[baseIdentifier] = count;

        context.ReportDiagnostic(Diagnostic.Create(
            collisionDiagnostic,
            Location.None,
            originalName,
            baseIdentifier));

        return $"{baseIdentifier}_{count}";
    }

    private static INamedTypeSymbol[] DistinctTypeSymbols(IEnumerable<INamedTypeSymbol> types)
    {
        var results = new List<INamedTypeSymbol>();

        foreach (var type in types)
        {
            if (results.Any(existing => SymbolEqualityComparer.Default.Equals(existing, type)))
                continue;

            results.Add(type);
        }

        return results.ToArray();
    }

    private static string SanitizeIdentifier(
        string rawName,
        string fallbackPrefix)
    {
        var tokens = new List<string>();
        var tokenBuilder = new StringBuilder();

        foreach (var character in rawName)
        {
            if (char.IsLetterOrDigit(character))
            {
                tokenBuilder.Append(character);
                continue;
            }

            FlushToken(tokens, tokenBuilder);
        }

        FlushToken(tokens, tokenBuilder);

        var identifier = tokens.Count == 0
            ? fallbackPrefix
            : string.Concat(tokens);

        if (string.IsNullOrWhiteSpace(identifier))
            identifier = fallbackPrefix;

        if (!SyntaxFacts.IsIdentifierStartCharacter(identifier[0]))
            identifier = fallbackPrefix + identifier;

        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
            ? identifier + "Value"
            : identifier;
    }

    private static void FlushToken(
        ICollection<string> tokens,
        StringBuilder tokenBuilder)
    {
        if (tokenBuilder.Length == 0)
            return;

        var token = tokenBuilder.ToString();
        tokenBuilder.Clear();

        if (token.Length == 1)
        {
            tokens.Add(token.ToUpperInvariant());
            return;
        }

        tokens.Add(char.ToUpperInvariant(token[0]) + token.Substring(1));
    }

    private static string GenerateAutoLoadSource(IReadOnlyList<GeneratedAutoLoadMember> members)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {GeneratedNamespace};");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("///     提供 project.godot 中 AutoLoad 单例的强类型访问入口。");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static partial class AutoLoads");
        builder.AppendLine("{");

        foreach (var member in members)
        {
            AppendAutoLoadMemberSource(builder, member);
        }

        AppendGetRequiredNodeSource(builder);
        AppendTryGetNodeSource(builder);
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void AppendAutoLoadMemberSource(
        StringBuilder builder,
        GeneratedAutoLoadMember member)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine($"    ///     获取 AutoLoad <c>{member.AutoLoadName}</c>。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            $"    public static {member.TypeName} {member.Identifier} => GetRequiredNode<{member.TypeName}>({SymbolDisplay.FormatLiteral(member.AutoLoadName, true)});");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>");
        builder.AppendLine($"    ///     尝试获取 AutoLoad <c>{member.AutoLoadName}</c>。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine(
            $"    public static bool TryGet{member.Identifier}(out {member.TypeName}? value)");
        builder.AppendLine("    {");
        builder.AppendLine(
            $"        return TryGetNode({SymbolDisplay.FormatLiteral(member.AutoLoadName, true)}, out value);");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendGetRequiredNodeSource(StringBuilder builder)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     获取一个必填的 AutoLoad 节点；缺失时抛出异常。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <typeparam name=\"TNode\">节点类型。</typeparam>");
        builder.AppendLine("    /// <param name=\"autoLoadName\">AutoLoad 名称。</param>");
        builder.AppendLine("    /// <returns>已解析的 AutoLoad 节点。</returns>");
        builder.AppendLine("    private static TNode GetRequiredNode<TNode>(string autoLoadName)");
        builder.AppendLine("        where TNode : global::Godot.Node");
        builder.AppendLine("    {");
        builder.AppendLine("        if (TryGetNode(autoLoadName, out TNode? value))");
        builder.AppendLine("        {");
        builder.AppendLine("            return value!;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        throw new global::System.InvalidOperationException($\"AutoLoad '{autoLoadName}' is not available on the active SceneTree root.\");");
        builder.AppendLine("    }");
        builder.AppendLine();
    }

    private static void AppendTryGetNodeSource(StringBuilder builder)
    {
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    ///     尝试从当前 SceneTree 根节点解析 AutoLoad。");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    /// <typeparam name=\"TNode\">节点类型。</typeparam>");
        builder.AppendLine("    /// <param name=\"autoLoadName\">AutoLoad 名称。</param>");
        builder.AppendLine("    /// <param name=\"value\">解析到的节点实例。</param>");
        builder.AppendLine("    /// <returns>若当前进程存在 SceneTree 且根节点中能解析到该 AutoLoad，则返回 <c>true</c>。</returns>");
        builder.AppendLine("    private static bool TryGetNode<TNode>(string autoLoadName, out TNode? value)");
        builder.AppendLine("        where TNode : global::Godot.Node");
        builder.AppendLine("    {");
        builder.AppendLine("        value = default;");
        builder.AppendLine();
        builder.AppendLine("        if (global::Godot.Engine.GetMainLoop() is not global::Godot.SceneTree sceneTree)");
        builder.AppendLine("        {");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var root = sceneTree.Root;");
        builder.AppendLine("        if (root is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return false;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        value = root.GetNodeOrNull<TNode>($\"/root/{autoLoadName}\");");
        builder.AppendLine("        return value is not null;");
        builder.AppendLine("    }");
    }

    private static string GenerateInputActionsSource(IReadOnlyList<GeneratedInputActionMember> members)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {GeneratedNamespace};");
        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine("///     提供 project.godot 中 Input Action 名称的强类型常量。");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("public static partial class InputActions");
        builder.AppendLine("{");

        foreach (var member in members)
        {
            builder.AppendLine("    /// <summary>");
            builder.AppendLine($"    ///     Input Action <c>{member.ActionName}</c> 的稳定名称。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine(
                $"    public const string {member.Identifier} = {SymbolDisplay.FormatLiteral(member.ActionName, true)};");
            builder.AppendLine();
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static ProjectMetadataParseResult ParseProjectFile(
        AdditionalText file,
        CancellationToken cancellationToken)
    {
        var text = file.GetText(cancellationToken);
        if (text is null)
        {
            return new ProjectMetadataParseResult(
                file.Path,
                ImmutableArray<ProjectAutoLoadEntry>.Empty,
                ImmutableArray<string>.Empty,
                ImmutableArray<Diagnostic>.Empty);
        }

        var currentSection = string.Empty;
        var autoLoads = new List<ProjectAutoLoadEntry>();
        var inputActions = new List<string>();
        var diagnostics = new List<Diagnostic>();
        var seenAutoLoads = new HashSet<string>(StringComparer.Ordinal);
        var seenInputActions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in text.Lines)
        {
            var content = line.ToString().Trim();
            if (string.IsNullOrWhiteSpace(content) || content.StartsWith(";", StringComparison.Ordinal))
                continue;

            if (TryUpdateSection(content, ref currentSection))
                continue;

            if (!TryParseAssignment(content, out var key, out var value))
                continue;

            if (TryCollectAutoLoadEntry(file, currentSection, key, value, seenAutoLoads, autoLoads, diagnostics))
                continue;

            TryCollectInputAction(currentSection, key, seenInputActions, inputActions, diagnostics, file.Path);
        }

        return new ProjectMetadataParseResult(
            file.Path,
            autoLoads.ToImmutableArray(),
            inputActions.ToImmutableArray(),
            diagnostics.ToImmutableArray());
    }

    private static bool TryUpdateSection(string content, ref string currentSection)
    {
        if (!content.StartsWith("[", StringComparison.Ordinal) ||
            !content.EndsWith("]", StringComparison.Ordinal))
        {
            return false;
        }

        currentSection = content.Substring(1, content.Length - 2).Trim();
        return true;
    }

    private static bool TryCollectAutoLoadEntry(
        AdditionalText file,
        string currentSection,
        string key,
        string value,
        ISet<string> seenAutoLoads,
        ICollection<ProjectAutoLoadEntry> autoLoads,
        ICollection<Diagnostic> diagnostics)
    {
        if (!string.Equals(currentSection, "autoload", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!seenAutoLoads.Add(key))
        {
            diagnostics.Add(Diagnostic.Create(
                GodotProjectDiagnostics.DuplicateAutoLoadEntry,
                CreateFileLocation(file.Path),
                key));
            return true;
        }

        autoLoads.Add(new ProjectAutoLoadEntry(
            key,
            NormalizeProjectPath(value)));
        return true;
    }

    private static void TryCollectInputAction(
        string currentSection,
        string key,
        ISet<string> seenInputActions,
        ICollection<string> inputActions,
        ICollection<Diagnostic> diagnostics,
        string filePath)
    {
        if (!string.Equals(currentSection, "input", StringComparison.OrdinalIgnoreCase))
            return;

        if (!seenInputActions.Add(key))
        {
            diagnostics.Add(Diagnostic.Create(
                GodotProjectDiagnostics.DuplicateInputActionEntry,
                CreateFileLocation(filePath),
                key));
            return;
        }

        inputActions.Add(key);
    }

    private static string NormalizeProjectPath(string rawValue)
    {
        var trimmed = rawValue.Trim();

        if (trimmed.Length >= 2 &&
            trimmed[0] == '"' &&
            trimmed[trimmed.Length - 1] == '"')
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }

        return trimmed.TrimStart('*');
    }

    private static bool TryParseAssignment(
        string line,
        out string key,
        out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
            return false;

        key = line.Substring(0, separatorIndex).Trim();
        if (string.IsNullOrWhiteSpace(key))
            return false;

        value = line.Substring(separatorIndex + 1).Trim();
        return true;
    }

    private static Location CreateFileLocation(string filePath)
    {
        return Location.Create(filePath, TextSpan.FromBounds(0, 0),
            new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));
    }

    private sealed class GodotTypeCandidate
    {
        /// <summary>
        ///     创建一个类型候选。
        /// </summary>
        /// <param name="classDeclaration">类型语法节点。</param>
        /// <param name="typeSymbol">类型符号。</param>
        public GodotTypeCandidate(
            ClassDeclarationSyntax classDeclaration,
            INamedTypeSymbol typeSymbol)
        {
            ClassDeclaration = classDeclaration;
            TypeSymbol = typeSymbol;
        }

        /// <summary>
        ///     获取类型声明语法。
        /// </summary>
        public ClassDeclarationSyntax ClassDeclaration { get; }

        /// <summary>
        ///     获取类型符号。
        /// </summary>
        public INamedTypeSymbol TypeSymbol { get; }
    }

    private sealed class ProjectAutoLoadEntry
    {
        /// <summary>
        ///     初始化 AutoLoad 条目。
        /// </summary>
        /// <param name="name">AutoLoad 名称。</param>
        /// <param name="resourcePath">资源路径。</param>
        public ProjectAutoLoadEntry(
            string name,
            string resourcePath)
        {
            Name = name;
            ResourcePath = resourcePath;
        }

        /// <summary>
        ///     获取 AutoLoad 名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     获取资源路径。
        /// </summary>
        public string ResourcePath { get; }
    }

    private sealed class GeneratedAutoLoadMember
    {
        /// <summary>
        ///     初始化一个生成后的 AutoLoad 成员描述。
        /// </summary>
        /// <param name="autoLoadName">原始 AutoLoad 名称。</param>
        /// <param name="identifier">生成后的标识符。</param>
        /// <param name="typeName">类型名。</param>
        /// <param name="resourcePath">资源路径。</param>
        public GeneratedAutoLoadMember(
            string autoLoadName,
            string identifier,
            string typeName,
            string resourcePath)
        {
            AutoLoadName = autoLoadName;
            Identifier = identifier;
            TypeName = typeName;
            ResourcePath = resourcePath;
        }

        /// <summary>
        ///     获取原始 AutoLoad 名称。
        /// </summary>
        public string AutoLoadName { get; }

        /// <summary>
        ///     获取生成后的标识符。
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        ///     获取类型名。
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        ///     获取资源路径。
        /// </summary>
        public string ResourcePath { get; }
    }

    private sealed class GeneratedInputActionMember
    {
        /// <summary>
        ///     初始化一个生成后的 Input Action 成员描述。
        /// </summary>
        /// <param name="actionName">原始动作名。</param>
        /// <param name="identifier">生成后的标识符。</param>
        public GeneratedInputActionMember(
            string actionName,
            string identifier)
        {
            ActionName = actionName;
            Identifier = identifier;
        }

        /// <summary>
        ///     获取原始动作名。
        /// </summary>
        public string ActionName { get; }

        /// <summary>
        ///     获取生成后的标识符。
        /// </summary>
        public string Identifier { get; }
    }

    private sealed class ProjectMetadataParseResult
    {
        /// <summary>
        ///     初始化一个项目元数据解析结果。
        /// </summary>
        /// <param name="filePath">项目文件路径。</param>
        /// <param name="autoLoads">AutoLoad 条目。</param>
        /// <param name="inputActions">Input Action 条目。</param>
        /// <param name="diagnostics">解析过程中的诊断。</param>
        public ProjectMetadataParseResult(
            string filePath,
            ImmutableArray<ProjectAutoLoadEntry> autoLoads,
            ImmutableArray<string> inputActions,
            ImmutableArray<Diagnostic> diagnostics)
        {
            FilePath = filePath;
            AutoLoads = autoLoads;
            InputActions = inputActions;
            Diagnostics = diagnostics;
        }

        /// <summary>
        ///     获取项目文件路径。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        ///     获取 AutoLoad 条目。
        /// </summary>
        public ImmutableArray<ProjectAutoLoadEntry> AutoLoads { get; }

        /// <summary>
        ///     获取 Input Action 条目。
        /// </summary>
        public ImmutableArray<string> InputActions { get; }

        /// <summary>
        ///     获取解析过程中的诊断。
        /// </summary>
        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
