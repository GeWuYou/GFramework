using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace GFramework.SourceGenerators.Tests.core;

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
    public static async Task RunAsync(
        string source,
        params (string filename, string content)[] generatedSources)
    {
        var test = new CSharpSourceGeneratorTest<TGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            }
        };

        // 添加期望的生成源文件到测试状态中
        foreach (var (filename, content) in generatedSources)
            test.TestState.GeneratedSources.Add(
                (typeof(TGenerator), filename, content));

        await test.RunAsync();
    }
}