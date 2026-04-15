using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
[Generator]
public sealed class CqrsHandlerRegistryGenerator : IIncrementalGenerator
{
    private const string CqrsNamespace = $"{PathContests.CoreAbstractionsNamespace}.Cqrs";
    private const string LoggingNamespace = $"{PathContests.CoreAbstractionsNamespace}.Logging";
    private const string IRequestHandlerMetadataName = $"{CqrsNamespace}.IRequestHandler`2";
    private const string INotificationHandlerMetadataName = $"{CqrsNamespace}.INotificationHandler`1";
    private const string IStreamRequestHandlerMetadataName = $"{CqrsNamespace}.IStreamRequestHandler`2";
    private const string ICqrsHandlerRegistryMetadataName = $"{CqrsNamespace}.ICqrsHandlerRegistry";
    private const string CqrsHandlerRegistryAttributeMetadataName = $"{CqrsNamespace}.CqrsHandlerRegistryAttribute";
    private const string ILoggerMetadataName = $"{LoggingNamespace}.ILogger";
    private const string IServiceCollectionMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string GeneratedNamespace = "GFramework.Generated.Cqrs";
    private const string GeneratedTypeName = "__GFrameworkGeneratedCqrsHandlerRegistry";
    private const string HintName = "CqrsHandlerRegistry.g.cs";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationEnabled = context.CompilationProvider
            .Select(static (compilation, _) => HasRequiredTypes(compilation));

        // Restrict semantic analysis to type declarations that can actually contribute implemented interfaces.
        var handlerCandidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsHandlerCandidate(node),
                static (syntaxContext, _) => TransformHandlerCandidate(syntaxContext))
            .Where(static candidate => candidate is not null)
            .Collect();

        context.RegisterSourceOutput(
            generationEnabled.Combine(handlerCandidates),
            static (productionContext, pair) => Execute(productionContext, pair.Left, pair.Right));
    }

    private static bool HasRequiredTypes(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(IRequestHandlerMetadataName) is not null &&
               compilation.GetTypeByMetadataName(INotificationHandlerMetadataName) is not null &&
               compilation.GetTypeByMetadataName(IStreamRequestHandlerMetadataName) is not null &&
               compilation.GetTypeByMetadataName(ICqrsHandlerRegistryMetadataName) is not null &&
               compilation.GetTypeByMetadataName(CqrsHandlerRegistryAttributeMetadataName) is not null &&
               compilation.GetTypeByMetadataName(ILoggerMetadataName) is not null &&
               compilation.GetTypeByMetadataName(IServiceCollectionMetadataName) is not null;
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
        if (!CanReferenceFromGeneratedRegistry(type) ||
            handlerInterfaces.Any(interfaceType => !CanReferenceFromGeneratedRegistry(interfaceType)))
        {
            return new HandlerCandidateAnalysis(
                implementationTypeDisplayName,
                ImmutableArray<HandlerRegistrationSpec>.Empty,
                true);
        }

        var implementationLogName = GetLogDisplayName(type);
        var registrations = ImmutableArray.CreateBuilder<HandlerRegistrationSpec>(handlerInterfaces.Length);
        foreach (var handlerInterface in handlerInterfaces)
        {
            registrations.Add(new HandlerRegistrationSpec(
                handlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                implementationTypeDisplayName,
                GetLogDisplayName(handlerInterface),
                implementationLogName));
        }

        return new HandlerCandidateAnalysis(
            implementationTypeDisplayName,
            registrations.MoveToImmutable(),
            false);
    }

    private static void Execute(SourceProductionContext context, bool generationEnabled,
        ImmutableArray<HandlerCandidateAnalysis?> candidates)
    {
        if (!generationEnabled)
            return;

        var registrations = CollectRegistrations(candidates, out var hasUnsupportedConcreteHandler);

        // If the assembly contains handlers that generated code cannot legally reference
        // (for example private nested handlers), keep the runtime on the reflection path
        // so registration behavior remains complete instead of silently dropping handlers.
        if (hasUnsupportedConcreteHandler || registrations.Count == 0)
            return;

        context.AddSource(HintName, GenerateSource(registrations));
    }

    private static List<HandlerRegistrationSpec> CollectRegistrations(
        ImmutableArray<HandlerCandidateAnalysis?> candidates,
        out bool hasUnsupportedConcreteHandler)
    {
        var registrations = new List<HandlerRegistrationSpec>();
        hasUnsupportedConcreteHandler = false;

        // Partial declarations surface the same symbol through multiple syntax nodes.
        // Collapse them by implementation type so generated registrations stay stable and duplicate-free.
        var uniqueCandidates = new Dictionary<string, HandlerCandidateAnalysis>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            if (candidate.Value.HasUnsupportedConcreteHandler)
            {
                hasUnsupportedConcreteHandler = true;
                return [];
            }

            uniqueCandidates[candidate.Value.ImplementationTypeDisplayName] = candidate.Value;
        }

        foreach (var candidate in uniqueCandidates.Values)
        {
            registrations.AddRange(candidate.Registrations);
        }

        registrations.Sort(static (left, right) =>
        {
            var implementationComparison = StringComparer.Ordinal.Compare(
                left.ImplementationLogName,
                right.ImplementationLogName);

            return implementationComparison != 0
                ? implementationComparison
                : StringComparer.Ordinal.Compare(left.HandlerInterfaceLogName, right.HandlerInterfaceLogName);
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

    private static bool CanReferenceFromGeneratedRegistry(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol arrayType:
                return CanReferenceFromGeneratedRegistry(arrayType.ElementType);
            case INamedTypeSymbol namedType:
                if (!IsTypeChainAccessible(namedType))
                    return false;

                return namedType.TypeArguments.All(CanReferenceFromGeneratedRegistry);
            case IPointerTypeSymbol pointerType:
                return CanReferenceFromGeneratedRegistry(pointerType.PointedAtType);
            case ITypeParameterSymbol:
                return false;
            default:
                return true;
        }
    }

    private static bool IsTypeChainAccessible(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (!IsSymbolAccessible(current))
                return false;
        }

        return true;
    }

    private static bool IsSymbolAccessible(ISymbol symbol)
    {
        return symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
            or Accessibility.ProtectedOrInternal;
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

    private static string GetTypeSortKey(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string GetLogDisplayName(ITypeSymbol type)
    {
        return GetTypeSortKey(type).Replace("global::", string.Empty);
    }

    private static string GenerateSource(IReadOnlyList<HandlerRegistrationSpec> registrations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.Append("[assembly: global::");
        builder.Append(CqrsNamespace);
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
        builder.Append(CqrsNamespace);
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
        builder.AppendLine();

        foreach (var registration in registrations)
        {
            builder.AppendLine(
                "        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
            builder.AppendLine("            services,");
            builder.Append("            typeof(");
            builder.Append(registration.HandlerInterfaceDisplayName);
            builder.AppendLine("),");
            builder.Append("            typeof(");
            builder.Append(registration.ImplementationTypeDisplayName);
            builder.AppendLine("));");
            builder.Append("        logger.Debug(\"Registered CQRS handler ");
            builder.Append(EscapeStringLiteral(registration.ImplementationLogName));
            builder.Append(" as ");
            builder.Append(EscapeStringLiteral(registration.HandlerInterfaceLogName));
            builder.AppendLine(".\");");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string EscapeStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private readonly record struct HandlerRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string ImplementationTypeDisplayName,
        string HandlerInterfaceLogName,
        string ImplementationLogName);

    private readonly struct HandlerCandidateAnalysis : IEquatable<HandlerCandidateAnalysis>
    {
        public HandlerCandidateAnalysis(
            string implementationTypeDisplayName,
            ImmutableArray<HandlerRegistrationSpec> registrations,
            bool hasUnsupportedConcreteHandler)
        {
            ImplementationTypeDisplayName = implementationTypeDisplayName;
            Registrations = registrations;
            HasUnsupportedConcreteHandler = hasUnsupportedConcreteHandler;
        }

        public string ImplementationTypeDisplayName { get; }

        public ImmutableArray<HandlerRegistrationSpec> Registrations { get; }

        public bool HasUnsupportedConcreteHandler { get; }

        public bool Equals(HandlerCandidateAnalysis other)
        {
            if (!string.Equals(ImplementationTypeDisplayName, other.ImplementationTypeDisplayName,
                    StringComparison.Ordinal) ||
                HasUnsupportedConcreteHandler != other.HasUnsupportedConcreteHandler ||
                Registrations.Length != other.Registrations.Length)
            {
                return false;
            }

            for (var index = 0; index < Registrations.Length; index++)
            {
                if (!Registrations[index].Equals(other.Registrations[index]))
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
                hashCode = (hashCode * 397) ^ HasUnsupportedConcreteHandler.GetHashCode();
                foreach (var registration in Registrations)
                {
                    hashCode = (hashCode * 397) ^ registration.GetHashCode();
                }

                return hashCode;
            }
        }
    }
}
