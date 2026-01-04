using GFramework.SourceGenerators.rule;
using GFramework.SourceGenerators.Tests.core;
using NUnit.Framework;

namespace GFramework.SourceGenerators.Tests.rule;

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

                              namespace GFramework.SourceGenerators.Abstractions.rule
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class ContextAwareAttribute : Attribute { }
                              }

                              namespace GFramework.Core.Abstractions.rule
                              {
                                  public interface IContextAware
                                  {
                                      void SetContext(
                                          GFramework.Core.Abstractions.architecture.IArchitectureContext context);

                                      GFramework.Core.Abstractions.architecture.IArchitectureContext GetContext();
                                  }
                              }

                              namespace GFramework.Core.Abstractions.architecture
                              {
                                  public interface IArchitectureContext { }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.rule;
                                  using GFramework.Core.Abstractions.rule;

                                  [ContextAware]
                                  public partial class MyRule : IContextAware
                                  {
                                  }
                              }
                              namespace GFramework.Core.architecture
                              {         
                                   using GFramework.Core.Abstractions.architecture;
                                   public static class GameContext{
                                      /// <summary>
                                      /// 获取字典中的第一个架构上下文
                                      /// </summary>
                                      /// <returns>返回字典中的第一个架构上下文实例</returns>
                                      /// <exception cref="InvalidOperationException">当字典为空时抛出</exception>
                                      public static IArchitectureContext GetFirstArchitectureContext()
                                      {
                                          return null;
                                      }
                                  }
                              }
                              """;

        // 执行生成器快照测试，将生成的代码与预期快照进行比较
        await GeneratorSnapshotTest<ContextAwareGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "rule",
                "snapshots",
                "ContextAwareGenerator"));
    }
}