// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text;
using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Godot.SourceGenerators;

/// <summary>
///     为带有 <c>[GetNode]</c> 的字段生成 Godot 节点获取逻辑。
/// </summary>
[Generator]
public sealed class GetNodeGenerator : IIncrementalGenerator
{
    private const string GodotAbsolutePathPrefix = "/";
    private const string GodotUniqueNamePrefix = "%";

    private const string GetNodeAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.GetNodeAttribute";

    private const string GetNodeLookupModeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.NodeLookupMode";

    private const string InjectionMethodName = "__InjectGetNodes_Generated";
    private const string ReadyHookMethodName = "OnGetNodeReadyGenerated";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (ctx, _) => Transform(ctx))
            .Where(static candidate => candidate is not null);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(compilationAndCandidates,
            static (spc, pair) => Execute(spc, pair.Left, pair.Right));
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        if (node is not VariableDeclaratorSyntax
            {
                Parent: VariableDeclarationSyntax
                {
                    Parent: FieldDeclarationSyntax fieldDeclaration
                }
            })
            return false;

        return fieldDeclaration.AttributeLists
            .SelectMany(static list => list.Attributes)
            .Any(static attribute => attribute.Name.ToString().Contains("GetNode", StringComparison.Ordinal));
    }

    private static FieldCandidate? Transform(GeneratorSyntaxContext context)
    {
        if (context.Node is not VariableDeclaratorSyntax variable)
            return null;

        if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, variable) is not IFieldSymbol fieldSymbol)
            return null;

        return new FieldCandidate(variable, fieldSymbol);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<FieldCandidate?> candidates)
    {
        if (candidates.IsDefaultOrEmpty)
            return;

        var getNodeAttribute = compilation.GetTypeByMetadataName(GetNodeAttributeMetadataName);
        var godotNodeSymbol = compilation.GetTypeByMetadataName("Godot.Node");

        if (getNodeAttribute is null || godotNodeSymbol is null)
            return;

        var fieldCandidates = candidates
            .Where(static candidate => candidate is not null)
            .Select(static candidate => candidate!)
            .Where(candidate => ResolveAttribute(candidate.FieldSymbol, getNodeAttribute) is not null)
            .ToList();

        foreach (var group in GroupByContainingType(fieldCandidates))
        {
            var typeSymbol = group.TypeSymbol;

            if (!CanGenerateForType(context, group, typeSymbol))
                continue;

            if (typeSymbol.ReportGeneratedMethodConflicts(
                    context,
                    group.Fields[0].Variable.Identifier.GetLocation(),
                    InjectionMethodName))
                continue;

            var bindings = new List<NodeBindingInfo>();

            foreach (var candidate in group.Fields)
            {
                var attribute = ResolveAttribute(candidate.FieldSymbol, getNodeAttribute);
                if (attribute is null)
                    continue;

                if (!TryCreateBinding(context, candidate, attribute, godotNodeSymbol, out var binding))
                    continue;

                bindings.Add(binding);
            }

            if (bindings.Count == 0)
                continue;

            ReportMissingReadyHookCall(context, group, typeSymbol);

            var source = GenerateSource(typeSymbol, bindings, FindReadyMethod(typeSymbol) is null);
            context.AddSource(GetHintName(typeSymbol), source);
        }
    }

    private static bool CanGenerateForType(
        SourceProductionContext context,
        TypeGroup group,
        INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                GetNodeDiagnostics.NestedClassNotSupported,
                group.Fields[0].Variable.Identifier.GetLocation(),
                typeSymbol.Name));
            return false;
        }

        if (IsPartial(typeSymbol))
            return true;

        context.ReportDiagnostic(Diagnostic.Create(
            CommonDiagnostics.ClassMustBePartial,
            group.Fields[0].Variable.Identifier.GetLocation(),
            typeSymbol.Name));

        return false;
    }

    private static bool TryCreateBinding(
        SourceProductionContext context,
        FieldCandidate candidate,
        AttributeData attribute,
        INamedTypeSymbol godotNodeSymbol,
        out NodeBindingInfo binding)
    {
        binding = default!;

        if (candidate.FieldSymbol.IsStatic)
        {
            ReportFieldDiagnostic(context,
                GetNodeDiagnostics.StaticFieldNotSupported,
                candidate);
            return false;
        }

        if (candidate.FieldSymbol.IsReadOnly)
        {
            ReportFieldDiagnostic(context,
                GetNodeDiagnostics.ReadOnlyFieldNotSupported,
                candidate);
            return false;
        }

        if (!IsGodotNodeType(candidate.FieldSymbol.Type, godotNodeSymbol))
        {
            ReportFieldDiagnostic(context,
                GetNodeDiagnostics.FieldTypeMustDeriveFromNode,
                candidate);
            return false;
        }

        if (!TryResolvePath(candidate.FieldSymbol, attribute, out var path))
        {
            ReportFieldDiagnostic(context,
                GetNodeDiagnostics.CannotInferNodePath,
                candidate);
            return false;
        }

        binding = new NodeBindingInfo(
            candidate.FieldSymbol,
            path,
            ResolveRequired(attribute));

        return true;
    }

    private static void ReportFieldDiagnostic(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        FieldCandidate candidate)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            candidate.Variable.Identifier.GetLocation(),
            candidate.FieldSymbol.Name));
    }

    private static void ReportMissingReadyHookCall(
        SourceProductionContext context,
        TypeGroup group,
        INamedTypeSymbol typeSymbol)
    {
        var readyMethod = FindReadyMethod(typeSymbol);
        if (readyMethod is null || CallsGeneratedInjection(readyMethod))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            GetNodeDiagnostics.ManualReadyHookRequired,
            readyMethod.Locations.FirstOrDefault() ?? group.Fields[0].Variable.Identifier.GetLocation(),
            typeSymbol.Name));
    }

    private static AttributeData? ResolveAttribute(
        IFieldSymbol fieldSymbol,
        INamedTypeSymbol getNodeAttribute)
    {
        return fieldSymbol.GetAttributes()
            .FirstOrDefault(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, getNodeAttribute));
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(static declaration =>
                declaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
    }

    private static bool IsGodotNodeType(ITypeSymbol typeSymbol, INamedTypeSymbol godotNodeSymbol)
    {
        var current = typeSymbol as INamedTypeSymbol;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, godotNodeSymbol) ||
                SymbolEqualityComparer.Default.Equals(current, godotNodeSymbol))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static IMethodSymbol? FindReadyMethod(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(static method => IsParameterlessInstanceMethod(method, "_Ready"));
    }

    private static bool CallsGeneratedInjection(IMethodSymbol readyMethod)
    {
        foreach (var syntaxReference in readyMethod.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
                continue;

            if (methodSyntax.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(IsGeneratedInjectionInvocation))
                return true;
        }

        return false;
    }

    private static bool IsGeneratedInjectionInvocation(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case IdentifierNameSyntax identifierName:
                return string.Equals(
                    identifierName.Identifier.ValueText,
                    InjectionMethodName,
                    StringComparison.Ordinal);
            case MemberAccessExpressionSyntax memberAccess:
                return string.Equals(
                    memberAccess.Name.Identifier.ValueText,
                    InjectionMethodName,
                    StringComparison.Ordinal);
            default:
                return false;
        }
    }

    private static bool ResolveRequired(AttributeData attribute)
    {
        return attribute.GetNamedArgument("Required", true);
    }

    private static bool IsParameterlessInstanceMethod(IMethodSymbol method, string methodName)
    {
        return string.Equals(method.Name, methodName, StringComparison.Ordinal) &&
               !method.IsStatic &&
               method.Parameters.Length == 0 &&
               method.MethodKind == MethodKind.Ordinary;
    }

    private static bool TryResolvePath(
        IFieldSymbol fieldSymbol,
        AttributeData attribute,
        out string path)
    {
        var explicitPath = ResolveExplicitPath(attribute);
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return ReturnResolvedPath(explicitPath!, out path);

        var inferredName = InferNodeName(fieldSymbol.Name);
        if (string.IsNullOrWhiteSpace(inferredName))
        {
            path = string.Empty;
            return false;
        }

        var resolvedName = inferredName!;
        return TryResolveInferredPath(attribute, resolvedName, out path);
    }

    private static bool ReturnResolvedPath(string resolvedPath, out string path)
    {
        path = resolvedPath;
        return true;
    }

    private static bool TryResolveInferredPath(
        AttributeData attribute,
        string inferredName,
        out string path)
    {
        path = BuildPathPrefix(ResolveLookup(attribute)) + inferredName;
        return true;
    }

    private static string BuildPathPrefix(NodeLookupModeValue lookupMode)
    {
        switch (lookupMode)
        {
            case NodeLookupModeValue.RelativePath:
                return string.Empty;
            case NodeLookupModeValue.AbsolutePath:
                return GodotAbsolutePathPrefix;
            default:
                return GodotUniqueNamePrefix;
        }
    }

    private static string? ResolveExplicitPath(AttributeData attribute)
    {
        var namedPath = attribute.GetNamedArgument<string>("Path");
        if (!string.IsNullOrWhiteSpace(namedPath))
            return namedPath;

        if (attribute.ConstructorArguments.Length == 0)
            return null;

        return attribute.ConstructorArguments[0].Value as string;
    }

    private static NodeLookupModeValue ResolveLookup(AttributeData attribute)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (!string.Equals(namedArgument.Key, "Lookup", StringComparison.Ordinal))
                continue;

            if (!string.Equals(
                    namedArgument.Value.Type?.ToDisplayString(),
                    GetNodeLookupModeMetadataName,
                    StringComparison.Ordinal))
                continue;

            if (namedArgument.Value.Value is int value)
                return (NodeLookupModeValue)value;
        }

        return NodeLookupModeValue.Auto;
    }

    private static string? InferNodeName(string fieldName)
    {
        var workingName = fieldName.TrimStart('_');
        if (workingName.StartsWith("m_", StringComparison.OrdinalIgnoreCase))
            workingName = workingName.Substring(2);

        workingName = workingName.TrimStart('_');
        if (string.IsNullOrWhiteSpace(workingName))
            return null;

        if (workingName.IndexOfAny(['_', '-', ' ']) >= 0)
        {
            var parts = workingName
                .Split(['_', '-', ' '], StringSplitOptions.RemoveEmptyEntries);

            return parts.Length == 0
                ? null
                : string.Concat(parts.Select(ToPascalToken));
        }

        return ToPascalToken(workingName);
    }

    private static string ToPascalToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return token;

        if (token.Length == 1)
            return token.ToUpperInvariant();

        return char.ToUpperInvariant(token[0]) + token.Substring(1);
    }

    private static string GenerateSource(
        INamedTypeSymbol typeSymbol,
        IReadOnlyList<NodeBindingInfo> bindings,
        bool generateReadyOverride)
    {
        var namespaceName = typeSymbol.GetNamespace();
        var generics = typeSymbol.ResolveGenerics();

        var sb = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendLine("#nullable enable");

        if (namespaceName is not null)
        {
            sb.AppendLine()
                .AppendLine($"namespace {namespaceName};");
        }

        sb.AppendLine()
            .AppendLine($"partial class {typeSymbol.Name}{generics.Parameters}");

        foreach (var constraint in generics.Constraints)
            sb.AppendLine($"    {constraint}");

        sb.AppendLine("{")
            .AppendLine($"    private void {InjectionMethodName}()")
            .AppendLine("    {");

        foreach (var binding in bindings)
        {
            var typeName = binding.FieldSymbol.Type
                .WithNullableAnnotation(NullableAnnotation.None)
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var accessor = binding.Required ? "GetNode" : "GetNodeOrNull";
            var pathLiteral = EscapeStringLiteral(binding.Path);
            sb.AppendLine(
                $"        {binding.FieldSymbol.Name} = {accessor}<{typeName}>(\"{pathLiteral}\");");
        }

        sb.AppendLine("    }");

        if (generateReadyOverride)
        {
            sb.AppendLine()
                .AppendLine($"    partial void {ReadyHookMethodName}();")
                .AppendLine()
                .AppendLine("    public override void _Ready()")
                .AppendLine("    {")
                .AppendLine($"        {InjectionMethodName}();")
                .AppendLine($"        {ReadyHookMethodName}();")
                .AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", string.Empty)
            .Replace(".", "_") + ".GetNode.g.cs";
    }

    private static string EscapeStringLiteral(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private static IReadOnlyList<TypeGroup> GroupByContainingType(IEnumerable<FieldCandidate> candidates)
    {
        var groupMap = new Dictionary<INamedTypeSymbol, TypeGroup>(SymbolEqualityComparer.Default);
        var orderedGroups = new List<TypeGroup>();

        foreach (var candidate in candidates)
        {
            var typeSymbol = candidate.FieldSymbol.ContainingType;
            if (!groupMap.TryGetValue(typeSymbol, out var group))
            {
                group = new TypeGroup(typeSymbol);
                groupMap.Add(typeSymbol, group);
                orderedGroups.Add(group);
            }

            group.Fields.Add(candidate);
        }

        return orderedGroups;
    }

    private sealed class FieldCandidate
    {
        public FieldCandidate(
            VariableDeclaratorSyntax variable,
            IFieldSymbol fieldSymbol)
        {
            Variable = variable;
            FieldSymbol = fieldSymbol;
        }

        public VariableDeclaratorSyntax Variable { get; }

        public IFieldSymbol FieldSymbol { get; }
    }

    private sealed class NodeBindingInfo
    {
        public NodeBindingInfo(
            IFieldSymbol fieldSymbol,
            string path,
            bool required)
        {
            FieldSymbol = fieldSymbol;
            Path = path;
            Required = required;
        }

        public IFieldSymbol FieldSymbol { get; }

        public string Path { get; }

        public bool Required { get; }
    }

    private enum NodeLookupModeValue
    {
        Auto = 0,
        UniqueName = 1,
        RelativePath = 2,
        AbsolutePath = 3
    }

    private sealed class TypeGroup
    {
        public TypeGroup(INamedTypeSymbol typeSymbol)
        {
            TypeSymbol = typeSymbol;
        }

        public INamedTypeSymbol TypeSymbol { get; }

        public List<FieldCandidate> Fields { get; } = new();
    }
}
