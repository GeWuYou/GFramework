// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.SourceGenerators.Common.Diagnostics;

namespace GFramework.SourceGenerators.Common.Extensions;

/// <summary>
///     提供生成方法名冲突校验的通用扩展。
/// </summary>
public static class GeneratedMethodConflictExtensions
{
    /// <summary>
    ///     检查目标类型上是否已存在与生成器保留方法同名的零参数方法，并在冲突时报告统一诊断。
    /// </summary>
    /// <param name="typeSymbol">待校验的目标类型。</param>
    /// <param name="context">源代码生成上下文。</param>
    /// <param name="fallbackLocation">当冲突成员缺少源码位置时使用的后备位置。</param>
    /// <param name="generatedMethodNames">生成器将保留的零参数方法名集合。</param>
    /// <returns>若发现任一冲突则返回 <c>true</c>。</returns>
    public static bool ReportGeneratedMethodConflicts(
        this INamedTypeSymbol typeSymbol,
        SourceProductionContext context,
        Location fallbackLocation,
        params string[] generatedMethodNames)
    {
        var hasConflict = false;

        foreach (var generatedMethodName in generatedMethodNames.Distinct(StringComparer.Ordinal))
        {
            var conflictingMethod = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(method =>
                    !method.IsImplicitlyDeclared &&
                    string.Equals(method.Name, generatedMethodName, StringComparison.Ordinal) &&
                    method.Parameters.Length == 0 &&
                    method.TypeParameters.Length == 0);

            if (conflictingMethod is null)
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                CommonDiagnostics.GeneratedMethodNameConflict,
                conflictingMethod.Locations.FirstOrDefault() ?? fallbackLocation,
                typeSymbol.Name,
                generatedMethodName));
            hasConflict = true;
        }

        return hasConflict;
    }
}