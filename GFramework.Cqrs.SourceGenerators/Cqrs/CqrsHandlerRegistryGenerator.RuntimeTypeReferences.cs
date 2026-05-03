// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
public sealed partial class CqrsHandlerRegistryGenerator
{
    /// <summary>
    ///     为无法直接在生成代码中书写的关闭处理器接口构造精确的运行时注册描述。
    /// </summary>
    /// <param name="compilation">
    ///     当前生成轮次对应的编译上下文，用于判断类型是否属于当前程序集，从而决定是生成直接类型引用还是延迟到运行时反射解析。
    /// </param>
    /// <param name="handlerInterface">
    ///     需要注册的关闭处理器接口。调用方应保证它来自受支持的 CQRS 处理器接口定义，并且其泛型参数顺序与运行时注册约定一致。
    /// </param>
    /// <param name="registration">
    ///     当方法返回 <see langword="true" /> 时，包含开放泛型处理器类型和每个运行时类型实参的精确描述；
    ///     当方法返回 <see langword="false" /> 时，为默认值，调用方应回退到基于实现类型的宽松反射发现路径。
    /// </param>
    /// <returns>
    ///     当接口上的所有运行时类型引用都能在生成阶段被稳定描述时返回 <see langword="true" />；
    ///     只要任一泛型实参无法安全编码到生成输出中，就返回 <see langword="false" />。
    /// </returns>
    private static bool TryCreatePreciseReflectedRegistration(
        Compilation compilation,
        INamedTypeSymbol handlerInterface,
        out PreciseReflectedRegistrationSpec registration)
    {
        var openHandlerTypeDisplayName = handlerInterface.OriginalDefinition
            .ConstructUnboundGenericType()
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeArguments =
            ImmutableArray.CreateBuilder<RuntimeTypeReferenceSpec>(handlerInterface.TypeArguments.Length);
        foreach (var typeArgument in handlerInterface.TypeArguments)
        {
            if (!TryCreateRuntimeTypeReference(compilation, typeArgument, out var runtimeTypeReference))
            {
                registration = default;
                return false;
            }

            typeArguments.Add(runtimeTypeReference!);
        }

        registration = new PreciseReflectedRegistrationSpec(
            openHandlerTypeDisplayName,
            GetLogDisplayName(handlerInterface),
            typeArguments.ToImmutable());
        return true;
    }

    /// <summary>
    ///     将 Roslyn 类型符号转换为生成注册器可消费的运行时类型引用描述。
    /// </summary>
    /// <param name="compilation">
    ///     当前编译上下文，用于区分可直接引用的外部可访问类型与必须通过当前程序集运行时反射查找的内部类型。
    /// </param>
    /// <param name="type">
    ///     需要转换的类型符号。该方法会递归处理数组元素类型和已构造泛型的类型实参，但不会为未绑定泛型或类型参数生成引用。
    /// </param>
    /// <param name="runtimeTypeReference">
    ///     当方法返回 <see langword="true" /> 时，包含可直接引用、数组、已构造泛型或反射查找中的一种运行时表示；
    ///     当方法返回 <see langword="false" /> 时为 <see langword="null" />，调用方应回退到更宽泛的实现类型反射扫描策略。
    /// </param>
    /// <returns>
    ///     当 <paramref name="type" /> 及其递归子结构都能映射为稳定的运行时引用时返回 <see langword="true" />；
    ///     若遇到类型参数、无法访问的运行时结构，或任一递归分支无法表示，则返回 <see langword="false" />。
    /// </returns>
    private static bool TryCreateRuntimeTypeReference(
        Compilation compilation,
        ITypeSymbol type,
        out RuntimeTypeReferenceSpec? runtimeTypeReference)
    {
        // CLR forbids pointer and function-pointer types from being used as generic arguments.
        // CQRS handler contracts are generic interfaces, so emitting runtime reconstruction code for these
        // shapes would only defer the failure to MakeGenericType(...) at runtime.
        if (type is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
        {
            runtimeTypeReference = null;
            return false;
        }

        // Roslyn models dynamic as a pseudo-type, but generated C# cannot emit typeof(dynamic).
        // Normalize it to the CLR runtime type so precise reflected registrations stay compilable.
        if (type.TypeKind == TypeKind.Dynamic)
        {
            runtimeTypeReference = RuntimeTypeReferenceSpec.FromDirectReference("global::System.Object");
            return true;
        }

        if (CanReferenceFromGeneratedRegistry(compilation, type))
        {
            runtimeTypeReference = RuntimeTypeReferenceSpec.FromDirectReference(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            return true;
        }

        if (type is IArrayTypeSymbol arrayType &&
            TryCreateRuntimeTypeReference(compilation, arrayType.ElementType, out var elementTypeReference))
        {
            runtimeTypeReference = RuntimeTypeReferenceSpec.FromArray(elementTypeReference!, arrayType.Rank);
            return true;
        }

        if (type is INamedTypeSymbol genericNamedType &&
            genericNamedType.IsGenericType &&
            !genericNamedType.IsUnboundGenericType)
        {
            return TryCreateConstructedGenericRuntimeTypeReference(
                compilation,
                genericNamedType,
                out runtimeTypeReference);
        }

        if (type is INamedTypeSymbol namedType &&
            TryCreateNamedRuntimeTypeReference(compilation, namedType, out var namedTypeReference))
        {
            runtimeTypeReference = namedTypeReference;
            return true;
        }

        runtimeTypeReference = null;
        return false;
    }

    /// <summary>
    ///     为已构造泛型类型构造运行时类型引用，并递归验证每个泛型实参都可以稳定编码到生成输出中。
    /// </summary>
    /// <param name="compilation">当前生成轮次的编译上下文。</param>
    /// <param name="genericNamedType">需要表示的已构造泛型类型。</param>
    /// <param name="runtimeTypeReference">
    ///     当方法返回 <see langword="true" /> 时，包含泛型定义和泛型实参的运行时重建描述。
    /// </param>
    /// <returns>当泛型定义和全部泛型实参都能表达时返回 <see langword="true" />。</returns>
    private static bool TryCreateConstructedGenericRuntimeTypeReference(
        Compilation compilation,
        INamedTypeSymbol genericNamedType,
        out RuntimeTypeReferenceSpec? runtimeTypeReference)
    {
        if (!TryCreateGenericTypeDefinitionReference(
                compilation,
                genericNamedType,
                out var genericTypeDefinitionReference))
        {
            runtimeTypeReference = null;
            return false;
        }

        var genericTypeArguments =
            ImmutableArray.CreateBuilder<RuntimeTypeReferenceSpec>(genericNamedType.TypeArguments.Length);
        foreach (var typeArgument in genericNamedType.TypeArguments)
        {
            if (!TryCreateRuntimeTypeReference(compilation, typeArgument, out var genericTypeArgumentReference))
            {
                runtimeTypeReference = null;
                return false;
            }

            genericTypeArguments.Add(genericTypeArgumentReference!);
        }

        runtimeTypeReference = RuntimeTypeReferenceSpec.FromConstructedGeneric(
            genericTypeDefinitionReference!,
            genericTypeArguments.ToImmutable());
        return true;
    }

    /// <summary>
    ///     为无法直接书写的命名类型选择当前程序集反射查找或外部程序集反射查找表示。
    /// </summary>
    /// <param name="compilation">当前生成轮次的编译上下文。</param>
    /// <param name="namedType">需要在运行时解析的命名类型。</param>
    /// <param name="runtimeTypeReference">
    ///     当方法返回 <see langword="true" /> 时，包含适合写入生成注册器的命名类型运行时引用；
    ///     当返回 <see langword="false" /> 时，调用方应回退到更保守的注册路径。
    /// </param>
    /// <returns>当命名类型可安全编码为运行时引用时返回 <see langword="true" />。</returns>
    private static bool TryCreateNamedRuntimeTypeReference(
        Compilation compilation,
        INamedTypeSymbol namedType,
        out RuntimeTypeReferenceSpec? runtimeTypeReference)
    {
        return TryCreateReflectionLookupReference(
            compilation,
            namedType,
            GetReflectionTypeMetadataName(namedType),
            out runtimeTypeReference);
    }

    /// <summary>
    ///     为已构造泛型类型解析其泛型定义的运行时引用描述。
    /// </summary>
    /// <param name="compilation">
    ///     当前编译上下文，用于判断泛型定义是否应以内联类型引用形式生成，或在运行时通过当前程序集反射解析。
    /// </param>
    /// <param name="genericNamedType">
    ///     已构造的泛型类型。该方法只处理其原始泛型定义，不负责递归解析类型实参。
    /// </param>
    /// <param name="genericTypeDefinitionReference">
    ///     当方法返回 <see langword="true" /> 时，包含泛型定义的直接引用或反射查找描述；
    ///     当方法返回 <see langword="false" /> 时为 <see langword="null" />，调用方应停止精确构造并回退到更保守的注册路径。
    /// </param>
    /// <returns>
    ///     当泛型定义能够以稳定方式编码到生成输出中时返回 <see langword="true" />；
    ///     若泛型定义既不能直接引用，也不属于当前程序集可供反射查找，则返回 <see langword="false" />。
    /// </returns>
    private static bool TryCreateGenericTypeDefinitionReference(
        Compilation compilation,
        INamedTypeSymbol genericNamedType,
        out RuntimeTypeReferenceSpec? genericTypeDefinitionReference)
    {
        var genericTypeDefinition = genericNamedType.OriginalDefinition;
        if (CanReferenceFromGeneratedRegistry(compilation, genericTypeDefinition))
        {
            genericTypeDefinitionReference = RuntimeTypeReferenceSpec.FromDirectReference(
                genericTypeDefinition
                    .ConstructUnboundGenericType()
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            return true;
        }

        return TryCreateReflectionLookupReference(
            compilation,
            genericTypeDefinition,
            GetReflectionTypeMetadataName(genericTypeDefinition),
            out genericTypeDefinitionReference);
    }

    /// <summary>
    ///     为当前程序集或外部程序集中的命名类型构造统一的运行时反射查找描述。
    /// </summary>
    /// <param name="compilation">当前生成轮次的编译上下文。</param>
    /// <param name="namedType">需要在运行时解析的命名类型。</param>
    /// <param name="metadataName">写入生成代码的反射元数据名称。</param>
    /// <param name="runtimeTypeReference">成功时返回可直接写入注册器的运行时类型引用描述。</param>
    /// <returns>当命名类型具备可稳定编码的程序集归属信息时返回 <see langword="true" />。</returns>
    private static bool TryCreateReflectionLookupReference(
        Compilation compilation,
        INamedTypeSymbol namedType,
        string metadataName,
        out RuntimeTypeReferenceSpec? runtimeTypeReference)
    {
        if (SymbolEqualityComparer.Default.Equals(namedType.ContainingAssembly, compilation.Assembly))
        {
            runtimeTypeReference = RuntimeTypeReferenceSpec.FromReflectionLookup(metadataName);
            return true;
        }

        if (namedType.ContainingAssembly is null)
        {
            runtimeTypeReference = null;
            return false;
        }

        runtimeTypeReference = RuntimeTypeReferenceSpec.FromExternalReflectionLookup(
            namedType.ContainingAssembly.Identity.ToString(),
            metadataName);
        return true;
    }

    private static bool CanReferenceFromGeneratedRegistry(Compilation compilation, ITypeSymbol type)
    {
        // Roslyn error symbols stringify to unresolved type names; emitting them via typeof(...) would turn
        // an existing user-code error into a second generator-produced compile error instead of falling back.
        if (type.TypeKind is TypeKind.Error or TypeKind.Dynamic)
            return false;

        switch (type)
        {
            case IArrayTypeSymbol arrayType:
                return CanReferenceFromGeneratedRegistry(compilation, arrayType.ElementType);
            case INamedTypeSymbol namedType:
                if (!compilation.IsSymbolAccessibleWithin(namedType, compilation.Assembly, throughType: null))
                    return false;

                foreach (var typeArgument in namedType.TypeArguments)
                {
                    if (!CanReferenceFromGeneratedRegistry(compilation, typeArgument))
                        return false;
                }

                return true;
            case IPointerTypeSymbol:
            case IFunctionPointerTypeSymbol:
                return false;
            case ITypeParameterSymbol:
                return false;
            default:
                // Remaining Roslyn type kinds that reach this branch have already been normalized by earlier guards
                // and can continue through the direct-reference path without emitting fallback reflection code.
                return true;
        }
    }

    private static bool ContainsExternalAssemblyTypeLookup(RuntimeTypeReferenceSpec runtimeTypeReference)
    {
        if (!string.IsNullOrWhiteSpace(runtimeTypeReference.ReflectionAssemblyName))
            return true;

        if (runtimeTypeReference.ArrayElementTypeReference is not null &&
            ContainsExternalAssemblyTypeLookup(runtimeTypeReference.ArrayElementTypeReference))
        {
            return true;
        }

        if (runtimeTypeReference.GenericTypeDefinitionReference is not null &&
            ContainsExternalAssemblyTypeLookup(runtimeTypeReference.GenericTypeDefinitionReference))
        {
            return true;
        }

        foreach (var genericTypeArgument in runtimeTypeReference.GenericTypeArguments)
        {
            if (ContainsExternalAssemblyTypeLookup(genericTypeArgument))
                return true;
        }

        return false;
    }
}
