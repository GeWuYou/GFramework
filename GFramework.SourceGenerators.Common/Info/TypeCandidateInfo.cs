// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GFramework.SourceGenerators.Common.Info;

/// <summary>
///     表示类型级生成器候选成员。
/// </summary>
/// <param name="Declaration">类型声明语法节点。</param>
/// <param name="TypeSymbol">类型符号。</param>
public sealed record TypeCandidateInfo(
    ClassDeclarationSyntax Declaration,
    INamedTypeSymbol TypeSymbol
);