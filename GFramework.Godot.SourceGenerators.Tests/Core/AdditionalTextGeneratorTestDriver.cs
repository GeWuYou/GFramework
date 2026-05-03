// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace GFramework.Godot.SourceGenerators.Tests.Core;

/// <summary>
///     提供基于 <see cref="AdditionalText" /> 的源生成器测试驱动。
/// </summary>
public static class AdditionalTextGeneratorTestDriver
{
    /// <summary>
    ///     运行指定的增量生成器，并返回生成结果。
    /// </summary>
    /// <typeparam name="TGenerator">要运行的生成器类型。</typeparam>
    /// <param name="source">输入源码。</param>
    /// <param name="additionalFiles">AdditionalFiles 集合。</param>
    /// <returns>生成器运行结果。</returns>
    public static GeneratorDriverRunResult Run<TGenerator>(
        string source,
        params (string path, string content)[] additionalFiles)
        where TGenerator : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            typeof(TGenerator).Name + "Tests",
            new[] { syntaxTree },
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additionalTexts = additionalFiles
            .Select(static item => (AdditionalText)new InMemoryAdditionalText(item.path, item.content))
            .ToImmutableArray();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new TGenerator().AsSourceGenerator() },
            additionalTexts: additionalTexts,
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    /// <summary>
    ///     将生成结果转换为文件名到文本的映射，便于断言。
    /// </summary>
    /// <param name="result">生成器运行结果。</param>
    /// <returns>按 HintName 索引的生成源码。</returns>
    public static IReadOnlyDictionary<string, string> ToGeneratedSourceMap(GeneratorDriverRunResult result)
    {
        return result.Results
            .Single()
            .GeneratedSources
            .ToDictionary(
                static item => item.HintName,
                static item => NormalizeLineEndings(item.SourceText.ToString()),
                StringComparer.Ordinal);
    }

    /// <summary>
    ///     规范化换行，避免测试在不同平台上产生伪差异。
    /// </summary>
    /// <param name="content">待规范化文本。</param>
    /// <returns>统一使用 LF (<c>\n</c>) 的内容。</returns>
    public static string NormalizeLineEndings(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
                                        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                                        ?? Array.Empty<string>();

        return trustedPlatformAssemblies
            .Select(static path => MetadataReference.CreateFromFile(path));
    }

    /// <summary>
    ///     用于测试 AdditionalFiles 的内存实现。
    /// </summary>
    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        /// <summary>
        ///     初始化一个内存 AdditionalText。
        /// </summary>
        /// <param name="path">虚拟文件路径。</param>
        /// <param name="content">文件内容。</param>
        public InMemoryAdditionalText(
            string path,
            string content)
        {
            Path = path;
            _text = SourceText.From(content);
        }

        /// <inheritdoc />
        public override string Path { get; }

        /// <inheritdoc />
        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return _text;
        }
    }
}
