using System.IO;
using GFramework.Core.SourceGenerators.Enums;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Enums;

[TestFixture]
public class EnumExtensionsGeneratorSnapshotTests
{
    [Test]
    public async Task Snapshot_BasicEnum_IsMethods()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Enums
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
                                  using GFramework.Core.SourceGenerators.Abstractions.Enums;

                                  [GenerateEnumExtensions]
                                  public enum Status
                                  {
                                      Active,
                                      Inactive,
                                      Pending
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "enums",
                "snapshots",
                "EnumExtensionsGenerator",
                "BasicEnum_IsMethods"));
    }

    [Test]
    public async Task Snapshot_BasicEnum_IsInMethod()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Enums
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
                                  using GFramework.Core.SourceGenerators.Abstractions.Enums;

                                  [GenerateEnumExtensions]
                                  public enum Status
                                  {
                                      Active,
                                      Inactive
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "enums",
                "snapshots",
                "EnumExtensionsGenerator",
                "BasicEnum_IsInMethod"));
    }

    [Test]
    public async Task Snapshot_EnumWithFlagValues()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Enums
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
                                  using GFramework.Core.SourceGenerators.Abstractions.Enums;

                                  [GenerateEnumExtensions]
                                  [Flags]
                                  public enum Permissions
                                  {
                                      None = 0,
                                      Read = 1,
                                      Write = 2,
                                      Execute = 4
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "enums",
                "snapshots",
                "EnumExtensionsGenerator",
                "EnumWithFlagValues"));
    }

    [Test]
    public async Task Snapshot_DisableIsMethods()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Enums
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
                                  using GFramework.Core.SourceGenerators.Abstractions.Enums;

                                  [GenerateEnumExtensions(GenerateIsMethods = false)]
                                  public enum Status
                                  {
                                      Active,
                                      Inactive
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "enums",
                "snapshots",
                "EnumExtensionsGenerator",
                "DisableIsMethods"));
    }

    [Test]
    public async Task Snapshot_DisableIsInMethod()
    {
        const string source = """
                              using System;

                              namespace GFramework.Core.SourceGenerators.Abstractions.Enums
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
                                  using GFramework.Core.SourceGenerators.Abstractions.Enums;

                                  [GenerateEnumExtensions(GenerateIsInMethod = false)]
                                  public enum Status
                                  {
                                      Active,
                                      Inactive
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<EnumExtensionsGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "enums",
                "snapshots",
                "EnumExtensionsGenerator",
                "DisableIsInMethod"));
    }
}
