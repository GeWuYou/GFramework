namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
public sealed partial class CqrsHandlerRegistryGenerator
{
    private readonly record struct RequestInvokerRegistrationSpec(
        string RequestTypeDisplayName,
        string ResponseTypeDisplayName);

    private readonly record struct HandlerRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string ImplementationTypeDisplayName,
        string HandlerInterfaceLogName,
        string ImplementationLogName,
        RequestInvokerRegistrationSpec? RequestInvokerRegistration);

    private readonly record struct ReflectedImplementationRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string HandlerInterfaceLogName);

    private readonly record struct OrderedRegistrationSpec(
        string HandlerInterfaceLogName,
        OrderedRegistrationKind Kind,
        int Index);

    private readonly record struct GeneratedRegistrySourceShape(
        bool HasReflectedImplementationRegistrations,
        bool HasPreciseReflectedRegistrations,
        bool HasReflectionTypeLookups,
        bool HasExternalAssemblyTypeLookups,
        bool SupportsRequestInvokerProvider,
        ImmutableArray<RequestInvokerEmissionSpec> RequestInvokerEmissions)
    {
        public bool RequiresRegistryAssemblyVariable =>
            HasReflectedImplementationRegistrations ||
            HasPreciseReflectedRegistrations ||
            HasReflectionTypeLookups;

        public bool HasRequestInvokerProvider => SupportsRequestInvokerProvider && !RequestInvokerEmissions.IsDefaultOrEmpty;
    }

    private readonly record struct RequestInvokerEmissionSpec(
        string RequestTypeDisplayName,
        string ResponseTypeDisplayName,
        string HandlerInterfaceDisplayName,
        int MethodIndex);

    /// <summary>
    ///     标记某条 handler 注册语句在生成阶段采用的表达策略。
    /// </summary>
    /// <remarks>
    ///     该枚举只服务于输出排序与代码分支选择，用来保证生成注册器在“直接注册”
    ///     “反射实现类型查找”和“精确运行时类型解析”之间保持稳定顺序。
    /// </remarks>
    private enum OrderedRegistrationKind
    {
        Direct,
        ReflectedImplementation,
        PreciseReflected
    }

    /// <summary>
    ///     描述生成注册器中某个运行时类型引用的构造方式。
    /// </summary>
    /// <remarks>
    ///     某些 handler 服务类型可以直接以 <c>typeof(...)</c> 输出，某些则需要在运行时补充
    ///     反射查找、数组封装或泛型实参重建。该记录把这些差异收敛为统一的递归结构，
    ///     供源码输出阶段生成稳定的类型解析语句。
    /// </remarks>
    private sealed record RuntimeTypeReferenceSpec(
        string? TypeDisplayName,
        string? ReflectionTypeMetadataName,
        string? ReflectionAssemblyName,
        RuntimeTypeReferenceSpec? ArrayElementTypeReference,
        int ArrayRank,
        RuntimeTypeReferenceSpec? GenericTypeDefinitionReference,
        ImmutableArray<RuntimeTypeReferenceSpec> GenericTypeArguments)
    {
        /// <summary>
        ///     创建一个可直接通过 <c>typeof(...)</c> 表达的类型引用。
        /// </summary>
        public static RuntimeTypeReferenceSpec FromDirectReference(string typeDisplayName)
        {
            return new RuntimeTypeReferenceSpec(
                typeDisplayName,
                null,
                null,
                null,
                0,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        /// <summary>
        ///     创建一个需要从当前消费端程序集反射解析的类型引用。
        /// </summary>
        public static RuntimeTypeReferenceSpec FromReflectionLookup(string reflectionTypeMetadataName)
        {
            return new RuntimeTypeReferenceSpec(
                null,
                reflectionTypeMetadataName,
                null,
                null,
                0,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        /// <summary>
        ///     创建一个需要从被引用程序集反射解析的类型引用。
        /// </summary>
        public static RuntimeTypeReferenceSpec FromExternalReflectionLookup(
            string reflectionAssemblyName,
            string reflectionTypeMetadataName)
        {
            return new RuntimeTypeReferenceSpec(
                null,
                reflectionTypeMetadataName,
                reflectionAssemblyName,
                null,
                0,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        /// <summary>
        ///     创建一个数组类型引用。
        /// </summary>
        public static RuntimeTypeReferenceSpec FromArray(RuntimeTypeReferenceSpec elementTypeReference, int arrayRank)
        {
            return new RuntimeTypeReferenceSpec(
                null,
                null,
                null,
                elementTypeReference,
                arrayRank,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        /// <summary>
        ///     创建一个封闭泛型类型引用。
        /// </summary>
        public static RuntimeTypeReferenceSpec FromConstructedGeneric(
            RuntimeTypeReferenceSpec genericTypeDefinitionReference,
            ImmutableArray<RuntimeTypeReferenceSpec> genericTypeArguments)
        {
            return new RuntimeTypeReferenceSpec(
                null,
                null,
                null,
                null,
                0,
                genericTypeDefinitionReference,
                genericTypeArguments);
        }
    }

    private readonly record struct PreciseReflectedRegistrationSpec(
        string OpenHandlerTypeDisplayName,
        string HandlerInterfaceLogName,
        ImmutableArray<RuntimeTypeReferenceSpec> ServiceTypeArguments);

    /// <summary>
    ///     描述单个程序集级 reflection fallback 特性实例的发射内容。
    /// </summary>
    /// <remarks>
    ///     某些运行时合同允许生成器把可直接引用的 fallback handlers 与必须按名称恢复的 handlers
    ///     拆成多个特性实例，以进一步减少运行时字符串查找成本。
    /// </remarks>
    private readonly record struct ReflectionFallbackAttributeEmissionSpec(
        bool EmitDirectTypeReferences,
        ImmutableArray<string> Values);

    /// <summary>
    ///     描述本轮生成应如何发射程序集级 reflection fallback 元数据。
    /// </summary>
    private readonly record struct ReflectionFallbackEmissionSpec(
        ImmutableArray<ReflectionFallbackAttributeEmissionSpec> Attributes)
    {
        /// <summary>
        ///     获取当前是否需要发射任何 fallback 元数据。
        /// </summary>
        public bool HasFallbackHandlers => !Attributes.IsDefaultOrEmpty;
    }

    private readonly record struct ImplementationRegistrationSpec(
        string ImplementationTypeDisplayName,
        string ImplementationLogName,
        ImmutableArray<HandlerRegistrationSpec> DirectRegistrations,
        ImmutableArray<ReflectedImplementationRegistrationSpec> ReflectedImplementationRegistrations,
        ImmutableArray<PreciseReflectedRegistrationSpec> PreciseReflectedRegistrations,
        string? ReflectionTypeMetadataName,
        string? ReflectionFallbackHandlerTypeDisplayName,
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
            string? reflectionFallbackHandlerTypeDisplayName,
            string? reflectionFallbackHandlerTypeMetadataName)
        {
            ImplementationTypeDisplayName = implementationTypeDisplayName;
            ImplementationLogName = implementationLogName;
            Registrations = registrations;
            ReflectedImplementationRegistrations = reflectedImplementationRegistrations;
            PreciseReflectedRegistrations = preciseReflectedRegistrations;
            ReflectionTypeMetadataName = reflectionTypeMetadataName;
            ReflectionFallbackHandlerTypeDisplayName = reflectionFallbackHandlerTypeDisplayName;
            ReflectionFallbackHandlerTypeMetadataName = reflectionFallbackHandlerTypeMetadataName;
        }

        public string ImplementationTypeDisplayName { get; }

        public string ImplementationLogName { get; }

        public ImmutableArray<HandlerRegistrationSpec> Registrations { get; }

        public ImmutableArray<ReflectedImplementationRegistrationSpec> ReflectedImplementationRegistrations { get; }

        public ImmutableArray<PreciseReflectedRegistrationSpec> PreciseReflectedRegistrations { get; }

        public string? ReflectionTypeMetadataName { get; }

        public string? ReflectionFallbackHandlerTypeDisplayName { get; }

        public string? ReflectionFallbackHandlerTypeMetadataName { get; }

        public bool Equals(HandlerCandidateAnalysis other)
        {
            if (!string.Equals(ImplementationTypeDisplayName, other.ImplementationTypeDisplayName,
                    StringComparison.Ordinal) ||
                !string.Equals(ImplementationLogName, other.ImplementationLogName, StringComparison.Ordinal) ||
                !string.Equals(ReflectionTypeMetadataName, other.ReflectionTypeMetadataName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    ReflectionFallbackHandlerTypeDisplayName,
                    other.ReflectionFallbackHandlerTypeDisplayName,
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
                {
                    return false;
                }
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
                           (ReflectionFallbackHandlerTypeDisplayName is null
                               ? 0
                               : StringComparer.Ordinal.GetHashCode(ReflectionFallbackHandlerTypeDisplayName));
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
        bool SupportsNamedReflectionFallbackTypes,
        bool SupportsDirectReflectionFallbackTypes,
        bool SupportsMultipleReflectionFallbackAttributes,
        bool SupportsRequestInvokerProvider);
}
