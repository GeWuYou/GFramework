using System.IO;
using GFramework.SourceGenerators.bases;
using GFramework.SourceGenerators.Tests.core;
using NUnit.Framework;

namespace GFramework.SourceGenerators.Tests.bases;

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

                              namespace GFramework.SourceGenerators.Abstractions.bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.bases;

                                  [Priority(10)]
                                  public partial class MySystem
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "bases",
                "snapshots",
                "PriorityGenerator",
                "BasicPriority"));
    }

    /// <summary>
    /// 测试负数优先级
    /// </summary>
    [Test]
    public async Task Snapshot_NegativePriority()
    {
        const string source = """
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.bases;

                                  [Priority(-100)]
                                  public partial class CriticalSystem
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "bases",
                "snapshots",
                "PriorityGenerator",
                "NegativePriority"));
    }

    /// <summary>
    /// 测试使用 PriorityGroup 枚举
    /// </summary>
    [Test]
    public async Task Snapshot_PriorityGroup()
    {
        const string source = """
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.bases
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
                                  using GFramework.SourceGenerators.Abstractions.bases;
                                  using GFramework.Core.Abstractions.bases;

                                  [Priority(PriorityGroup.High)]
                                  public partial class HighPrioritySystem
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "bases",
                "snapshots",
                "PriorityGenerator",
                "PriorityGroup"));
    }

    /// <summary>
    /// 测试泛型类支持
    /// </summary>
    [Test]
    public async Task Snapshot_GenericClass()
    {
        const string source = """
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.bases;

                                  [Priority(20)]
                                  public partial class GenericSystem<T>
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "bases",
                "snapshots",
                "PriorityGenerator",
                "GenericClass"));
    }

    /// <summary>
    /// 测试嵌套类支持
    /// </summary>
    [Test]
    public async Task Snapshot_NestedClass()
    {
        const string source = """
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.bases
                              {
                                  [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                                  public sealed class PriorityAttribute : Attribute
                                  {
                                      public int Value { get; }
                                      public PriorityAttribute(int value) { Value = value; }
                                  }
                              }

                              namespace GFramework.Core.Abstractions.bases
                              {
                                  public interface IPrioritized
                                  {
                                      int Priority { get; }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.bases;

                                  public class OuterClass
                                  {
                                      [Priority(30)]
                                      public partial class NestedSystem
                                      {
                                      }
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<PriorityGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "bases",
                "snapshots",
                "PriorityGenerator",
                "NestedClass"));
    }
}