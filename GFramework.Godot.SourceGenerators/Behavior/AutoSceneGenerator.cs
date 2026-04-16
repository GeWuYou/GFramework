using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Godot.SourceGenerators.Behavior;

/// <summary>
///     为标记了 <c>[AutoScene]</c> 的 Godot 节点生成场景行为样板。
/// </summary>
/// <remarks>
///     该生成器会为兼容的非嵌套 <c>partial</c> Godot 节点类型生成 <c>SceneKeyStr</c> 与 <c>GetScene</c>，
///     以便通过 <c>SceneBehaviorFactory</c> 延迟创建并缓存场景行为实例。
///     生成管线仅处理显式标记了 <c>AutoSceneAttribute</c> 的类，并在类型不满足基类、<c>partial</c>、
///     成员冲突或属性参数约束时通过诊断停止生成，而不是静默回退到不完整输出。
/// </remarks>
[Generator]
public sealed class AutoSceneGenerator : IIncrementalGenerator
{
    private const string AutoSceneAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.UI.AutoSceneAttribute";

    private static readonly string[] GeneratedMemberNames =
    [
        "SceneKeyStr",
        "__autoSceneBehavior_Generated"
    ];

    /// <summary>
    ///     配置 <c>AutoScene</c> 的增量生成管线。
    /// </summary>
    /// <param name="context">用于注册语法筛选、语义转换和源输出阶段的增量生成上下文。</param>
    /// <remarks>
    ///     管线首先通过语法节点名称快速筛选潜在候选，再结合语义模型确认类型符号。
    ///     最终输出阶段仅在 <c>AutoSceneAttribute</c>、<c>Godot.Node</c> 等依赖可解析且目标类型满足生成约束时产出源码；
    ///     否则会报告对应诊断，或在宿主依赖缺失时直接跳过生成。
    /// </remarks>
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

            if (ReportGeneratedMemberConflicts(
                    context,
                    candidate.TypeSymbol,
                    candidate.ClassDeclaration.Identifier.GetLocation(),
                    GeneratedMemberNames))
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

    /// <summary>
    ///     报告与生成器保留成员名冲突的字段或属性，避免生成代码出现重复成员编译错误。
    /// </summary>
    /// <param name="context">用于上报诊断的源代码生成上下文。</param>
    /// <param name="typeSymbol">当前待生成的类型符号。</param>
    /// <param name="fallbackLocation">冲突成员无定位信息时的后备位置。</param>
    /// <param name="memberNames">需要校验的生成器保留成员名集合。</param>
    /// <returns>存在任意冲突时返回 <c>true</c>。</returns>
    private static bool ReportGeneratedMemberConflicts(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        Location fallbackLocation,
        string[] memberNames)
    {
        var hasConflict = false;

        foreach (var memberName in memberNames)
        {
            var conflict = typeSymbol.GetMembers(memberName)
                .FirstOrDefault(member =>
                    !member.IsImplicitlyDeclared &&
                    member is IPropertySymbol or IFieldSymbol);

            if (conflict is null)
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                CommonDiagnostics.GeneratedMethodNameConflict,
                conflict.Locations.FirstOrDefault() ?? fallbackLocation,
                typeSymbol.Name,
                memberName));
            hasConflict = true;
        }

        return hasConflict;
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
