// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace GFramework.Core.SourceGenerators.Analyzers;

/// <summary>
///     分析 Context Get 使用点是否能在所属架构中找到静态可见的 Model、System、Utility 注册。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContextRegistrationAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyDictionary<string, ComponentKind> _contextAwareBindingNames =
        new Dictionary<string, ComponentKind>(StringComparer.Ordinal)
        {
            ["GetModel"] = ComponentKind.Model,
            ["GetModels"] = ComponentKind.Model,
            ["GetSystem"] = ComponentKind.System,
            ["GetSystems"] = ComponentKind.System,
            ["GetUtility"] = ComponentKind.Utility,
            ["GetUtilities"] = ComponentKind.Utility
        };

    /// <summary>
    ///     当前分析器支持的诊断规则。
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            ContextRegistrationDiagnostics.ModelRegistrationMissing,
            ContextRegistrationDiagnostics.SystemRegistrationMissing,
            ContextRegistrationDiagnostics.UtilityRegistrationMissing);

    /// <summary>
    ///     初始化分析器并注册字段注入与手写 GetX 调用分析。
    /// </summary>
    /// <param name="context">分析器上下文。</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var symbols = SymbolCache.Create(compilationContext.Compilation);
            if (!symbols.IsReady)
                return;

            var registrationIndex = new Lazy<RegistrationIndex>(
                () => RegistrationIndex.Build(compilationContext.Compilation, symbols),
                LazyThreadSafetyMode.ExecutionAndPublication);

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeField(syntaxContext, symbols, registrationIndex.Value),
                SyntaxKind.VariableDeclarator);

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, symbols, registrationIndex.Value),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeField(
        SyntaxNodeAnalysisContext context,
        SymbolCache symbols,
        RegistrationIndex registrationIndex)
    {
        if (context.Node is not VariableDeclaratorSyntax variableDeclarator)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is not IFieldSymbol
            fieldSymbol)
            return;

        if (!TryCreateBindingRequest(fieldSymbol, variableDeclarator.Identifier.GetLocation(), symbols,
                out var request))
            return;

        ReportMissingRegistration(context, registrationIndex, request);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        SymbolCache symbols,
        RegistrationIndex registrationIndex)
    {
        if (context.Operation is not IInvocationOperation invocation)
            return;

        if (!TryCreateBindingRequest(invocation, context.ContainingSymbol?.ContainingType, symbols, out var request))
            return;

        ReportMissingRegistration(context, registrationIndex, request);
    }

    private static void ReportMissingRegistration(
        SyntaxNodeAnalysisContext context,
        RegistrationIndex registrationIndex,
        BindingRequest request)
    {
        if (!registrationIndex.TryGetOwningArchitecture(request.OwnerType, out var architectureType))
            return;

        if (registrationIndex.HasRegistration(architectureType, request.Kind, request.ServiceType))
            return;

        context.ReportDiagnostic(CreateMissingRegistrationDiagnostic(request, architectureType));
    }

    private static void ReportMissingRegistration(
        OperationAnalysisContext context,
        RegistrationIndex registrationIndex,
        BindingRequest request)
    {
        if (!registrationIndex.TryGetOwningArchitecture(request.OwnerType, out var architectureType))
            return;

        if (registrationIndex.HasRegistration(architectureType, request.Kind, request.ServiceType))
            return;

        context.ReportDiagnostic(CreateMissingRegistrationDiagnostic(request, architectureType));
    }

    private static Diagnostic CreateMissingRegistrationDiagnostic(
        BindingRequest request,
        INamedTypeSymbol architectureType)
    {
        return Diagnostic.Create(
            request.Kind switch
            {
                ComponentKind.Model => ContextRegistrationDiagnostics.ModelRegistrationMissing,
                ComponentKind.System => ContextRegistrationDiagnostics.SystemRegistrationMissing,
                ComponentKind.Utility => ContextRegistrationDiagnostics.UtilityRegistrationMissing,
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            },
            request.Location,
            request.ServiceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            request.OwnerType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            architectureType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
    }

    private static bool TryCreateBindingRequest(
        IFieldSymbol fieldSymbol,
        Location location,
        SymbolCache symbols,
        out BindingRequest request)
    {
        request = default;

        if (fieldSymbol.ContainingType is not INamedTypeSymbol ownerType)
            return false;

        foreach (var attribute in fieldSymbol.GetAttributes())
        {
            if (!TryMapAttribute(attribute.AttributeClass, symbols, out var componentKind, out var expectsCollection))
                continue;

            var serviceType = expectsCollection
                ? TryGetCollectionElementType(fieldSymbol.Type, symbols)
                : fieldSymbol.Type as INamedTypeSymbol;

            if (serviceType == null)
                return false;

            request = new BindingRequest(componentKind, ownerType, serviceType, location);
            return true;
        }

        return false;
    }

    private static bool TryCreateBindingRequest(
        IInvocationOperation invocation,
        INamedTypeSymbol? ownerType,
        SymbolCache symbols,
        out BindingRequest request)
    {
        request = default;

        var targetMethod = invocation.TargetMethod;
        if (!targetMethod.IsGenericMethod || targetMethod.TypeArguments.Length != 1)
            return false;

        if (invocation.Syntax.SyntaxTree.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (targetMethod.TypeArguments[0] is not INamedTypeSymbol serviceType)
            return false;

        if (_contextAwareBindingNames.TryGetValue(targetMethod.Name, out var modelKind))
        {
            if (!IsSupportedGetInvocationTarget(targetMethod, symbols))
                return false;
        }
        else
        {
            return false;
        }

        if (ownerType == null)
            return false;

        request = new BindingRequest(modelKind, ownerType, serviceType, invocation.Syntax.GetLocation());
        return true;
    }

    private static bool TryMapAttribute(
        INamedTypeSymbol? attributeType,
        SymbolCache symbols,
        out ComponentKind componentKind,
        out bool expectsCollection)
    {
        componentKind = default;
        expectsCollection = false;

        if (attributeType == null)
            return false;

        if (SymbolEqualityComparer.Default.Equals(attributeType, symbols.GetModelAttribute))
        {
            componentKind = ComponentKind.Model;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(attributeType, symbols.GetModelsAttribute))
        {
            componentKind = ComponentKind.Model;
            expectsCollection = true;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(attributeType, symbols.GetSystemAttribute))
        {
            componentKind = ComponentKind.System;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(attributeType, symbols.GetSystemsAttribute))
        {
            componentKind = ComponentKind.System;
            expectsCollection = true;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(attributeType, symbols.GetUtilityAttribute))
        {
            componentKind = ComponentKind.Utility;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(attributeType, symbols.GetUtilitiesAttribute))
        {
            componentKind = ComponentKind.Utility;
            expectsCollection = true;
            return true;
        }

        return false;
    }

    private static bool IsSupportedGetInvocationTarget(
        IMethodSymbol targetMethod,
        SymbolCache symbols)
    {
        if (targetMethod.ContainingType == null)
            return false;

        if (SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, symbols.ContextAwareServiceExtensions))
            return true;

        return SymbolHelpers.IsAssignableTo(targetMethod.ContainingType, symbols.IArchitectureContext);
    }

    private static INamedTypeSymbol? TryGetCollectionElementType(
        ITypeSymbol fieldType,
        SymbolCache symbols)
    {
        if (fieldType is not INamedTypeSymbol namedType)
            return null;

        if (!SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, symbols.IReadOnlyList))
            return null;

        return namedType.TypeArguments[0] as INamedTypeSymbol;
    }

    private enum ComponentKind
    {
        Model,
        System,
        Utility
    }

    private readonly record struct BindingRequest(
        ComponentKind Kind,
        INamedTypeSymbol OwnerType,
        INamedTypeSymbol ServiceType,
        Location Location);

    private sealed class SymbolCache
    {
        private SymbolCache(
            INamedTypeSymbol? architectureType,
            INamedTypeSymbol? iArchitectureType,
            INamedTypeSymbol? iArchitectureModuleType,
            INamedTypeSymbol? iArchitectureContextType,
            INamedTypeSymbol? iReadOnlyListType,
            INamedTypeSymbol? contextAwareServiceExtensionsType,
            INamedTypeSymbol? getModelAttribute,
            INamedTypeSymbol? getModelsAttribute,
            INamedTypeSymbol? getSystemAttribute,
            INamedTypeSymbol? getSystemsAttribute,
            INamedTypeSymbol? getUtilityAttribute,
            INamedTypeSymbol? getUtilitiesAttribute)
        {
            ArchitectureType = architectureType;
            IArchitectureType = iArchitectureType;
            IArchitectureModuleType = iArchitectureModuleType;
            IArchitectureContext = iArchitectureContextType;
            IReadOnlyList = iReadOnlyListType;
            ContextAwareServiceExtensions = contextAwareServiceExtensionsType;
            GetModelAttribute = getModelAttribute;
            GetModelsAttribute = getModelsAttribute;
            GetSystemAttribute = getSystemAttribute;
            GetSystemsAttribute = getSystemsAttribute;
            GetUtilityAttribute = getUtilityAttribute;
            GetUtilitiesAttribute = getUtilitiesAttribute;
        }

        public INamedTypeSymbol? ArchitectureType { get; }

        public INamedTypeSymbol? IArchitectureType { get; }

        public INamedTypeSymbol? IArchitectureModuleType { get; }

        public INamedTypeSymbol? IArchitectureContext { get; }

        public INamedTypeSymbol? IReadOnlyList { get; }

        public INamedTypeSymbol? ContextAwareServiceExtensions { get; }

        public INamedTypeSymbol? GetModelAttribute { get; }

        public INamedTypeSymbol? GetModelsAttribute { get; }

        public INamedTypeSymbol? GetSystemAttribute { get; }

        public INamedTypeSymbol? GetSystemsAttribute { get; }

        public INamedTypeSymbol? GetUtilityAttribute { get; }

        public INamedTypeSymbol? GetUtilitiesAttribute { get; }

        public bool IsReady =>
            ArchitectureType != null &&
            IArchitectureType != null &&
            IArchitectureModuleType != null &&
            IArchitectureContext != null &&
            IReadOnlyList != null &&
            ContextAwareServiceExtensions != null &&
            GetModelAttribute != null &&
            GetModelsAttribute != null &&
            GetSystemAttribute != null &&
            GetSystemsAttribute != null &&
            GetUtilityAttribute != null &&
            GetUtilitiesAttribute != null;

        public static SymbolCache Create(Compilation compilation)
        {
            return new SymbolCache(
                compilation.GetTypeByMetadataName($"{PathContests.CoreNamespace}.Architectures.Architecture"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.CoreAbstractionsNamespace}.Architectures.IArchitecture"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.CoreAbstractionsNamespace}.Architectures.IArchitectureModule"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.CoreAbstractionsNamespace}.Architectures.IArchitectureContext"),
                compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.CoreNamespace}.Extensions.ContextAwareServiceExtensions"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetModelAttribute"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetModelsAttribute"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetSystemAttribute"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetSystemsAttribute"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetUtilityAttribute"),
                compilation.GetTypeByMetadataName(
                    $"{PathContests.SourceGeneratorsAbstractionsPath}.Rule.GetUtilitiesAttribute"));
        }
    }

    private sealed class RegistrationIndex
    {
        private readonly Compilation _compilation;
        private readonly IReadOnlyDictionary<INamedTypeSymbol, ArchitectureRegistrationData> _registrations;

        private RegistrationIndex(
            Compilation compilation,
            IReadOnlyDictionary<INamedTypeSymbol, ArchitectureRegistrationData> registrations)
        {
            _compilation = compilation;
            _registrations = registrations;
        }

        public static RegistrationIndex Build(
            Compilation compilation,
            SymbolCache symbols)
        {
            var registrations =
                new Dictionary<INamedTypeSymbol, ArchitectureRegistrationData>(SymbolEqualityComparer.Default);

            foreach (var type in SymbolHelpers.EnumerateNamedTypes(compilation.Assembly.GlobalNamespace))
            {
                if (!SymbolHelpers.IsAssignableTo(type, symbols.ArchitectureType))
                    continue;

                if (type.IsAbstract)
                    continue;

                var data = AnalyzeArchitecture(compilation, symbols, type);
                if (data.IsEmpty)
                    continue;

                registrations[type] = data;
            }

            return new RegistrationIndex(compilation, registrations);
        }

        public bool TryGetOwningArchitecture(
            INamedTypeSymbol ownerType,
            out INamedTypeSymbol architectureType)
        {
            architectureType = default!;

            if (_registrations.ContainsKey(ownerType))
            {
                architectureType = ownerType;
                return true;
            }

            INamedTypeSymbol? candidate = null;
            foreach (var pair in _registrations)
            {
                if (!pair.Value.ContainsOwner(ownerType, _compilation))
                    continue;

                if (candidate != null)
                    return false;

                candidate = pair.Key;
            }

            if (candidate == null)
                return false;

            architectureType = candidate;
            return true;
        }

        public bool HasRegistration(
            INamedTypeSymbol architectureType,
            ComponentKind componentKind,
            INamedTypeSymbol serviceType)
        {
            if (!_registrations.TryGetValue(architectureType, out var data))
                return false;

            return data.HasRegistration(componentKind, serviceType, _compilation);
        }

        private static ArchitectureRegistrationData AnalyzeArchitecture(
            Compilation compilation,
            SymbolCache symbols,
            INamedTypeSymbol architectureType)
        {
            var data = new ArchitectureRegistrationData();
            var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var pendingMethods = new Queue<IMethodSymbol>();
            var visitedModules = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var rootMethod in GetArchitectureRootMethods(architectureType))
                pendingMethods.Enqueue(rootMethod);

            while (pendingMethods.Count > 0)
            {
                var method = pendingMethods.Dequeue();
                if (!visitedMethods.Add(method))
                    continue;

                foreach (var invocation in SymbolHelpers.GetInvocationOperations(method, compilation))
                {
                    if (TryGetRegistration(invocation, symbols, out var kind, out var registeredType))
                    {
                        data.Add(kind, registeredType);
                        continue;
                    }

                    if (TryGetInstalledModuleType(invocation, symbols, out var moduleType) &&
                        visitedModules.Add(moduleType))
                    {
                        AnalyzeModule(compilation, symbols, moduleType, data, visitedModules);
                        continue;
                    }

                    if (TryResolveArchitectureHelperMethod(invocation, architectureType, out var helperMethod))
                        pendingMethods.Enqueue(helperMethod);
                }
            }

            return data;
        }

        private static void AnalyzeModule(
            Compilation compilation,
            SymbolCache symbols,
            INamedTypeSymbol moduleType,
            ArchitectureRegistrationData data,
            ISet<INamedTypeSymbol> visitedModules)
        {
            var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var pendingMethods = new Queue<IMethodSymbol>();

            foreach (var rootMethod in GetModuleRootMethods(moduleType))
                pendingMethods.Enqueue(rootMethod);

            while (pendingMethods.Count > 0)
            {
                var method = pendingMethods.Dequeue();
                if (!visitedMethods.Add(method))
                    continue;

                foreach (var invocation in SymbolHelpers.GetInvocationOperations(method, compilation))
                {
                    if (TryGetRegistration(invocation, symbols, out var kind, out var registeredType))
                    {
                        data.Add(kind, registeredType);
                        continue;
                    }

                    if (TryGetInstalledModuleType(invocation, symbols, out var nestedModuleType) &&
                        visitedModules.Add(nestedModuleType))
                    {
                        AnalyzeModule(compilation, symbols, nestedModuleType, data, visitedModules);
                        continue;
                    }

                    if (TryResolveModuleHelperMethod(invocation, moduleType, out var helperMethod))
                        pendingMethods.Enqueue(helperMethod);
                }
            }
        }

        private static IEnumerable<IMethodSymbol> GetArchitectureRootMethods(INamedTypeSymbol architectureType)
        {
            foreach (var type in SymbolHelpers.EnumerateTypeHierarchy(architectureType))
            {
                foreach (var member in type.GetMembers())
                {
                    if (member is not IMethodSymbol method)
                        continue;

                    if (method.IsStatic)
                        continue;

                    if (method.Name is not ("OnInitialize" or "InstallModules"))
                        continue;

                    if (method.DeclaringSyntaxReferences.Length == 0)
                        continue;

                    yield return method;
                }
            }
        }

        private static IEnumerable<IMethodSymbol> GetModuleRootMethods(INamedTypeSymbol moduleType)
        {
            foreach (var type in SymbolHelpers.EnumerateTypeHierarchy(moduleType))
            {
                foreach (var member in type.GetMembers("Install").OfType<IMethodSymbol>())
                {
                    if (member.IsStatic || member.Parameters.Length != 1)
                        continue;

                    if (member.DeclaringSyntaxReferences.Length == 0)
                        continue;

                    yield return member;
                }
            }
        }

        private static bool TryGetInstalledModuleType(
            IInvocationOperation invocation,
            SymbolCache symbols,
            out INamedTypeSymbol moduleType)
        {
            moduleType = default!;

            if (invocation.Arguments.Length == 0)
                return false;

            if (invocation.TargetMethod.Name is not ("InstallModule" or "InstallGodotModule"))
                return false;

            var candidateType = SymbolHelpers.TryGetCreatedType(invocation.Arguments[0].Value);
            if (candidateType == null)
                return false;

            if (!SymbolHelpers.IsAssignableTo(candidateType, symbols.IArchitectureModuleType))
                return false;

            moduleType = candidateType;
            return true;
        }

        private static bool TryGetRegistration(
            IInvocationOperation invocation,
            SymbolCache symbols,
            out ComponentKind componentKind,
            out INamedTypeSymbol registeredType)
        {
            componentKind = default;
            registeredType = default!;

            var targetMethod = invocation.TargetMethod;
            if (!targetMethod.IsGenericMethod || targetMethod.TypeArguments.Length != 1)
                return false;

            if (targetMethod.TypeArguments[0] is not INamedTypeSymbol namedType)
                return false;

            if (string.Equals(targetMethod.Name, "RegisterModel", StringComparison.Ordinal) &&
                SymbolHelpers.IsAssignableTo(targetMethod.ContainingType, symbols.IArchitectureType))
            {
                componentKind = ComponentKind.Model;
                registeredType = namedType;
                return true;
            }

            if (string.Equals(targetMethod.Name, "RegisterSystem", StringComparison.Ordinal) &&
                SymbolHelpers.IsAssignableTo(targetMethod.ContainingType, symbols.IArchitectureType))
            {
                componentKind = ComponentKind.System;
                registeredType = namedType;
                return true;
            }

            if (string.Equals(targetMethod.Name, "RegisterUtility", StringComparison.Ordinal) &&
                SymbolHelpers.IsAssignableTo(targetMethod.ContainingType, symbols.IArchitectureType))
            {
                componentKind = ComponentKind.Utility;
                registeredType = namedType;
                return true;
            }

            return false;
        }

        private static bool TryResolveArchitectureHelperMethod(
            IInvocationOperation invocation,
            INamedTypeSymbol architectureType,
            out IMethodSymbol helperMethod)
        {
            return TryResolveHelperMethod(invocation, architectureType, out helperMethod);
        }

        private static bool TryResolveModuleHelperMethod(
            IInvocationOperation invocation,
            INamedTypeSymbol moduleType,
            out IMethodSymbol helperMethod)
        {
            return TryResolveHelperMethod(invocation, moduleType, out helperMethod);
        }

        /// <summary>
        ///     解析架构/模块分析中的辅助方法调用。
        ///     普通虚调用应跟随到具体类型上的 override，而显式 <c>base.Xxx()</c> 必须保留基类语义。
        /// </summary>
        private static bool TryResolveHelperMethod(
            IInvocationOperation invocation,
            INamedTypeSymbol concreteType,
            out IMethodSymbol helperMethod)
        {
            helperMethod = default!;
            var targetMethod = invocation.TargetMethod;

            if (targetMethod.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor))
                return false;

            if (!SymbolHelpers.IsWithinTypeHierarchy(targetMethod.ContainingType, concreteType))
                return false;

            if (SymbolHelpers.IsExplicitBaseInvocation(invocation))
            {
                helperMethod = targetMethod;
                return helperMethod.DeclaringSyntaxReferences.Length > 0;
            }

            helperMethod = SymbolHelpers.ResolveHierarchyMethodImplementation(targetMethod, concreteType) ??
                           targetMethod;
            return helperMethod.DeclaringSyntaxReferences.Length > 0;
        }
    }

    private sealed class ArchitectureRegistrationData
    {
        private readonly HashSet<INamedTypeSymbol> _models = new(SymbolEqualityComparer.Default);
        private readonly HashSet<INamedTypeSymbol> _systems = new(SymbolEqualityComparer.Default);
        private readonly HashSet<INamedTypeSymbol> _utilities = new(SymbolEqualityComparer.Default);

        public bool IsEmpty => _models.Count == 0 && _systems.Count == 0 && _utilities.Count == 0;

        public void Add(ComponentKind componentKind, INamedTypeSymbol registeredType)
        {
            GetCollection(componentKind).Add(registeredType);
        }

        public bool HasRegistration(
            ComponentKind componentKind,
            INamedTypeSymbol requestedType,
            Compilation compilation)
        {
            return GetCollection(componentKind).Any(candidate =>
                SymbolHelpers.IsServiceCompatible(candidate, requestedType, compilation));
        }

        public bool ContainsOwner(
            INamedTypeSymbol ownerType,
            Compilation compilation)
        {
            return _models.Any(candidate => SymbolHelpers.IsOwnershipCompatible(ownerType, candidate, compilation)) ||
                   _systems.Any(candidate => SymbolHelpers.IsOwnershipCompatible(ownerType, candidate, compilation)) ||
                   _utilities.Any(candidate => SymbolHelpers.IsOwnershipCompatible(ownerType, candidate, compilation));
        }

        private ISet<INamedTypeSymbol> GetCollection(ComponentKind componentKind)
        {
            return componentKind switch
            {
                ComponentKind.Model => _models,
                ComponentKind.System => _systems,
                ComponentKind.Utility => _utilities,
                _ => throw new ArgumentOutOfRangeException(nameof(componentKind))
            };
        }
    }

    private static class SymbolHelpers
    {
        public static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var nestedType in EnumerateNamedTypes(namespaceMember))
                    yield return nestedType;
            }

            foreach (var typeMember in namespaceSymbol.GetTypeMembers())
            {
                yield return typeMember;

                foreach (var nestedType in EnumerateNestedTypes(typeMember))
                    yield return nestedType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> EnumerateTypeHierarchy(INamedTypeSymbol type)
        {
            for (var current = type; current != null; current = current.BaseType)
                yield return current;
        }

        public static bool IsAssignableTo(ITypeSymbol? fromType, ITypeSymbol? toType)
        {
            if (fromType == null || toType == null)
                return false;

            if (SymbolEqualityComparer.Default.Equals(fromType, toType))
                return true;

            return fromType.AllInterfaces.Any(interfaceType =>
                       SymbolEqualityComparer.Default.Equals(interfaceType, toType)) ||
                   EnumerateBaseTypes(fromType).Any(baseType =>
                       SymbolEqualityComparer.Default.Equals(baseType, toType));
        }

        public static bool IsWithinTypeHierarchy(
            INamedTypeSymbol? candidateType,
            INamedTypeSymbol ownerType)
        {
            if (candidateType == null)
                return false;

            return EnumerateTypeHierarchy(ownerType).Any(type =>
                SymbolEqualityComparer.Default.Equals(type, candidateType));
        }

        public static bool IsExplicitBaseInvocation(IInvocationOperation invocation)
        {
            return invocation.Syntax is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: BaseExpressionSyntax
                }
            };
        }

        public static IMethodSymbol? ResolveHierarchyMethodImplementation(
            IMethodSymbol method,
            INamedTypeSymbol ownerType)
        {
            foreach (var type in EnumerateTypeHierarchy(ownerType))
            {
                foreach (var candidate in type.GetMembers(method.Name).OfType<IMethodSymbol>())
                {
                    if (!HasCompatibleSignature(candidate, method))
                        continue;

                    if (SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, method.OriginalDefinition))
                        return candidate;

                    for (var overridden = candidate.OverriddenMethod;
                         overridden != null;
                         overridden = overridden.OverriddenMethod)
                    {
                        if (SymbolEqualityComparer.Default.Equals(overridden.OriginalDefinition,
                                method.OriginalDefinition))
                            return candidate;
                    }
                }
            }

            return null;
        }

        public static IEnumerable<IInvocationOperation> GetInvocationOperations(
            IMethodSymbol method,
            Compilation compilation)
        {
            foreach (var syntaxReference in method.DeclaringSyntaxReferences)
            {
                var syntax = syntaxReference.GetSyntax();
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                var operation = syntax switch
                {
                    MethodDeclarationSyntax { Body: not null } methodDeclaration =>
                        semanticModel.GetOperation(methodDeclaration.Body),
                    MethodDeclarationSyntax { ExpressionBody: not null } methodDeclaration =>
                        semanticModel.GetOperation(methodDeclaration.ExpressionBody.Expression),
                    ConstructorDeclarationSyntax { Body: not null } constructorDeclaration =>
                        semanticModel.GetOperation(constructorDeclaration.Body),
                    ConstructorDeclarationSyntax { ExpressionBody: not null } constructorDeclaration =>
                        semanticModel.GetOperation(constructorDeclaration.ExpressionBody.Expression),
                    _ => null
                };

                if (operation == null)
                    continue;

                foreach (var invocation in operation.DescendantsAndSelf().OfType<IInvocationOperation>())
                    yield return invocation;
            }
        }

        public static INamedTypeSymbol? TryGetCreatedType(IOperation operation)
        {
            var current = operation;

            while (current is IConversionOperation conversionOperation)
                current = conversionOperation.Operand;

            return current switch
            {
                IObjectCreationOperation objectCreationOperation => objectCreationOperation.Type as INamedTypeSymbol,
                _ => null
            };
        }

        public static bool IsServiceCompatible(
            INamedTypeSymbol registeredType,
            INamedTypeSymbol requestedType,
            Compilation compilation)
        {
            if (IsAssignableTo(registeredType, requestedType))
                return true;

            return compilation.ClassifyConversion(registeredType, requestedType).IsImplicit;
        }

        public static bool IsOwnershipCompatible(
            INamedTypeSymbol ownerType,
            INamedTypeSymbol registeredType,
            Compilation compilation)
        {
            if (IsAssignableTo(ownerType, registeredType) || IsAssignableTo(registeredType, ownerType))
                return true;

            return compilation.ClassifyConversion(ownerType, registeredType).IsImplicit ||
                   compilation.ClassifyConversion(registeredType, ownerType).IsImplicit;
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nestedType in type.GetTypeMembers())
            {
                yield return nestedType;

                foreach (var childType in EnumerateNestedTypes(nestedType))
                    yield return childType;
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateBaseTypes(ITypeSymbol type)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
                yield return current;
        }

        private static bool HasCompatibleSignature(
            IMethodSymbol candidate,
            IMethodSymbol target)
        {
            if (candidate.Parameters.Length != target.Parameters.Length)
                return false;

            if (candidate.TypeParameters.Length != target.TypeParameters.Length)
                return false;

            return true;
        }
    }
}
