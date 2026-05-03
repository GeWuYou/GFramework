// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Common.Extensions;

/// <summary>
///     提供 <see cref="ITypeSymbol" /> 的通用符号判断扩展。
/// </summary>
public static class ITypeSymbolExtensions
{
    /// <summary>
    ///     判断当前类型是否等于或实现/继承目标类型。
    /// </summary>
    /// <param name="typeSymbol">当前类型符号。</param>
    /// <param name="targetType">目标类型符号。</param>
    /// <returns>若等于、实现或继承则返回 <c>true</c>。</returns>
    public static bool IsAssignableTo(
        this ITypeSymbol typeSymbol,
        INamedTypeSymbol? targetType)
    {
        if (targetType is null)
            return false;

        if (SymbolEqualityComparer.Default.Equals(typeSymbol, targetType))
            return true;

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            if (namedType.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i, targetType)))
                return true;

            for (var current = namedType.BaseType; current is not null; current = current.BaseType)
                if (SymbolEqualityComparer.Default.Equals(current, targetType))
                    return true;
        }

        return false;
    }
}