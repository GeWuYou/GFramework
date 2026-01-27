using System.IO;
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
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.logging
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class LogAttribute : Attribute
                                  {
                                      public string Name { get; set; }
                                      public string FieldName { get; set; }
                                      public string AccessModifier { get; set; }
                                      public bool IsStatic { get; set; } = true;
                                  }
                              }

                              namespace GFramework.Core.Abstractions.logging
                              {
                                  public interface ILogger
                                  {
                                      void Info(string message);
                                      void Error(string message);
                                      void Warn(string message);
                                      void Debug(string message);
                                      void Trace(string message);
                                      void Fatal(string message);
                                  }
                              }

                              namespace GFramework.Core.logging
                              {
                                  using GFramework.Core.Abstractions.logging;

                                  public static class LoggerFactoryResolver
                                  {
                                      public static ILoggerProvider Provider { get; set; }

                                      public static ILoggerProvider CreateLogger(string name)
                                      {
                                          return Provider ?? new MockLoggerProvider();
                                      }
                                  }

                                  public interface ILoggerProvider
                                  {
                                      ILogger CreateLogger(string name);
                                  }

                                  internal class MockLoggerProvider : ILoggerProvider
                                  {
                                      public ILogger CreateLogger(string name)
                                      {
                                          return new MockLogger(name);
                                      }
                                  }

                                  internal class MockLogger : ILogger
                                  {
                                      private readonly string _name;

                                      public MockLogger(string name)
                                      {
                                          _name = name;
                                      }

                                      public void Info(string message) { }
                                      public void Error(string message) { }
                                      public void Warn(string message) { }
                                      public void Debug(string message) { }
                                      public void Trace(string message) { }
                                      public void Fatal(string message) { }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.logging;

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
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.logging
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class LogAttribute : Attribute
                                  {
                                      public string Name { get; set; }
                                      public string FieldName { get; set; }
                                      public string AccessModifier { get; set; }
                                      public bool IsStatic { get; set; } = true;
                                  }
                              }

                              namespace GFramework.Core.Abstractions.logging
                              {
                                  public interface ILogger
                                  {
                                      void Info(string message);
                                      void Error(string message);
                                      void Warn(string message);
                                      void Debug(string message);
                                      void Trace(string message);
                                      void Fatal(string message);
                                  }
                              }

                              namespace GFramework.Core.logging
                              {
                                  using GFramework.Core.Abstractions.logging;

                                  public static class LoggerFactoryResolver
                                  {
                                      public static ILoggerProvider Provider { get; set; }

                                      public static ILoggerProvider CreateLogger(string name)
                                      {
                                          return Provider ?? new MockLoggerProvider();
                                      }
                                  }

                                  public interface ILoggerProvider
                                  {
                                      ILogger CreateLogger(string name);
                                  }

                                  internal class MockLoggerProvider : ILoggerProvider
                                  {
                                      public ILogger CreateLogger(string name)
                                      {
                                          return new MockLogger(name);
                                      }
                                  }

                                  internal class MockLogger : ILogger
                                  {
                                      private readonly string _name;

                                      public MockLogger(string name)
                                      {
                                          _name = name;
                                      }

                                      public void Info(string message) { }
                                      public void Error(string message) { }
                                      public void Warn(string message) { }
                                      public void Debug(string message) { }
                                      public void Trace(string message) { }
                                      public void Fatal(string message) { }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.logging;

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
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.logging
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class LogAttribute : Attribute
                                  {
                                      public string Name { get; set; }
                                      public string FieldName { get; set; }
                                      public string AccessModifier { get; set; }
                                      public bool IsStatic { get; set; } = true;
                                  }
                              }

                              namespace GFramework.Core.Abstractions.logging
                              {
                                  public interface ILogger
                                  {
                                      void Info(string message);
                                      void Error(string message);
                                      void Warn(string message);
                                      void Debug(string message);
                                      void Trace(string message);
                                      void Fatal(string message);
                                  }
                              }

                              namespace GFramework.Core.logging
                              {
                                  using GFramework.Core.Abstractions.logging;

                                  public static class LoggerFactoryResolver
                                  {
                                      public static ILoggerProvider Provider { get; set; }

                                      public static ILoggerProvider CreateLogger(string name)
                                      {
                                          return Provider ?? new MockLoggerProvider();
                                      }
                                  }

                                  public interface ILoggerProvider
                                  {
                                      ILogger CreateLogger(string name);
                                  }

                                  internal class MockLoggerProvider : ILoggerProvider
                                  {
                                      public ILogger CreateLogger(string name)
                                      {
                                          return new MockLogger(name);
                                      }
                                  }

                                  internal class MockLogger : ILogger
                                  {
                                      private readonly string _name;

                                      public MockLogger(string name)
                                      {
                                          _name = name;
                                      }

                                      public void Info(string message) { }
                                      public void Error(string message) { }
                                      public void Warn(string message) { }
                                      public void Debug(string message) { }
                                      public void Trace(string message) { }
                                      public void Fatal(string message) { }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.logging;

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
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.logging
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class LogAttribute : Attribute
                                  {
                                      public string Name { get; set; }
                                      public string FieldName { get; set; }
                                      public string AccessModifier { get; set; }
                                      public bool IsStatic { get; set; } = true;
                                  }
                              }

                              namespace GFramework.Core.Abstractions.logging
                              {
                                  public interface ILogger
                                  {
                                      void Info(string message);
                                      void Error(string message);
                                      void Warn(string message);
                                      void Debug(string message);
                                      void Trace(string message);
                                      void Fatal(string message);
                                  }
                              }

                              namespace GFramework.Core.logging
                              {
                                  using GFramework.Core.Abstractions.logging;

                                  public static class LoggerFactoryResolver
                                  {
                                      public static ILoggerProvider Provider { get; set; }

                                      public static ILoggerProvider CreateLogger(string name)
                                      {
                                          return Provider ?? new MockLoggerProvider();
                                      }
                                  }

                                  public interface ILoggerProvider
                                  {
                                      ILogger CreateLogger(string name);
                                  }

                                  internal class MockLoggerProvider : ILoggerProvider
                                  {
                                      public ILogger CreateLogger(string name)
                                      {
                                          return new MockLogger(name);
                                      }
                                  }

                                  internal class MockLogger : ILogger
                                  {
                                      private readonly string _name;

                                      public MockLogger(string name)
                                      {
                                          _name = name;
                                      }

                                      public void Info(string message) { }
                                      public void Error(string message) { }
                                      public void Warn(string message) { }
                                      public void Debug(string message) { }
                                      public void Trace(string message) { }
                                      public void Fatal(string message) { }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.logging;

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
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.logging
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class LogAttribute : Attribute
                                  {
                                      public string Name { get; set; }
                                      public string FieldName { get; set; }
                                      public string AccessModifier { get; set; }
                                      public bool IsStatic { get; set; } = true;
                                  }
                              }

                              namespace GFramework.Core.Abstractions.logging
                              {
                                  public interface ILogger
                                  {
                                      void Info(string message);
                                      void Error(string message);
                                      void Warn(string message);
                                      void Debug(string message);
                                      void Trace(string message);
                                      void Fatal(string message);
                                  }
                              }

                              namespace GFramework.Core.logging
                              {
                                  using GFramework.Core.Abstractions.logging;

                                  public static class LoggerFactoryResolver
                                  {
                                      public static ILoggerProvider Provider { get; set; }

                                      public static ILoggerProvider CreateLogger(string name)
                                      {
                                          return Provider ?? new MockLoggerProvider();
                                      }
                                  }

                                  public interface ILoggerProvider
                                  {
                                      ILogger CreateLogger(string name);
                                  }

                                  internal class MockLoggerProvider : ILoggerProvider
                                  {
                                      public ILogger CreateLogger(string name)
                                      {
                                          return new MockLogger(name);
                                      }
                                  }

                                  internal class MockLogger : ILogger
                                  {
                                      private readonly string _name;

                                      public MockLogger(string name)
                                      {
                                          _name = name;
                                      }

                                      public void Info(string message) { }
                                      public void Error(string message) { }
                                      public void Warn(string message) { }
                                      public void Debug(string message) { }
                                      public void Trace(string message) { }
                                      public void Fatal(string message) { }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.logging;

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
                              using System;

                              namespace GFramework.SourceGenerators.Abstractions.logging
                              {
                                  [AttributeUsage(AttributeTargets.Class)]
                                  public sealed class LogAttribute : Attribute
                                  {
                                      public string Name { get; set; }
                                      public string FieldName { get; set; }
                                      public string AccessModifier { get; set; }
                                      public bool IsStatic { get; set; } = true;
                                  }
                              }

                              namespace GFramework.Core.Abstractions.logging
                              {
                                  public interface ILogger
                                  {
                                      void Info(string message);
                                      void Error(string message);
                                      void Warn(string message);
                                      void Debug(string message);
                                      void Trace(string message);
                                      void Fatal(string message);
                                  }
                              }

                              namespace GFramework.Core.logging
                              {
                                  using GFramework.Core.Abstractions.logging;

                                  public static class LoggerFactoryResolver
                                  {
                                      public static ILoggerProvider Provider { get; set; }

                                      public static ILoggerProvider CreateLogger(string name)
                                      {
                                          return Provider ?? new MockLoggerProvider();
                                      }
                                  }

                                  public interface ILoggerProvider
                                  {
                                      ILogger CreateLogger(string name);
                                  }

                                  internal class MockLoggerProvider : ILoggerProvider
                                  {
                                      public ILogger CreateLogger(string name)
                                      {
                                          return new MockLogger(name);
                                      }
                                  }

                                  internal class MockLogger : ILogger
                                  {
                                      private readonly string _name;

                                      public MockLogger(string name)
                                      {
                                          _name = name;
                                      }

                                      public void Info(string message) { }
                                      public void Error(string message) { }
                                      public void Warn(string message) { }
                                      public void Debug(string message) { }
                                      public void Trace(string message) { }
                                      public void Fatal(string message) { }
                                  }
                              }

                              namespace TestApp
                              {
                                  using GFramework.SourceGenerators.Abstractions.logging;

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