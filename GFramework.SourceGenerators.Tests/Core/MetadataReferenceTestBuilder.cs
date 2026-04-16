using System.Collections.Immutable;
using System.IO;

namespace GFramework.SourceGenerators.Tests.Core;

/// <summary>
///     为多程序集源生成器测试构建内存元数据引用。
/// </summary>
public static class MetadataReferenceTestBuilder
{
    // Reuse the runtime reference set across generator tests to avoid reparsing TRUSTED_PLATFORM_ASSEMBLIES
    // for every in-memory compilation.
    private static readonly Lazy<ImmutableArray<MetadataReference>> CachedRuntimeReferences =
        new(CreateRuntimeMetadataReferences);

    /// <summary>
    ///     将给定源码编译为内存程序集，并返回可供测试编译消费的元数据引用。
    /// </summary>
    /// <param name="assemblyName">目标程序集名称。</param>
    /// <param name="source">待编译源码。</param>
    /// <param name="additionalReferences">附加元数据引用，用于构造依赖链。</param>
    /// <returns>编译成功后的内存元数据引用。</returns>
    public static MetadataReference CreateFromSource(
        string assemblyName,
        string source,
        params MetadataReference[] additionalReferences)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = CachedRuntimeReferences.Value
            .Concat(additionalReferences)
            .ToImmutableArray();
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var diagnostics = string.Join(
                Environment.NewLine,
                emitResult.Diagnostics
                    .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(static diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException(
                $"Failed to build metadata reference '{assemblyName}'.{Environment.NewLine}{diagnostics}");
        }

        stream.Position = 0;
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    /// <summary>
    ///     获取当前测试运行时可直接复用的基础元数据引用集合。
    /// </summary>
    /// <returns>当前运行时可信平台程序集对应的元数据引用。</returns>
    public static ImmutableArray<MetadataReference> GetRuntimeMetadataReferences()
    {
        return CachedRuntimeReferences.Value;
    }

    private static ImmutableArray<MetadataReference> CreateRuntimeMetadataReferences()
    {
        var trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
                                        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                                        ?? Array.Empty<string>();

        return trustedPlatformAssemblies
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }
}
