using GFramework.SourceGenerators.enums;
using GFramework.SourceGenerators.Tests.core;
using NUnit.Framework;

namespace GFramework.SourceGenerators.Tests.enums;

[TestFixture]
public class EnumExtensionsGeneratorSnapshotTests
{
    [Test]
    public async Task Snapshot_BasicEnum_IsMethods()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.enums;

                              namespace TestApp
                              {
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
                              using GFramework.SourceGenerators.Abstractions.enums;

                              namespace TestApp
                              {
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
                              using GFramework.SourceGenerators.Abstractions.enums;
                              using System;

                              namespace TestApp
                              {
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
                              using GFramework.SourceGenerators.Abstractions.enums;

                              namespace TestApp
                              {
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
                              using GFramework.SourceGenerators.Abstractions.enums;

                              namespace TestApp
                              {
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