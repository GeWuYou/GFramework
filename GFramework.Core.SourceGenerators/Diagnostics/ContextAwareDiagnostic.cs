// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.SourceGenerators.Diagnostics;

/// <summary>
///     提供与上下文感知相关的诊断规则定义
/// </summary>
public static class ContextAwareDiagnostic
{
    /// <summary>
    ///     诊断规则：ContextAwareAttribute只能应用于类
    /// </summary>
    public static readonly DiagnosticDescriptor ContextAwareOnlyForClass = new(
        "GF_Rule_001",
        "ContextAware can only be applied to class",
        "ContextAwareAttribute can only be applied to class '{0}'",
        "GFramework.SourceGenerators.Rule",
        DiagnosticSeverity.Error,
        true
    );
}
