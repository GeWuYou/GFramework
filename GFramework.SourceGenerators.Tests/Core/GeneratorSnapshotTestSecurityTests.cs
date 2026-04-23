using System.IO;
using GFramework.Core.SourceGenerators.Enums;

namespace GFramework.SourceGenerators.Tests.Core;

/// <summary>
///     验证快照测试辅助器对快照文件路径映射的安全约束。
/// </summary>
[TestFixture]
public class GeneratorSnapshotTestSecurityTests
{
    private const string EnumAttributeNamespace = "GFramework.Core.SourceGenerators.Abstractions.Enums";

    /// <summary>
    ///     验证快照文件名映射返回绝对路径时，会在访问文件系统前被拒绝。
    /// </summary>
    [Test]
    public void RunAsync_SnapshotFileNameSelectorReturnsAbsolutePath_ThrowsInvalidOperationException()
    {
        var snapshotRoot = CreateSnapshotRoot();
        var source = BuildSource();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
                source,
                snapshotRoot,
                _ => Path.Combine(snapshotRoot, "Status.EnumExtensions.g.cs")));
    }

    /// <summary>
    ///     验证快照文件名映射尝试通过父级目录片段逃逸根目录时，会在访问文件系统前被拒绝。
    /// </summary>
    [Test]
    public void RunAsync_SnapshotFileNameSelectorEscapesSnapshotRoot_ThrowsInvalidOperationException()
    {
        var snapshotRoot = CreateSnapshotRoot();
        var source = BuildSource();

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
                source,
                snapshotRoot,
                _ => Path.Combine("..", "escaped", "Status.EnumExtensions.g.cs")));
    }

    /// <summary>
    ///     为安全测试创建隔离的快照根目录路径，避免不同用例共享状态。
    /// </summary>
    /// <returns>当前用例专属的快照根目录绝对路径。</returns>
    private static string CreateSnapshotRoot()
    {
        return Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "temp-snapshots",
            TestContext.CurrentContext.Test.ID,
            Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    ///     构造可稳定触发枚举扩展生成器输出的最小测试源码。
    /// </summary>
    /// <returns>包含测试属性与目标枚举的完整源码。</returns>
    private static string BuildSource()
    {
        return $$"""
                 using System;

                 namespace {{EnumAttributeNamespace}}
                 {
                     [AttributeUsage(AttributeTargets.Enum)]
                     public sealed class GenerateEnumExtensionsAttribute : Attribute
                     {
                         public bool GenerateIsMethods { get; set; } = true;
                         public bool GenerateIsInMethod { get; set; } = true;
                     }
                 }

                 namespace TestApp
                 {
                     using {{EnumAttributeNamespace}};

                     [GenerateEnumExtensions]
                     public enum Status
                     {
                         Active,
                         Inactive
                     }
                 }
                 """;
    }
}
