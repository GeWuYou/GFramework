using GFramework.SourceGenerators.Common.Constants;

namespace GFramework.SourceGenerators.Cqrs;

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

    private const string ILoggerMetadataName = $"{LoggingNamespace}.ILogger";
    private const string IServiceCollectionMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string GeneratedNamespace = "GFramework.Generated.Cqrs";
    private const string GeneratedTypeName = "__GFrameworkGeneratedCqrsHandlerRegistry";
    private const string HintName = "CqrsHandlerRegistry.g.cs";

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

        return new GenerationEnvironment(generationEnabled);
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
        if (!CanReferenceFromGeneratedRegistry(type) ||
            handlerInterfaces.Any(interfaceType => !CanReferenceFromGeneratedRegistry(interfaceType)))
        {
            // Non-public handlers and handlers closed over non-public message types cannot appear in typeof(...)
            // expressions inside generated code. Preserve generator hit rate by resolving just that implementation
            // type back from the current assembly instead of asking the runtime registrar to rescan the assembly.
            return new HandlerCandidateAnalysis(
                implementationTypeDisplayName,
                implementationLogName,
                ImmutableArray<HandlerRegistrationSpec>.Empty,
                GetReflectionTypeMetadataName(type));
        }

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
            implementationLogName,
            registrations.MoveToImmutable(),
            null);
    }

    private static void Execute(SourceProductionContext context, GenerationEnvironment generationEnvironment,
        ImmutableArray<HandlerCandidateAnalysis?> candidates)
    {
        if (!generationEnvironment.GenerationEnabled)
            return;

        var registrations = CollectRegistrations(candidates);

        if (registrations.Count == 0)
            return;

        context.AddSource(
            HintName,
            GenerateSource(registrations));
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
                candidate.ReflectionTypeMetadataName));
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

    private static string GenerateSource(
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        var hasReflectionRegistrations = registrations.Any(static registration =>
            !string.IsNullOrWhiteSpace(registration.ReflectionTypeMetadataName));
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
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
        if (hasReflectionRegistrations)
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

        foreach (var registration in registrations)
        {
            if (!string.IsNullOrWhiteSpace(registration.ReflectionTypeMetadataName))
            {
                AppendReflectionRegistration(builder, registration.ReflectionTypeMetadataName!);
                continue;
            }

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

        builder.AppendLine("    }");

        if (hasReflectionRegistrations)
        {
            builder.AppendLine();
            AppendReflectionHelpers(builder);
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendReflectionRegistration(StringBuilder builder, string reflectionTypeMetadataName)
    {
        builder.Append("        RegisterReflectedHandler(services, logger, registryAssembly, \"");
        builder.Append(EscapeStringLiteral(reflectionTypeMetadataName));
        builder.AppendLine("\");");
    }

    private static void AppendReflectionHelpers(StringBuilder builder)
    {
        // Emit the runtime helper methods only when at least one handler requires metadata-name lookup.
        builder.AppendLine(
            "    private static void RegisterReflectedHandler(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::GFramework.Core.Abstractions.Logging.ILogger logger, global::System.Reflection.Assembly registryAssembly, string implementationTypeMetadataName)");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        var implementationType = registryAssembly.GetType(implementationTypeMetadataName, throwOnError: false, ignoreCase: false);");
        builder.AppendLine("        if (implementationType is null)");
        builder.AppendLine("            return;");
        builder.AppendLine();
        builder.AppendLine("        var handlerInterfaces = implementationType.GetInterfaces();");
        builder.AppendLine("        global::System.Array.Sort(handlerInterfaces, CompareTypes);");
        builder.AppendLine();
        builder.AppendLine("        foreach (var handlerInterface in handlerInterfaces)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (!IsSupportedHandlerInterface(handlerInterface))");
        builder.AppendLine("                continue;");
        builder.AppendLine();
        builder.AppendLine(
            "            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
        builder.AppendLine("                services,");
        builder.AppendLine("                handlerInterface,");
        builder.AppendLine("                implementationType);");
        builder.AppendLine(
            "            logger.Debug($\"Registered CQRS handler {GetRuntimeTypeDisplayName(implementationType)} as {GetRuntimeTypeDisplayName(handlerInterface)}.\");");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static int CompareTypes(global::System.Type left, global::System.Type right)");
        builder.AppendLine("    {");
        builder.AppendLine(
            "        return global::System.StringComparer.Ordinal.Compare(GetRuntimeTypeDisplayName(left), GetRuntimeTypeDisplayName(right));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static bool IsSupportedHandlerInterface(global::System.Type interfaceType)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (!interfaceType.IsGenericType)");
        builder.AppendLine("            return false;");
        builder.AppendLine();
        builder.AppendLine("        var definitionFullName = interfaceType.GetGenericTypeDefinition().FullName;");
        builder.AppendLine(
            $"        return global::System.StringComparer.Ordinal.Equals(definitionFullName, \"{IRequestHandlerMetadataName}\")");
        builder.AppendLine(
            $"            || global::System.StringComparer.Ordinal.Equals(definitionFullName, \"{INotificationHandlerMetadataName}\")");
        builder.AppendLine(
            $"            || global::System.StringComparer.Ordinal.Equals(definitionFullName, \"{IStreamRequestHandlerMetadataName}\");");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static string GetRuntimeTypeDisplayName(global::System.Type type)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (type == typeof(string))");
        builder.AppendLine("            return \"string\";");
        builder.AppendLine("        if (type == typeof(int))");
        builder.AppendLine("            return \"int\";");
        builder.AppendLine("        if (type == typeof(long))");
        builder.AppendLine("            return \"long\";");
        builder.AppendLine("        if (type == typeof(short))");
        builder.AppendLine("            return \"short\";");
        builder.AppendLine("        if (type == typeof(byte))");
        builder.AppendLine("            return \"byte\";");
        builder.AppendLine("        if (type == typeof(bool))");
        builder.AppendLine("            return \"bool\";");
        builder.AppendLine("        if (type == typeof(object))");
        builder.AppendLine("            return \"object\";");
        builder.AppendLine("        if (type == typeof(void))");
        builder.AppendLine("            return \"void\";");
        builder.AppendLine("        if (type == typeof(uint))");
        builder.AppendLine("            return \"uint\";");
        builder.AppendLine("        if (type == typeof(ulong))");
        builder.AppendLine("            return \"ulong\";");
        builder.AppendLine("        if (type == typeof(ushort))");
        builder.AppendLine("            return \"ushort\";");
        builder.AppendLine("        if (type == typeof(sbyte))");
        builder.AppendLine("            return \"sbyte\";");
        builder.AppendLine("        if (type == typeof(float))");
        builder.AppendLine("            return \"float\";");
        builder.AppendLine("        if (type == typeof(double))");
        builder.AppendLine("            return \"double\";");
        builder.AppendLine("        if (type == typeof(decimal))");
        builder.AppendLine("            return \"decimal\";");
        builder.AppendLine("        if (type == typeof(char))");
        builder.AppendLine("            return \"char\";");
        builder.AppendLine();
        builder.AppendLine("        if (type.IsArray)");
        builder.AppendLine("            return GetRuntimeTypeDisplayName(type.GetElementType()!) + \"[]\";");
        builder.AppendLine();
        builder.AppendLine("        if (!type.IsGenericType)");
        builder.AppendLine("            return (type.FullName ?? type.Name).Replace('+', '.');");
        builder.AppendLine();
        builder.AppendLine("        var genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.Name;");
        builder.AppendLine("        var arityIndex = genericTypeName.IndexOf('`');");
        builder.AppendLine("        if (arityIndex >= 0)");
        builder.AppendLine("            genericTypeName = genericTypeName[..arityIndex];");
        builder.AppendLine();
        builder.AppendLine("        genericTypeName = genericTypeName.Replace('+', '.');");
        builder.AppendLine("        var arguments = type.GetGenericArguments();");
        builder.AppendLine("        var builder = new global::System.Text.StringBuilder();");
        builder.AppendLine("        builder.Append(genericTypeName);");
        builder.AppendLine("        builder.Append('<');");
        builder.AppendLine();
        builder.AppendLine("        for (var index = 0; index < arguments.Length; index++)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (index > 0)");
        builder.AppendLine("                builder.Append(\", \");");
        builder.AppendLine();
        builder.AppendLine("            builder.Append(GetRuntimeTypeDisplayName(arguments[index]));");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        builder.Append('>');");
        builder.AppendLine("        return builder.ToString();");
        builder.AppendLine("    }");
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

    private readonly record struct ImplementationRegistrationSpec(
        string ImplementationTypeDisplayName,
        string ImplementationLogName,
        ImmutableArray<HandlerRegistrationSpec> DirectRegistrations,
        string? ReflectionTypeMetadataName);

    private readonly struct HandlerCandidateAnalysis : IEquatable<HandlerCandidateAnalysis>
    {
        public HandlerCandidateAnalysis(
            string implementationTypeDisplayName,
            string implementationLogName,
            ImmutableArray<HandlerRegistrationSpec> registrations,
            string? reflectionTypeMetadataName)
        {
            ImplementationTypeDisplayName = implementationTypeDisplayName;
            ImplementationLogName = implementationLogName;
            Registrations = registrations;
            ReflectionTypeMetadataName = reflectionTypeMetadataName;
        }

        public string ImplementationTypeDisplayName { get; }

        public string ImplementationLogName { get; }

        public ImmutableArray<HandlerRegistrationSpec> Registrations { get; }

        public string? ReflectionTypeMetadataName { get; }

        public bool Equals(HandlerCandidateAnalysis other)
        {
            if (!string.Equals(ImplementationTypeDisplayName, other.ImplementationTypeDisplayName,
                    StringComparison.Ordinal) ||
                !string.Equals(ImplementationLogName, other.ImplementationLogName, StringComparison.Ordinal) ||
                !string.Equals(ReflectionTypeMetadataName, other.ReflectionTypeMetadataName,
                    StringComparison.Ordinal) ||
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
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ImplementationLogName);
                hashCode = (hashCode * 397) ^
                           (ReflectionTypeMetadataName is null
                               ? 0
                               : StringComparer.Ordinal.GetHashCode(ReflectionTypeMetadataName));
                foreach (var registration in Registrations)
                {
                    hashCode = (hashCode * 397) ^ registration.GetHashCode();
                }

                return hashCode;
            }
        }
    }

    private readonly record struct GenerationEnvironment(bool GenerationEnabled);
}
