using System.IO;
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
    /// <summary>
    ///     测试ContextAwareGenerator源代码生成器的快照功能
    ///     验证生成器对带有ContextAware特性的类的处理结果
    /// </summary>
    /// <returns>异步任务，无返回值</returns>
    [Test]
    public async Task Snapshot_ContextAwareGenerator()
    {
        // 定义测试用的源代码，包含ContextAware特性和相关接口定义
        const string source = """
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

                                  public static class GameContext
                                  {
                                      public static IArchitectureContext GetFirstArchitectureContext() => null;
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.Core.SourceGenerators.Abstractions.Rule;
                                  using GFramework.Core.Abstractions.Rule;

                                  [ContextAware]
                                  public partial class MyRule : IContextAware
                                  {
                                  }
                              }
                              """;

        // 执行生成器快照测试，将生成的代码与预期快照进行比较
        await GeneratorSnapshotTest<ContextAwareGenerator>.RunAsync(
            source,
            GetSnapshotFolder());
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
