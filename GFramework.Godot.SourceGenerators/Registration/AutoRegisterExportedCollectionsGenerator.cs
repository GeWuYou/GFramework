using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Godot.SourceGenerators.Registration;

/// <summary>
///     为导出集合生成批量注册样板方法。
/// </summary>
/// <remarks>
///     该生成器会扫描标记了 <c>AutoRegisterExportedCollectionsAttribute</c> 的 <c>partial</c> 类型，
///     为其中使用 <c>RegisterExportedCollectionAttribute</c> 声明的集合成员生成集中注册方法。
///     仅当集合可枚举、元素类型可推导、注册表成员存在且可找到兼容的实例注册方法时才会输出代码；
///     否则通过 <c>GF_AutoExport_001</c> 到 <c>GF_AutoExport_005</c> 以及公共 <c>ClassMustBePartial</c> 诊断显式阻止生成。
/// </remarks>
[Generator]
public sealed class AutoRegisterExportedCollectionsGenerator : IIncrementalGenerator
{
    private const string AutoRegisterExportedCollectionsAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.AutoRegisterExportedCollectionsAttribute";

    private const string RegisterExportedCollectionAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.RegisterExportedCollectionAttribute";

    private const string GeneratedMethodName = "__RegisterExportedCollections_Generated";

    /// <summary>
    ///     配置导出集合自动注册的增量生成管线。
    /// </summary>
    /// <param name="context">用于注册候选筛选、语义转换和最终源输出的增量生成上下文。</param>
    /// <remarks>
    ///     管线先通过语法名称筛选减少分析范围，再在输出阶段验证特性、集合形状、注册目标与方法签名。
    ///     当依赖类型无法解析时，生成器不会报告噪声诊断而是直接跳过；当用户代码违反生成约束时，会报告明确诊断并停止该类型的生成。
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
                   .Any(static attribute =>
                       attribute.Name.ToString().Contains("AutoRegisterExportedCollections", StringComparison.Ordinal));
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

        var autoRegisterAttribute =
            compilation.GetTypeByMetadataName(AutoRegisterExportedCollectionsAttributeMetadataName);
        var registerCollectionAttribute =
            compilation.GetTypeByMetadataName(RegisterExportedCollectionAttributeMetadataName);
        var enumerableType = compilation.GetTypeByMetadataName("System.Collections.IEnumerable");

        if (autoRegisterAttribute is null || registerCollectionAttribute is null || enumerableType is null)
            return;

        foreach (var candidate in candidates.Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!))
        {
            if (!candidate.TypeSymbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, autoRegisterAttribute)))
            {
                continue;
            }

            if (!CanGenerateForType(context, candidate))
                continue;

            if (candidate.TypeSymbol.ReportGeneratedMethodConflicts(
                    context,
                    candidate.ClassDeclaration.Identifier.GetLocation(),
                    GeneratedMethodName))
            {
                continue;
            }

            var registrations = CollectRegistrations(
                context,
                candidate.TypeSymbol,
                registerCollectionAttribute,
                enumerableType);

            if (registrations.Count == 0)
                continue;

            context.AddSource(GetHintName(candidate.TypeSymbol), GenerateSource(candidate.TypeSymbol, registrations));
        }
    }

    private static bool CanGenerateForType(SourceProductionContext context, TypeCandidate candidate)
    {
        if (candidate.TypeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.NestedClassNotSupported,
                candidate.ClassDeclaration.Identifier.GetLocation(),
                candidate.TypeSymbol.Name));
            return false;
        }

        if (IsPartial(candidate.TypeSymbol))
            return true;

        context.ReportDiagnostic(Diagnostic.Create(
            CommonDiagnostics.ClassMustBePartial,
            candidate.ClassDeclaration.Identifier.GetLocation(),
            candidate.TypeSymbol.Name));
        return false;
    }

    private static List<RegistrationSpec> CollectRegistrations(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol registerCollectionAttribute,
        INamedTypeSymbol enumerableType)
    {
        var registrations = new List<RegistrationSpec>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IFieldSymbol and not IPropertySymbol)
                continue;

            var attribute = member.GetAttributes()
                .FirstOrDefault(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, registerCollectionAttribute));

            if (attribute is null)
                continue;

            if (!TryCreateRegistration(context, typeSymbol, member, attribute, enumerableType, out var registration))
                continue;

            registrations.Add(registration);
        }

        return registrations;
    }

    private static bool TryCreateRegistration(
        SourceProductionContext context,
        INamedTypeSymbol ownerType,
        ISymbol collectionMember,
        AttributeData attribute,
        INamedTypeSymbol enumerableType,
        out RegistrationSpec registration)
    {
        registration = null!;

        var collectionType = collectionMember switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };

        if (collectionType is null)
            return false;

        if (!collectionType.IsAssignableTo(enumerableType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.CollectionTypeMustBeEnumerable,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                collectionMember.Name));
            return false;
        }

        if (!TryGetRegistrationAttributeArguments(attribute, out var registryMemberName, out var registerMethodName))
            return false;

        var registryMember = ownerType.GetMembers(registryMemberName)
            .FirstOrDefault(member => member is IFieldSymbol or IPropertySymbol);

        if (registryMember is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.RegistryMemberNotFound,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                registryMemberName,
                collectionMember.Name,
                ownerType.Name));
            return false;
        }

        var registryType = registryMember switch
        {
            IFieldSymbol field => field.Type as INamedTypeSymbol,
            IPropertySymbol property => property.Type as INamedTypeSymbol,
            _ => null
        };

        if (registryType is null)
            return false;

        var elementType = TryGetElementType(collectionType);
        if (elementType is null)
        {
            // Non-generic IEnumerable exposes elements as object at compile time, which is not safe
            // for validating or generating a strongly typed registry call.
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.CollectionElementTypeCouldNotBeInferred,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                collectionMember.Name));
            return false;
        }

        var hasCompatibleMethod = registryType.GetMembers(registerMethodName)
            .OfType<IMethodSymbol>()
            .Any(method =>
                !method.IsStatic &&
                method.Parameters.Length == 1 &&
                elementType.IsAssignableTo(method.Parameters[0].Type as INamedTypeSymbol));

        if (!hasCompatibleMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.RegisterMethodNotFound,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                registerMethodName,
                registryMemberName,
                collectionMember.Name));
            return false;
        }

        registration = new RegistrationSpec(collectionMember.Name, registryMemberName, registerMethodName);
        return true;
    }

    private static bool TryGetRegistrationAttributeArguments(
        AttributeData attribute,
        out string registryMemberName,
        out string registerMethodName)
    {
        registryMemberName = string.Empty;
        registerMethodName = string.Empty;

        if (attribute.ConstructorArguments.Length != 2 ||
            attribute.ConstructorArguments[0].Value is not string registryName ||
            attribute.ConstructorArguments[1].Value is not string methodName)
        {
            return false;
        }

        registryMemberName = registryName;
        registerMethodName = methodName;
        return true;
    }

    private static ITypeSymbol? TryGetElementType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
            return arrayType.ElementType;

        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            return namedType.TypeArguments[0];
        }

        var enumerableInterface = typeSymbol.AllInterfaces
            .FirstOrDefault(interfaceType =>
                interfaceType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);

        return enumerableInterface?.TypeArguments[0];
    }

    private static string GenerateSource(INamedTypeSymbol typeSymbol, IReadOnlyList<RegistrationSpec> registrations)
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
        builder.AppendLine($"    private void {GeneratedMethodName}()");
        builder.AppendLine("    {");

        foreach (var registration in registrations)
        {
            builder.Append("        if (this.");
            builder.Append(registration.CollectionMemberName);
            builder.Append(" is not null && this.");
            builder.Append(registration.RegistryMemberName);
            builder.AppendLine(" is not null)");
            builder.AppendLine("        {");
            builder.Append("            foreach (var __generatedItem in this.");
            builder.Append(registration.CollectionMemberName);
            builder.AppendLine(")");
            builder.AppendLine("            {");
            builder.Append("                this.");
            builder.Append(registration.RegistryMemberName);
            builder.Append('.');
            builder.Append(registration.RegisterMethodName);
            builder.AppendLine("(__generatedItem);");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
        }

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
        return prefix.Replace('.', '_') + ".AutoRegisterExportedCollections.g.cs";
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

    private sealed class RegistrationSpec
    {
        public RegistrationSpec(string collectionMemberName, string registryMemberName, string registerMethodName)
        {
            CollectionMemberName = collectionMemberName;
            RegistryMemberName = registryMemberName;
            RegisterMethodName = registerMethodName;
        }

        public string CollectionMemberName { get; }

        public string RegistryMemberName { get; }

        public string RegisterMethodName { get; }
    }
}
