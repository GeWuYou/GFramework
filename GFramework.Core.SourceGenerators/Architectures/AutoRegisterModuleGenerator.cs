using GFramework.Core.SourceGenerators.Abstractions.Architectures;
using GFramework.Core.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Core.SourceGenerators.Architectures;

/// <summary>
///     为标记了 <see cref="AutoRegisterModuleAttribute" /> 的模块生成固定顺序的组件注册代码。
/// </summary>
[Generator]
public sealed class AutoRegisterModuleGenerator : IIncrementalGenerator
{
    private const string AutoRegisterModuleAttributeMetadataName =
        $"{PathContests.SourceGeneratorsAbstractionsPath}.Architectures.AutoRegisterModuleAttribute";

    private const string RegisterModelAttributeMetadataName =
        $"{PathContests.SourceGeneratorsAbstractionsPath}.Architectures.RegisterModelAttribute";

    private const string RegisterSystemAttributeMetadataName =
        $"{PathContests.SourceGeneratorsAbstractionsPath}.Architectures.RegisterSystemAttribute";

    private const string RegisterUtilityAttributeMetadataName =
        $"{PathContests.SourceGeneratorsAbstractionsPath}.Architectures.RegisterUtilityAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (syntaxContext, _) => Transform(syntaxContext))
            .Where(static candidate => candidate is not null);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(
            compilationAndCandidates,
            static (spc, pair) => Execute(spc, pair.Left, pair.Right));
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration &&
               classDeclaration.AttributeLists
                   .SelectMany(static list => list.Attributes)
                   .Any(static attribute =>
                       attribute.Name.ToString().Contains("AutoRegisterModule", StringComparison.Ordinal));
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

        var autoRegisterModuleAttribute = compilation.GetTypeByMetadataName(AutoRegisterModuleAttributeMetadataName);
        var registerModelAttribute = compilation.GetTypeByMetadataName(RegisterModelAttributeMetadataName);
        var registerSystemAttribute = compilation.GetTypeByMetadataName(RegisterSystemAttributeMetadataName);
        var registerUtilityAttribute = compilation.GetTypeByMetadataName(RegisterUtilityAttributeMetadataName);
        var architectureType =
            compilation.GetTypeByMetadataName($"{PathContests.CoreAbstractionsNamespace}.Architectures.IArchitecture");
        var modelType = compilation.GetTypeByMetadataName($"{PathContests.CoreAbstractionsNamespace}.Model.IModel");
        var systemType = compilation.GetTypeByMetadataName($"{PathContests.CoreAbstractionsNamespace}.Systems.ISystem");
        var utilityType =
            compilation.GetTypeByMetadataName($"{PathContests.CoreAbstractionsNamespace}.Utility.IUtility");

        if (autoRegisterModuleAttribute is null ||
            registerModelAttribute is null ||
            registerSystemAttribute is null ||
            registerUtilityAttribute is null ||
            architectureType is null ||
            modelType is null ||
            systemType is null ||
            utilityType is null)
        {
            return;
        }

        foreach (var candidate in candidates.Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!))
        {
            if (!candidate.TypeSymbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, autoRegisterModuleAttribute)))
            {
                continue;
            }

            if (!CanGenerateForType(context, candidate))
                continue;

            if (HasInstallConflict(context, candidate, architectureType))
                continue;

            var registrations = CollectRegistrations(
                context,
                candidate.TypeSymbol,
                registerModelAttribute,
                registerSystemAttribute,
                registerUtilityAttribute,
                modelType,
                systemType,
                utilityType);

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
                AutoRegisterModuleDiagnostics.NestedClassNotSupported,
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

    private static bool HasInstallConflict(
        SourceProductionContext context,
        TypeCandidate candidate,
        INamedTypeSymbol architectureType)
    {
        var installMethod = candidate.TypeSymbol.GetMembers("Install")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method =>
                !method.IsImplicitlyDeclared &&
                method.Parameters.Length == 1 &&
                method.TypeParameters.Length == 0 &&
                method.ReturnsVoid &&
                method.Parameters[0].Type is ITypeSymbol parameterType &&
                SymbolEqualityComparer.Default.Equals(parameterType, architectureType));

        if (installMethod is null)
            return false;

        context.ReportDiagnostic(Diagnostic.Create(
            AutoRegisterModuleDiagnostics.InstallMethodConflict,
            installMethod.Locations.FirstOrDefault() ?? candidate.ClassDeclaration.Identifier.GetLocation(),
            candidate.TypeSymbol.Name));
        return true;
    }

    private static List<RegistrationSpec> CollectRegistrations(
        SourceProductionContext context,
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol registerModelAttribute,
        INamedTypeSymbol registerSystemAttribute,
        INamedTypeSymbol registerUtilityAttribute,
        INamedTypeSymbol modelType,
        INamedTypeSymbol systemType,
        INamedTypeSymbol utilityType)
    {
        var registrations = new List<RegistrationSpec>();

        foreach (var attribute in typeSymbol.GetAttributes()
                     // Roslyn 会把 partial 类型上的属性合并到同一个集合中。
                     // 先按语法树标识排序，才能让每个文件内的 Span.Start 成为可比较的稳定顺序键。
                     .OrderBy(GetAttributeSyntaxTreeOrderKey, StringComparer.Ordinal)
                     .ThenBy(GetAttributeOrder)
                     .ThenBy(GetAttributeTypeOrderKey, StringComparer.Ordinal))
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, registerModelAttribute))
            {
                if (TryCreateRegistration(
                        context,
                        typeSymbol,
                        attribute,
                        "RegisterModelAttribute",
                        modelType,
                        RegistrationKind.Model,
                        out var registration))
                {
                    registrations.Add(registration);
                }

                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, registerSystemAttribute))
            {
                if (TryCreateRegistration(
                        context,
                        typeSymbol,
                        attribute,
                        "RegisterSystemAttribute",
                        systemType,
                        RegistrationKind.System,
                        out var registration))
                {
                    registrations.Add(registration);
                }

                continue;
            }

            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, registerUtilityAttribute))
                continue;

            if (TryCreateRegistration(
                    context,
                    typeSymbol,
                    attribute,
                    "RegisterUtilityAttribute",
                    utilityType,
                    RegistrationKind.Utility,
                    out var utilityRegistration))
            {
                registrations.Add(utilityRegistration);
            }
        }

        return registrations;
    }

    private static bool TryCreateRegistration(
        SourceProductionContext context,
        INamedTypeSymbol ownerType,
        AttributeData attribute,
        string attributeDisplayName,
        INamedTypeSymbol expectedInterface,
        RegistrationKind registrationKind,
        out RegistrationSpec registration)
    {
        registration = default;

        if (attribute.ConstructorArguments.Length != 1 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol componentType)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterModuleDiagnostics.RegistrationTypeRequired,
                GetAttributeLocation(attribute),
                attributeDisplayName,
                ownerType.Name));
            return false;
        }

        if (!componentType.IsAssignableTo(expectedInterface))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterModuleDiagnostics.RegistrationTypeMustImplementExpectedInterface,
                GetAttributeLocation(attribute),
                componentType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                ownerType.Name,
                expectedInterface.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return false;
        }

        if (componentType.IsAbstract ||
            !componentType.InstanceConstructors.Any(ctor =>
                ctor.Parameters.Length == 0 &&
                ctor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterModuleDiagnostics.RegistrationTypeMustHaveParameterlessConstructor,
                GetAttributeLocation(attribute),
                componentType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                ownerType.Name));
            return false;
        }

        registration = new RegistrationSpec(
            registrationKind,
            componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        return true;
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
        builder.AppendLine(
            $"    public void Install(global::{PathContests.CoreAbstractionsNamespace}.Architectures.IArchitecture architecture)");
        builder.AppendLine("    {");

        foreach (var registration in registrations)
        {
            builder.Append("        architecture.");
            builder.Append(registration.Kind switch
            {
                RegistrationKind.Model => "RegisterModel",
                RegistrationKind.System => "RegisterSystem",
                RegistrationKind.Utility => "RegisterUtility",
                _ => throw new ArgumentOutOfRangeException(nameof(registration.Kind))
            });
            builder.Append("(new ");
            builder.Append(registration.ComponentTypeDisplayName);
            builder.AppendLine("());");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        var prefix = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? typeSymbol.Name
            : $"{typeSymbol.ContainingNamespace.ToDisplayString()}.{typeSymbol.Name}";
        return prefix.Replace('.', '_') + ".AutoRegisterModule.g.cs";
    }

    private static bool IsPartial(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(static declaration =>
                declaration.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)));
    }

    private static int GetAttributeOrder(AttributeData attribute)
    {
        return attribute.ApplicationSyntaxReference?.Span.Start ?? int.MaxValue;
    }

    private static string GetAttributeSyntaxTreeOrderKey(AttributeData attribute)
    {
        var syntaxTree = attribute.ApplicationSyntaxReference?.SyntaxTree;
        if (syntaxTree is null)
            return string.Empty;

        if (!string.IsNullOrEmpty(syntaxTree.FilePath))
            return syntaxTree.FilePath;

        // In-memory compilations may not assign file paths. Fall back to the syntax tree text so
        // attributes from different partial declarations still get a deterministic cross-file order.
        return syntaxTree.ToString();
    }

    private static string GetAttributeTypeOrderKey(AttributeData attribute)
    {
        return attribute.ConstructorArguments.FirstOrDefault().Value is INamedTypeSymbol componentType
            ? componentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : string.Empty;
    }

    private static Location GetAttributeLocation(AttributeData attribute)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
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

    private sealed record TypeCandidate(ClassDeclarationSyntax ClassDeclaration, INamedTypeSymbol TypeSymbol);

    private readonly record struct RegistrationSpec(RegistrationKind Kind, string ComponentTypeDisplayName);

    private enum RegistrationKind
    {
        Model,
        System,
        Utility
    }
}
