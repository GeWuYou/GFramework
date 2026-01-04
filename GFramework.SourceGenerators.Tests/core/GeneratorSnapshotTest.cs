using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace GFramework.SourceGenerators.Tests.core;

/// <summary>
///     用于测试源代码生成器的快照测试类
/// </summary>
/// <typeparam name="TGenerator">要测试的源代码生成器类型</typeparam>
public static class GeneratorSnapshotTest<TGenerator>
    where TGenerator : new()
{
    /// <summary>
    ///     运行源代码生成器的快照测试
    /// </summary>
    /// <param name="source">输入的源代码字符串</param>
    /// <param name="snapshotFolder">快照文件存储的文件夹路径</param>
    /// <returns>异步任务</returns>
    public static async Task RunAsync(
        string source,
        string snapshotFolder)
    {
        var test = new CSharpSourceGeneratorTest<TGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source }
            },
            TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
            DisabledDiagnostics = { "GF_Common_Trace_001" }
        };

        await test.RunAsync();

        var generated = test.TestState.GeneratedSources;

        foreach (var (filename, content) in generated)
        {
            var path = Path.Combine(
                snapshotFolder,
                filename);

            if (!File.Exists(path))
            {
                // 第一次运行：生成 snapshot
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, content.ToString());

                Assert.Fail(
                    $"Snapshot not found. Generated new snapshot at:\n{path}");
            }

            var expected = await File.ReadAllTextAsync(path);

            Assert.That(
                Normalize(expected),
                Is.EqualTo(Normalize(content.ToString())),
                $"Snapshot mismatch: {filename}");
        }
    }

    /// <summary>
    ///     标准化文本内容，将换行符统一为\n并去除首尾空白
    /// </summary>
    /// <param name="text">要标准化的文本</param>
    /// <returns>标准化后的文本</returns>
    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Trim();
    }
}