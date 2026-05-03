// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

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
///     否则通过 <c>GF_AutoExport_001</c> 到 <c>GF_AutoExport_008</c> 以及公共 <c>ClassMustBePartial</c> 诊断显式阻止生成。
/// </remarks>
[Generator]
public sealed class AutoRegisterExportedCollectionsGenerator : IIncrementalGenerator
{
    private const string AutoRegisterExportedCollectionsAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.UI.AutoRegisterExportedCollectionsAttribute";

    private const string RegisterExportedCollectionAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.UI.RegisterExportedCollectionAttribute";

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

        foreach (var candidate in candidates
                     .Where(static candidate => candidate is not null)
                     .Select(static candidate => candidate!)
                     .GroupBy(static candidate => candidate.TypeSymbol, SymbolEqualityComparer.Default)
                     .Select(static group => group.First()))
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
                compilation,
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
        Compilation compilation,
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

            if (!TryCreateRegistration(
                    context,
                    compilation,
                    typeSymbol,
                    member,
                    attribute,
                    enumerableType,
                    out var registration))
            {
                continue;
            }

            registrations.Add(registration);
        }

        return registrations;
    }

    private static bool TryCreateRegistration(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol ownerType,
        ISymbol collectionMember,
        AttributeData attribute,
        INamedTypeSymbol enumerableType,
        out RegistrationSpec registration)
    {
        registration = null!;

        if (!TryResolveCollectionType(context, collectionMember, enumerableType, out var collectionType))
            return false;

        if (!TryResolveRegistryTarget(
                context,
                compilation,
                ownerType,
                collectionMember,
                attribute,
                out var registryMemberName,
                out var registerMethodName,
                out var registryType))
        {
            return false;
        }

        if (!TryResolveElementType(context, collectionMember, collectionType, out var elementType))
            return false;

        if (!HasCompatibleRegisterMethod(compilation, ownerType, registryType, registerMethodName, elementType))
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

    private static bool TryResolveCollectionType(
        SourceProductionContext context,
        ISymbol collectionMember,
        INamedTypeSymbol enumerableType,
        out ITypeSymbol collectionType)
    {
        collectionType = null!;

        if (!IsInstanceReadableMember(collectionMember))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.CollectionMemberMustBeInstanceReadable,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                collectionMember.Name));
            return false;
        }

        var resolvedType = GetMemberType(collectionMember);
        if (resolvedType is null)
            return false;

        if (!resolvedType.IsAssignableTo(enumerableType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.CollectionTypeMustBeEnumerable,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                collectionMember.Name));
            return false;
        }

        collectionType = resolvedType;
        return true;
    }

    private static bool TryResolveRegistryTarget(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol ownerType,
        ISymbol collectionMember,
        AttributeData attribute,
        out string registryMemberName,
        out string registerMethodName,
        out INamedTypeSymbol registryType)
    {
        registryMemberName = string.Empty;
        registerMethodName = string.Empty;
        registryType = null!;

        if (!TryGetRegistrationAttributeArguments(
                context,
                collectionMember,
                attribute,
                out registryMemberName,
                out registerMethodName))
        {
            return false;
        }

        var registryMember = FindRegistryMember(ownerType, registryMemberName);
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

        if (!IsInstanceReadableMember(registryMember) ||
            !compilation.IsSymbolAccessibleWithin(registryMember, ownerType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.RegistryMemberMustBeInstanceReadable,
                registryMember.Locations.FirstOrDefault() ?? Location.None,
                registryMemberName,
                collectionMember.Name));
            return false;
        }

        var resolvedRegistryType = GetMemberType(registryMember) as INamedTypeSymbol;
        if (resolvedRegistryType is null)
            return false;

        registryType = resolvedRegistryType;
        return true;
    }

    private static bool TryResolveElementType(
        SourceProductionContext context,
        ISymbol collectionMember,
        ITypeSymbol collectionType,
        out ITypeSymbol elementType)
    {
        elementType = null!;

        var resolvedElementType = TryGetElementType(collectionType);
        if (resolvedElementType is null)
        {
            // Non-generic IEnumerable exposes elements as object at compile time, which is not safe
            // for validating or generating a strongly typed registry call.
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.CollectionElementTypeCouldNotBeInferred,
                collectionMember.Locations.FirstOrDefault() ?? Location.None,
                collectionMember.Name));
            return false;
        }

        elementType = resolvedElementType;
        return true;
    }

    private static bool HasCompatibleRegisterMethod(
        Compilation compilation,
        INamedTypeSymbol ownerType,
        INamedTypeSymbol registryType,
        string registerMethodName,
        ITypeSymbol elementType)
    {
        return EnumerateCandidateMethods(registryType, registerMethodName)
            .Any(method =>
                !method.IsStatic &&
                method.Parameters.Length == 1 &&
                compilation.IsSymbolAccessibleWithin(method, ownerType) &&
                CanAcceptElementType(compilation, elementType, method.Parameters[0].Type));
    }

    private static ITypeSymbol? GetMemberType(ISymbol member)
    {
        return member switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null
        };
    }

    private static bool IsInstanceReadableMember(ISymbol member)
    {
        // Generated code always reads through `this.<member>`, so only instance fields and
        // readable non-indexer instance properties are valid targets.
        return member switch
        {
            IFieldSymbol field => !field.IsStatic,
            IPropertySymbol property =>
                !property.IsStatic &&
                property.Parameters.Length == 0 &&
                property.GetMethod is not null,
            _ => false
        };
    }

    private static bool CanAcceptElementType(
        Compilation compilation,
        ITypeSymbol elementType,
        ITypeSymbol parameterType)
    {
        if (elementType.IsAssignableTo(parameterType as INamedTypeSymbol))
            return true;

        // Fall back to Roslyn's conversion rules so arrays and other non-named types are
        // validated the same way the generated invocation will be bound by the compiler.
        return compilation.ClassifyConversion(elementType, parameterType).IsImplicit;
    }

    private static ISymbol? FindRegistryMember(
        INamedTypeSymbol ownerType,
        string registryMemberName)
    {
        for (var currentType = ownerType; currentType is not null; currentType = currentType.BaseType)
        {
            // Search the owner hierarchy one level at a time so the generator follows the same
            // name-hiding order as `this.<member>` in generated code.
            var candidateMember = currentType.GetMembers(registryMemberName)
                .FirstOrDefault(static member => member is IFieldSymbol or IPropertySymbol);

            if (candidateMember is not null)
                return candidateMember;
        }

        return null;
    }

    /// <summary>
    ///     枚举给定注册表类型上可能承载批量注册入口的候选实例方法。
    /// </summary>
    /// <param name="registryType">声明注册表成员的静态类型。</param>
    /// <param name="registerMethodName">特性参数中声明的注册方法名称。</param>
    /// <returns>
    ///     按“当前类型 -> 基类链 -> 接口继承链（仅当静态类型本身是接口）”顺序返回所有同名方法，
    ///     供后续签名和可访问性筛选使用。
    /// </returns>
    /// <remarks>
    ///     生成器需要沿当前类型和基类链查找方法，因为用户代码可能通过派生类字段引用基类实现；
    ///     当注册表成员本身声明为接口类型时，还要继续沿接口继承链查找由父接口声明的契约方法。
    ///     对类或结构体不遍历 <see cref="INamedTypeSymbol.AllInterfaces"/>，避免把仅能通过接口调用的显式实现
    ///     误判为可由 <c>this.&lt;registry&gt;.&lt;method&gt;(...)</c> 直接访问的方法。
    ///     这里故意不做去重：同一个语义方法可能同时经由覆盖链、接口继承或显式声明被枚举多次，但当前调用方只使用
    ///     <c>Any</c> 判断“是否存在至少一个可用候选”，因此重复项只会带来额外的符号检查成本，不会改变生成结果或诊断边界。
    /// </remarks>
    private static IEnumerable<IMethodSymbol> EnumerateCandidateMethods(
        INamedTypeSymbol registryType,
        string registerMethodName)
    {
        // Start from the declared registry type so directly declared overloads win the cheap checks
        // before we expand into inherited declarations.
        foreach (var method in registryType.GetMembers(registerMethodName).OfType<IMethodSymbol>())
            yield return method;

        // Concrete registry types can inherit callable implementations from base classes. When the
        // registry itself is an interface, BaseType is null and this phase intentionally yields nothing.
        for (var baseType = registryType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            foreach (var method in baseType.GetMembers(registerMethodName).OfType<IMethodSymbol>())
                yield return method;
        }

        // Only interface-typed registry members should search interface inheritance. For classes or
        // structs this avoids accepting explicit interface implementations that generated code cannot
        // call through `this.<registry>.<method>(...)`. AllInterfaces is already transitive, so the
        // same semantic contract may appear multiple times; that is safe because the caller only uses Any().
        if (registryType.TypeKind != TypeKind.Interface)
            yield break;

        foreach (var interfaceType in registryType.AllInterfaces)
        {
            foreach (var method in interfaceType.GetMembers(registerMethodName).OfType<IMethodSymbol>())
                yield return method;
        }
    }

    private static bool TryGetRegistrationAttributeArguments(
        SourceProductionContext context,
        ISymbol collectionMember,
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
            context.ReportDiagnostic(Diagnostic.Create(
                AutoRegisterExportedCollectionsDiagnostics.InvalidAttributeArguments,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? collectionMember.Locations.FirstOrDefault()
                ?? Location.None,
                collectionMember.Name));
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
        return typeSymbol switch
        {
            { IsRecord: true, TypeKind: TypeKind.Struct } => "partial record struct",
            { IsRecord: true } => "partial record",
            { TypeKind: TypeKind.Struct } => "partial struct",
            { TypeKind: TypeKind.Class } => "partial class",
            { TypeKind: TypeKind.Interface } => "partial interface",
            _ => throw new NotSupportedException($"Unsupported type: {typeSymbol.TypeKind}")
        };
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
