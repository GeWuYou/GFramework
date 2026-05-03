// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Runtime.CompilerServices;
using GFramework.Core.Architectures;
using GFramework.Core.Coroutine.Extensions;

namespace GFramework.Core.Tests.Packaging;

/// <summary>
///     验证运行时模块在构建期间会自动生成 transitive global usings 资产。
///     该测试覆盖命名空间自动发现、框架侧过滤和消费者侧排除钩子的最终构建产物。
/// </summary>
[TestFixture]
public class TransitiveGlobalUsingsPackagingTests
{
    /// <summary>
    ///     使用真实类型派生架构命名空间，避免测试断言和命名空间重构脱节。
    /// </summary>
    private static readonly string ArchitectureNamespace = typeof(Architecture).Namespace
                                                           ?? throw new InvalidOperationException(
                                                               "Architecture namespace should not be null.");

    /// <summary>
    ///     使用真实类型派生扩展命名空间，避免对字面量命名空间字符串的重复维护。
    /// </summary>
    private static readonly string ExtensionsNamespace = typeof(ContextAwareEnvironmentExtensions).Namespace
                                                         ?? throw new InvalidOperationException(
                                                             "Extensions namespace should not be null.");

    /// <summary>
    ///     使用真实类型派生协程扩展命名空间，确保断言和源码自动发现保持一致。
    /// </summary>
    private static readonly string CoroutineExtensionsNamespace = typeof(CoroutineExtensions).Namespace
                                                                  ?? throw new InvalidOperationException(
                                                                      "Coroutine extensions namespace should not be null.");

    /// <summary>
    ///     验证 GFramework.Core 在构建后会生成 transitive global usings props，
    ///     且 props 内容来自源码自动发现，并保留消费者侧排除机制。
    /// </summary>
    [Test]
    public void CoreBuild_Should_Generate_AutoDiscovered_TransitiveGlobalUsingsProps()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var propsPath = Path.Combine(
            repositoryRoot,
            "GFramework.Core",
            "obj",
            "gframework",
            "GeWuYou.GFramework.Core.props");

        Assert.That(File.Exists(propsPath), Is.True, $"Expected generated props to exist: {propsPath}");
        var propsContent = File.ReadAllText(propsPath);

        Assert.That(propsContent, Does.Contain(ExtensionsNamespace));
        Assert.That(propsContent, Does.Contain(ArchitectureNamespace));
        Assert.That(propsContent, Does.Contain(CoroutineExtensionsNamespace));
        Assert.That(propsContent, Does.Contain("Remove=\"@(GFrameworkExcludedUsing)\""));
        Assert.That(propsContent, Does.Not.Contain("System.Runtime.CompilerServices"));
    }

    /// <summary>
    ///     基于当前测试源文件的已知位置解析仓库根目录。
    ///     这里不扫描解决方案文件，避免测试对仓库布局演进产生额外脆弱性。
    /// </summary>
    /// <param name="sourceFilePath">由编译器注入的当前测试源文件绝对路径。</param>
    /// <returns>仓库根目录绝对路径。</returns>
    private static string ResolveRepositoryRoot([CallerFilePath] string sourceFilePath = "")
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            throw new InvalidOperationException("Caller file path is required to resolve the repository root.");
        }

        var sourceDirectory = Path.GetDirectoryName(sourceFilePath)
                              ?? throw new DirectoryNotFoundException(
                                  $"Could not determine the directory for source file path: {sourceFilePath}");

        return Path.GetFullPath(Path.Combine(sourceDirectory, "..", ".."));
    }
}