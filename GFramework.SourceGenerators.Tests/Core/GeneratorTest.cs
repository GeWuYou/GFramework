namespace GFramework.SourceGenerators.Tests.Core;

/// <summary>
///     提供源代码生成器测试的通用功能
/// </summary>
/// <typeparam name="TGenerator">要测试的源代码生成器类型，必须具有无参构造函数</typeparam>
public static class GeneratorTest<TGenerator>
    where TGenerator : new()
{
    /// <summary>
    ///     运行源代码生成器测试
    /// </summary>
    /// <param name="source">输入的源代码</param>
    /// <param name="generatedSources">期望生成的源文件集合，包含文件名和内容的元组</param>
    /// <returns>异步操作任务</returns>
    public static Task RunAsync(
        string source,
        params (string filename, string content)[] generatedSources)
    {
        return RunAsync(
            source,
            additionalReferences: [],
            generatedSources);
    }

    /// <summary>
    ///     运行源代码生成器测试，并为测试编译显式追加元数据引用。
    /// </summary>
    /// <param name="source">输入的源代码。</param>
    /// <param name="additionalReferences">附加元数据引用，用于构造多程序集场景。</param>
    /// <param name="generatedSources">期望生成的源文件集合，包含文件名和内容的元组。</param>
    /// <returns>异步操作任务。</returns>
    public static Task RunAsync(
        string source,
        IEnumerable<MetadataReference> additionalReferences,
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

        // 添加期望的生成源文件到测试状态中
        foreach (var (filename, content) in generatedSources)
            test.TestState.GeneratedSources.Add(
                (typeof(TGenerator), filename, NormalizeLineEndings(content)));

        foreach (var additionalReference in additionalReferences)
            test.TestState.AdditionalReferences.Add(additionalReference);

        return test.RunAsync();
    }

    /// <summary>
    ///     将测试快照统一为当前平台换行符，避免不同系统上的源生成输出比较出现伪差异。
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
