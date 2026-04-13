using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Godot.SourceGenerators.Behavior;

/// <summary>
///     为标记了 <c>[AutoScene]</c> 的 Godot 节点生成场景行为样板。
/// </summary>
[Generator]
public sealed class AutoSceneGenerator : IIncrementalGenerator
{
    private const string AutoSceneAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.AutoSceneAttribute";

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
                   .Any(static attribute => attribute.Name.ToString().Contains("AutoScene", StringComparison.Ordinal));
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

        var autoSceneAttribute = compilation.GetTypeByMetadataName(AutoSceneAttributeMetadataName);
        var godotNodeType = compilation.GetTypeByMetadataName("Godot.Node");

        if (autoSceneAttribute is null || godotNodeType is null)
            return;

        foreach (var candidate in candidates.Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!))
        {
            var attribute = candidate.TypeSymbol.GetAttributes()
                .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, autoSceneAttribute));

            if (attribute is null)
                continue;

            if (!CanGenerateForType(context, candidate, godotNodeType))
                continue;

            if (candidate.TypeSymbol.ReportGeneratedMethodConflicts(
                    context,
                    candidate.ClassDeclaration.Identifier.GetLocation(),
                    "GetScene"))
            {
                continue;
            }

            if (!TryGetSceneKey(context, candidate.TypeSymbol, attribute, out var key))
                continue;

            context.AddSource(GetHintName(candidate.TypeSymbol), GenerateSource(candidate.TypeSymbol, key));
        }
    }

    private static bool CanGenerateForType(
        SourceProductionContext context,
        TypeCandidate candidate,
        INamedTypeSymbol requiredBaseType)
    {
        if (candidate.TypeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoBehaviorDiagnostics.NestedClassNotSupported,
                candidate.ClassDeclaration.Identifier.GetLocation(),
                "AutoScene",
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
            "AutoScene"));
        return false;
    }

    private static bool TryGetSceneKey(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        AttributeData attribute,
        out string key)
    {
        key = string.Empty;

        if (attribute.ConstructorArguments.Length == 1 &&
            attribute.ConstructorArguments[0].Value is string sceneKey)
        {
            key = sceneKey;
            return true;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            AutoBehaviorDiagnostics.InvalidAttributeArguments,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
            typeSymbol.Locations.FirstOrDefault() ??
            Location.None,
            "AutoSceneAttribute",
            typeSymbol.Name,
            "a single string scene key argument"));
        return false;
    }

    private static string GenerateSource(INamedTypeSymbol typeSymbol, string key)
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
            "    private global::GFramework.Game.Abstractions.Scene.ISceneBehavior? __autoSceneBehavior_Generated;");
        builder.AppendLine();
        builder.Append("    public static string SceneKeyStr => ");
        builder.Append(SymbolDisplay.FormatLiteral(key, true));
        builder.AppendLine(";");
        builder.AppendLine();
        builder.AppendLine("    public global::GFramework.Game.Abstractions.Scene.ISceneBehavior GetScene()");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        return __autoSceneBehavior_Generated ??= global::GFramework.Godot.Scene.SceneBehaviorFactory.Create(this, SceneKeyStr);");
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
        return prefix.Replace('.', '_') + ".AutoScene.g.cs";
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
                constraints.Add("class");

            if (typeParameter.HasValueTypeConstraint)
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
}
