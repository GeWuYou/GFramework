// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Linq;
using GFramework.Core.SourceGenerators.Rule;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Rule;

/// <summary>
///     上下文感知生成器快照测试类
///     用于测试ContextAwareGenerator源代码生成器的输出快照
/// </summary>
[TestFixture]
public class ContextAwareGeneratorSnapshotTests
{
    private const string SharedContextAwareInfrastructure = """
                                                            using System;

                                                            namespace GFramework.Core.SourceGenerators.Abstractions.Rule
                                                            {
                                                                [AttributeUsage(AttributeTargets.Class)]
                                                                public sealed class ContextAwareAttribute : Attribute { }
                                                            }

                                                            namespace GFramework.Core.Abstractions.Rule
                                                            {
                                                                public interface IContextAware
                                                                {
                                                                    void SetContext(
                                                                        GFramework.Core.Abstractions.Architectures.IArchitectureContext context);

                                                                    GFramework.Core.Abstractions.Architectures.IArchitectureContext GetContext();
                                                                }
                                                            }

                                                            namespace GFramework.Core.Abstractions.Architectures
                                                            {
                                                                public interface IArchitectureContext { }

                                                                public interface IArchitectureContextProvider
                                                                {
                                                                    IArchitectureContext GetContext();
                                                                    bool TryGetContext<T>(out T? context) where T : class, IArchitectureContext;
                                                                }
                                                            }

                                                            namespace GFramework.Core.Architectures
                                                            {
                                                                using GFramework.Core.Abstractions.Architectures;

                                                                public sealed class GameContextProvider : IArchitectureContextProvider
                                                                {
                                                                    public IArchitectureContext GetContext() => null;
                                                                    public bool TryGetContext<T>(out T? context) where T : class, IArchitectureContext
                                                                    {
                                                                        context = null;
                                                                        return false;
                                                                    }
                                                                }
                                                            """;

    private const string GameContextHelperSource = """

                                                    public static class GameContext
                                                    {
                                                        public static IArchitectureContext GetFirstArchitectureContext() => null;
                                                    }
                                                    """;

    /// <summary>
    ///     测试ContextAwareGenerator源代码生成器的快照功能
    ///     验证生成器对带有ContextAware特性的类的处理结果
    /// </summary>
    /// <returns>异步任务，无返回值</returns>
    [Test]
    public async Task Snapshot_ContextAwareGenerator()
    {
        // 执行生成器快照测试，将生成的代码与预期快照进行比较
        await GeneratorSnapshotTest<ContextAwareGenerator>.RunAsync(
            CreateContextAwareTestSource(
                """
                [ContextAware]
                public partial class MyRule : IContextAware
                {
                }
                """,
                includeGameContextHelper: true),
            GetSnapshotFolder());
    }

    /// <summary>
    ///     验证生成器在用户 partial 类型已经声明常见上下文字段名时仍能生成可编译代码。
    /// </summary>
    /// <returns>异步任务，无返回值。</returns>
    [Test]
    public async Task Snapshot_ContextAwareGenerator_With_User_Field_Name_Collisions()
    {
        await GeneratorSnapshotTest<ContextAwareGenerator>.RunAsync(
            CreateContextAwareTestSource(
                """
                using GFramework.Core.Abstractions.Architectures;

                [ContextAware]
                public partial class CollisionProneRule : IContextAware
                {
                    private readonly string _context = "user-field";
                    private static readonly string _contextProvider = "user-provider";
                    private static readonly object _contextSync = new();
                    private IArchitectureContext? _gFrameworkContextAwareContext;
                    private static IArchitectureContextProvider? _gFrameworkContextAwareProvider;
                    private static readonly object _gFrameworkContextAwareSync = new();
                }
                """),
            GetSnapshotFolder());
    }

    /// <summary>
    ///     验证生成器在基类已经占用自动生成字段名时，也会为派生规则类型分配带后缀的唯一成员名。
    /// </summary>
    /// <returns>异步任务，无返回值。</returns>
    [Test]
    public async Task Snapshot_ContextAwareGenerator_With_Inherited_Field_Name_Collisions()
    {
        await GeneratorSnapshotTest<ContextAwareGenerator>.RunAsync(
            CreateContextAwareTestSource(
                """
                using GFramework.Core.Abstractions.Architectures;

                public abstract class ContextAwareRuleBase
                {
                    protected IArchitectureContext? _gFrameworkContextAwareContext;
                    protected static IArchitectureContextProvider? _gFrameworkContextAwareProvider;
                    protected static readonly object _gFrameworkContextAwareSync = new();
                }

                [ContextAware]
                public partial class InheritedCollisionRule : ContextAwareRuleBase, IContextAware
                {
                }
                """),
            GetSnapshotFolder());
    }

    /// <summary>
    ///     组装 ContextAwareGenerator 快照测试共用的最小宿主源码，避免每个用例都重复长块样板代码。
    /// </summary>
    /// <param name="testTypeDeclarations">放在 <c>TestApp</c> 命名空间内的测试类型声明。</param>
    /// <param name="includeGameContextHelper">是否额外包含兼容旧快照输入的 <c>GameContext</c> 帮助类型。</param>
    /// <returns>可直接交给生成器测试驱动的完整源码文本。</returns>
    private static string CreateContextAwareTestSource(string testTypeDeclarations, bool includeGameContextHelper = false)
    {
        var gameContextHelper = includeGameContextHelper ? GameContextHelperSource : string.Empty;
        var testAppDeclarations = IndentBlock(testTypeDeclarations, 4);

        return string.Concat(
            SharedContextAwareInfrastructure,
            gameContextHelper,
            """
            }

            namespace TestApp
            {
                using GFramework.Core.SourceGenerators.Abstractions.Rule;
                using GFramework.Core.Abstractions.Rule;

            """,
            testAppDeclarations,
            """

            }
            """);
    }

    /// <summary>
    ///     为内嵌源码片段补齐缩进，使其能安全插入原始字符串模板中的命名空间块。
    /// </summary>
    /// <param name="text">要缩进的源码文本。</param>
    /// <param name="spaces">每行前要补齐的空格数。</param>
    /// <returns>已经补齐统一缩进的多行文本。</returns>
    private static string IndentBlock(string text, int spaces)
    {
        var indentation = new string(' ', spaces);
        return string.Join(
            Environment.NewLine,
            text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Trim()
                .Split('\n')
                .Select(line => indentation + line));
    }

    /// <summary>
    ///     将运行时测试目录映射回仓库内已提交的上下文感知生成器快照目录。
    /// </summary>
    /// <returns>快照目录的绝对路径。</returns>
    private static string GetSnapshotFolder()
    {
        return Path.GetFullPath(
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "..",
                "..",
                "..",
                "Rule",
                "snapshots",
                "ContextAwareGenerator"));
    }
}
