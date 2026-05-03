// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.IO;
using GFramework.Game.SourceGenerators.Config;

namespace GFramework.SourceGenerators.Tests.Config;

/// <summary>
///     为 schema 配置生成器提供测试驱动。
///     该驱动直接使用 Roslyn GeneratorDriver 运行 AdditionalFiles 场景，
///     以便测试基于 schema 文件的代码生成行为。
/// </summary>
public static class SchemaGeneratorTestDriver
{
    /// <summary>
    ///     运行 schema 配置生成器，并返回生成结果。
    /// </summary>
    /// <param name="source">测试用源码。</param>
    /// <param name="additionalFiles">AdditionalFiles 集合。</param>
    /// <returns>生成器运行结果。</returns>
    public static GeneratorDriverRunResult Run(
        string source,
        params (string path, string content)[] additionalFiles)
    {
        return Run(
            source,
            additionalFiles
                .Select(static item => (AdditionalText)new InMemoryAdditionalText(item.path, item.content))
                .ToArray());
    }

    /// <summary>
    ///     运行 schema 配置生成器，并允许测试自定义 AdditionalText 行为。
    /// </summary>
    /// <param name="source">测试用源码。</param>
    /// <param name="additionalTexts">自定义 AdditionalText 集合。</param>
    /// <returns>生成器运行结果。</returns>
    public static GeneratorDriverRunResult Run(
        string source,
        params AdditionalText[] additionalTexts)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "SchemaConfigGeneratorTests",
            new[] { syntaxTree },
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { new SchemaConfigGenerator().AsSourceGenerator() },
            additionalTexts: additionalTexts.ToImmutableArray(),
            parseOptions: (CSharpParseOptions)syntaxTree.Options);

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    /// <summary>
    ///     获取测试编译所需的运行时元数据引用。
    /// </summary>
    /// <returns>元数据引用集合。</returns>
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
        ///     创建内存 AdditionalText。
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
