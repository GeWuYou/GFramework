// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using GFramework.Godot.SourceGenerators.Diagnostics;
using GFramework.SourceGenerators.Common.Constants;
using GFramework.SourceGenerators.Common.Diagnostics;
using GFramework.SourceGenerators.Common.Extensions;

namespace GFramework.Godot.SourceGenerators;

/// <summary>
///     为带有 <c>[BindNodeSignal]</c> 的方法生成 Godot 节点事件绑定与解绑逻辑。
/// </summary>
[Generator]
public sealed class BindNodeSignalGenerator : IIncrementalGenerator
{
    private const string BindNodeSignalAttributeMetadataName =
        $"{PathContests.GodotSourceGeneratorsAbstractionsPath}.BindNodeSignalAttribute";

    private const string BindMethodName = "__BindNodeSignals_Generated";
    private const string UnbindMethodName = "__UnbindNodeSignals_Generated";

    /// <summary>
    ///     初始化增量生成器。
    /// </summary>
    /// <param name="context">生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidate(node),
                static (ctx, _) => Transform(ctx))
            .Where(static candidate => candidate is not null);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(compilationAndCandidates,
            static (spc, pair) => Execute(spc, pair.Left, pair.Right));
    }

    private static bool IsCandidate(SyntaxNode node)
    {
        if (node is not MethodDeclarationSyntax methodDeclaration)
            return false;

        return methodDeclaration.AttributeLists
            .SelectMany(static list => list.Attributes)
            .Any(static attribute => IsBindNodeSignalAttributeName(attribute.Name));
    }

    private static MethodCandidate? Transform(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration) is not IMethodSymbol methodSymbol)
            return null;

        return new MethodCandidate(methodDeclaration, methodSymbol);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<MethodCandidate?> candidates)
    {
        if (candidates.IsDefaultOrEmpty)
            return;

        var bindNodeSignalAttribute = compilation.GetTypeByMetadataName(BindNodeSignalAttributeMetadataName);
        var godotNodeSymbol = compilation.GetTypeByMetadataName("Godot.Node");

        if (bindNodeSignalAttribute is null || godotNodeSymbol is null)
            return;

        var methodAttributes = BuildMethodAttributeMap(candidates, bindNodeSignalAttribute);
        var methodCandidates = CollectMethodCandidates(methodAttributes);

        foreach (var group in GroupByContainingType(methodCandidates))
        {
            var typeSymbol = group.TypeSymbol;
            if (!CanGenerateForType(context, group, typeSymbol))
                continue;

            if (typeSymbol.ReportGeneratedMethodConflicts(
                    context,
                    group.Methods[0].Method.Identifier.GetLocation(),
                    BindMethodName,
                    UnbindMethodName))
                continue;

            var bindings = CollectBindings(context, group, methodAttributes, godotNodeSymbol);
            if (bindings.Count == 0)
                continue;

            ReportMissingLifecycleHookCall(
                context,
                group,
                typeSymbol,
                "_Ready",
                BindMethodName,
                BindNodeSignalDiagnostics.ManualReadyHookRequired);

            ReportMissingLifecycleHookCall(
                context,
                group,
                typeSymbol,
                "_ExitTree",
                UnbindMethodName,
                BindNodeSignalDiagnostics.ManualExitTreeHookRequired);

            context.AddSource(GetHintName(typeSymbol), GenerateSource(typeSymbol, bindings));
        }
    }

    private static bool CanGenerateForType(
        SourceProductionContext context,
        TypeGroup group,
        INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                BindNodeSignalDiagnostics.NestedClassNotSupported,
                group.Methods[0].Method.Identifier.GetLocation(),
                typeSymbol.Name));
            return false;
        }

        if (typeSymbol.AreAllDeclarationsPartial())
            return true;

        context.ReportDiagnostic(Diagnostic.Create(
            CommonDiagnostics.ClassMustBePartial,
            group.Methods[0].Method.Identifier.GetLocation(),
            typeSymbol.Name));

        return false;
    }

    private static bool TryCreateBinding(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        INamedTypeSymbol godotNodeSymbol,
        out SignalBindingInfo binding)
    {
        binding = default!;

        if (candidate.MethodSymbol.IsStatic)
        {
            ReportStaticMethodDiagnostic(context, candidate, attribute);
            return false;
        }

        if (!TryResolveBindingTargetNames(context, candidate, attribute, out var nodeFieldName, out var signalName))
            return false;

        if (!TryFindCompatibleField(context, candidate, attribute, godotNodeSymbol, nodeFieldName, out var fieldSymbol))
            return false;

        if (!TryFindCompatibleEvent(context, candidate, attribute, fieldSymbol, signalName, out var eventSymbol))
            return false;

        if (!IsMethodCompatibleWithEvent(candidate.MethodSymbol, eventSymbol))
        {
            ReportIncompatibleSignatureDiagnostic(context, candidate, attribute, eventSymbol, fieldSymbol);
            return false;
        }

        binding = new SignalBindingInfo(fieldSymbol, eventSymbol, candidate.MethodSymbol);
        return true;
    }

    private static Dictionary<MethodCandidate, IReadOnlyList<AttributeData>> BuildMethodAttributeMap(
        ImmutableArray<MethodCandidate?> candidates,
        INamedTypeSymbol bindNodeSignalAttribute)
    {
        return candidates
            .Where(static candidate => candidate is not null)
            .Select(static candidate => candidate!)
            .ToDictionary(
                static candidate => candidate,
                candidate => ResolveAttributes(candidate.MethodSymbol, bindNodeSignalAttribute),
                ReferenceEqualityComparer.Instance);
    }

    private static List<MethodCandidate> CollectMethodCandidates(
        IReadOnlyDictionary<MethodCandidate, IReadOnlyList<AttributeData>> methodAttributes)
    {
        return methodAttributes
            .Where(static pair => pair.Value.Count > 0)
            .Select(static pair => pair.Key)
            .ToList();
    }

    private static List<SignalBindingInfo> CollectBindings(
        SourceProductionContext context,
        TypeGroup group,
        IReadOnlyDictionary<MethodCandidate, IReadOnlyList<AttributeData>> methodAttributes,
        INamedTypeSymbol godotNodeSymbol)
    {
        var bindings = new List<SignalBindingInfo>();

        foreach (var candidate in group.Methods)
        {
            foreach (var attribute in methodAttributes[candidate])
            {
                if (!TryCreateBinding(context, candidate, attribute, godotNodeSymbol, out var binding))
                    continue;

                bindings.Add(binding);
            }
        }

        return bindings;
    }

    private static void ReportStaticMethodDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.StaticMethodNotSupported,
            candidate,
            attribute,
            candidate.MethodSymbol.Name);
    }

    private static bool TryResolveBindingTargetNames(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        out string nodeFieldName,
        out string signalName)
    {
        nodeFieldName = string.Empty;
        signalName = string.Empty;

        if (!TryResolveCtorString(attribute, 0, out nodeFieldName))
        {
            ReportInvalidConstructorArgumentDiagnostic(context, candidate, attribute, "nodeFieldName");
            return false;
        }

        if (!TryResolveCtorString(attribute, 1, out signalName))
        {
            ReportInvalidConstructorArgumentDiagnostic(context, candidate, attribute, "signalName");
            return false;
        }

        return true;
    }

    private static void ReportInvalidConstructorArgumentDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        string argumentName)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.InvalidConstructorArgument,
            candidate,
            attribute,
            candidate.MethodSymbol.Name,
            argumentName);
    }

    private static bool TryFindCompatibleField(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        INamedTypeSymbol godotNodeSymbol,
        string nodeFieldName,
        out IFieldSymbol fieldSymbol)
    {
        fieldSymbol = null!;

        var resolvedField = FindField(candidate.MethodSymbol.ContainingType, nodeFieldName);
        if (resolvedField is null)
        {
            ReportNodeFieldNotFoundDiagnostic(context, candidate, attribute, nodeFieldName);
            return false;
        }

        if (resolvedField.IsStatic)
        {
            ReportNodeFieldMustBeInstanceDiagnostic(context, candidate, attribute, resolvedField);
            return false;
        }

        if (!resolvedField.Type.IsAssignableTo(godotNodeSymbol))
        {
            ReportFieldTypeMustDeriveFromNodeDiagnostic(context, candidate, attribute, resolvedField);
            return false;
        }

        fieldSymbol = resolvedField;
        return true;
    }

    private static void ReportNodeFieldNotFoundDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        string nodeFieldName)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.NodeFieldNotFound,
            candidate,
            attribute,
            candidate.MethodSymbol.Name,
            nodeFieldName,
            candidate.MethodSymbol.ContainingType.Name);
    }

    private static void ReportNodeFieldMustBeInstanceDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        IFieldSymbol fieldSymbol)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.NodeFieldMustBeInstanceField,
            candidate,
            attribute,
            candidate.MethodSymbol.Name,
            fieldSymbol.Name);
    }

    private static void ReportFieldTypeMustDeriveFromNodeDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        IFieldSymbol fieldSymbol)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.FieldTypeMustDeriveFromNode,
            candidate,
            attribute,
            fieldSymbol.Name);
    }

    private static bool TryFindCompatibleEvent(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        IFieldSymbol fieldSymbol,
        string signalName,
        out IEventSymbol eventSymbol)
    {
        eventSymbol = null!;

        var resolvedEvent = FindEvent(fieldSymbol.Type, signalName);
        if (resolvedEvent is null)
        {
            ReportSignalNotFoundDiagnostic(context, candidate, attribute, fieldSymbol, signalName);
            return false;
        }

        eventSymbol = resolvedEvent;
        return true;
    }

    private static void ReportSignalNotFoundDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        IFieldSymbol fieldSymbol,
        string signalName)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.SignalNotFound,
            candidate,
            attribute,
            fieldSymbol.Name,
            signalName);
    }

    private static void ReportIncompatibleSignatureDiagnostic(
        SourceProductionContext context,
        MethodCandidate candidate,
        AttributeData attribute,
        IEventSymbol eventSymbol,
        IFieldSymbol fieldSymbol)
    {
        ReportMethodDiagnostic(
            context,
            BindNodeSignalDiagnostics.MethodSignatureNotCompatible,
            candidate,
            attribute,
            candidate.MethodSymbol.Name,
            eventSymbol.Name,
            fieldSymbol.Name);
    }

    private static void ReportMethodDiagnostic(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        MethodCandidate candidate,
        AttributeData attribute,
        params object[] messageArgs)
    {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                       candidate.Method.Identifier.GetLocation();

        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }

    private static bool TryResolveCtorString(
        AttributeData attribute,
        int index,
        out string value)
    {
        value = string.Empty;

        if (attribute.ConstructorArguments.Length <= index)
            return false;

        var ctorArgument = attribute.ConstructorArguments[index];
        if (ctorArgument.Kind != TypedConstantKind.Primitive || ctorArgument.Value is not string ctorString)
            return false;

        if (string.IsNullOrWhiteSpace(ctorString))
            return false;

        value = ctorString;
        return true;
    }

    private static IReadOnlyList<AttributeData> ResolveAttributes(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol bindNodeSignalAttribute)
    {
        return methodSymbol.GetAttributes()
            .Where(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, bindNodeSignalAttribute))
            .ToList();
    }

    private static IFieldSymbol? FindField(
        INamedTypeSymbol typeSymbol,
        string nodeFieldName)
    {
        return typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field => string.Equals(field.Name, nodeFieldName, StringComparison.Ordinal));
    }

    private static IEventSymbol? FindEvent(
        ITypeSymbol typeSymbol,
        string signalName)
    {
        for (var current = typeSymbol as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            var eventSymbol = current.GetMembers()
                .OfType<IEventSymbol>()
                .FirstOrDefault(evt => string.Equals(evt.Name, signalName, StringComparison.Ordinal));

            if (eventSymbol is not null)
                return eventSymbol;
        }

        return null;
    }

    private static bool IsMethodCompatibleWithEvent(
        IMethodSymbol methodSymbol,
        IEventSymbol eventSymbol)
    {
        if (!methodSymbol.ReturnsVoid)
            return false;

        if (methodSymbol.TypeParameters.Length > 0)
            return false;

        if (eventSymbol.Type is not INamedTypeSymbol delegateType)
            return false;

        var invokeMethod = delegateType.DelegateInvokeMethod;
        if (invokeMethod is null || !invokeMethod.ReturnsVoid)
            return false;

        if (methodSymbol.Parameters.Length != invokeMethod.Parameters.Length)
            return false;

        // 这里采用“精确签名匹配”而不是宽松推断，确保生成代码的订阅行为可预测且诊断明确。
        for (var index = 0; index < methodSymbol.Parameters.Length; index++)
        {
            var methodParameter = methodSymbol.Parameters[index];
            var delegateParameter = invokeMethod.Parameters[index];

            if (methodParameter.RefKind != delegateParameter.RefKind)
                return false;

            var methodParameterType = methodParameter.Type.WithNullableAnnotation(NullableAnnotation.None);
            var delegateParameterType = delegateParameter.Type.WithNullableAnnotation(NullableAnnotation.None);

            if (!SymbolEqualityComparer.Default.Equals(methodParameterType, delegateParameterType))
                return false;
        }

        return true;
    }

    private static void ReportMissingLifecycleHookCall(
        SourceProductionContext context,
        TypeGroup group,
        INamedTypeSymbol typeSymbol,
        string lifecycleMethodName,
        string generatedMethodName,
        DiagnosticDescriptor descriptor)
    {
        var lifecycleMethod = FindLifecycleMethod(typeSymbol, lifecycleMethodName);
        if (lifecycleMethod is null || CallsGeneratedMethod(lifecycleMethod, generatedMethodName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            lifecycleMethod.Locations.FirstOrDefault() ?? group.Methods[0].Method.Identifier.GetLocation(),
            typeSymbol.Name));
    }

    private static IMethodSymbol? FindLifecycleMethod(
        INamedTypeSymbol typeSymbol,
        string methodName)
    {
        return typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(method => IsParameterlessInstanceMethod(method, methodName));
    }

    private static bool CallsGeneratedMethod(
        IMethodSymbol methodSymbol,
        string generatedMethodName)
    {
        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
                continue;

            if (methodSyntax.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => IsGeneratedMethodInvocation(invocation, generatedMethodName)))
                return true;
        }

        return false;
    }

    private static bool IsGeneratedMethodInvocation(
        InvocationExpressionSyntax invocation,
        string generatedMethodName)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax identifierName => string.Equals(
                identifierName.Identifier.ValueText,
                generatedMethodName,
                StringComparison.Ordinal),
            MemberAccessExpressionSyntax memberAccess => string.Equals(
                memberAccess.Name.Identifier.ValueText,
                generatedMethodName,
                StringComparison.Ordinal),
            _ => false
        };
    }

    private static bool IsParameterlessInstanceMethod(IMethodSymbol method, string methodName)
    {
        return string.Equals(method.Name, methodName, StringComparison.Ordinal) &&
               !method.IsStatic &&
               method.Parameters.Length == 0 &&
               method.MethodKind == MethodKind.Ordinary;
    }

    private static bool IsBindNodeSignalAttributeName(NameSyntax attributeName)
    {
        var simpleName = GetAttributeSimpleName(attributeName);
        return simpleName is "BindNodeSignal" or "BindNodeSignalAttribute";
    }

    private static string? GetAttributeSimpleName(NameSyntax attributeName)
    {
        return attributeName switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            QualifiedNameSyntax qualifiedName => GetAttributeSimpleName(qualifiedName.Right),
            AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name.Identifier.ValueText,
            _ => null
        };
    }

    private static string GenerateSource(
        INamedTypeSymbol typeSymbol,
        IReadOnlyList<SignalBindingInfo> bindings)
    {
        var namespaceName = typeSymbol.GetNamespace();
        var generics = typeSymbol.ResolveGenerics();

        var sb = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendLine("#nullable enable");

        if (namespaceName is not null)
        {
            sb.AppendLine()
                .AppendLine($"namespace {namespaceName};");
        }

        sb.AppendLine()
            .AppendLine($"partial class {typeSymbol.Name}{generics.Parameters}");

        foreach (var constraint in generics.Constraints)
            sb.AppendLine($"    {constraint}");

        sb.AppendLine("{")
            .AppendLine($"    private void {BindMethodName}()")
            .AppendLine("    {");

        foreach (var binding in bindings)
            sb.AppendLine(
                $"        {binding.FieldSymbol.Name}.{binding.EventSymbol.Name} += {binding.MethodSymbol.Name};");

        sb.AppendLine("    }")
            .AppendLine()
            .AppendLine($"    private void {UnbindMethodName}()")
            .AppendLine("    {");

        foreach (var binding in bindings)
            sb.AppendLine(
                $"        {binding.FieldSymbol.Name}.{binding.EventSymbol.Name} -= {binding.MethodSymbol.Name};");

        sb.AppendLine("    }")
            .AppendLine("}");

        return sb.ToString();
    }

    private static string GetHintName(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", string.Empty)
            .Replace(".", "_") + ".BindNodeSignal.g.cs";
    }

    private static IReadOnlyList<TypeGroup> GroupByContainingType(IEnumerable<MethodCandidate> candidates)
    {
        var groupMap = new Dictionary<INamedTypeSymbol, TypeGroup>(SymbolEqualityComparer.Default);
        var orderedGroups = new List<TypeGroup>();

        foreach (var candidate in candidates)
        {
            var typeSymbol = candidate.MethodSymbol.ContainingType;
            if (!groupMap.TryGetValue(typeSymbol, out var group))
            {
                group = new TypeGroup(typeSymbol);
                groupMap.Add(typeSymbol, group);
                orderedGroups.Add(group);
            }

            group.Methods.Add(candidate);
        }

        return orderedGroups;
    }

    private sealed class MethodCandidate
    {
        public MethodCandidate(
            MethodDeclarationSyntax method,
            IMethodSymbol methodSymbol)
        {
            Method = method;
            MethodSymbol = methodSymbol;
        }

        public MethodDeclarationSyntax Method { get; }

        public IMethodSymbol MethodSymbol { get; }
    }

    private sealed class SignalBindingInfo
    {
        public SignalBindingInfo(
            IFieldSymbol fieldSymbol,
            IEventSymbol eventSymbol,
            IMethodSymbol methodSymbol)
        {
            FieldSymbol = fieldSymbol;
            EventSymbol = eventSymbol;
            MethodSymbol = methodSymbol;
        }

        public IFieldSymbol FieldSymbol { get; }

        public IEventSymbol EventSymbol { get; }

        public IMethodSymbol MethodSymbol { get; }
    }

    private sealed class TypeGroup
    {
        public TypeGroup(INamedTypeSymbol typeSymbol)
        {
            TypeSymbol = typeSymbol;
            Methods = new List<MethodCandidate>();
        }

        public INamedTypeSymbol TypeSymbol { get; }

        public List<MethodCandidate> Methods { get; }
    }

    /// <summary>
    ///     使用引用相等比较 MethodCandidate，确保缓存字典复用同一语法候选对象。
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<MethodCandidate>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(
            MethodCandidate? x,
            MethodCandidate? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(MethodCandidate obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
