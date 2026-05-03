// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.IO;

namespace GFramework.SourceGenerators.Tests.Core;

/// <summary>
///     用于测试源代码生成器的快照测试类
/// </summary>
/// <typeparam name="TGenerator">要测试的源代码生成器类型</typeparam>
public static class GeneratorSnapshotTest<TGenerator>
    where TGenerator : new()
{
    /// <summary>
    ///     运行指定源生成器的端到端快照测试。
    /// </summary>
    /// <param name="source">输入的源代码字符串。</param>
    /// <param name="snapshotFolder">用于存放已提交快照文件的根目录。</param>
    /// <param name="snapshotFileNameSelector">将生成文件名映射为快照文件名的规则；为空时使用原始生成文件名。</param>
    /// <returns>当所有生成输出都通过快照校验后完成的异步任务。</returns>
    /// <remarks>
    ///     该辅助器会手动构建 Roslyn 编译并执行生成器，然后依次验证生成器自身诊断、更新后编译诊断、生成输出数量和快照内容。
    ///     若生成器报告错误、生成后的编译出现错误、生成器没有任何输出，或首次运行缺少快照文件，测试都会失败。
    ///     首次缺少快照时，本方法会先将当前输出写入 <paramref name="snapshotFolder" />，再通过断言中断测试，提示调用方提交快照资产。
    ///     <paramref name="snapshotFileNameSelector" /> 的返回值还必须保持在 <paramref name="snapshotFolder" /> 根目录之内，否则会抛出异常。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当快照文件名映射结果为空、为绝对路径，或逃逸出快照根目录时抛出。</exception>
    public static async Task RunAsync(
        string source,
        string snapshotFolder,
        Func<string, string>? snapshotFileNameSelector = null)
    {
        var (driver, updatedCompilation, generatorDiagnostics) = RunGenerator(source);
        AssertNoGeneratorErrors(generatorDiagnostics);
        AssertNoCompilationErrors(updatedCompilation);

        var generatedSources = GetGeneratedSources(driver);
        await AssertGeneratedSnapshotsAsync(generatedSources, snapshotFolder, snapshotFileNameSelector).ConfigureAwait(false);
    }

    /// <summary>
    ///     标准化文本内容，将换行符统一为\n并去除首尾空白
    /// </summary>
    /// <param name="text">要标准化的文本</param>
    /// <returns>标准化后的文本</returns>
    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }

    /// <summary>
    ///     构建测试编译并执行目标生成器，返回更新后的编译结果和生成器诊断。
    /// </summary>
    /// <param name="source">要交给生成器处理的输入源码。</param>
    /// <returns>包含驱动、更新后编译和生成器诊断的元组。</returns>
    private static (GeneratorDriver Driver, Compilation UpdatedCompilation, ImmutableArray<Diagnostic> GeneratorDiagnostics)
        RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CreateCompilation(syntaxTree);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [CreateGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var generatorDiagnostics);

        return (driver, updatedCompilation, generatorDiagnostics);
    }

    /// <summary>
    ///     为快照测试创建最小可运行的 Roslyn 编译上下文。
    /// </summary>
    /// <param name="syntaxTree">由测试输入生成的语法树。</param>
    /// <returns>包含运行时元数据引用的动态链接库编译对象。</returns>
    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
    {
        return CSharpCompilation.Create(
            $"{typeof(TGenerator).Name}SnapshotTests",
            [syntaxTree],
            MetadataReferenceTestBuilder.GetRuntimeMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    ///     断言生成器自身没有报告错误级诊断。
    /// </summary>
    /// <param name="generatorDiagnostics">生成器执行期间产生的诊断集合。</param>
    private static void AssertNoGeneratorErrors(ImmutableArray<Diagnostic> generatorDiagnostics)
    {
        var generatorErrors = generatorDiagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.That(
            generatorErrors,
            Is.Empty,
            () =>
                $"执行生成器时出现错误：{Environment.NewLine}{string.Join(Environment.NewLine, generatorErrors.Select(static diagnostic => diagnostic.ToString()))}");
    }

    /// <summary>
    ///     断言合并生成结果后的最终编译仍然可通过。
    /// </summary>
    /// <param name="updatedCompilation">已注入生成输出的编译对象。</param>
    private static void AssertNoCompilationErrors(Compilation updatedCompilation)
    {
        var compilationErrors = updatedCompilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.That(
            compilationErrors,
            Is.Empty,
            () =>
                $"编译生成的代码时出现错误：{Environment.NewLine}{string.Join(Environment.NewLine, compilationErrors.Select(static diagnostic => diagnostic.ToString()))}");
    }

    /// <summary>
    ///     收集并排序生成器输出，保持快照断言顺序稳定。
    /// </summary>
    /// <param name="driver">已经执行完成的生成器驱动。</param>
    /// <returns>按 HintName 排序后的生成文件名与内容。</returns>
    private static (string Filename, string Content)[] GetGeneratedSources(GeneratorDriver driver)
    {
        var generatedSources = driver.GetRunResult()
            .Results
            .SelectMany(static result => result.GeneratedSources)
            .OrderBy(static source => source.HintName, StringComparer.Ordinal)
            .Select(static source => (source.HintName, source.SourceText.ToString()))
            .ToArray();

        Assert.That(
            generatedSources,
            Is.Not.Empty,
            $"生成器 '{typeof(TGenerator).FullName}' 未产生任何输出。");

        return generatedSources;
    }

    /// <summary>
    ///     逐个比对生成输出与已提交快照，必要时写出缺失快照并中断测试。
    /// </summary>
    /// <param name="generatedSources">已排序的生成文件名与内容。</param>
    /// <param name="snapshotFolder">快照根目录。</param>
    /// <param name="snapshotFileNameSelector">可选的快照文件名映射规则。</param>
    /// <returns>当全部快照比对完成后结束的异步任务。</returns>
    private static async Task AssertGeneratedSnapshotsAsync(
        (string Filename, string Content)[] generatedSources,
        string snapshotFolder,
        Func<string, string>? snapshotFileNameSelector)
    {
        foreach (var (filename, content) in generatedSources)
        {
            var snapshotFileName = snapshotFileNameSelector?.Invoke(filename) ?? filename;
            var expected = await ReadExpectedSnapshotAsync(
                    snapshotFolder,
                    snapshotFileName,
                    content)
                .ConfigureAwait(false);

            Assert.That(
                Normalize(expected),
                Is.EqualTo(Normalize(content)),
                $"快照不匹配：{snapshotFileName}");
        }
    }

    /// <summary>
    ///     读取指定快照；若快照不存在，则先写出当前生成结果并通过断言提示调用方提交资产。
    /// </summary>
    /// <param name="snapshotFolder">快照根目录。</param>
    /// <param name="snapshotFileName">映射后的快照文件名。</param>
    /// <param name="generatedContent">当前生成器输出内容。</param>
    /// <returns>现有快照的文本内容。</returns>
    private static async Task<string> ReadExpectedSnapshotAsync(
        string snapshotFolder,
        string snapshotFileName,
        string generatedContent)
    {
        // 不同测试套件可能需要将生成文件映射到非 .cs 快照，以避免测试资产被当作可编译源码参与构建。
        var path = ResolveSnapshotPath(snapshotFolder, snapshotFileName);
        if (!File.Exists(path))
        {
            await WriteMissingSnapshotAndFailAsync(path, generatedContent).ConfigureAwait(false);
        }

        return await File.ReadAllTextAsync(path).ConfigureAwait(false);
    }

    /// <summary>
    ///     为首次运行缺失的快照写入当前结果，并立即终止测试以提醒提交新资产。
    /// </summary>
    /// <param name="path">目标快照绝对路径。</param>
    /// <param name="generatedContent">要写入的生成输出。</param>
    private static async Task WriteMissingSnapshotAndFailAsync(string path, string generatedContent)
    {
        // ResolveSnapshotPath 保证快照不会越界，但根目录路径仍会让 GetDirectoryName 返回 null。
        var snapshotDirectory = Path.GetDirectoryName(path)
                                ?? throw new InvalidOperationException(
                                    $"Snapshot path '{path}' must include a parent directory.");
        Directory.CreateDirectory(snapshotDirectory);
        await File.WriteAllTextAsync(path, generatedContent).ConfigureAwait(false);
        Assert.Fail($"未找到快照文件，已在以下路径生成新快照：\n{path}");
    }

    /// <summary>
    ///     创建可由 Roslyn 驱动直接执行的源生成器实例，并统一兼容经典与增量生成器。
    /// </summary>
    /// <returns>适配后的源生成器实例。</returns>
    /// <exception cref="InvalidOperationException">当测试类型既不是源生成器也不是增量生成器时抛出。</exception>
    private static ISourceGenerator CreateGenerator()
    {
        var generator = new TGenerator();
        return generator switch
        {
            ISourceGenerator sourceGenerator => sourceGenerator,
            IIncrementalGenerator incrementalGenerator => incrementalGenerator.AsSourceGenerator(),
            _ => throw new InvalidOperationException(
                $"Generator type '{typeof(TGenerator).FullName}' must implement {nameof(ISourceGenerator)} or {nameof(IIncrementalGenerator)}.")
        };
    }

    /// <summary>
    ///     解析并验证快照路径，确保文件名映射不会逃逸出当前快照根目录。
    /// </summary>
    /// <param name="snapshotFolder">快照根目录。</param>
    /// <param name="snapshotFileName">映射后的快照文件名。</param>
    /// <returns>可安全访问的快照绝对路径。</returns>
    /// <exception cref="InvalidOperationException">
    ///     当映射结果为空白、为绝对路径，或通过相对路径越界到快照目录之外时抛出。
    /// </exception>
    private static string ResolveSnapshotPath(string snapshotFolder, string snapshotFileName)
    {
        if (string.IsNullOrWhiteSpace(snapshotFileName) || Path.IsPathRooted(snapshotFileName))
        {
            throw new InvalidOperationException($"Invalid snapshot file name: {snapshotFileName}");
        }

        // 先规范化根目录再做包含关系判断，避免 `..` 或平台大小写差异导致的目录逃逸。
        var snapshotRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(snapshotFolder));
        var snapshotPath = Path.GetFullPath(Path.Combine(snapshotRoot, snapshotFileName));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!snapshotPath.StartsWith(snapshotRoot + Path.DirectorySeparatorChar, comparison))
        {
            throw new InvalidOperationException($"Snapshot path escapes root folder: {snapshotFileName}");
        }

        return snapshotPath;
    }
}
