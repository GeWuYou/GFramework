using Microsoft.CodeAnalysis.Diagnostics;

namespace GFramework.SourceGenerators.Tests.Core;

/// <summary>
///     提供 Roslyn 分析器测试的通用运行入口。
/// </summary>
/// <typeparam name="TAnalyzer">要验证的分析器类型。</typeparam>
public static class AnalyzerTestDriver<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    ///     运行分析器测试并断言期望诊断。
    /// </summary>
    /// <param name="source">测试输入源码。</param>
    /// <param name="diagnostics">期望诊断集合。</param>
    /// <returns>异步测试任务。</returns>
    public static Task RunAsync(
        string source,
        params DiagnosticResult[] diagnostics)
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            DisabledDiagnostics = { "GF_Common_Trace_001" }
        };

        test.ExpectedDiagnostics.AddRange(diagnostics);
        return test.RunAsync();
    }
}
