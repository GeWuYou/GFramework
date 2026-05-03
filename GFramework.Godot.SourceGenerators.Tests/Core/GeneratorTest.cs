// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Godot.SourceGenerators.Tests.Core;

/// <summary>
///     提供源代码生成器测试的通用功能。
/// </summary>
/// <typeparam name="TGenerator">要测试的源代码生成器类型，必须具有无参构造函数。</typeparam>
public static class GeneratorTest<TGenerator>
    where TGenerator : new()
{
    /// <summary>
    ///     运行源代码生成器测试。
    /// </summary>
    /// <param name="source">输入源代码。</param>
    /// <param name="generatedSources">期望生成的源文件集合。</param>
    public static async Task RunAsync(
        string source,
        params (string filename, string content)[] generatedSources)
    {
        var test = new CSharpSourceGeneratorTest<TGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" }
        };

        foreach (var (filename, content) in generatedSources)
            test.TestState.GeneratedSources.Add(
                (typeof(TGenerator), filename, NormalizeLineEndings(content)));

        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     将测试内联快照统一为当前平台换行符，避免不同系统上的源生成输出比较出现伪差异。
    /// </summary>
    /// <param name="content">原始快照内容。</param>
    /// <returns>使用当前平台换行符的快照内容。</returns>
    private static string NormalizeLineEndings(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }
}
