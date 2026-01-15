using GFramework.SourceGenerators.logging;
using GFramework.SourceGenerators.Tests.core;
using NUnit.Framework;

namespace GFramework.SourceGenerators.Tests.logging;

[TestFixture]
public class LoggerGeneratorSnapshotTests
{
    [Test]
    public async Task Snapshot_DefaultConfiguration_Class()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.logging;

                              namespace TestApp
                              {
                                  [Log]
                                  public partial class MyService
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "logging",
                "snapshots",
                "LoggerGenerator",
                "DefaultConfiguration_Class"));
    }

    [Test]
    public async Task Snapshot_CustomName_Class()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.logging;

                              namespace TestApp
                              {
                                  [Log(Name = "CustomLogger")]
                                  public partial class MyService
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "logging",
                "snapshots",
                "LoggerGenerator",
                "CustomName_Class"));
    }

    [Test]
    public async Task Snapshot_CustomFieldName_Class()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.logging;

                              namespace TestApp
                              {
                                  [Log(FieldName = "MyLogger")]
                                  public partial class MyService
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "logging",
                "snapshots",
                "LoggerGenerator",
                "CustomFieldName_Class"));
    }

    [Test]
    public async Task Snapshot_InstanceField_Class()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.logging;

                              namespace TestApp
                              {
                                  [Log(IsStatic = false)]
                                  public partial class MyService
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "logging",
                "snapshots",
                "LoggerGenerator",
                "InstanceField_Class"));
    }

    [Test]
    public async Task Snapshot_PublicField_Class()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.logging;

                              namespace TestApp
                              {
                                  [Log(AccessModifier = "public")]
                                  public partial class MyService
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "logging",
                "snapshots",
                "LoggerGenerator",
                "PublicField_Class"));
    }

    [Test]
    public async Task Snapshot_GenericClass()
    {
        const string source = """
                              using GFramework.SourceGenerators.Abstractions.logging;

                              namespace TestApp
                              {
                                  [Log]
                                  public partial class MyService<T>
                                  {
                                  }
                              }
                              """;

        await GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            source,
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "logging",
                "snapshots",
                "LoggerGenerator",
                "GenericClass"));
    }
}