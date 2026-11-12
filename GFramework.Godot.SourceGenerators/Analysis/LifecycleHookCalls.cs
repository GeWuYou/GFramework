// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GFramework.Godot.SourceGenerators.Analysis;

/// <summary>Recognizes current-instance hook calls reachable through source-defined helper methods.</summary>
/// <remarks>
/// Generated methods are absent from the input compilation, so their receiver and name are checked syntactically.
/// Existing helper symbols identify same-type instance methods and directly invoked local functions. Deferred bodies
/// are visited only through such calls. Iterator bodies are excluded because invocation only creates an iterator.
/// This is structural call analysis, with execution order owned by the consumer.
/// </remarks>
internal static class LifecycleHookCalls
{
    /// <summary>Checks a handwritten lifecycle entry and its same-instance helper graph for a generated hook call.</summary>
    /// <param name="compilation">The generator's input compilation.</param>
    /// <param name="entry">The handwritten lifecycle method.</param>
    /// <param name="generatedMethodName">The reserved zero-argument generated method.</param>
    /// <param name="cancellationToken">Cancels analysis when the compilation is replaced.</param>
    /// <returns>Whether a supported current-instance call was found.</returns>
    internal static bool Contains(Compilation compilation, IMethodSymbol entry, string generatedMethodName,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<IMethodSymbol>();
        var visited = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        pending.Push(entry);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var method = pending.Pop().OriginalDefinition;
            method = method.PartialImplementationPart ?? method;
            if (!visited.Add(method))
                continue;

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var body = GetBody(reference.GetSyntax(cancellationToken));
                if (body is null)
                    continue;

                // A helper or local iterator runs on enumeration, which this call graph does not establish.
                // Ignore yields in nested deferred bodies when classifying the enclosing method.
                if (body.DescendantNodesAndSelf(DescendIntoExecutableBody).Any(node => node is YieldStatementSyntax))
                    continue;

                var model = compilation.GetSemanticModel(body.SyntaxTree);
                foreach (var node in body.DescendantNodesAndSelf(DescendIntoExecutableBody))
                {
                    if (node is not InvocationExpressionSyntax invocation || !HasCurrentReceiver(invocation))
                        continue;

                    var target = model.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
                    if (IsGeneratedCall(invocation, target, entry.ContainingType, generatedMethodName))
                        return true;

                    if (IsSourceHelper(target, entry.ContainingType))
                        pending.Push(target!);
                }
            }
        }
        return false;
    }

    private static SyntaxNode? GetBody(SyntaxNode declaration) => declaration switch
    {
        MethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody?.Expression,
        _ => null,
    };

    private static bool DescendIntoExecutableBody(SyntaxNode node) =>
        node is not LocalFunctionStatementSyntax and not AnonymousFunctionExpressionSyntax;

    private static bool HasCurrentReceiver(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        SimpleNameSyntax => true,
        MemberAccessExpressionSyntax member => IsThis(member.Expression),
        _ => false,
    };

    private static bool IsThis(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parentheses)
            expression = parentheses.Expression;
        return expression is ThisExpressionSyntax;
    }

    private static bool IsGeneratedCall(InvocationExpressionSyntax invocation, IMethodSymbol? target,
        INamedTypeSymbol owner, string generatedMethodName)
    {
        var name = invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax identifier } => identifier.Identifier.ValueText,
            _ => null,
        };
        // A resolved local function, delegate or inherited member can shadow the reserved name.
        return string.Equals(name, generatedMethodName, StringComparison.Ordinal) && invocation.ArgumentList.Arguments.Count == 0 &&
               (target is null || (target.MethodKind == MethodKind.Ordinary &&
                                  SymbolEqualityComparer.Default.Equals(target.ContainingType, owner)));
    }

    private static bool IsSourceHelper(IMethodSymbol? method, INamedTypeSymbol owner) =>
        method is { DeclaringSyntaxReferences.Length: > 0 } &&
        SymbolEqualityComparer.Default.Equals(method.ContainingType, owner) &&
        (method.MethodKind == MethodKind.LocalFunction || (method.MethodKind == MethodKind.Ordinary && !method.IsStatic));
}
