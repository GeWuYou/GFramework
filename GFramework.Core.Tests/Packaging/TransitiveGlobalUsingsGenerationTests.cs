using System.Diagnostics;
using System.IO;

namespace GFramework.Core.Tests.Packaging;

/// <summary>
///     验证模块级可选 Global Usings 的生成脚本与仓库中的已提交产物保持同步。
///     该测试用于防止新增模块、命名空间清单或打包声明发生漂移后静默进入仓库。
/// </summary>
[TestFixture]
public class TransitiveGlobalUsingsGenerationTests
{
    /// <summary>
    ///     验证生成脚本的检查模式可以在当前仓库状态下通过。
    ///     如果此断言失败，说明清单、生成的 props 文件或 csproj 打包声明至少有一处未同步。
    /// </summary>
    [Test]
    public void GenerateModuleGlobalUsingsScript_CheckMode_Should_Pass()
    {
        var repositoryRoot = FindRepositoryRoot();
        using var process = StartScriptCheck(repositoryRoot);

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.That(
            process.ExitCode,
            Is.EqualTo(0),
            $"Expected module global usings generation to be up to date.{System.Environment.NewLine}" +
            $"stdout:{System.Environment.NewLine}{standardOutput}{System.Environment.NewLine}" +
            $"stderr:{System.Environment.NewLine}{standardError}");
    }

    /// <summary>
    ///     启动生成脚本的检查模式。
    ///     该模式不会修改仓库内容，只验证仓库中的生成产物是否已与当前规则对齐。
    /// </summary>
    /// <param name="repositoryRoot">仓库根目录。</param>
    /// <returns>已启动的进程实例。</returns>
    private static Process StartScriptCheck(string repositoryRoot)
    {
        var startInfo = new ProcessStartInfo("python3", "scripts/generate-module-global-usings.py --check")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        return Process.Start(startInfo)
               ?? throw new InvalidOperationException("Failed to start the module global usings generation check.");
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