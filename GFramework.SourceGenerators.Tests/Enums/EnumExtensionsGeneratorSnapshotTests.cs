using System.IO;
using GFramework.Core.SourceGenerators.Enums;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Enums;

/// <summary>
///     验证枚举扩展生成器在不同属性开关组合下的快照输出。
/// </summary>
[TestFixture]
public class EnumExtensionsGeneratorSnapshotTests
{
    private const string EnumAttributeNamespace = "GFramework.Core.SourceGenerators.Abstractions.Enums";

    /// <summary>
    ///     验证默认配置会为普通枚举生成逐项判断方法与集合判断方法。
    /// </summary>
    [Test]
    public async Task Snapshot_BasicEnum_IsMethods()
    {
        var source = BuildSource(
            """
            public enum Status
            {
                Active,
                Inactive,
                Pending
            }
            """);

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            GetSnapshotFolder("BasicEnum_IsMethods"));
    }

    /// <summary>
    ///     验证默认配置在较小枚举上仍会生成集合判断方法。
    /// </summary>
    [Test]
    public async Task Snapshot_BasicEnum_IsInMethod()
    {
        var source = BuildSource(
            """
            public enum Status
            {
                Active,
                Inactive
            }
            """);

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            GetSnapshotFolder("BasicEnum_IsInMethod"));
    }

    /// <summary>
    ///     验证带显式位标志值的枚举也会生成对应扩展方法。
    /// </summary>
    [Test]
    public async Task Snapshot_EnumWithFlagValues()
    {
        var source = BuildSource(
            """
            [Flags]
            public enum Permissions
            {
                None = 0,
                Read = 1,
                Write = 2,
                Execute = 4
            }
            """);

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            GetSnapshotFolder("EnumWithFlagValues"));
    }

    /// <summary>
    ///     验证关闭逐项判断开关后仅保留集合判断方法。
    /// </summary>
    [Test]
    public async Task Snapshot_DisableIsMethods()
    {
        var source = BuildSource(
            """
            public enum Status
            {
                Active,
                Inactive
            }
            """,
            "[GenerateEnumExtensions(GenerateIsMethods = false)]");

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            GetSnapshotFolder("DisableIsMethods"));
    }

    /// <summary>
    ///     验证关闭集合判断开关后仅保留逐项判断方法。
    /// </summary>
    [Test]
    public async Task Snapshot_DisableIsInMethod()
    {
        var source = BuildSource(
            """
            public enum Status
            {
                Active,
                Inactive
            }
            """,
            "[GenerateEnumExtensions(GenerateIsInMethod = false)]");

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            GetSnapshotFolder("DisableIsInMethod"));
    }

    /// <summary>
    ///     验证同时关闭两个生成开关时不会输出任何扩展方法。
    /// </summary>
    [Test]
    public async Task Snapshot_DisableAllGeneratedMethods()
    {
        var source = BuildSource(
            """
            public enum Status
            {
                Active,
                Inactive
            }
            """,
            "[GenerateEnumExtensions(GenerateIsMethods = false, GenerateIsInMethod = false)]");

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            GetSnapshotFolder("DisableAllGeneratedMethods"));
    }

    /// <summary>
    ///     将运行时测试目录映射回仓库内已提交的枚举快照目录。
    /// </summary>
    /// <param name="scenarioName">快照场景名称。</param>
    /// <returns>场景对应的绝对快照目录。</returns>
    private static string GetSnapshotFolder(string scenarioName)
    {
        return Path.GetFullPath(
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..",
                "..",
                "..",
                "Enums",
                "snapshots",
                "EnumExtensionsGenerator",
                scenarioName));
    }

    /// <summary>
    ///     构造最小自洽的测试输入源码，以稳定驱动枚举扩展生成器的快照测试。
    /// </summary>
    /// <param name="enumBody">要注入到测试命名空间中的枚举声明文本。</param>
    /// <param name="attributeUsage">枚举上的属性使用方式，默认启用所有生成选项。</param>
    /// <returns>包含内联测试属性与目标枚举声明的完整源码。</returns>
    /// <remarks>
    ///     这里内联声明 <c>GenerateEnumExtensionsAttribute</c>，以便每个快照输入保持最小自洽。
    ///     属性命名空间必须与生成器按 metadata name 查找的契约保持一致；如果命名空间、属性名或参数发生变更，
    ///     需要同步更新该模板与相关快照，否则测试可能出现静默漂移。
    /// </remarks>
    private static string BuildSource(string enumBody, string attributeUsage = "[GenerateEnumExtensions]")
    {
        // 保持属性声明与测试输入同处一个模板中，能够明确锁定生成器对元数据名称和可选参数的语义假设。
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

                     {{attributeUsage}}
                     {{enumBody}}
                 }
                 """;
    }
}
