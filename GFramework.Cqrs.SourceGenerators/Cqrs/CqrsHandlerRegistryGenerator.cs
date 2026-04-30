using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
[Generator]
public sealed partial class CqrsHandlerRegistryGenerator : IIncrementalGenerator
{
    private const string CqrsContractsNamespace = $"{PathContests.CqrsAbstractionsNamespace}.Cqrs";
    private const string CqrsRuntimeNamespace = PathContests.CqrsNamespace;
    private const string LoggingNamespace = $"{PathContests.CoreAbstractionsNamespace}.Logging";
    private const string IRequestHandlerMetadataName = $"{CqrsContractsNamespace}.IRequestHandler`2";
    private const string INotificationHandlerMetadataName = $"{CqrsContractsNamespace}.INotificationHandler`1";
    private const string IStreamRequestHandlerMetadataName = $"{CqrsContractsNamespace}.IStreamRequestHandler`2";
    private const string ICqrsHandlerRegistryMetadataName = $"{CqrsRuntimeNamespace}.ICqrsHandlerRegistry";
    private const string ICqrsRequestInvokerProviderMetadataName = $"{CqrsRuntimeNamespace}.ICqrsRequestInvokerProvider";
    private const string IEnumeratesCqrsRequestInvokerDescriptorsMetadataName =
        $"{CqrsRuntimeNamespace}.IEnumeratesCqrsRequestInvokerDescriptors";
    private const string CqrsRequestInvokerDescriptorMetadataName =
        $"{CqrsRuntimeNamespace}.CqrsRequestInvokerDescriptor";
    private const string CqrsRequestInvokerDescriptorEntryMetadataName =
        $"{CqrsRuntimeNamespace}.CqrsRequestInvokerDescriptorEntry";

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
        var reflectionFallbackAttributeType =
            compilation.GetTypeByMetadataName(CqrsReflectionFallbackAttributeMetadataName);
        var generationEnabled = compilation.GetTypeByMetadataName(IRequestHandlerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(INotificationHandlerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(IStreamRequestHandlerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(ICqrsHandlerRegistryMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(
                                    CqrsHandlerRegistryAttributeMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(ILoggerMetadataName) is not null &&
                                compilation.GetTypeByMetadataName(IServiceCollectionMetadataName) is not null;
        var supportsRequestInvokerProvider =
            compilation.GetTypeByMetadataName(ICqrsRequestInvokerProviderMetadataName) is not null &&
            compilation.GetTypeByMetadataName(IEnumeratesCqrsRequestInvokerDescriptorsMetadataName) is not null &&
            compilation.GetTypeByMetadataName(CqrsRequestInvokerDescriptorMetadataName) is not null &&
            compilation.GetTypeByMetadataName(CqrsRequestInvokerDescriptorEntryMetadataName) is not null;
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var typeType = compilation.GetTypeByMetadataName("System.Type");
        var supportsNamedReflectionFallbackTypes = reflectionFallbackAttributeType is not null &&
                                                   HasParamsArrayConstructor(
                                                       reflectionFallbackAttributeType,
                                                       stringType);
        var supportsDirectReflectionFallbackTypes = reflectionFallbackAttributeType is not null &&
                                                    typeType is not null &&
                                                    HasParamsArrayConstructor(
                                                        reflectionFallbackAttributeType,
                                                        typeType);
        var supportsMultipleReflectionFallbackAttributes = reflectionFallbackAttributeType is not null &&
                                                           SupportsMultipleAttributeInstances(
                                                               reflectionFallbackAttributeType);

        return new GenerationEnvironment(
            generationEnabled,
            supportsNamedReflectionFallbackTypes,
            supportsDirectReflectionFallbackTypes,
            supportsMultipleReflectionFallbackAttributes,
            supportsRequestInvokerProvider);
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

        var handlerInterfaces = GetSupportedHandlerInterfaces(type);

        if (handlerInterfaces.IsDefaultOrEmpty)
            return null;

        return CreateHandlerCandidateAnalysis(context.SemanticModel.Compilation, type, handlerInterfaces);
    }

    /// <summary>
    ///     收集当前实现类型已经关闭的 CQRS handler 接口，并按稳定显示名排序以保证生成输出可重复。
    /// </summary>
    /// <param name="type">当前语义模型发现的具体 handler 实现类型。</param>
    /// <returns>可由 CQRS 注册器生成器处理的 handler 接口集合。</returns>
    private static ImmutableArray<INamedTypeSymbol> GetSupportedHandlerInterfaces(INamedTypeSymbol type)
    {
        return type.AllInterfaces
            .Where(IsSupportedHandlerInterface)
            .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    /// <summary>
    ///     将单个实现类型的 handler 接口拆分为直接注册、实现类型反射注册、精确反射注册和兜底 fallback 四类结果。
    /// </summary>
    /// <param name="compilation">当前生成轮次的编译上下文，用于判断类型可访问性。</param>
    /// <param name="type">需要分析的 handler 实现类型。</param>
    /// <param name="handlerInterfaces">该实现类型声明的受支持 CQRS handler 接口。</param>
    /// <returns>供最终生成阶段消费的 handler 候选分析结果。</returns>
    private static HandlerCandidateAnalysis CreateHandlerCandidateAnalysis(
        Compilation compilation,
        INamedTypeSymbol type,
        ImmutableArray<INamedTypeSymbol> handlerInterfaces)
    {
        var implementationTypeDisplayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var implementationLogName = GetLogDisplayName(type);
        var canReferenceImplementation = CanReferenceFromGeneratedRegistry(compilation, type);
        var registrations = ImmutableArray.CreateBuilder<HandlerRegistrationSpec>(handlerInterfaces.Length);
        var reflectedImplementationRegistrations =
            ImmutableArray.CreateBuilder<ReflectedImplementationRegistrationSpec>(handlerInterfaces.Length);
        var preciseReflectedRegistrations =
            ImmutableArray.CreateBuilder<PreciseReflectedRegistrationSpec>(handlerInterfaces.Length);
        string? reflectionFallbackHandlerTypeDisplayName = null;
        string? reflectionFallbackHandlerTypeMetadataName = null;
        foreach (var handlerInterface in handlerInterfaces)
        {
            if (TryAddStaticHandlerRegistration(
                    compilation,
                    handlerInterface,
                    canReferenceImplementation,
                    implementationTypeDisplayName,
                    implementationLogName,
                    registrations,
                    reflectedImplementationRegistrations,
                    preciseReflectedRegistrations))
            {
                continue;
            }

            // Concrete closed handler contracts should now always map to either direct registrations,
            // reflected implementation registrations, or precise runtime type references.
            // If a future Roslyn type shape still slips through this net, keep the generator conservative:
            // preserve the static registrations we do understand, and let the runtime recover the remaining
            // interfaces via the existing assembly-level targeted reflection fallback contract.
            if (canReferenceImplementation)
                reflectionFallbackHandlerTypeDisplayName ??= implementationTypeDisplayName;

            reflectionFallbackHandlerTypeMetadataName ??= GetReflectionTypeMetadataName(type);
        }

        return new HandlerCandidateAnalysis(
            implementationTypeDisplayName,
            implementationLogName,
            registrations.ToImmutable(),
            reflectedImplementationRegistrations.ToImmutable(),
            preciseReflectedRegistrations.ToImmutable(),
            canReferenceImplementation ? null : GetReflectionTypeMetadataName(type),
            reflectionFallbackHandlerTypeDisplayName,
            reflectionFallbackHandlerTypeMetadataName);
    }

    /// <summary>
    ///     尝试为单个 handler 接口选择无需程序集级 fallback 的注册表示。
    /// </summary>
    /// <param name="compilation">当前生成轮次的编译上下文。</param>
    /// <param name="handlerInterface">正在分类的关闭 handler 接口。</param>
    /// <param name="canReferenceImplementation">生成代码是否可直接引用实现类型。</param>
    /// <param name="implementationTypeDisplayName">实现类型在生成源码中的全限定显示名。</param>
    /// <param name="implementationLogName">实现类型用于日志输出的稳定显示名。</param>
    /// <param name="registrations">直接类型注册集合。</param>
    /// <param name="reflectedImplementationRegistrations">实现类型需要反射解析、接口可直接引用的注册集合。</param>
    /// <param name="preciseReflectedRegistrations">接口类型需要运行时精确构造的注册集合。</param>
    /// <returns>
    ///     当当前接口已经被添加到某个静态注册集合时返回 <see langword="true" />；否则调用方应记录 reflection fallback 元数据。
    /// </returns>
    private static bool TryAddStaticHandlerRegistration(
        Compilation compilation,
        INamedTypeSymbol handlerInterface,
        bool canReferenceImplementation,
        string implementationTypeDisplayName,
        string implementationLogName,
        ImmutableArray<HandlerRegistrationSpec>.Builder registrations,
        ImmutableArray<ReflectedImplementationRegistrationSpec>.Builder reflectedImplementationRegistrations,
        ImmutableArray<PreciseReflectedRegistrationSpec>.Builder preciseReflectedRegistrations)
    {
        var canReferenceHandlerInterface = CanReferenceFromGeneratedRegistry(compilation, handlerInterface);
        if (canReferenceImplementation && canReferenceHandlerInterface)
        {
            registrations.Add(new HandlerRegistrationSpec(
                handlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                implementationTypeDisplayName,
                GetLogDisplayName(handlerInterface),
                implementationLogName,
                TryCreateRequestInvokerRegistrationSpec(handlerInterface, out var requestInvokerRegistration)
                    ? requestInvokerRegistration
                    : null));
            return true;
        }

        if (!canReferenceImplementation && canReferenceHandlerInterface)
        {
            reflectedImplementationRegistrations.Add(new ReflectedImplementationRegistrationSpec(
                handlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                GetLogDisplayName(handlerInterface)));
            return true;
        }

        if (!TryCreatePreciseReflectedRegistration(compilation, handlerInterface, out var preciseReflectedRegistration))
            return false;

        preciseReflectedRegistrations.Add(preciseReflectedRegistration);
        return true;
    }

    /// <summary>
    ///     当当前直接注册项属于请求处理器时，提取 request invoker provider 所需的请求/响应类型显示名。
    /// </summary>
    private static bool TryCreateRequestInvokerRegistrationSpec(
        INamedTypeSymbol handlerInterface,
        out RequestInvokerRegistrationSpec requestInvokerRegistration)
    {
        if (!string.Equals(
                handlerInterface.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                $"global::{CqrsContractsNamespace}.IRequestHandler<TRequest, TResponse>",
                StringComparison.Ordinal))
        {
            requestInvokerRegistration = default;
            return false;
        }

        if (handlerInterface.TypeArguments.Length != 2)
        {
            requestInvokerRegistration = default;
            return false;
        }

        requestInvokerRegistration = new RequestInvokerRegistrationSpec(
            handlerInterface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            handlerInterface.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        return true;
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

        var reflectionFallbackEmission = CreateReflectionFallbackEmissionSpec(generationEnvironment, registrations);

        if (!CanEmitGeneratedRegistry(
                generationEnvironment,
                reflectionFallbackEmission))
        {
            ReportMissingReflectionFallbackContractDiagnostic(
                context,
                registrations);
            return;
        }

        context.AddSource(
            HintName,
            GenerateSource(generationEnvironment, registrations, reflectionFallbackEmission));
    }

    /// <summary>
    ///     判断当前轮次是否允许输出生成注册器。
    /// </summary>
    /// <param name="generationEnvironment">当前轮次可用的 fallback 合同能力。</param>
    /// <param name="reflectionFallbackEmission">当前轮次选定的 fallback 元数据发射策略。</param>
    /// <returns>
    ///     当没有 handler 依赖 fallback，或 runtime 已提供本轮策略所需的元数据承载重载时返回 <see langword="true" />；
    ///     否则返回 <see langword="false" />，调用方必须放弃生成以避免输出会静默漏注册的半成品注册器。
    /// </returns>
    private static bool CanEmitGeneratedRegistry(
        GenerationEnvironment generationEnvironment,
        ReflectionFallbackEmissionSpec reflectionFallbackEmission)
    {
        if (!reflectionFallbackEmission.HasFallbackHandlers)
            return true;

        foreach (var attributeEmission in reflectionFallbackEmission.Attributes)
        {
            if (attributeEmission.EmitDirectTypeReferences)
            {
                if (!generationEnvironment.SupportsDirectReflectionFallbackTypes)
                    return false;

                continue;
            }

            if (!generationEnvironment.SupportsNamedReflectionFallbackTypes)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     报告当前轮次因缺少 fallback 元数据承载契约而无法安全生成注册器的诊断。
    /// </summary>
    /// <param name="context">源生产上下文。</param>
    /// <param name="registrations">当前轮次汇总后的 handler 注册描述。</param>
    private static void ReportMissingReflectionFallbackContractDiagnostic(
        SourceProductionContext context,
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        var fallbackHandlerTypeMetadataNames = registrations
            .Select(static registration => registration.ReflectionFallbackHandlerTypeMetadataName)
            .Where(static typeMetadataName => !string.IsNullOrWhiteSpace(typeMetadataName))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
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
                candidate.ReflectionFallbackHandlerTypeDisplayName,
                candidate.ReflectionFallbackHandlerTypeMetadataName));
        }

        registrations.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.ImplementationLogName, right.ImplementationLogName));
        return registrations;
    }

    /// <summary>
    ///     选择本轮生成应采用的 fallback 元数据发射策略。
    /// </summary>
    /// <remarks>
    ///     当所有 fallback handlers 都能被生成代码直接引用，且 runtime 暴露了 <c>params Type[]</c> 重载时，
    ///     优先输出单个直接 <see cref="Type" /> 元数据特性；当 runtime 同时支持多个特性实例时，
    ///     mixed 场景会拆分成“直接 <see cref="Type" /> + 字符串类型名”两类特性；其余场景统一回退到字符串元数据。
    /// </remarks>
    private static ReflectionFallbackEmissionSpec CreateReflectionFallbackEmissionSpec(
        GenerationEnvironment generationEnvironment,
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        var fallbackCandidates = CollectFallbackCandidates(registrations);
        if (fallbackCandidates.Count == 0)
            return new ReflectionFallbackEmissionSpec(ImmutableArray<ReflectionFallbackAttributeEmissionSpec>.Empty);

        var fallbackHandlerTypeMetadataNames = GetSortedFallbackMetadataNames(fallbackCandidates);
        var fallbackHandlerTypeDisplayNames = GetSortedDirectFallbackDisplayNames(fallbackCandidates);

        if (TryCreateDirectFallbackEmission(
                generationEnvironment,
                fallbackHandlerTypeDisplayNames,
                fallbackHandlerTypeMetadataNames,
                out var directFallbackEmission))
        {
            return directFallbackEmission;
        }

        if (TryCreateMixedFallbackEmission(
                generationEnvironment,
                fallbackCandidates,
                fallbackHandlerTypeDisplayNames,
                out var mixedFallbackEmission))
        {
            return mixedFallbackEmission;
        }

        return CreateNamedFallbackEmission(fallbackHandlerTypeMetadataNames);
    }

    /// <summary>
    ///     收集本轮所有 fallback handlers 的稳定元数据名和可选直接引用显示名。
    /// </summary>
    private static Dictionary<string, string?> CollectFallbackCandidates(
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        var fallbackCandidates = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var registration in registrations)
        {
            if (string.IsNullOrWhiteSpace(registration.ReflectionFallbackHandlerTypeMetadataName))
                continue;

            fallbackCandidates[registration.ReflectionFallbackHandlerTypeMetadataName!] =
                registration.ReflectionFallbackHandlerTypeDisplayName;
        }

        return fallbackCandidates;
    }

    /// <summary>
    ///     获取按稳定顺序排列的 fallback handler 元数据名称集合。
    /// </summary>
    private static ImmutableArray<string> GetSortedFallbackMetadataNames(
        IReadOnlyDictionary<string, string?> fallbackCandidates)
    {
        return fallbackCandidates.Keys
            .OrderBy(static metadataName => metadataName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    /// <summary>
    ///     获取按稳定顺序排列的可直接引用 fallback handler 显示名集合。
    /// </summary>
    private static ImmutableArray<string> GetSortedDirectFallbackDisplayNames(
        IReadOnlyDictionary<string, string?> fallbackCandidates)
    {
        return fallbackCandidates.Values
            .Where(static typeDisplayName => !string.IsNullOrWhiteSpace(typeDisplayName))
            .Cast<string>()
            .OrderBy(static typeDisplayName => typeDisplayName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    /// <summary>
    ///     当全部 fallback handlers 都可直接引用时，尝试创建直接 <see cref="Type" /> 元数据发射策略。
    /// </summary>
    private static bool TryCreateDirectFallbackEmission(
        GenerationEnvironment generationEnvironment,
        ImmutableArray<string> fallbackHandlerTypeDisplayNames,
        ImmutableArray<string> fallbackHandlerTypeMetadataNames,
        out ReflectionFallbackEmissionSpec emission)
    {
        if (generationEnvironment.SupportsDirectReflectionFallbackTypes &&
            fallbackHandlerTypeDisplayNames.Length == fallbackHandlerTypeMetadataNames.Length)
        {
            emission = new ReflectionFallbackEmissionSpec(
                [
                    new ReflectionFallbackAttributeEmissionSpec(
                        EmitDirectTypeReferences: true,
                        fallbackHandlerTypeDisplayNames)
                ]);
            return true;
        }

        emission = default;
        return false;
    }

    /// <summary>
    ///     当 runtime 允许多个 fallback 特性实例时，尝试为 mixed 场景拆分直接 <see cref="Type" /> 与字符串元数据。
    /// </summary>
    private static bool TryCreateMixedFallbackEmission(
        GenerationEnvironment generationEnvironment,
        IReadOnlyDictionary<string, string?> fallbackCandidates,
        ImmutableArray<string> fallbackHandlerTypeDisplayNames,
        out ReflectionFallbackEmissionSpec emission)
    {
        if (!generationEnvironment.SupportsDirectReflectionFallbackTypes ||
            !generationEnvironment.SupportsNamedReflectionFallbackTypes ||
            !generationEnvironment.SupportsMultipleReflectionFallbackAttributes ||
            fallbackHandlerTypeDisplayNames.Length == 0)
        {
            emission = default;
            return false;
        }

        var namedOnlyFallbackMetadataNames = fallbackCandidates
            .Where(static pair => string.IsNullOrWhiteSpace(pair.Value))
            .Select(static pair => pair.Key)
            .OrderBy(static metadataName => metadataName, StringComparer.Ordinal)
            .ToImmutableArray();

        if (namedOnlyFallbackMetadataNames.Length == 0)
        {
            emission = default;
            return false;
        }

        emission = new ReflectionFallbackEmissionSpec(
            [
                new ReflectionFallbackAttributeEmissionSpec(
                    EmitDirectTypeReferences: true,
                    fallbackHandlerTypeDisplayNames),
                new ReflectionFallbackAttributeEmissionSpec(
                    EmitDirectTypeReferences: false,
                    namedOnlyFallbackMetadataNames)
            ]);
        return true;
    }

    /// <summary>
    ///     创建统一的字符串 fallback 元数据发射策略。
    /// </summary>
    private static ReflectionFallbackEmissionSpec CreateNamedFallbackEmission(
        ImmutableArray<string> fallbackHandlerTypeMetadataNames)
    {
        return new ReflectionFallbackEmissionSpec(
            [
                new ReflectionFallbackAttributeEmissionSpec(
                    EmitDirectTypeReferences: false,
                    fallbackHandlerTypeMetadataNames)
            ]);
    }

    /// <summary>
    ///     判断目标特性是否暴露了指定元素类型的 <c>params T[]</c> 构造函数。
    /// </summary>
    private static bool HasParamsArrayConstructor(INamedTypeSymbol attributeType, ITypeSymbol elementType)
    {
        foreach (var constructor in attributeType.InstanceConstructors)
        {
            if (constructor.Parameters.Length != 1)
                continue;

            var parameter = constructor.Parameters[0];
            if (!parameter.IsParams)
                continue;

            if (parameter.Type is IArrayTypeSymbol { Rank: 1 } arrayType &&
                SymbolEqualityComparer.Default.Equals(arrayType.ElementType, elementType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     判断目标特性的 <see cref="AttributeUsageAttribute" /> 是否允许在同一程序集上声明多个实例。
    /// </summary>
    private static bool SupportsMultipleAttributeInstances(INamedTypeSymbol attributeType)
    {
        foreach (var attribute in attributeType.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    "global::System.AttributeUsageAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (string.Equals(namedArgument.Key, "AllowMultiple", StringComparison.Ordinal) &&
                    namedArgument.Value.Value is bool allowMultiple)
                {
                    return allowMultiple;
                }
            }

            return false;
        }

        return false;
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
}
