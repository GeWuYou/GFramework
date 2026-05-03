// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GFramework.SourceGenerators.Common.Info;

/// <summary>
///     表示字段级生成器候选成员。
/// </summary>
/// <param name="Variable">字段变量语法节点。</param>
/// <param name="FieldSymbol">字段符号。</param>
public sealed record FieldCandidateInfo(
    VariableDeclaratorSyntax Variable,
    IFieldSymbol FieldSymbol
);