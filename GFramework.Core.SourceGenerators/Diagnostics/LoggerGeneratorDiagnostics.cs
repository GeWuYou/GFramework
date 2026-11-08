// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Constants;
using Microsoft.CodeAnalysis;

namespace GFramework.Core.SourceGenerators.Diagnostics;

/// <summary>
///     提供 <c>[Log]</c> 日志生成器相关诊断。
/// </summary>
public static class LoggerGeneratorDiagnostics
{
    private const string LoggingRuleCategory = $"{PathContests.SourceGeneratorsPath}.Logging";

    /// <summary>
    ///     当 <c>[Log]</c> 标注的类型无法获得架构上下文（未实现 IContextAware、未派生
    ///     ContextAwareBase，也未标注 ContextAwareAttribute）时报告。
    /// </summary>
    /// <remarks>
    ///     新日志契约下不存在进程级解析器：生成代码必须经由绑定到当前实例的
    ///     架构上下文解析 ILoggerFactory。面向 DI 容器的普通服务应直接构造注入
    ///     ILogger&lt;T&gt;，而不是使用本特性。
    /// </remarks>
    public static readonly DiagnosticDescriptor ContextAwareRequired = new(
        "GF_Logger_001",
        "Log attribute requires an architecture context",
        "'{0}' uses [Log] but has no architecture context: implement IContextAware, derive from ContextAwareBase, or mark the type [ContextAware]; DI-activated services should inject ILogger<T> instead",
        LoggingRuleCategory,
        DiagnosticSeverity.Error,
isEnabledByDefault: true);
}
