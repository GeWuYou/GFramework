// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>Checks consumer runtime contracts before emitting optional strongly typed invocation metadata.</summary>
public sealed partial class CqrsHandlerRegistryGenerator
{
    /// <summary>Rejects mismatched runtime packages instead of generating a degraded registration.</summary>
    private static readonly DiagnosticDescriptor IncompatibleRuntimeContractDiagnostic = new(
        "GF_Cqrs_002",
        "CQRS runtime and generator contracts do not match",
        "The selected GFramework.Cqrs runtime does not provide the required typed invoker factories. Upgrade the runtime and source generator together; older runtime contracts are not supported.",
        "GFramework.Cqrs.SourceGenerators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Requires the exact accessible factory signature used by generated method-group conversions.</summary>
    /// <param name="compilation">The consumer compilation, including its selected runtime version.</param>
    /// <param name="descriptorMetadataName">The request or stream descriptor type.</param>
    /// <param name="stream">Whether the delegate returns an asynchronous sequence instead of a value task.</param>
    /// <returns>Whether optional typed invocation metadata can compile against the consumer runtime.</returns>
    private static bool HasTypedInvokerFactory(Compilation compilation, string descriptorMetadataName, bool stream)
    {
        var descriptor = compilation.GetTypeByMetadataName(descriptorMetadataName);
        var type = compilation.GetTypeByMetadataName("System.Type");
        var token = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        var func = compilation.GetTypeByMetadataName("System.Func`4");
        var result = compilation.GetTypeByMetadataName(stream
            ? "System.Collections.Generic.IAsyncEnumerable`1"
            : "System.Threading.Tasks.ValueTask`1");
        if (descriptor is null || type is null || token is null || func is null || result is null)
            return false;

        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        foreach (var method in descriptor.GetMembers("Create").OfType<IMethodSymbol>())
        {
            if (!method.IsStatic || method.DeclaredAccessibility != Accessibility.Public ||
                method.Arity != 1 || method.Parameters.Length != 2 ||
                method.ReturnsByRef || method.ReturnsByRefReadonly ||
                !SymbolEqualityComparer.Default.Equals(method.ReturnType, descriptor))
                continue;

            var response = method.TypeParameters[0];
            // Extra generic constraints cannot be assumed for arbitrary consumer responses.
            if (response.HasConstructorConstraint || response.HasReferenceTypeConstraint ||
                response.HasValueTypeConstraint || response.HasUnmanagedTypeConstraint ||
                response.HasNotNullConstraint || !response.ConstraintTypes.IsDefaultOrEmpty)
                continue;

            var expectedDelegate = func.Construct(objectType, objectType, token, result.Construct(response));
            if (method.Parameters.All(static parameter => parameter.RefKind == RefKind.None) &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, type) &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, expectedDelegate))
                return true;
        }

        // The caller reports GF_Cqrs_002 and emits no partial registry for this mismatch.
        return false;
    }
}
