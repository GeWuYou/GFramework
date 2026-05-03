// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Reflection;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 LoggerFactory 相关功能的测试类。
/// </summary>
[TestFixture]
[NonParallelizable]
public class LoggerFactoryTests
{
    /// <summary>
    ///     测试 ConsoleLoggerFactory 的 GetLogger 方法是否返回 ConsoleLogger 实例。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_GetLogger_ShouldReturnConsoleLogger()
    {
        var factory = new ConsoleLoggerFactory();
        var logger = factory.GetLogger("TestLogger");

        Assert.That(logger, Is.Not.Null);
        Assert.That(logger, Is.InstanceOf<ConsoleLogger>());
        Assert.That(logger.Name(), Is.EqualTo("TestLogger"));
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactory 使用不同名称获取不同的 logger 实例。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_GetLogger_WithDifferentNames_ShouldReturnDifferentLoggers()
    {
        var factory = new ConsoleLoggerFactory();
        var logger1 = factory.GetLogger("Logger1");
        var logger2 = factory.GetLogger("Logger2");

        Assert.That(logger1.Name(), Is.EqualTo("Logger1"));
        Assert.That(logger2.Name(), Is.EqualTo("Logger2"));
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactory 使用默认最小级别时的行为。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_GetLogger_WithDefaultMinLevel_ShouldUseInfo()
    {
        var factory = new ConsoleLoggerFactory();
        _ = (ConsoleLogger)factory.GetLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Info, stringWriter, false);

        testLogger.Debug("Debug message");
        testLogger.Info("Info message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Not.Contain("Debug message"));
        Assert.That(output, Does.Contain("Info message"));
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactoryProvider 创建 logger 时使用提供者的最小级别设置。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_CreateLogger_ShouldReturnLoggerWithProviderMinLevel()
    {
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Debug };
        _ = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Debug, stringWriter, false);

        testLogger.Debug("Debug message");
        testLogger.Trace("Trace message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Debug message"));
        Assert.That(output, Does.Not.Contain("Trace message"));
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactoryProvider 创建 logger 时使用提供的名称。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_CreateLogger_ShouldUseProvidedName()
    {
        var provider = new ConsoleLoggerFactoryProvider();
        var logger = provider.CreateLogger("MyLogger");

        Assert.That(logger.Name(), Is.EqualTo("MyLogger"));
    }

    /// <summary>
    ///     测试 LoggerFactoryResolver 的 Provider 属性是否有默认值。
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_Provider_ShouldHaveDefaultValue()
    {
        Assert.That(LoggerFactoryResolver.Provider, Is.Not.Null);
        Assert.That(LoggerFactoryResolver.Provider, Is.InstanceOf<ConsoleLoggerFactoryProvider>());
    }

    /// <summary>
    ///     测试 LoggerFactoryResolver 的 Provider 属性可以被更改。
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_Provider_CanBeChanged()
    {
        var customProvider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Debug };
        var originalProvider = LoggerFactoryResolver.Provider;

        LoggerFactoryResolver.Provider = customProvider;

        Assert.That(LoggerFactoryResolver.Provider, Is.SameAs(customProvider));

        LoggerFactoryResolver.Provider = originalProvider;
    }

    /// <summary>
    ///     测试 LoggerFactoryResolver 的 MinLevel 属性是否有默认值。
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_MinLevel_ShouldHaveDefaultValue()
    {
        Assert.That(LoggerFactoryResolver.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    /// <summary>
    ///     测试 LoggerFactoryResolver 的 MinLevel 属性可以被更改。
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_MinLevel_CanBeChanged()
    {
        var originalLevel = LoggerFactoryResolver.MinLevel;

        LoggerFactoryResolver.MinLevel = LogLevel.Debug;

        Assert.That(LoggerFactoryResolver.MinLevel, Is.EqualTo(LogLevel.Debug));

        LoggerFactoryResolver.MinLevel = originalLevel;
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactoryProvider 的 MinLevel 属性是否有默认值。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_ShouldHaveDefaultValue()
    {
        var provider = new ConsoleLoggerFactoryProvider();

        Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactoryProvider 的 MinLevel 属性可以被更改。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_CanBeChanged()
    {
        var provider = new ConsoleLoggerFactoryProvider();

        provider.MinLevel = LogLevel.Debug;

        Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Debug));
    }

    /// <summary>
    ///     测试 LoggerFactoryResolver 的 Provider 创建 logger 时使用提供者设置。
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_Provider_CreateLogger_ShouldUseProviderSettings()
    {
        var originalProvider = LoggerFactoryResolver.Provider;
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Warning };

        LoggerFactoryResolver.Provider = provider;

        _ = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Warning, stringWriter, false);

        testLogger.Warn("Warn message");
        testLogger.Info("Info message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Warn message"));
        Assert.That(output, Does.Not.Contain("Info message"));

        LoggerFactoryResolver.Provider = originalProvider;
    }

    /// <summary>
    ///     测试 LoggerFactoryResolver 的 MinLevel 属性影响新创建的 logger。
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_MinLevel_AffectsNewLoggers()
    {
        var originalMinLevel = LoggerFactoryResolver.MinLevel;

        LoggerFactoryResolver.MinLevel = LogLevel.Error;

        var provider = LoggerFactoryResolver.Provider;
        _ = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Error, stringWriter, false);

        testLogger.Error("Error message");
        testLogger.Warn("Warn message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Not.Contain("Warn message"));

        LoggerFactoryResolver.MinLevel = originalMinLevel;
    }

    /// <summary>
    ///     验证默认 provider 激活失败时会回退到静默 provider。
    /// </summary>
    [Test]
    public void
        LoggerFactoryResolver_Provider_Should_Fall_Back_To_SilentProvider_When_DefaultProvider_Activation_Fails()
    {
        var originalProvider = LoggerFactoryResolver.Provider;
        var originalTypeName = GetDefaultProviderTypeName();

        try
        {
            ResetProvider();
            SetDefaultProviderTypeName(typeof(ThrowingLoggerFactoryProvider).AssemblyQualifiedName!);

            var provider = LoggerFactoryResolver.Provider;
            var logger = provider.CreateLogger("Fallback");

            Assert.Multiple(() =>
            {
                Assert.That(provider.GetType().Name, Is.EqualTo("SilentLoggerFactoryProvider"));
                Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Info));
                Assert.That(logger.IsEnabledForLevel(LogLevel.Error), Is.False);
            });
        }
        finally
        {
            SetDefaultProviderTypeName(originalTypeName);
            LoggerFactoryResolver.Provider = originalProvider;
        }
    }

    /// <summary>
    ///     验证并发首次访问默认 provider 时只会创建一个实例，并向所有调用方返回相同引用。
    /// </summary>
    [Test]
    public async Task
        LoggerFactoryResolver_Provider_Should_Create_A_Single_Default_Instance_When_Accessed_Concurrently()
    {
        var originalProvider = LoggerFactoryResolver.Provider;
        var originalTypeName = GetDefaultProviderTypeName();

        try
        {
            BlockingLoggerFactoryProvider.Reset();
            ResetProvider();
            SetDefaultProviderTypeName(typeof(BlockingLoggerFactoryProvider).AssemblyQualifiedName!);

            var startGate = new ManualResetEventSlim(false);
            var tasks = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(() =>
                {
                    startGate.Wait();
                    return LoggerFactoryResolver.Provider;
                }))
                .ToArray();

            startGate.Set();

            Assert.That(
                SpinWait.SpinUntil(
                    () => BlockingLoggerFactoryProvider.ConstructionCount >= 1,
                    TimeSpan.FromSeconds(2)),
                Is.True,
                "The test provider should start construction after concurrent access begins.");

            BlockingLoggerFactoryProvider.ReleaseConstruction();

            var providers = await Task.WhenAll(tasks);

            Assert.Multiple(() =>
            {
                Assert.That(BlockingLoggerFactoryProvider.ConstructionCount, Is.EqualTo(1));
                Assert.That(providers.Distinct().Count(), Is.EqualTo(1));
                Assert.That(LoggerFactoryResolver.Provider, Is.SameAs(providers[0]));
            });
        }
        finally
        {
            BlockingLoggerFactoryProvider.ReleaseConstruction();
            BlockingLoggerFactoryProvider.Reset();
            SetDefaultProviderTypeName(originalTypeName);
            LoggerFactoryResolver.Provider = originalProvider;
        }
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactory 创建的多个 logger 实例是独立的。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_MultipleLoggers_ShouldBeIndependent()
    {
        var factory = new ConsoleLoggerFactory();
        var logger1 = factory.GetLogger("Logger1");
        var logger2 = factory.GetLogger("Logger2", LogLevel.Debug);

        Assert.That(logger1.Name(), Is.EqualTo("Logger1"));
        Assert.That(logger2.Name(), Is.EqualTo("Logger2"));
    }

    /// <summary>
    ///     测试 ConsoleLoggerFactoryProvider 的 MinLevel 不会影响已创建的 logger。
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_DoesNotAffectCreatedLogger()
    {
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Error };
        provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Error, stringWriter, false);

        testLogger.Error("Error message");
        testLogger.Fatal("Fatal message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Contain("Fatal message"));
    }

    private static string GetDefaultProviderTypeName()
    {
        return (string)GetResolverField("DefaultProviderTypeName").GetValue(null)!;
    }

    private static void SetDefaultProviderTypeName(string typeName)
    {
        GetResolverField("DefaultProviderTypeName").SetValue(null, typeName);
    }

    private static void ResetProvider()
    {
        GetResolverField("_provider").SetValue(null, null);
    }

    private static FieldInfo GetResolverField(string fieldName)
    {
        return typeof(LoggerFactoryResolver).GetField(
                   fieldName,
                   BindingFlags.NonPublic | BindingFlags.Static)
               ?? throw new InvalidOperationException(
                   $"Failed to locate LoggerFactoryResolver.{fieldName}.");
    }

    /// <summary>
    ///     用于触发默认 provider 激活失败回退路径的测试桩。
    /// </summary>
    public sealed class ThrowingLoggerFactoryProvider : ILoggerFactoryProvider
    {
        /// <summary>
        ///     初始化一个始终抛出异常的 provider。
        /// </summary>
        /// <exception cref="InvalidOperationException">始终抛出，用于覆盖回退路径。</exception>
        public ThrowingLoggerFactoryProvider()
        {
            throw new InvalidOperationException("Simulated provider activation failure.");
        }

        /// <summary>
        ///     获取或设置最小日志级别。
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        /// <summary>
        ///     创建日志器。
        /// </summary>
        /// <param name="name">日志器名称。</param>
        /// <returns>该测试桩永远不会成功创建日志器。</returns>
        /// <exception cref="NotSupportedException">始终抛出，因为该方法不应被调用。</exception>
        public ILogger CreateLogger(string name)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    ///     用于验证并发首次初始化路径只创建单个 provider 实例的测试桩。
    /// </summary>
    public sealed class BlockingLoggerFactoryProvider : ILoggerFactoryProvider
    {
        private static int _constructionCount;
        private static ManualResetEventSlim _constructionGate = new(false);

        /// <summary>
        ///     初始化一个会阻塞构造完成的 provider，用于放大并发首次访问竞争窗口。
        /// </summary>
        public BlockingLoggerFactoryProvider()
        {
            Interlocked.Increment(ref _constructionCount);
            _constructionGate.Wait(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        ///     获取已经发生的构造次数。
        /// </summary>
        public static int ConstructionCount => Volatile.Read(ref _constructionCount);

        /// <summary>
        ///     获取或设置最小日志级别。
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.Info;

        /// <summary>
        ///     创建测试日志器。
        /// </summary>
        /// <param name="name">日志器名称。</param>
        /// <returns>带有当前最小级别设置的控制台日志器。</returns>
        public ILogger CreateLogger(string name)
        {
            return new ConsoleLogger(name, MinLevel, TextWriter.Null, false);
        }

        /// <summary>
        ///     重置该测试桩的并发观测状态。
        /// </summary>
        public static void Reset()
        {
            _constructionGate = new ManualResetEventSlim(false);
            Interlocked.Exchange(ref _constructionCount, 0);
        }

        /// <summary>
        ///     释放当前被阻塞的 provider 构造过程。
        /// </summary>
        public static void ReleaseConstruction()
        {
            _constructionGate.Set();
        }
    }
}
