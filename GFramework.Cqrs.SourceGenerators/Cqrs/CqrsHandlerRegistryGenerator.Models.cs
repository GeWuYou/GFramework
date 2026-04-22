namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
public sealed partial class CqrsHandlerRegistryGenerator
{
    private readonly record struct HandlerRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string ImplementationTypeDisplayName,
        string HandlerInterfaceLogName,
        string ImplementationLogName);

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
        bool HasExternalAssemblyTypeLookups)
    {
        public bool RequiresRegistryAssemblyVariable =>
            HasReflectedImplementationRegistrations ||
            HasPreciseReflectedRegistrations ||
            HasReflectionTypeLookups;
    }

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
        RuntimeTypeReferenceSpec? PointerElementTypeReference,
        RuntimeTypeReferenceSpec? GenericTypeDefinitionReference,
        ImmutableArray<RuntimeTypeReferenceSpec> GenericTypeArguments)
    {
        public static RuntimeTypeReferenceSpec FromDirectReference(string typeDisplayName)
        {
            return new RuntimeTypeReferenceSpec(
                typeDisplayName,
                null,
                null,
                null,
                0,
                null,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        public static RuntimeTypeReferenceSpec FromReflectionLookup(string reflectionTypeMetadataName)
        {
            return new RuntimeTypeReferenceSpec(
                null,
                reflectionTypeMetadataName,
                null,
                null,
                0,
                null,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

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
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

        public static RuntimeTypeReferenceSpec FromArray(RuntimeTypeReferenceSpec elementTypeReference, int arrayRank)
        {
            return new RuntimeTypeReferenceSpec(
                null,
                null,
                null,
                elementTypeReference,
                arrayRank,
                null,
                null,
                ImmutableArray<RuntimeTypeReferenceSpec>.Empty);
        }

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
                null,
                genericTypeDefinitionReference,
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
