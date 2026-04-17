using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
[Generator]
public sealed class CqrsHandlerRegistryGenerator : IIncrementalGenerator
{
    private const string CqrsContractsNamespace = $"{PathContests.CqrsAbstractionsNamespace}.Cqrs";
    private const string CqrsRuntimeNamespace = PathContests.CqrsNamespace;
    private const string LoggingNamespace = $"{PathContests.CoreAbstractionsNamespace}.Logging";
    private const string IRequestHandlerMetadataName = $"{CqrsContractsNamespace}.IRequestHandler`2";
    private const string INotificationHandlerMetadataName = $"{CqrsContractsNamespace}.INotificationHandler`1";
    private const string IStreamRequestHandlerMetadataName = $"{CqrsContractsNamespace}.IStreamRequestHandler`2";
    private const string ICqrsHandlerRegistryMetadataName = $"{CqrsRuntimeNamespace}.ICqrsHandlerRegistry";

    private const string CqrsHandlerRegistryAttributeMetadataName =
        $"{CqrsRuntimeNamespace}.CqrsHandlerRegistryAttribute";

    private const string CqrsReflectionFallbackAttributeMetadataName =
        $"{CqrsRuntimeNamespace}.CqrsReflectionFallbackAttribute";

    private const string ILoggerMetadataName = $"{LoggingNamespace}.ILogger";
    private const string IServiceCollectionMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string GeneratedNamespace = "GFramework.Generated.Cqrs";
    private const string GeneratedTypeName = "__GFrameworkGeneratedCqrsHandlerRegistry";
    private const string HintName = "CqrsHandlerRegistry.g.cs";

    private static readonly DiagnosticDescriptor MissingReflectionFallbackContractDiagnostic = new(
        "GF_Cqrs_001",
        "Cannot emit CQRS registry without reflection fallback contract",
        "Cannot generate CQRS handler registry because fallback metadata is required for handler(s): {0}, but runtime contract '{1}' is unavailable",
        "GFramework.Cqrs.SourceGenerators",
        DiagnosticSeverity.Error,
        true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationEnvironment = context.CompilationProvider
            .Select(static (compilation, _) => CreateGenerationEnvironment(compilation));

        // Restrict semantic analysis to type declarations that can actually contribute implemented interfaces.
        var handlerCandidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsHandlerCandidate(node),
                static (syntaxContext, _) => TransformHandlerCandidate(syntaxContext))
            .Where(static candidate => candidate is not null)
            .Collect();

        context.RegisterSourceOutput(
            generationEnvironment.Combine(handlerCandidates),
            static (productionContext, pair) => Execute(productionContext, pair.Left, pair.Right));
    }

    private static GenerationEnvironment CreateGenerationEnvironment(Compilation compilation)
    {
        var generationEnabled = compilation.GetTypeByMetadataName(IRequestHandlerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(INotificationHandlerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(IStreamRequestHandlerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(ICqrsHandlerRegistryMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(
                                    CqrsHandlerRegistryAttributeMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(ILoggerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(IServiceCollectionMetadataName) is not null;
        var supportsReflectionFallbackAttribute =
            compilation.GetTypeByMetadataName(CqrsReflectionFallbackAttributeMetadataName) is not null;

        return new GenerationEnvironment(generationEnabled, supportsReflectionFallbackAttribute);
    }

    private static bool IsHandlerCandidate(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax
        {
            BaseList.Types.Count: > 0
        };
    }

    private static HandlerCandidateAnalysis? TransformHandlerCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol type)
            return null;

        if (!IsConcreteHandlerType(type))
            return null;

        var handlerInterfaces = type.AllInterfaces
            .Where(IsSupportedHandlerInterface)
            .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
            .ToImmutableArray();

        if (handlerInterfaces.IsDefaultOrEmpty)
            return null;

        var implementationTypeDisplayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationLogName = GetLogDisplayName(type);
        var canReferenceImplementation = CanReferenceFromGeneratedRegistry(context.SemanticModel.Compilation, type);
        var registrations = ImmutableArray.CreateBuilder<HandlerRegistrationSpec>(handlerInterfaces.Length);
        var reflectedImplementationRegistrations =
            ImmutableArray.CreateBuilder<ReflectedImplementationRegistrationSpec>(handlerInterfaces.Length);
        var preciseReflectedRegistrations =
            ImmutableArray.CreateBuilder<PreciseReflectedRegistrationSpec>(handlerInterfaces.Length);
        string? reflectionFallbackHandlerTypeMetadataName = null;
        foreach (var handlerInterface in handlerInterfaces)
        {
            var canReferenceHandlerInterface =
                CanReferenceFromGeneratedRegistry(context.SemanticModel.Compilation, handlerInterface);
            if (canReferenceImplementation && canReferenceHandlerInterface)
            {
                registrations.Add(new HandlerRegistrationSpec(
                    handlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    implementationTypeDisplayName,
                    GetLogDisplayName(handlerInterface),
                    implementationLogName));
                continue;
            }

            if (!canReferenceImplementation && canReferenceHandlerInterface)
            {
                reflectedImplementationRegistrations.Add(new ReflectedImplementationRegistrationSpec(
                    handlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetLogDisplayName(handlerInterface)));
                continue;
            }

            if (TryCreatePreciseReflectedRegistration(
                    context.SemanticModel.Compilation,
                    handlerInterface,
                    out var preciseReflectedRegistration))
            {
                preciseReflectedRegistrations.Add(preciseReflectedRegistration);
                continue;
            }

            // Concrete closed handler contracts should now always map to either direct registrations,
            // reflected implementation registrations, or precise runtime type references.
            // If a future Roslyn type shape still slips through this net, keep the generator conservative:
            // preserve the static registrations we do understand, and let the runtime recover the remaining
            // interfaces via the existing assembly-level targeted reflection fallback contract.
            reflectionFallbackHandlerTypeMetadataName ??= GetReflectionTypeMetadataName(type);
        }

        return new HandlerCandidateAnalysis(
            implementationTypeDisplayName,
            implementationLogName,
            registrations.ToImmutable(),
            reflectedImplementationRegistrations.ToImmutable(),
            preciseReflectedRegistrations.ToImmutable(),
            canReferenceImplementation ? null : GetReflectionTypeMetadataName(type),
            reflectionFallbackHandlerTypeMetadataName);
    }

    /// <summary>
    ///     执行 CQRS handler registry 生成管线的最终发射阶段，负责将候选 handler 分析结果汇总为单个
    ///     <c>CqrsHandlerRegistry.g.cs</c>，并在需要时附带程序集级 reflection fallback 元数据。
    /// </summary>
    /// <param name="context">用于报告诊断并发射生成源码的源生产上下文。</param>
    /// <param name="generationEnvironment">
    ///     当前编译轮次可用的 runtime 合同快照。
    ///     只有当 CQRS 注册器生成所需的基础契约齐备时，才允许继续生成；当存在
    ///     <c>CqrsReflectionFallbackAttribute</c> 时，才允许输出依赖 fallback 元数据恢复的注册结果。
    /// </param>
    /// <param name="candidates">
    ///     来自语法和语义分析阶段的 handler 候选结果。
    ///     集合中可能包含 <see langword="null" /> 占位项，且同一实现类型可能因 partial 声明重复出现，后续会统一去重并聚合。
    /// </param>
    /// <remarks>
    ///     <para>
    ///         该方法负责发射两类生成结果：注册器类型本体，以及在静态类型信息不足时用于运行时补全注册的程序集级
    ///         <c>CqrsReflectionFallbackAttribute</c> 元数据。生成这些结果的目标是把可静态确定的 handler 注册尽量前移到编译期，
    ///         从而减少运行时程序集扫描成本，同时保留对少数复杂类型形态的兼容回退路径。
    ///     </para>
    ///     <para>
    ///         该阶段依赖两个语义前提：一是 runtime 已提供 CQRS 注册器生成所需的基础合同；二是只要存在任何 handler
    ///         需要通过 reflection fallback 恢复，就必须同时存在承载该元数据的
    ///         <c>CqrsReflectionFallbackAttribute</c>。如果基础合同缺失，生成器会静默跳过本轮发射；如果候选集合去重后没有任何可注册
    ///         handler，也会直接跳过 <c>AddSource</c>，避免输出空注册器。
    ///     </para>
    ///     <para>
    ///         当 fallback handler 元数据非空但 runtime 缺少 <c>CqrsReflectionFallbackAttribute</c> 时，
    ///         该方法会报告 <c>GF_Cqrs_001</c> 并停止发射源码。这样可以避免生成一个表面可用、但会静默漏掉部分 handler 注册的半成品
    ///         registry。只有在静态注册结果与 fallback 契约同时成立时，才允许调用 <c>AddSource</c>。
    ///     </para>
    /// </remarks>
    private static void Execute(
        SourceProductionContext context,
        GenerationEnvironment generationEnvironment,
        ImmutableArray<HandlerCandidateAnalysis?> candidates)
    {
        if (!generationEnvironment.GenerationEnabled)
            return;

        var registrations = CollectRegistrations(candidates);

        if (registrations.Count == 0)
            return;

        var fallbackHandlerTypeMetadataNames = registrations
            .Select(static registration => registration.ReflectionFallbackHandlerTypeMetadataName)
            .Where(static typeMetadataName => !string.IsNullOrWhiteSpace(typeMetadataName))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();

        if (!CanEmitGeneratedRegistry(
                generationEnvironment.SupportsReflectionFallbackAttribute,
                fallbackHandlerTypeMetadataNames.Length))
        {
            ReportMissingReflectionFallbackContractDiagnostic(
                context,
                fallbackHandlerTypeMetadataNames);
            return;
        }

        context.AddSource(
            HintName,
            GenerateSource(generationEnvironment, registrations, fallbackHandlerTypeMetadataNames));
    }

    /// <summary>
    ///     判断当前轮次是否允许输出生成注册器。
    /// </summary>
    /// <param name="supportsReflectionFallbackAttribute">
    ///     runtime 合同中是否存在 <c>CqrsReflectionFallbackAttribute</c>，以承载生成器无法静态精确表达的 handler 回退元数据。
    /// </param>
    /// <param name="fallbackHandlerTypeCount">
    ///     当前轮次需要依赖程序集级 reflection fallback 元数据恢复的 handler 数量。
    /// </param>
    /// <returns>
    ///     当没有 handler 依赖 fallback，或 runtime 已提供承载该元数据的特性契约时返回 <see langword="true" />；
    ///     否则返回 <see langword="false" />，调用方必须放弃生成以避免输出会静默漏注册的半成品注册器。
    /// </returns>
    private static bool CanEmitGeneratedRegistry(
        bool supportsReflectionFallbackAttribute,
        int fallbackHandlerTypeCount)
    {
        return fallbackHandlerTypeCount == 0 || supportsReflectionFallbackAttribute;
    }

    /// <summary>
    ///     报告当前轮次因缺少 fallback 元数据承载契约而无法安全生成注册器的诊断。
    /// </summary>
    /// <param name="context">源生产上下文。</param>
    /// <param name="fallbackHandlerTypeMetadataNames">需要通过程序集级 reflection fallback 元数据恢复的 handler 元数据名称。</param>
    private static void ReportMissingReflectionFallbackContractDiagnostic(
        SourceProductionContext context,
        IReadOnlyList<string> fallbackHandlerTypeMetadataNames)
    {
        var handlerList = string.Join(
            ", ",
            fallbackHandlerTypeMetadataNames.OrderBy(static name => name, StringComparer.Ordinal));
        context.ReportDiagnostic(Diagnostic.Create(
            MissingReflectionFallbackContractDiagnostic,
            Location.None,
            handlerList,
            CqrsReflectionFallbackAttributeMetadataName));
    }

    private static List<ImplementationRegistrationSpec> CollectRegistrations(
        ImmutableArray<HandlerCandidateAnalysis?> candidates)
    {
        var registrations = new List<ImplementationRegistrationSpec>();

        // Partial declarations surface the same symbol through multiple syntax nodes.
        // Collapse them by implementation type so direct and reflected registrations stay stable and duplicate-free.
        var uniqueCandidates = new Dictionary<string, HandlerCandidateAnalysis>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            uniqueCandidates[candidate.Value.ImplementationTypeDisplayName] = candidate.Value;
        }

        foreach (var candidate in uniqueCandidates.Values)
        {
            registrations.Add(new ImplementationRegistrationSpec(
                candidate.ImplementationTypeDisplayName,
                candidate.ImplementationLogName,
                candidate.Registrations,
                candidate.ReflectedImplementationRegistrations,
                candidate.PreciseReflectedRegistrations,
                candidate.ReflectionTypeMetadataName,
                candidate.ReflectionFallbackHandlerTypeMetadataName));
        }

        registrations.Sort(static (left, right) =>
        {
            var implementationComparison = StringComparer.Ordinal.Compare(
                left.ImplementationLogName,
                right.ImplementationLogName);

            return implementationComparison;
        });

        return registrations;
    }

    private static bool IsConcreteHandlerType(INamedTypeSymbol type)
    {
        return type.TypeKind is TypeKind.Class or TypeKind.Struct &&
               !type.IsAbstract &&
               !ContainsGenericParameters(type);
    }

    private static bool ContainsGenericParameters(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
                return true;
        }

        return false;
    }

    private static bool IsSupportedHandlerInterface(INamedTypeSymbol interfaceType)
    {
        if (!interfaceType.IsGenericType)
            return false;

        var definitionMetadataName = GetFullyQualifiedMetadataName(interfaceType.OriginalDefinition);
        return string.Equals(definitionMetadataName, IRequestHandlerMetadataName, StringComparison.Ordinal) ||
               string.Equals(definitionMetadataName, INotificationHandlerMetadataName, StringComparison.Ordinal) ||
               string.Equals(definitionMetadataName, IStreamRequestHandlerMetadataName, StringComparison.Ordinal);
    }

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
            !genericNamedType.IsUnboundGenericType &&
            TryCreateGenericTypeDefinitionReference(compilation, genericNamedType,
                out var genericTypeDefinitionReference))
        {
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

        if (type is INamedTypeSymbol namedType)
        {
            if (SymbolEqualityComparer.Default.Equals(namedType.ContainingAssembly, compilation.Assembly))
            {
                runtimeTypeReference = RuntimeTypeReferenceSpec.FromReflectionLookup(
                    GetReflectionTypeMetadataName(namedType));
                return true;
            }

            runtimeTypeReference = RuntimeTypeReferenceSpec.FromExternalReflectionLookup(
                namedType.ContainingAssembly.Identity.ToString(),
                GetReflectionTypeMetadataName(namedType));
            return true;
        }

        runtimeTypeReference = null;
        return false;
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

        if (SymbolEqualityComparer.Default.Equals(genericTypeDefinition.ContainingAssembly, compilation.Assembly))
        {
            genericTypeDefinitionReference = RuntimeTypeReferenceSpec.FromReflectionLookup(
                GetReflectionTypeMetadataName(genericTypeDefinition));
            return true;
        }

        genericTypeDefinitionReference = RuntimeTypeReferenceSpec.FromExternalReflectionLookup(
            genericTypeDefinition.ContainingAssembly.Identity.ToString(),
            GetReflectionTypeMetadataName(genericTypeDefinition));
        return true;
    }

    private static bool CanReferenceFromGeneratedRegistry(Compilation compilation, ITypeSymbol type)
    {
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
            case IPointerTypeSymbol pointerType:
                return CanReferenceFromGeneratedRegistry(compilation, pointerType.PointedAtType);
            case ITypeParameterSymbol:
                return false;
            default:
                // Treat other Roslyn type kinds, such as dynamic or unresolved error types, as referenceable for now.
                // If a real-world case proves unsafe, tighten this branch instead of broadening the named-type path above.
                return true;
        }
    }

    private static string GetFullyQualifiedMetadataName(INamedTypeSymbol type)
    {
        var nestedTypes = new Stack<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            nestedTypes.Push(current.MetadataName);
        }

        var builder = new StringBuilder();
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append(type.ContainingNamespace.ToDisplayString());
            builder.Append('.');
        }

        while (nestedTypes.Count > 0)
        {
            builder.Append(nestedTypes.Pop());
            if (nestedTypes.Count > 0)
                builder.Append('.');
        }

        return builder.ToString();
    }

    private static string GetReflectionTypeMetadataName(INamedTypeSymbol type)
    {
        var nestedTypes = new Stack<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            nestedTypes.Push(current.MetadataName);
        }

        var builder = new StringBuilder();
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            builder.Append(type.ContainingNamespace.ToDisplayString());
            builder.Append('.');
        }

        var isFirstType = true;
        while (nestedTypes.Count > 0)
        {
            if (!isFirstType)
                builder.Append('+');

            builder.Append(nestedTypes.Pop());
            isFirstType = false;
        }

        return builder.ToString();
    }

    private static string GetTypeSortKey(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string GetLogDisplayName(ITypeSymbol type)
    {
        return GetTypeSortKey(type).Replace("global::", string.Empty);
    }

    /// <summary>
    ///     生成程序集级 CQRS handler 注册器源码。
    /// </summary>
    /// <param name="generationEnvironment">
    ///     当前轮次的生成环境，用于决定 runtime 是否提供 <c>CqrsReflectionFallbackAttribute</c> 契约，以及是否需要在输出中发射对应的程序集级元数据。
    /// </param>
    /// <param name="registrations">
    ///     已整理并排序的 handler 注册描述。方法会据此生成 <c>CqrsHandlerRegistry.g.cs</c>，其中包含直接注册、实现类型反射注册、精确运行时类型查找等分支。
    /// </param>
    /// <param name="fallbackHandlerTypeMetadataNames">
    ///     仍需依赖程序集级 reflection fallback 元数据恢复的 handler 元数据名称集合。
    ///     调用方必须先确保：若该集合非空，则 <paramref name="generationEnvironment" /> 已声明支持对应的 fallback attribute 契约；
    ///     否则应在进入本方法前报告诊断并放弃生成，而不是输出会静默漏注册的半成品注册器。
    /// </param>
    /// <returns>完整的注册器源代码文本。</returns>
    /// <remarks>
    ///     当 <paramref name="fallbackHandlerTypeMetadataNames" /> 为空时，输出只包含程序集级 <c>CqrsHandlerRegistryAttribute</c> 和注册器实现。
    ///     当其非空且 runtime 合同可用时，输出还会附带程序集级 <c>CqrsReflectionFallbackAttribute</c>，让运行时补齐生成阶段无法精确表达的剩余 handler。
    ///     该方法本身不报告诊断；“fallback 必需但 runtime 契约缺失”的错误由调用方在进入本方法前处理。
    /// </remarks>
    private static string GenerateSource(
        GenerationEnvironment generationEnvironment,
        IReadOnlyList<ImplementationRegistrationSpec> registrations,
        IReadOnlyList<string> fallbackHandlerTypeMetadataNames)
    {
        var hasReflectedImplementationRegistrations = registrations.Any(static registration =>
            !registration.ReflectedImplementationRegistrations.IsDefaultOrEmpty);
        var hasPreciseReflectedRegistrations = registrations.Any(static registration =>
            !registration.PreciseReflectedRegistrations.IsDefaultOrEmpty);
        var hasExternalAssemblyTypeLookups = registrations.Any(static registration =>
            registration.PreciseReflectedRegistrations.Any(static preciseRegistration =>
                preciseRegistration.ServiceTypeArguments.Any(ContainsExternalAssemblyTypeLookup)));
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        if (generationEnvironment.SupportsReflectionFallbackAttribute && fallbackHandlerTypeMetadataNames.Count > 0)
        {
            builder.Append("[assembly: global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".CqrsReflectionFallbackAttribute(");
            for (var index = 0; index < fallbackHandlerTypeMetadataNames.Count; index++)
            {
                if (index > 0)
                    builder.Append(", ");

                builder.Append('"');
                builder.Append(EscapeStringLiteral(fallbackHandlerTypeMetadataNames[index]));
                builder.Append('"');
            }

            builder.AppendLine(")]");
            builder.AppendLine();
        }

        builder.Append("[assembly: global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.Append(".CqrsHandlerRegistryAttribute(typeof(global::");
        builder.Append(GeneratedNamespace);
        builder.Append('.');
        builder.Append(GeneratedTypeName);
        builder.AppendLine("))]");

        builder.AppendLine();
        builder.Append("namespace ");
        builder.Append(GeneratedNamespace);
        builder.AppendLine(";");
        builder.AppendLine();
        builder.Append("internal sealed class ");
        builder.Append(GeneratedTypeName);
        builder.Append(" : global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.AppendLine(".ICqrsHandlerRegistry");
        builder.AppendLine("{");
        builder.Append(
            "    public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::");
        builder.Append(LoggingNamespace);
        builder.AppendLine(".ILogger logger)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (services is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(services));");
        builder.AppendLine("        if (logger is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(logger));");
        if (hasReflectedImplementationRegistrations || hasPreciseReflectedRegistrations ||
            registrations.Any(static registration =>
                !string.IsNullOrWhiteSpace(registration.ReflectionTypeMetadataName)))
        {
            builder.AppendLine();
            builder.Append("        var registryAssembly = typeof(global::");
            builder.Append(GeneratedNamespace);
            builder.Append('.');
            builder.Append(GeneratedTypeName);
            builder.AppendLine(").Assembly;");
        }

        if (registrations.Count > 0)
            builder.AppendLine();

        for (var registrationIndex = 0; registrationIndex < registrations.Count; registrationIndex++)
        {
            var registration = registrations[registrationIndex];
            if (!registration.ReflectedImplementationRegistrations.IsDefaultOrEmpty ||
                !registration.PreciseReflectedRegistrations.IsDefaultOrEmpty)
            {
                AppendOrderedImplementationRegistrations(builder, registration, registrationIndex);
            }
            else if (!registration.DirectRegistrations.IsDefaultOrEmpty)
            {
                AppendDirectRegistrations(builder, registration);
            }
        }

        builder.AppendLine("    }");

        if (hasExternalAssemblyTypeLookups)
        {
            builder.AppendLine();
            AppendReflectionHelpers(builder, hasExternalAssemblyTypeLookups);
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendDirectRegistrations(
        StringBuilder builder,
        ImplementationRegistrationSpec registration)
    {
        foreach (var directRegistration in registration.DirectRegistrations)
        {
            builder.AppendLine(
                "        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
            builder.AppendLine("            services,");
            builder.Append("            typeof(");
            builder.Append(directRegistration.HandlerInterfaceDisplayName);
            builder.AppendLine("),");
            builder.Append("            typeof(");
            builder.Append(directRegistration.ImplementationTypeDisplayName);
            builder.AppendLine("));");
            builder.Append("        logger.Debug(\"Registered CQRS handler ");
            builder.Append(EscapeStringLiteral(directRegistration.ImplementationLogName));
            builder.Append(" as ");
            builder.Append(EscapeStringLiteral(directRegistration.HandlerInterfaceLogName));
            builder.AppendLine(".\");");
        }
    }

    private static void AppendOrderedImplementationRegistrations(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        int registrationIndex)
    {
        var orderedRegistrations =
            new List<(string HandlerInterfaceLogName, OrderedRegistrationKind Kind, int Index)>(
                registration.DirectRegistrations.Length +
                registration.ReflectedImplementationRegistrations.Length +
                registration.PreciseReflectedRegistrations.Length);

        for (var directIndex = 0; directIndex < registration.DirectRegistrations.Length; directIndex++)
        {
            orderedRegistrations.Add((
                registration.DirectRegistrations[directIndex].HandlerInterfaceLogName,
                OrderedRegistrationKind.Direct,
                directIndex));
        }

        for (var reflectedIndex = 0;
             reflectedIndex < registration.ReflectedImplementationRegistrations.Length;
             reflectedIndex++)
        {
            orderedRegistrations.Add((
                registration.ReflectedImplementationRegistrations[reflectedIndex].HandlerInterfaceLogName,
                OrderedRegistrationKind.ReflectedImplementation,
                reflectedIndex));
        }

        for (var preciseIndex = 0;
             preciseIndex < registration.PreciseReflectedRegistrations.Length;
             preciseIndex++)
        {
            orderedRegistrations.Add((
                registration.PreciseReflectedRegistrations[preciseIndex].HandlerInterfaceLogName,
                OrderedRegistrationKind.PreciseReflected,
                preciseIndex));
        }

        orderedRegistrations.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.HandlerInterfaceLogName, right.HandlerInterfaceLogName));

        var implementationVariableName = $"implementationType{registrationIndex}";
        if (string.IsNullOrWhiteSpace(registration.ReflectionTypeMetadataName))
        {
            builder.Append("        var ");
            builder.Append(implementationVariableName);
            builder.Append(" = typeof(");
            builder.Append(registration.ImplementationTypeDisplayName);
            builder.AppendLine(");");
        }
        else
        {
            builder.Append("        var ");
            builder.Append(implementationVariableName);
            builder.Append(" = registryAssembly.GetType(\"");
            builder.Append(EscapeStringLiteral(registration.ReflectionTypeMetadataName!));
            builder.AppendLine("\", throwOnError: false, ignoreCase: false);");
        }

        builder.Append("        if (");
        builder.Append(implementationVariableName);
        builder.AppendLine(" is not null)");
        builder.AppendLine("        {");

        foreach (var orderedRegistration in orderedRegistrations)
        {
            switch (orderedRegistration.Kind)
            {
                case OrderedRegistrationKind.Direct:
                    var directRegistration = registration.DirectRegistrations[orderedRegistration.Index];
                    builder.AppendLine(
                        "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
                    builder.AppendLine("                services,");
                    builder.Append("                typeof(");
                    builder.Append(directRegistration.HandlerInterfaceDisplayName);
                    builder.AppendLine("),");
                    builder.Append("                ");
                    builder.Append(implementationVariableName);
                    builder.AppendLine(");");
                    builder.Append("            logger.Debug(\"Registered CQRS handler ");
                    builder.Append(EscapeStringLiteral(registration.ImplementationLogName));
                    builder.Append(" as ");
                    builder.Append(EscapeStringLiteral(directRegistration.HandlerInterfaceLogName));
                    builder.AppendLine(".\");");
                    break;
                case OrderedRegistrationKind.ReflectedImplementation:
                    var reflectedRegistration =
                        registration.ReflectedImplementationRegistrations[orderedRegistration.Index];
                    builder.AppendLine(
                        "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
                    builder.AppendLine("                services,");
                    builder.Append("                typeof(");
                    builder.Append(reflectedRegistration.HandlerInterfaceDisplayName);
                    builder.AppendLine("),");
                    builder.Append("                ");
                    builder.Append(implementationVariableName);
                    builder.AppendLine(");");
                    builder.Append("            logger.Debug(\"Registered CQRS handler ");
                    builder.Append(EscapeStringLiteral(registration.ImplementationLogName));
                    builder.Append(" as ");
                    builder.Append(EscapeStringLiteral(reflectedRegistration.HandlerInterfaceLogName));
                    builder.AppendLine(".\");");
                    break;
                case OrderedRegistrationKind.PreciseReflected:
                    var preciseRegistration = registration.PreciseReflectedRegistrations[orderedRegistration.Index];
                    var registrationVariablePrefix = $"serviceType{registrationIndex}_{orderedRegistration.Index}";
                    AppendPreciseReflectedTypeResolution(
                        builder,
                        preciseRegistration.ServiceTypeArguments,
                        registrationVariablePrefix,
                        implementationVariableName,
                        preciseRegistration.OpenHandlerTypeDisplayName,
                        registration.ImplementationLogName,
                        preciseRegistration.HandlerInterfaceLogName,
                        3);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported ordered CQRS registration kind {orderedRegistration.Kind}.");
            }
        }

        builder.AppendLine("        }");
    }

    private static void AppendPreciseReflectedTypeResolution(
        StringBuilder builder,
        ImmutableArray<RuntimeTypeReferenceSpec> serviceTypeArguments,
        string registrationVariablePrefix,
        string implementationVariableName,
        string openHandlerTypeDisplayName,
        string implementationLogName,
        string handlerInterfaceLogName,
        int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var nestedIndent = new string(' ', (indentLevel + 1) * 4);
        var resolvedArgumentNames = new string[serviceTypeArguments.Length];
        var reflectedArgumentNames = new List<string>();

        for (var argumentIndex = 0; argumentIndex < serviceTypeArguments.Length; argumentIndex++)
        {
            resolvedArgumentNames[argumentIndex] = AppendRuntimeTypeReferenceResolution(
                builder,
                serviceTypeArguments[argumentIndex],
                $"{registrationVariablePrefix}Argument{argumentIndex}",
                reflectedArgumentNames,
                indent);
        }

        if (reflectedArgumentNames.Count > 0)
        {
            builder.Append(indent);
            builder.Append("if (");
            for (var index = 0; index < reflectedArgumentNames.Count; index++)
            {
                if (index > 0)
                    builder.Append(" && ");

                builder.Append(reflectedArgumentNames[index]);
                builder.Append(" is not null");
            }

            builder.AppendLine(")");
            builder.Append(indent);
            builder.AppendLine("{");
            indent = nestedIndent;
        }

        builder.Append(indent);
        builder.Append("var ");
        builder.Append(registrationVariablePrefix);
        builder.Append(" = typeof(");
        builder.Append(openHandlerTypeDisplayName);
        builder.Append(").MakeGenericType(");
        for (var index = 0; index < resolvedArgumentNames.Length; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(resolvedArgumentNames[index]);
        }

        builder.AppendLine(");");
        builder.Append(indent);
        builder.AppendLine(
            "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
        builder.Append(indent);
        builder.AppendLine("    services,");
        builder.Append(indent);
        builder.Append("    ");
        builder.Append(registrationVariablePrefix);
        builder.AppendLine(",");
        builder.Append(indent);
        builder.Append("    ");
        builder.Append(implementationVariableName);
        builder.AppendLine(");");
        builder.Append(indent);
        builder.Append("logger.Debug(\"Registered CQRS handler ");
        builder.Append(EscapeStringLiteral(implementationLogName));
        builder.Append(" as ");
        builder.Append(EscapeStringLiteral(handlerInterfaceLogName));
        builder.AppendLine(".\");");

        if (reflectedArgumentNames.Count > 0)
        {
            builder.Append(new string(' ', indentLevel * 4));
            builder.AppendLine("}");
        }
    }

    private static string AppendRuntimeTypeReferenceResolution(
        StringBuilder builder,
        RuntimeTypeReferenceSpec runtimeTypeReference,
        string variableBaseName,
        ICollection<string> reflectedArgumentNames,
        string indent)
    {
        if (!string.IsNullOrWhiteSpace(runtimeTypeReference.TypeDisplayName))
            return $"typeof({runtimeTypeReference.TypeDisplayName})";

        if (runtimeTypeReference.ArrayElementTypeReference is not null)
        {
            var elementExpression = AppendRuntimeTypeReferenceResolution(
                builder,
                runtimeTypeReference.ArrayElementTypeReference,
                $"{variableBaseName}Element",
                reflectedArgumentNames,
                indent);

            return runtimeTypeReference.ArrayRank == 1
                ? $"{elementExpression}.MakeArrayType()"
                : $"{elementExpression}.MakeArrayType({runtimeTypeReference.ArrayRank})";
        }

        if (runtimeTypeReference.GenericTypeDefinitionReference is not null)
        {
            var genericTypeDefinitionExpression = AppendRuntimeTypeReferenceResolution(
                builder,
                runtimeTypeReference.GenericTypeDefinitionReference,
                $"{variableBaseName}GenericDefinition",
                reflectedArgumentNames,
                indent);
            var genericArgumentExpressions = new string[runtimeTypeReference.GenericTypeArguments.Length];
            for (var argumentIndex = 0;
                 argumentIndex < runtimeTypeReference.GenericTypeArguments.Length;
                 argumentIndex++)
            {
                genericArgumentExpressions[argumentIndex] = AppendRuntimeTypeReferenceResolution(
                    builder,
                    runtimeTypeReference.GenericTypeArguments[argumentIndex],
                    $"{variableBaseName}GenericArgument{argumentIndex}",
                    reflectedArgumentNames,
                    indent);
            }

            return
                $"{genericTypeDefinitionExpression}.MakeGenericType({string.Join(", ", genericArgumentExpressions)})";
        }

        var reflectionTypeMetadataName = runtimeTypeReference.ReflectionTypeMetadataName!;
        reflectedArgumentNames.Add(variableBaseName);
        builder.Append(indent);
        builder.Append("var ");
        builder.Append(variableBaseName);
        if (string.IsNullOrWhiteSpace(runtimeTypeReference.ReflectionAssemblyName))
        {
            builder.Append(" = registryAssembly.GetType(\"");
            builder.Append(EscapeStringLiteral(reflectionTypeMetadataName));
            builder.AppendLine("\", throwOnError: false, ignoreCase: false);");
        }
        else
        {
            builder.Append(" = ResolveReferencedAssemblyType(\"");
            builder.Append(EscapeStringLiteral(runtimeTypeReference.ReflectionAssemblyName!));
            builder.Append("\", \"");
            builder.Append(EscapeStringLiteral(reflectionTypeMetadataName));
            builder.AppendLine("\");");
        }

        return variableBaseName;
    }

    private static void AppendReflectionHelpers(
        StringBuilder builder,
        bool includeExternalAssemblyTypeLookupHelpers)
    {
        if (includeExternalAssemblyTypeLookupHelpers)
        {
            builder.AppendLine(
                "    private static global::System.Type? ResolveReferencedAssemblyType(string assemblyIdentity, string typeMetadataName)");
            builder.AppendLine("    {");
            builder.AppendLine("        var assembly = ResolveReferencedAssembly(assemblyIdentity);");
            builder.AppendLine(
                "        return assembly?.GetType(typeMetadataName, throwOnError: false, ignoreCase: false);");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine(
                "    private static global::System.Reflection.Assembly? ResolveReferencedAssembly(string assemblyIdentity)");
            builder.AppendLine("    {");
            builder.AppendLine("        global::System.Reflection.AssemblyName targetAssemblyName;");
            builder.AppendLine("        try");
            builder.AppendLine("        {");
            builder.AppendLine(
                "            targetAssemblyName = new global::System.Reflection.AssemblyName(assemblyIdentity);");
            builder.AppendLine("        }");
            builder.AppendLine("        catch");
            builder.AppendLine("        {");
            builder.AppendLine("            return null;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine(
                "        foreach (var assembly in global::System.AppDomain.CurrentDomain.GetAssemblies())");
            builder.AppendLine("        {");
            builder.AppendLine(
                "            if (global::System.Reflection.AssemblyName.ReferenceMatchesDefinition(targetAssemblyName, assembly.GetName()))");
            builder.AppendLine("                return assembly;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        try");
            builder.AppendLine("        {");
            builder.AppendLine(
                "            return global::System.Reflection.Assembly.Load(targetAssemblyName);");
            builder.AppendLine("        }");
            builder.AppendLine("        catch");
            builder.AppendLine("        {");
            builder.AppendLine("            return null;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
        }
    }

    private static string EscapeStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
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

    private readonly record struct HandlerRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string ImplementationTypeDisplayName,
        string HandlerInterfaceLogName,
        string ImplementationLogName);

    private readonly record struct ReflectedImplementationRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string HandlerInterfaceLogName);

    private enum OrderedRegistrationKind
    {
        Direct,
        ReflectedImplementation,
        PreciseReflected
    }

    private sealed record RuntimeTypeReferenceSpec(
        string? TypeDisplayName,
        string? ReflectionTypeMetadataName,
        string? ReflectionAssemblyName,
        RuntimeTypeReferenceSpec? ArrayElementTypeReference,
        int ArrayRank,
        RuntimeTypeReferenceSpec? GenericTypeDefinitionReference,
        ImmutableArray<RuntimeTypeReferenceSpec> GenericTypeArguments)
    {
        public static RuntimeTypeReferenceSpec FromDirectReference(string typeDisplayName)
        {
            return new RuntimeTypeReferenceSpec(typeDisplayName, null, null, null, 0, null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        public static RuntimeTypeReferenceSpec FromReflectionLookup(string reflectionTypeMetadataName)
        {
            return new RuntimeTypeReferenceSpec(null, reflectionTypeMetadataName, null, null, 0, null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        public static RuntimeTypeReferenceSpec FromExternalReflectionLookup(
            string reflectionAssemblyName,
            string reflectionTypeMetadataName)
        {
            return new RuntimeTypeReferenceSpec(null, reflectionTypeMetadataName, reflectionAssemblyName, null, 0,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        public static RuntimeTypeReferenceSpec FromArray(RuntimeTypeReferenceSpec elementTypeReference, int arrayRank)
        {
            return new RuntimeTypeReferenceSpec(null, null, null, elementTypeReference, arrayRank, null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        public static RuntimeTypeReferenceSpec FromConstructedGeneric(
            RuntimeTypeReferenceSpec genericTypeDefinitionReference,
            ImmutableArray<RuntimeTypeReferenceSpec> genericTypeArguments)
        {
            return new RuntimeTypeReferenceSpec(null, null, null, null, 0, genericTypeDefinitionReference,
                genericTypeArguments);
        }
    }

    private readonly record struct PreciseReflectedRegistrationSpec(
        string OpenHandlerTypeDisplayName,
        string HandlerInterfaceLogName,
        ImmutableArray<RuntimeTypeReferenceSpec> ServiceTypeArguments);

    private readonly record struct ImplementationRegistrationSpec(
        string ImplementationTypeDisplayName,
        string ImplementationLogName,
        ImmutableArray<HandlerRegistrationSpec> DirectRegistrations,
        ImmutableArray<ReflectedImplementationRegistrationSpec> ReflectedImplementationRegistrations,
        ImmutableArray<PreciseReflectedRegistrationSpec> PreciseReflectedRegistrations,
        string? ReflectionTypeMetadataName,
        string? ReflectionFallbackHandlerTypeMetadataName);

    private readonly struct HandlerCandidateAnalysis : IEquatable<HandlerCandidateAnalysis>
    {
        public HandlerCandidateAnalysis(
            string implementationTypeDisplayName,
            string implementationLogName,
            ImmutableArray<HandlerRegistrationSpec> registrations,
            ImmutableArray<ReflectedImplementationRegistrationSpec> reflectedImplementationRegistrations,
            ImmutableArray<PreciseReflectedRegistrationSpec> preciseReflectedRegistrations,
            string? reflectionTypeMetadataName,
            string? reflectionFallbackHandlerTypeMetadataName)
        {
            ImplementationTypeDisplayName = implementationTypeDisplayName;
            ImplementationLogName = implementationLogName;
            Registrations = registrations;
            ReflectedImplementationRegistrations = reflectedImplementationRegistrations;
            PreciseReflectedRegistrations = preciseReflectedRegistrations;
            ReflectionTypeMetadataName = reflectionTypeMetadataName;
            ReflectionFallbackHandlerTypeMetadataName = reflectionFallbackHandlerTypeMetadataName;
        }

        public string ImplementationTypeDisplayName { get; }

        public string ImplementationLogName { get; }

        public ImmutableArray<HandlerRegistrationSpec> Registrations { get; }

        public ImmutableArray<ReflectedImplementationRegistrationSpec> ReflectedImplementationRegistrations { get; }

        public ImmutableArray<PreciseReflectedRegistrationSpec> PreciseReflectedRegistrations { get; }

        public string? ReflectionTypeMetadataName { get; }

        public string? ReflectionFallbackHandlerTypeMetadataName { get; }

        public bool Equals(HandlerCandidateAnalysis other)
        {
            if (!string.Equals(ImplementationTypeDisplayName, other.ImplementationTypeDisplayName,
                    StringComparison.Ordinal) ||
                !string.Equals(ImplementationLogName, other.ImplementationLogName, StringComparison.Ordinal) ||
                !string.Equals(ReflectionTypeMetadataName, other.ReflectionTypeMetadataName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    ReflectionFallbackHandlerTypeMetadataName,
                    other.ReflectionFallbackHandlerTypeMetadataName,
                    StringComparison.Ordinal) ||
                Registrations.Length != other.Registrations.Length ||
                ReflectedImplementationRegistrations.Length != other.ReflectedImplementationRegistrations.Length ||
                PreciseReflectedRegistrations.Length != other.PreciseReflectedRegistrations.Length)
            {
                return false;
            }

            for (var index = 0; index < Registrations.Length; index++)
            {
                if (!Registrations[index].Equals(other.Registrations[index]))
                    return false;
            }

            for (var index = 0; index < ReflectedImplementationRegistrations.Length; index++)
            {
                if (!ReflectedImplementationRegistrations[index].Equals(
                        other.ReflectedImplementationRegistrations[index]))
                    return false;
            }

            for (var index = 0; index < PreciseReflectedRegistrations.Length; index++)
            {
                if (!PreciseReflectedRegistrations[index].Equals(other.PreciseReflectedRegistrations[index]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is HandlerCandidateAnalysis other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StringComparer.Ordinal.GetHashCode(ImplementationTypeDisplayName);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ImplementationLogName);
                hashCode = (hashCode * 397) ^
                           (ReflectionTypeMetadataName is null
                               ? 0
                               : StringComparer.Ordinal.GetHashCode(ReflectionTypeMetadataName));
                hashCode = (hashCode * 397) ^
                           (ReflectionFallbackHandlerTypeMetadataName is null
                               ? 0
                               : StringComparer.Ordinal.GetHashCode(ReflectionFallbackHandlerTypeMetadataName));
                foreach (var registration in Registrations)
                {
                    hashCode = (hashCode * 397) ^ registration.GetHashCode();
                }

                foreach (var reflectedImplementationRegistration in ReflectedImplementationRegistrations)
                {
                    hashCode = (hashCode * 397) ^ reflectedImplementationRegistration.GetHashCode();
                }

                foreach (var preciseReflectedRegistration in PreciseReflectedRegistrations)
                {
                    hashCode = (hashCode * 397) ^ preciseReflectedRegistration.GetHashCode();
                }

                return hashCode;
            }
        }
    }

    private readonly record struct GenerationEnvironment(
        bool GenerationEnabled,
        bool SupportsReflectionFallbackAttribute);
}
