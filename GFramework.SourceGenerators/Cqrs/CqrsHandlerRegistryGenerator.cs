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
        context.RegisterSourceOutput(
            context.CompilationProvider,
            static (productionContext, compilation) => Execute(productionContext, compilation));
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        var requestHandlerType = compilation.GetTypeByMetadataName(IRequestHandlerMetadataName);
        var notificationHandlerType = compilation.GetTypeByMetadataName(INotificationHandlerMetadataName);
        var streamHandlerType = compilation.GetTypeByMetadataName(IStreamRequestHandlerMetadataName);
        var registryInterfaceType = compilation.GetTypeByMetadataName(ICqrsHandlerRegistryMetadataName);
        var registryAttributeType = compilation.GetTypeByMetadataName(CqrsHandlerRegistryAttributeMetadataName);
        var loggerType = compilation.GetTypeByMetadataName(ILoggerMetadataName);
        var serviceCollectionType = compilation.GetTypeByMetadataName(IServiceCollectionMetadataName);

        if (requestHandlerType is null ||
            notificationHandlerType is null ||
            streamHandlerType is null ||
            registryInterfaceType is null ||
            registryAttributeType is null ||
            loggerType is null ||
            serviceCollectionType is null)
        {
            return;
        }

        var registrations = CollectRegistrations(
            compilation.Assembly.GlobalNamespace,
            requestHandlerType,
            notificationHandlerType,
            streamHandlerType,
            out var hasUnsupportedConcreteHandler);

        // If the assembly contains handlers that generated code cannot legally reference
        // (for example private nested handlers), keep the runtime on the reflection path
        // so registration behavior remains complete instead of silently dropping handlers.
        if (hasUnsupportedConcreteHandler || registrations.Count == 0)
            return;

        context.AddSource(HintName, GenerateSource(registrations));
    }

    private static List<HandlerRegistrationSpec> CollectRegistrations(
        INamespaceSymbol rootNamespace,
        INamedTypeSymbol requestHandlerType,
        INamedTypeSymbol notificationHandlerType,
        INamedTypeSymbol streamHandlerType,
        out bool hasUnsupportedConcreteHandler)
    {
        var registrations = new List<HandlerRegistrationSpec>();
        hasUnsupportedConcreteHandler = false;

        foreach (var type in EnumerateTypes(rootNamespace))
        {
            if (!IsConcreteHandlerType(type))
                continue;

            var handlerInterfaces = type.AllInterfaces
                .Where(interfaceType => IsSupportedHandlerInterface(
                    interfaceType,
                    requestHandlerType,
                    notificationHandlerType,
                    streamHandlerType))
                .OrderBy(GetTypeSortKey, StringComparer.Ordinal)
                .ToList();

            if (handlerInterfaces.Count == 0)
                continue;

            if (!CanReferenceFromGeneratedRegistry(type) ||
                handlerInterfaces.Any(interfaceType => !CanReferenceFromGeneratedRegistry(interfaceType)))
            {
                hasUnsupportedConcreteHandler = true;
                return [];
            }

            var implementationTypeDisplayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var implementationLogName = GetLogDisplayName(type);

            foreach (var handlerInterface in handlerInterfaces)
            {
                registrations.Add(new HandlerRegistrationSpec(
                    handlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    implementationTypeDisplayName,
                    GetLogDisplayName(handlerInterface),
                    implementationLogName));
            }
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

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol childNamespace:
                    foreach (var type in EnumerateTypes(childNamespace))
                        yield return type;

                    break;

                case INamedTypeSymbol namedType:
                    foreach (var type in EnumerateTypes(namedType))
                        yield return type;

                    break;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamedTypeSymbol typeSymbol)
    {
        yield return typeSymbol;

        foreach (var nestedType in typeSymbol.GetTypeMembers())
        {
            foreach (var descendant in EnumerateTypes(nestedType))
                yield return descendant;
        }
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

    private static bool IsSupportedHandlerInterface(
        INamedTypeSymbol interfaceType,
        INamedTypeSymbol requestHandlerType,
        INamedTypeSymbol notificationHandlerType,
        INamedTypeSymbol streamHandlerType)
    {
        if (!interfaceType.IsGenericType)
            return false;

        var definition = interfaceType.OriginalDefinition;
        return SymbolEqualityComparer.Default.Equals(definition, requestHandlerType) ||
               SymbolEqualityComparer.Default.Equals(definition, notificationHandlerType) ||
               SymbolEqualityComparer.Default.Equals(definition, streamHandlerType);
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
            .Replace("\"", "\\\"");
    }

    private readonly record struct HandlerRegistrationSpec(
        string HandlerInterfaceDisplayName,
        string ImplementationTypeDisplayName,
        string HandlerInterfaceLogName,
        string ImplementationLogName);
}
