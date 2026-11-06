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
    ///     验证默认配置下的类日志属性快照。
    /// </summary>
    [Test]
    public Task Snapshot_DefaultConfiguration_Class()
    {
        return RunScenarioAsync(
            "DefaultConfiguration_Class",
            "[Log]",
            ContextAwareClass("public partial class MyService"));
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
            ContextAwareClass("public partial class MyService"));
    }

    /// <summary>
    ///     验证自定义属性名会反映到生成快照。
    /// </summary>
    [Test]
    public Task Snapshot_CustomFieldName_Class()
    {
        return RunScenarioAsync(
            "CustomFieldName_Class",
            "[Log(FieldName = \"MyLogger\")]",
            ContextAwareClass("public partial class MyService"));
    }

    /// <summary>
    ///     验证公共属性可见性会反映到生成快照。
    /// </summary>
    [Test]
    public Task Snapshot_PublicField_Class()
    {
        return RunScenarioAsync(
            "PublicField_Class",
            "[Log(AccessModifier = \"public\")]",
            ContextAwareClass("public partial class MyService"));
    }

    /// <summary>
    ///     验证泛型类声明的日志属性快照。
    /// </summary>
    [Test]
    public Task Snapshot_GenericClass()
    {
        return RunScenarioAsync(
            "GenericClass",
            "[Log]",
            ContextAwareClass("public partial class MyService<T>"));
    }

    /// <summary>
    ///     让测试类型实现 IContextAware，满足生成器的上下文访问前提。
    /// </summary>
    /// <param name="classDeclaration">目标 partial 类型声明。</param>
    /// <returns>实现上下文契约的类型声明。</returns>
    private static string ContextAwareClass(string classDeclaration)
    {
        var index = classDeclaration.IndexOf(" class ", StringComparison.Ordinal);
        var head = classDeclaration[..(index + " class ".Length)];
        var tail = classDeclaration[(index + " class ".Length)..];
        // 泛型声明的约束部分保持原样；直接在声明后追加实现。
        return head + tail.TrimEnd() + " : GFramework.Core.Abstractions.Rule.IContextAware";
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
                   }
               }

               namespace GFramework.Core.SourceGenerators.Abstractions.Rule
               {
                   [AttributeUsage(AttributeTargets.Class)]
                   public sealed class ContextAwareAttribute : Attribute
                   {
                   }
               }
               """;
    }

    /// <summary>
    ///     生成标准日志契约与上下文契约的最小桩源码，供测试编译图引用。
    /// </summary>
    /// <returns>包含 ILogger/ILoggerFactory 与 IContextAware 的源码片段。</returns>
    private static string CreateLoggingContractsSource()
    {
        return """
               namespace Microsoft.Extensions.Logging
               {
                   public interface ILogger
                   {
                   }

                   public interface ILoggerFactory
                   {
                       ILogger CreateLogger(string categoryName);
                   }
               }

               namespace GFramework.Core.Abstractions.Architectures
               {
                   public interface IArchitectureContext
                   {
                       TService GetService<TService>() where TService : class;
                   }
               }

               namespace GFramework.Core.Abstractions.Rule
               {
                   using GFramework.Core.Abstractions.Architectures;

                   public interface IContextAware
                   {
                       void SetContext(IArchitectureContext context);

                       IArchitectureContext GetContext();
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
                         // 模拟 ContextAwareBase 的受保护上下文访问，供生成代码引用。
                         private GFramework.Core.Abstractions.Architectures.IArchitectureContext Context => null!;

                         public void SetContext(GFramework.Core.Abstractions.Architectures.IArchitectureContext context)
                         {
                         }

                         public GFramework.Core.Abstractions.Architectures.IArchitectureContext GetContext() => Context;
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
