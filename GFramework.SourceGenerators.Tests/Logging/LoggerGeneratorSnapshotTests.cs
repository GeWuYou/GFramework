// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Core.SourceGenerators.Logging;
using GFramework.SourceGenerators.Tests.Core;

namespace GFramework.SourceGenerators.Tests.Logging;

/// <summary>
///     验证 <see cref="LoggerGenerator" /> 在常见日志声明配置下的快照输出保持稳定。
/// </summary>
[TestFixture]
public class LoggerGeneratorSnapshotTests
{
    /// <summary>
    ///     验证默认配置下的类日志字段快照。
    /// </summary>
    [Test]
    public Task Snapshot_DefaultConfiguration_Class()
    {
        return RunScenarioAsync(
            "DefaultConfiguration_Class",
            "[Log]",
            "public partial class MyService");
    }

    /// <summary>
    ///     验证自定义 logger 名称会反映到生成快照。
    /// </summary>
    [Test]
    public Task Snapshot_CustomName_Class()
    {
        return RunScenarioAsync(
            "CustomName_Class",
            "[Log(Name = \"CustomLogger\")]",
            "public partial class MyService");
    }

    /// <summary>
    ///     验证自定义字段名会反映到生成快照。
    /// </summary>
    [Test]
    public Task Snapshot_CustomFieldName_Class()
    {
        return RunScenarioAsync(
            "CustomFieldName_Class",
            "[Log(FieldName = \"MyLogger\")]",
            "public partial class MyService");
    }

    /// <summary>
    ///     验证实例字段模式会反映到生成快照。
    /// </summary>
    [Test]
    public Task Snapshot_InstanceField_Class()
    {
        return RunScenarioAsync(
            "InstanceField_Class",
            "[Log(IsStatic = false)]",
            "public partial class MyService");
    }

    /// <summary>
    ///     验证公共字段可见性会反映到生成快照。
    /// </summary>
    [Test]
    public Task Snapshot_PublicField_Class()
    {
        return RunScenarioAsync(
            "PublicField_Class",
            "[Log(AccessModifier = \"public\")]",
            "public partial class MyService");
    }

    /// <summary>
    ///     验证泛型类声明的日志字段快照。
    /// </summary>
    [Test]
    public Task Snapshot_GenericClass()
    {
        return RunScenarioAsync(
            "GenericClass",
            "[Log]",
            "public partial class MyService<T>");
    }

    /// <summary>
    ///     为给定场景组装最小测试源并执行快照校验。
    /// </summary>
    /// <param name="scenarioName">快照场景名称。</param>
    /// <param name="logAttributeLine">目标类型上的 <c>[Log(...)]</c> 声明。</param>
    /// <param name="classDeclaration">目标 partial 类型声明。</param>
    /// <returns>表示快照测试完成的异步任务。</returns>
    private static Task RunScenarioAsync(string scenarioName, string logAttributeLine, string classDeclaration)
    {
        return GeneratorSnapshotTest<LoggerGenerator>.RunAsync(
            CreateSource(logAttributeLine, classDeclaration),
            GetSnapshotFolder(scenarioName));
    }

    /// <summary>
    ///     生成日志源生成器测试所需的最小宿主源代码。
    /// </summary>
    /// <param name="logAttributeLine">目标类型上的 <c>[Log(...)]</c> 声明。</param>
    /// <param name="classDeclaration">目标 partial 类型声明。</param>
    /// <returns>可直接送入快照测试的完整源码字符串。</returns>
    private static string CreateSource(string logAttributeLine, string classDeclaration)
    {
        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            CreateLoggingAttributeSource(),
            CreateLoggingContractsSource(),
            CreateLoggingRuntimeSource(),
            CreateTestAppSource(logAttributeLine, classDeclaration));
    }

    /// <summary>
    ///     生成日志测试使用的 attribute 定义源码。
    /// </summary>
    /// <returns>包含 <c>LogAttribute</c> 的源码片段。</returns>
    private static string CreateLoggingAttributeSource()
    {
        return """
               using System;

               namespace GFramework.Core.SourceGenerators.Abstractions.Logging
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
               """;
    }

    /// <summary>
    ///     生成日志抽象契约源码，供测试编译图引用。
    /// </summary>
    /// <returns>包含 <c>ILogger</c> 的源码片段。</returns>
    private static string CreateLoggingContractsSource()
    {
        return """
               namespace GFramework.Core.Abstractions.Logging
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
               """;
    }

    /// <summary>
    ///     生成最小运行时宿主源码，供生成器解析 logger provider 依赖。
    /// </summary>
    /// <returns>包含 provider 与 mock logger 的源码片段。</returns>
    private static string CreateLoggingRuntimeSource()
    {
        return """
               namespace GFramework.Core.Logging
               {
                   using GFramework.Core.Abstractions.Logging;

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
               """;
    }

    /// <summary>
    ///     生成实际承载 <c>[Log]</c> 声明的测试类型源码。
    /// </summary>
    /// <param name="logAttributeLine">目标类型上的 <c>[Log(...)]</c> 声明。</param>
    /// <param name="classDeclaration">目标 partial 类型声明。</param>
    /// <returns>测试应用命名空间下的目标类型源码片段。</returns>
    private static string CreateTestAppSource(string logAttributeLine, string classDeclaration)
    {
        return $$"""
                 namespace TestApp
                 {
                     using GFramework.Core.SourceGenerators.Abstractions.Logging;

                     {{logAttributeLine}}
                     {{classDeclaration}}
                     {
                     }
                 }
                 """;
    }

    /// <summary>
    ///     将运行时测试目录映射回仓库内已提交的日志生成器快照目录。
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
                "Logging",
                "snapshots",
                "LoggerGenerator",
                scenarioName));
    }
}
