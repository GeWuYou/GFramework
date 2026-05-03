// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;
using GFramework.SourceGenerators.Common.Info;

namespace GFramework.Core.SourceGenerators.Rule;

/// <summary>
///     为上下文感知类生成 Core 上下文 Get 注入方法。
/// </summary>
[Generator]
public sealed class ContextGetGenerator : IIncrementalGenerator
{
    private const string InjectionMethodName = "__InjectContextBindings_Generated";

    private const string GetAllAttributeMetadataName =
        $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetAllAttribute";

    private const string ContextAwareAttributeMetadataName =
        $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.ContextAwareAttribute";

    private const string IContextAwareMetadataName =
        $"{PathContests.CoreAbstractionsNamespace}.Rule.IContextAware";

    private const string ContextAwareBaseMetadataName =
        $"{PathContests.CoreNamespace}.Rule.ContextAwareBase";

    private const string IModelMetadataName =
        $"{PathContests.CoreAbstractionsNamespace}.Model.IModel";

    private const string ISystemMetadataName =
        $"{PathContests.CoreAbstractionsNamespace}.Systems.ISystem";

    private const string IUtilityMetadataName =
        $"{PathContests.CoreAbstractionsNamespace}.Utility.IUtility";

    private const string IReadOnlyListMetadataName =
        "System.Collections.Generic.IReadOnlyList`1";

    private const string GodotNodeMetadataName = "Godot.Node";

    private static readonly ImmutableArray<BindingDescriptor> BindingDescriptors =
    [
        new(
            BindingKind.Service,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetServiceAttribute",
            "GetService",
            false),
        new(
            BindingKind.Services,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetServicesAttribute",
            "GetServices",
            true),
        new(
            BindingKind.System,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetSystemAttribute",
            "GetSystem",
            false),
        new(
            BindingKind.Systems,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetSystemsAttribute",
            "GetSystems",
            true),
        new(
            BindingKind.Model,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetModelAttribute",
            "GetModel",
            false),
        new(
            BindingKind.Models,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetModelsAttribute",
            "GetModels",
            true),
        new(
            BindingKind.Utility,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetUtilityAttribute",
            "GetUtility",
            false),
        new(
            BindingKind.Utilities,
            $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetUtilitiesAttribute",
            "GetUtilities",
            true)
    ];

    private static readonly ImmutableHashSet<string> FieldCandidateAttributeNames = BindingDescriptors
        .SelectMany(static descriptor => new[]
        {
            descriptor.AttributeName,
            descriptor.AttributeName + "Attribute"
        })
        .ToImmutableHashSet(StringComparer.Ordinal);

    private static readonly ImmutableHashSet<string> TypeCandidateAttributeNames =
    [
        "GetAll",
        "GetAllAttribute"
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fieldCandidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsFieldCandidate(node),
                static (ctx, _) => TransformField(ctx))
            .Where(static candidate => candidate is not null)
            .Collect();

        var typeCandidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsTypeCandidate(node),
                static (ctx, _) => TransformType(ctx))
            .Where(static candidate => candidate is not null)
            .Collect();

        var compilationAndFields = context.CompilationProvider.Combine(fieldCandidates);
        var generationInput = compilationAndFields.Combine(typeCandidates);

        context.RegisterSourceOutput(generationInput,
            static (spc, pair) => Execute(
                spc,
                pair.Left.Left,
                pair.Left.Right,
                pair.Right));
    }

    private static bool IsFieldCandidate(SyntaxNode node)
    {
        if (node is not VariableDeclaratorSyntax
            {
                Parent: VariableDeclarationSyntax
                {
                    Parent: FieldDeclarationSyntax fieldDeclaration
                }
            })
            return false;

        return HasCandidateAttribute(fieldDeclaration.AttributeLists, FieldCandidateAttributeNames);
    }

    private static FieldCandidateInfo? TransformField(GeneratorSyntaxContext context)
    {
        if (context.Node is not VariableDeclaratorSyntax variable)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
            return null;

        return HasAnyBindingAttribute(fieldSymbol, context.SemanticModel.Compilation)
            ? new FieldCandidateInfo(variable, fieldSymbol)
            : null;
    }

    private static bool IsTypeCandidate(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
            return false;

        return HasCandidateAttribute(classDeclaration.AttributeLists, TypeCandidateAttributeNames);
    }

    private static TypeCandidateInfo? TransformType(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol typeSymbol)
            return null;

        return HasAttribute(typeSymbol, context.SemanticModel.Compilation, GetAllAttributeMetadataName)
            ? new TypeCandidateInfo(classDeclaration, typeSymbol)
            : null;
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<FieldCandidateInfo?> fieldCandidates,
        ImmutableArray<TypeCandidateInfo?> typeCandidates)
    {
        if (fieldCandidates.IsDefaultOrEmpty && typeCandidates.IsDefaultOrEmpty)
            return;

        var descriptors = ResolveBindingDescriptors(compilation);
        var getAllAttribute = compilation.GetTypeByMetadataName(GetAllAttributeMetadataName);
        if (descriptors.Length == 0 && getAllAttribute is null)
            return;

        var symbols = CreateContextSymbols(compilation);
        var workItems = CollectWorkItems(
            fieldCandidates,
            typeCandidates,
            descriptors,
            getAllAttribute);

        GenerateSources(context, descriptors, symbols, workItems);
    }

    private static ContextSymbols CreateContextSymbols(Compilation compilation)
    {
        return new ContextSymbols(
            compilation.GetTypeByMetadataName(ContextAwareAttributeMetadataName),
            compilation.GetTypeByMetadataName(IContextAwareMetadataName),
            compilation.GetTypeByMetadataName(ContextAwareBaseMetadataName),
            compilation.GetTypeByMetadataName(IModelMetadataName),
            compilation.GetTypeByMetadataName(ISystemMetadataName),
            compilation.GetTypeByMetadataName(IUtilityMetadataName),
            compilation.GetTypeByMetadataName(IReadOnlyListMetadataName),
            compilation.GetTypeByMetadataName(GodotNodeMetadataName));
    }

    private static void GenerateSources(
        SourceProductionContext context,
        ImmutableArray<ResolvedBindingDescriptor> descriptors,
        ContextSymbols symbols,
        Dictionary<INamedTypeSymbol, TypeWorkItem> workItems)
    {
        foreach (var workItem in workItems.Values)
        {
            if (!CanGenerateForType(context, workItem, symbols))
                continue;

            if (workItem.TypeSymbol.ReportGeneratedMethodConflicts(
                    context,
                    GetTypeLocation(workItem),
                    InjectionMethodName))
                continue;

            var bindings = CollectBindings(context, workItem, descriptors, symbols);
            if (bindings.Count == 0 && workItem.GetAllDeclaration is null)
                continue;

            var source = GenerateSource(workItem.TypeSymbol, bindings);
            context.AddSource(GetHintName(workItem.TypeSymbol), source);
        }
    }

    private static List<BindingInfo> CollectBindings(
        SourceProductionContext context,
        TypeWorkItem workItem,
        ImmutableArray<ResolvedBindingDescriptor> descriptors,
        ContextSymbols symbols)
    {
        var bindings = new List<BindingInfo>();
        var explicitFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);

        AddExplicitBindings(context, workItem, descriptors, symbols, bindings, explicitFields);
        AddInferredBindings(context, workItem, symbols, bindings, explicitFields);

        return bindings;
    }

    private static void AddExplicitBindings(
        SourceProductionContext context,
        TypeWorkItem workItem,
        ImmutableArray<ResolvedBindingDescriptor> descriptors,
        ContextSymbols symbols,
        ICollection<BindingInfo> bindings,
        ISet<IFieldSymbol> explicitFields)
    {
        foreach (var candidate in workItem.FieldCandidates
                     .OrderBy(static candidate => candidate.Variable.SpanStart)
                     .ThenBy(static candidate => candidate.FieldSymbol.Name, StringComparer.Ordinal))
        {
            var matches = ResolveExplicitBindings(candidate.FieldSymbol, descriptors);
            if (matches.Length == 0)
                continue;

            explicitFields.Add(candidate.FieldSymbol);

            if (matches.Length > 1)
            {
                ReportFieldDiagnostic(
                    context,
                    ContextGetDiagnostics.MultipleBindingAttributesNotSupported,
                    candidate);
                continue;
            }

            if (!TryCreateExplicitBinding(
                    context,
                    candidate,
                    matches[0],
                    symbols,
                    out var binding))
                continue;

            bindings.Add(binding);
        }
    }

    private static void AddInferredBindings(
        SourceProductionContext context,
        TypeWorkItem workItem,
        ContextSymbols symbols,
        ICollection<BindingInfo> bindings,
        ISet<IFieldSymbol> explicitFields)
    {
        if (workItem.GetAllDeclaration is null)
            return;

        foreach (var field in GetAllFields(workItem.TypeSymbol))
        {
            if (explicitFields.Contains(field))
                continue;

            // Const fields are compile-time constants, so [GetAll] should skip them explicitly instead of relying on
            // type inference to fall through implicitly.
            if (field.IsConst)
                continue;

            // Infer the target first so [GetAll] only warns for fields it would otherwise bind.
            if (!TryCreateInferredBinding(field, symbols, out var binding))
                continue;

            if (!CanApplyInferredBinding(context, field))
                continue;

            bindings.Add(binding);
        }
    }

    private static bool CanApplyInferredBinding(SourceProductionContext context, IFieldSymbol field)
    {
        if (field.IsStatic)
        {
            ReportFieldDiagnostic(
                context,
                ContextGetDiagnostics.GetAllStaticFieldSkipped,
                field);
            return false;
        }

        if (!field.IsReadOnly)
            return true;

        ReportFieldDiagnostic(
            context,
            ContextGetDiagnostics.GetAllReadOnlyFieldSkipped,
            field);
        return false;
    }

    private static bool HasCandidateAttribute(
        SyntaxList<AttributeListSyntax> attributeLists,
        ImmutableHashSet<string> candidateNames)
    {
        return attributeLists
            .SelectMany(static list => list.Attributes)
            .Any(attribute => TryGetAttributeSimpleName(attribute.Name, out var name) && candidateNames.Contains(name));
    }

    private static bool TryGetAttributeSimpleName(NameSyntax attributeName, out string name)
    {
        switch (attributeName)
        {
            case SimpleNameSyntax simpleName:
                name = simpleName.Identifier.ValueText;
                return true;

            case QualifiedNameSyntax qualifiedName:
                name = qualifiedName.Right.Identifier.ValueText;
                return true;

            case AliasQualifiedNameSyntax aliasQualifiedName:
                name = aliasQualifiedName.Name.Identifier.ValueText;
                return true;

            default:
                name = string.Empty;
                return false;
        }
    }

    private static bool HasAnyBindingAttribute(IFieldSymbol fieldSymbol, Compilation compilation)
    {
        return Enumerable.Any(BindingDescriptors,
            descriptor => HasAttribute(fieldSymbol, compilation, descriptor.MetadataName));
    }

    private static bool HasAttribute(
        ISymbol symbol,
        Compilation compilation,
        string metadataName)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(metadataName);
        return attributeSymbol is not null &&
               symbol.GetAttributes().Any(attribute =>
                   SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol));
    }

    private static Dictionary<INamedTypeSymbol, TypeWorkItem> CollectWorkItems(
        ImmutableArray<FieldCandidateInfo?> fieldCandidates,
        ImmutableArray<TypeCandidateInfo?> typeCandidates,
        ImmutableArray<ResolvedBindingDescriptor> descriptors,
        INamedTypeSymbol? getAllAttribute)
    {
        var workItems = new Dictionary<INamedTypeSymbol, TypeWorkItem>(SymbolEqualityComparer.Default);

        foreach (var candidate in fieldCandidates
                     .Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!))
        {
            if (ResolveExplicitBindings(candidate.FieldSymbol, descriptors).Length == 0)
                continue;

            var typeSymbol = candidate.FieldSymbol.ContainingType;
            if (!workItems.TryGetValue(typeSymbol, out var workItem))
            {
                workItem = new TypeWorkItem(typeSymbol);
                workItems.Add(typeSymbol, workItem);
            }

            workItem.FieldCandidates.Add(candidate);
        }

        if (getAllAttribute is null)
            return workItems;

        foreach (var candidate in typeCandidates
                     .Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!))
        {
            if (!candidate.TypeSymbol.GetAttributes().Any(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, getAllAttribute)))
                continue;

            if (!workItems.TryGetValue(candidate.TypeSymbol, out var workItem))
            {
                workItem = new TypeWorkItem(candidate.TypeSymbol);
                workItems.Add(candidate.TypeSymbol, workItem);
            }

            workItem.GetAllDeclaration ??= candidate.Declaration;
        }

        return workItems;
    }

    private static bool CanGenerateForType(
        SourceProductionContext context,
        TypeWorkItem workItem,
        ContextSymbols symbols)
    {
        if (workItem.TypeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ContextGetDiagnostics.NestedClassNotSupported,
                GetTypeLocation(workItem),
                workItem.TypeSymbol.Name));
            return false;
        }

        if (!workItem.TypeSymbol.AreAllDeclarationsPartial())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CommonDiagnostics.ClassMustBePartial,
                GetTypeLocation(workItem),
                workItem.TypeSymbol.Name));
            return false;
        }

        if (IsContextAwareType(workItem.TypeSymbol, symbols))
            return true;

        context.ReportDiagnostic(Diagnostic.Create(
            ContextGetDiagnostics.ContextAwareTypeRequired,
            GetTypeLocation(workItem),
            workItem.TypeSymbol.Name));
        return false;
    }

    private static bool IsContextAwareType(
        INamedTypeSymbol typeSymbol,
        ContextSymbols symbols)
    {
        if (symbols.ContextAwareAttribute is not null &&
            typeSymbol.GetAttributes().Any(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.ContextAwareAttribute)))
            return true;

        return typeSymbol.IsAssignableTo(symbols.IContextAware) ||
               typeSymbol.IsAssignableTo(symbols.ContextAwareBase);
    }

    private static ImmutableArray<ResolvedBindingDescriptor> ResolveBindingDescriptors(Compilation compilation)
    {
        var builder = ImmutableArray.CreateBuilder<ResolvedBindingDescriptor>(BindingDescriptors.Length);

        foreach (var descriptor in BindingDescriptors)
        {
            var attributeSymbol = compilation.GetTypeByMetadataName(descriptor.MetadataName);
            if (attributeSymbol is null)
                continue;

            builder.Add(new ResolvedBindingDescriptor(descriptor, attributeSymbol));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<ResolvedBindingDescriptor> ResolveExplicitBindings(
        IFieldSymbol fieldSymbol,
        ImmutableArray<ResolvedBindingDescriptor> descriptors)
    {
        if (descriptors.IsDefaultOrEmpty)
            return [];

        var builder = ImmutableArray.CreateBuilder<ResolvedBindingDescriptor>();

        foreach (var descriptor in descriptors.Where(descriptor => fieldSymbol.GetAttributes().Any(attribute =>
                     SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, descriptor.AttributeSymbol))))
        {
            builder.Add(descriptor);
        }

        return builder.ToImmutable();
    }

    private static bool TryCreateExplicitBinding(
        SourceProductionContext context,
        FieldCandidateInfo candidate,
        ResolvedBindingDescriptor descriptor,
        ContextSymbols symbols,
        out BindingInfo binding)
    {
        binding = default;

        if (candidate.FieldSymbol.IsStatic)
        {
            ReportFieldDiagnostic(
                context,
                ContextGetDiagnostics.StaticFieldNotSupported,
                candidate);
            return false;
        }

        if (candidate.FieldSymbol.IsReadOnly)
        {
            ReportFieldDiagnostic(
                context,
                ContextGetDiagnostics.ReadOnlyFieldNotSupported,
                candidate);
            return false;
        }

        if (!TryResolveBindingTarget(candidate.FieldSymbol.Type, descriptor.Definition.Kind, symbols,
                out var targetType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ContextGetDiagnostics.InvalidBindingType,
                candidate.Variable.Identifier.GetLocation(),
                candidate.FieldSymbol.Name,
                candidate.FieldSymbol.Type.ToDisplayString(),
                descriptor.Definition.AttributeName));
            return false;
        }

        binding = new BindingInfo(candidate.FieldSymbol, descriptor.Definition.Kind, targetType);
        return true;
    }

    private static bool TryCreateInferredBinding(
        IFieldSymbol fieldSymbol,
        ContextSymbols symbols,
        out BindingInfo binding)
    {
        binding = default;

        if (symbols.GodotNode is not null && fieldSymbol.Type.IsAssignableTo(symbols.GodotNode))
            return false;

        if (TryResolveCollectionElement(fieldSymbol.Type, symbols.IReadOnlyList, out var elementType))
        {
            if (elementType.IsAssignableTo(symbols.IModel))
            {
                binding = new BindingInfo(fieldSymbol, BindingKind.Models, elementType);
                return true;
            }

            if (elementType.IsAssignableTo(symbols.ISystem))
            {
                binding = new BindingInfo(fieldSymbol, BindingKind.Systems, elementType);
                return true;
            }

            if (elementType.IsAssignableTo(symbols.IUtility))
            {
                binding = new BindingInfo(fieldSymbol, BindingKind.Utilities, elementType);
                return true;
            }

            // Service collections stay opt-in for the same reason as single services.
            return false;
        }

        if (fieldSymbol.Type.IsAssignableTo(symbols.IModel))
        {
            binding = new BindingInfo(fieldSymbol, BindingKind.Model, fieldSymbol.Type);
            return true;
        }

        if (fieldSymbol.Type.IsAssignableTo(symbols.ISystem))
        {
            binding = new BindingInfo(fieldSymbol, BindingKind.System, fieldSymbol.Type);
            return true;
        }

        if (fieldSymbol.Type.IsAssignableTo(symbols.IUtility))
        {
            binding = new BindingInfo(fieldSymbol, BindingKind.Utility, fieldSymbol.Type);
            return true;
        }

        // Service bindings stay opt-in because arbitrary reference types are too ambiguous to infer safely.
        return false;
    }

    private static bool TryResolveBindingTarget(
        ITypeSymbol fieldType,
        BindingKind kind,
        ContextSymbols symbols,
        out ITypeSymbol targetType)
    {
        targetType = null!;

        switch (kind)
        {
            case BindingKind.Service:
                if (!fieldType.IsReferenceType)
                    return false;

                targetType = fieldType;
                return true;

            case BindingKind.Model:
                if (!fieldType.IsAssignableTo(symbols.IModel))
                    return false;

                targetType = fieldType;
                return true;

            case BindingKind.System:
                if (!fieldType.IsAssignableTo(symbols.ISystem))
                    return false;

                targetType = fieldType;
                return true;

            case BindingKind.Utility:
                if (!fieldType.IsAssignableTo(symbols.IUtility))
                    return false;

                targetType = fieldType;
                return true;

            case BindingKind.Services:
                return TryResolveReferenceCollection(fieldType, symbols.IReadOnlyList, out targetType);

            case BindingKind.Models:
                return TryResolveConstrainedCollection(fieldType, symbols.IReadOnlyList, symbols.IModel,
                    out targetType);

            case BindingKind.Systems:
                return TryResolveConstrainedCollection(fieldType, symbols.IReadOnlyList, symbols.ISystem,
                    out targetType);

            case BindingKind.Utilities:
                return TryResolveConstrainedCollection(fieldType, symbols.IReadOnlyList, symbols.IUtility,
                    out targetType);

            default:
                return false;
        }
    }

    private static bool TryResolveReferenceCollection(
        ITypeSymbol fieldType,
        INamedTypeSymbol? readOnlyList,
        out ITypeSymbol elementType)
    {
        elementType = null!;

        if (!TryResolveCollectionElement(fieldType, readOnlyList, out var candidate))
            return false;

        if (!candidate.IsReferenceType)
            return false;

        elementType = candidate;
        return true;
    }

    private static bool TryResolveConstrainedCollection(
        ITypeSymbol fieldType,
        INamedTypeSymbol? readOnlyList,
        INamedTypeSymbol? constraintType,
        out ITypeSymbol elementType)
    {
        elementType = null!;

        if (!TryResolveCollectionElement(fieldType, readOnlyList, out var candidate))
            return false;

        if (!candidate.IsAssignableTo(constraintType))
            return false;

        elementType = candidate;
        return true;
    }

    private static bool TryResolveCollectionElement(
        ITypeSymbol fieldType,
        INamedTypeSymbol? readOnlyList,
        out ITypeSymbol elementType)
    {
        elementType = null!;

        if (readOnlyList is null || fieldType is not INamedTypeSymbol targetType)
            return false;

        foreach (var candidateType in EnumerateCollectionTypeCandidates(targetType))
        {
            if (candidateType.TypeArguments.Length != 1)
                continue;

            var candidateElementType = candidateType.TypeArguments[0];
            var expectedSourceType = readOnlyList.Construct(candidateElementType);
            if (!expectedSourceType.IsAssignableTo(targetType))
                continue;

            elementType = candidateElementType;
            return true;
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateCollectionTypeCandidates(INamedTypeSymbol typeSymbol)
    {
        yield return typeSymbol;

        foreach (var interfaceType in typeSymbol.AllInterfaces)
            yield return interfaceType;
    }

    private static IEnumerable<IFieldSymbol> GetAllFields(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(static field => !field.IsImplicitlyDeclared)
            .OrderBy(static field => field.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
            .ThenBy(static field => field.Name, StringComparer.Ordinal);
    }

    private static void ReportFieldDiagnostic(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        FieldCandidateInfo candidate)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            candidate.Variable.Identifier.GetLocation(),
            candidate.FieldSymbol.Name));
    }

    private static void ReportFieldDiagnostic(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        IFieldSymbol fieldSymbol)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            fieldSymbol.Locations.FirstOrDefault() ?? Location.None,
            fieldSymbol.Name));
    }

    private static Location GetTypeLocation(TypeWorkItem workItem)
    {
        if (workItem.GetAllDeclaration is not null)
            return workItem.GetAllDeclaration.Identifier.GetLocation();

        return workItem.FieldCandidates[0].Variable.Identifier.GetLocation();
    }

    private static string GenerateSource(
        INamedTypeSymbol typeSymbol,
        IReadOnlyList<BindingInfo> bindings)
    {
        var namespaceName = typeSymbol.GetNamespace();
        var generics = typeSymbol.ResolveGenerics();
        var orderedBindings = bindings
            .OrderBy(static binding => binding.Field.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
            .ThenBy(static binding => binding.Field.Name, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using GFramework.Core.Extensions;");
        sb.AppendLine();

        if (namespaceName is not null)
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {typeSymbol.Name}{generics.Parameters}");
        foreach (var constraint in generics.Constraints)
            sb.AppendLine($"    {constraint}");

        sb.AppendLine("{");
        sb.AppendLine($"    private void {InjectionMethodName}()");
        sb.AppendLine("    {");

        foreach (var binding in orderedBindings)
        {
            var targetType = binding.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.AppendLine($"        {binding.Field.Name} = {ResolveAccessor(binding.Kind, targetType)};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string ResolveAccessor(BindingKind kind, string targetType)
    {
        return kind switch
        {
            BindingKind.Service => $"this.GetService<{targetType}>()",
            BindingKind.Services => $"this.GetServices<{targetType}>()",
            BindingKind.System => $"this.GetSystem<{targetType}>()",
            BindingKind.Systems => $"this.GetSystems<{targetType}>()",
            BindingKind.Model => $"this.GetModel<{targetType}>()",
            BindingKind.Models => $"this.GetModels<{targetType}>()",
            BindingKind.Utility => $"this.GetUtility<{targetType}>()",
            BindingKind.Utilities => $"this.GetUtilities<{targetType}>()",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        var hintName = typeSymbol.GetNamespace() is { Length: > 0 } namespaceName
            ? $"{namespaceName}.{typeSymbol.GetFullClassName()}"
            : typeSymbol.GetFullClassName();

        return hintName.Replace('.', '_') + ".ContextGet.g.cs";
    }

    private enum BindingKind
    {
        Service,
        Services,
        System,
        Systems,
        Model,
        Models,
        Utility,
        Utilities
    }

    private sealed record BindingDescriptor(
        BindingKind Kind,
        string MetadataName,
        string AttributeName,
        bool IsCollection);

    private readonly record struct ResolvedBindingDescriptor(
        BindingDescriptor Definition,
        INamedTypeSymbol AttributeSymbol);

    private readonly record struct BindingInfo(
        IFieldSymbol Field,
        BindingKind Kind,
        ITypeSymbol TargetType);

    private readonly record struct ContextSymbols(
        INamedTypeSymbol? ContextAwareAttribute,
        INamedTypeSymbol? IContextAware,
        INamedTypeSymbol? ContextAwareBase,
        INamedTypeSymbol? IModel,
        INamedTypeSymbol? ISystem,
        INamedTypeSymbol? IUtility,
        INamedTypeSymbol? IReadOnlyList,
        INamedTypeSymbol? GodotNode);

    private sealed class TypeWorkItem(INamedTypeSymbol typeSymbol)
    {
        public INamedTypeSymbol TypeSymbol { get; } = typeSymbol;
        public List<FieldCandidateInfo> FieldCandidates { get; } = [];
        public ClassDeclarationSyntax? GetAllDeclaration { get; set; }
    }
}
