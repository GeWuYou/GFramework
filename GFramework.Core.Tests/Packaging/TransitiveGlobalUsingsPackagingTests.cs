using System.IO;

namespace GFramework.Core.Tests.Packaging;

/// <summary>
///     验证运行时模块在构建期间会自动生成 transitive global usings 资产。
///     该测试覆盖命名空间自动发现、框架侧过滤和消费者侧排除钩子的最终构建产物。
/// </summary>
[TestFixture]
public class TransitiveGlobalUsingsPackagingTests
{
    /// <summary>
    ///     验证 GFramework.Core 在构建后会生成 transitive global usings props，
    ///     且 props 内容来自源码自动发现，并保留消费者侧排除机制。
    /// </summary>
    [Test]
    public void CoreBuild_Should_Generate_AutoDiscovered_TransitiveGlobalUsingsProps()
    {
        var repositoryRoot = FindRepositoryRoot();
        var propsPath = Path.Combine(
            repositoryRoot,
            "GFramework.Core",
            "obj",
            "gframework",
            "GeWuYou.GFramework.Core.props");

        Assert.That(File.Exists(propsPath), Is.True, $"Expected generated props to exist: {propsPath}");
        var propsContent = File.ReadAllText(propsPath);

        Assert.That(propsContent, Does.Contain("GFramework.Core.Extensions"));
        Assert.That(propsContent, Does.Contain("GFramework.Core.Architectures"));
        Assert.That(propsContent, Does.Contain("GFramework.Core.Coroutine.Extensions"));
        Assert.That(propsContent, Does.Contain("Remove=\"@(GFrameworkExcludedUsing)\""));
        Assert.That(propsContent, Does.Not.Contain("System.Runtime.CompilerServices"));
    }

    /// <summary>
    ///     从测试输出目录向上回溯，定位包含解决方案文件的仓库根目录。
    /// </summary>
    /// <returns>仓库根目录绝对路径。</returns>
    private static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (currentDirectory != null)
        {
            var solutionPath = Path.Combine(currentDirectory.FullName, "GFramework.sln");
            if (File.Exists(solutionPath))
                return currentDirectory.FullName;

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root for GFramework.");
    }
}