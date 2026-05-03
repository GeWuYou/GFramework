// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Godot.SourceGenerators.Behavior;

/// <summary>
///     为标记了 <c>[AutoUiPage]</c> 的 Godot CanvasItem 生成页面行为样板。
/// </summary>
[Generator]
public sealed class AutoUiPageGenerator : IIncrementalGenerator
{
    private const string AutoUiPageAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.UI.AutoUiPageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (syntaxContext, _) => Transform(syntaxContext))
            .Where(static candidate => candidate is not null);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());
        context.RegisterSourceOutput(compilationAndCandidates,
            static (spc, pair) => Execute(spc, pair.Left, pair.Right));
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration &&
               classDeclaration.AttributeLists
                   .SelectMany(static list => list.Attributes)
                   .Any(static attribute => attribute.Name.ToString().Contains("AutoUiPage", StringComparison.Ordinal));
    }

    private static TypeCandidate? Transform(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        return new TypeCandidate(classDeclaration, typeSymbol);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<TypeCandidate?> candidates)
    {
        if (candidates.IsDefaultOrEmpty)
            return;

        var autoUiPageAttribute = compilation.GetTypeByMetadataName(AutoUiPageAttributeMetadataName);
        var canvasItemType = compilation.GetTypeByMetadataName("Godot.CanvasItem");
        var uiLayerType = compilation.GetTypeByMetadataName("GFramework.Game.Abstractions.Enums.UiLayer");

        if (autoUiPageAttribute is null || canvasItemType is null || uiLayerType is null)
            return;

        foreach (var candidate in candidates.Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!))
        {
            var attribute = candidate.TypeSymbol.GetAttributes()
                .FirstOrDefault(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, autoUiPageAttribute));

            if (attribute is null)
                continue;

            if (!CanGenerateForType(context, candidate, canvasItemType, "AutoUiPage"))
                continue;

            if (candidate.TypeSymbol.ReportGeneratedMethodConflicts(
                    context,
                    candidate.ClassDeclaration.Identifier.GetLocation(),
                    "GetPage"))
            {
                continue;
            }

            if (!TryCreateSpec(context, candidate.TypeSymbol, attribute, uiLayerType, out var spec))
                continue;

            context.AddSource(GetHintName(candidate.TypeSymbol), GenerateSource(candidate.TypeSymbol, spec));
        }
    }

    private static bool CanGenerateForType(
        SourceProductionContext context,
        TypeCandidate candidate,
        INamedTypeSymbol requiredBaseType,
        string generatorName)
    {
        if (candidate.TypeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoBehaviorDiagnostics.NestedClassNotSupported,
                candidate.ClassDeclaration.Identifier.GetLocation(),
                generatorName,
                candidate.TypeSymbol.Name));
            return false;
        }

        if (!IsPartial(candidate.TypeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CommonDiagnostics.ClassMustBePartial,
                candidate.ClassDeclaration.Identifier.GetLocation(),
                candidate.TypeSymbol.Name));
            return false;
        }

        if (candidate.TypeSymbol.IsAssignableTo(requiredBaseType))
            return true;

        context.ReportDiagnostic(Diagnostic.Create(
            AutoBehaviorDiagnostics.MissingBaseType,
            candidate.ClassDeclaration.Identifier.GetLocation(),
            candidate.TypeSymbol.Name,
            requiredBaseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            generatorName));
        return false;
    }

    private static bool TryCreateSpec(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        AttributeData attribute,
        INamedTypeSymbol uiLayerType,
        out UiPageSpec spec)
    {
        spec = null!;

        if (attribute.ConstructorArguments.Length != 2 ||
            attribute.ConstructorArguments[0].Value is not string key ||
            attribute.ConstructorArguments[1].Value is not string layerName)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoBehaviorDiagnostics.InvalidAttributeArguments,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? typeSymbol.Locations.FirstOrDefault()
                ?? Location.None,
                "AutoUiPageAttribute",
                typeSymbol.Name,
                "a string key argument and a string UiLayer name argument"));
            return false;
        }

        if (!uiLayerType.GetMembers(layerName).Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoBehaviorDiagnostics.InvalidUiLayerName,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None,
                layerName,
                typeSymbol.Name));
            return false;
        }

        spec = new UiPageSpec(key, layerName);
        return true;
    }

    private static string GenerateSource(INamedTypeSymbol typeSymbol, UiPageSpec spec)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        if (ns is not null)
        {
            builder.AppendLine($"namespace {ns};");
            builder.AppendLine();
        }

        builder.AppendLine($"{GetTypeDeclarationKeyword(typeSymbol)} {GetTypeDeclarationName(typeSymbol)}");
        AppendTypeConstraints(builder, typeSymbol);
        builder.AppendLine("{");
        builder.AppendLine(
            "    private global::GFramework.Game.Abstractions.UI.IUiPageBehavior? __autoUiPageBehavior_Generated;");
        builder.AppendLine();
        builder.Append("    public static string UiKeyStr => ");
        builder.Append(SymbolDisplay.FormatLiteral(spec.Key, true));
        builder.AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("    public global::GFramework.Game.Abstractions.UI.IUiPageBehavior GetPage()");
        builder.AppendLine("    {");
        builder.AppendLine(
            $"        return __autoUiPageBehavior_Generated ??= global::GFramework.Godot.UI.UiPageBehaviorFactory.Create(this, UiKeyStr, global::GFramework.Game.Abstractions.Enums.UiLayer.{spec.LayerName});");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(static declaration =>
                declaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        var prefix = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? typeSymbol.Name
            : $"{typeSymbol.ContainingNamespace.ToDisplayString()}.{typeSymbol.Name}";
        return prefix.Replace('.', '_') + ".AutoUiPage.g.cs";
    }

    private static string GetTypeDeclarationKeyword(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.IsRecord
            ? typeSymbol.TypeKind == TypeKind.Struct ? "partial record struct" : "partial record"
            : typeSymbol.TypeKind == TypeKind.Struct
                ? "partial struct"
                : "partial class";
    }

    private static string GetTypeDeclarationName(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeParameters.Length == 0)
            return typeSymbol.Name;

        return
            $"{typeSymbol.Name}<{string.Join(", ", typeSymbol.TypeParameters.Select(static parameter => parameter.Name))}>";
    }

    private static void AppendTypeConstraints(StringBuilder builder, INamedTypeSymbol typeSymbol)
    {
        foreach (var typeParameter in typeSymbol.TypeParameters)
        {
            var constraints = new List<string>();

            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add(
                    typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                        ? "class?"
                        : "class");
            }

            if (typeParameter.HasNotNullConstraint)
                constraints.Add("notnull");

            // unmanaged implies the value-type constraint and must replace struct in generated constraints.
            if (typeParameter.HasUnmanagedTypeConstraint)
                constraints.Add("unmanaged");
            else if (typeParameter.HasValueTypeConstraint)
                constraints.Add("struct");

            constraints.AddRange(typeParameter.ConstraintTypes.Select(static constraint =>
                constraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            if (typeParameter.HasConstructorConstraint)
                constraints.Add("new()");

            if (constraints.Count == 0)
                continue;

            builder.Append("    where ");
            builder.Append(typeParameter.Name);
            builder.Append(" : ");
            builder.AppendLine(string.Join(", ", constraints));
        }
    }

    private sealed class TypeCandidate
    {
        public TypeCandidate(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol typeSymbol)
        {
            ClassDeclaration = classDeclaration;
            TypeSymbol = typeSymbol;
        }

        public ClassDeclarationSyntax ClassDeclaration { get; }

        public INamedTypeSymbol TypeSymbol { get; }
    }

    private sealed class UiPageSpec
    {
        public UiPageSpec(string key, string layerName)
        {
            Key = key;
            LayerName = layerName;
        }

        public string Key { get; }

        public string LayerName { get; }
    }
}
