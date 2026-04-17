using System.IO;
using GFramework.Core.SourceGenerators.Bases;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Bases;

/// <summary>
/// Priority 生成器快照测试类
/// </summary>
[TestFixture]
public class PriorityGeneratorSnapshotTests
{
    /// <summary>
    /// 测试基本的 Priority 特性生成
    /// </summary>
    [Test]
    public async Task Snapshot_BasicPriority()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.Bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.Core.SourceGenerators.Abstractions.Bases;

                                  [Priority(10)]
                                  public partial class MySystem
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            GetSnapshotFolder("BasicPriority"));
    }

    /// <summary>
    /// 测试负数优先级
    /// </summary>
    [Test]
    public async Task Snapshot_NegativePriority()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.Bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.Core.SourceGenerators.Abstractions.Bases;

                                  [Priority(-100)]
                                  public partial class CriticalSystem
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            GetSnapshotFolder("NegativePriority"));
    }

    /// <summary>
    /// 测试使用 PriorityGroup 枚举
    /// </summary>
    [Test]
    public async Task Snapshot_PriorityGroup()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.Bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }

                                  public static class PriorityGroup
                                  {
                                      public const int Critical = -100;
                                      public const int High = -50;
                                      public const int Normal = 0;
                                      public const int Low = 50;
                                      public const int Deferred = 100;
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.Core.SourceGenerators.Abstractions.Bases;
                                  using GFramework.Core.Abstractions.Bases;

                                  [Priority(PriorityGroup.High)]
                                  public partial class HighPrioritySystem
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            GetSnapshotFolder("PriorityGroup"));
    }

    /// <summary>
    /// 测试泛型类支持
    /// </summary>
    [Test]
    public async Task Snapshot_GenericClass()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.Bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.Core.SourceGenerators.Abstractions.Bases;

                                  [Priority(20)]
                                  public partial class GenericSystem<T>
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            GetSnapshotFolder("GenericClass"));
    }

    /// <summary>
    ///     将运行时测试目录映射回仓库内已提交的 Priority 生成器快照目录。
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
                "Bases",
                "snapshots",
                "PriorityGenerator",
                scenarioName));
    }
}
