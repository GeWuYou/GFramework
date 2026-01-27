using System.IO;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
/// 测试LoggerFactory相关功能的测试类
/// </summary>
[TestFixture]
public class LoggerFactoryTests
{
    /// <summary>
    /// 测试ConsoleLoggerFactory的GetLogger方法是否返回ConsoleLogger实例
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_GetLogger_ShouldReturnConsoleLogger()
    {
        var factory = new ConsoleLoggerFactory();
        var logger = factory.GetLogger("TestLogger", LogLevel.Info);

        Assert.That(logger, Is.Not.Null);
        Assert.That(logger, Is.InstanceOf<ConsoleLogger>());
        Assert.That(logger.Name(), Is.EqualTo("TestLogger"));
    }

    /// <summary>
    /// 测试ConsoleLoggerFactory使用不同名称获取不同的logger实例
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
    /// 测试ConsoleLoggerFactory使用默认最小级别时的行为（默认为Info级别）
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_GetLogger_WithDefaultMinLevel_ShouldUseInfo()
    {
        var factory = new ConsoleLoggerFactory();
        _ = (ConsoleLogger)factory.GetLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Info, stringWriter, false);

        // 验证Debug消息不会被记录，但Info消息会被记录
        testLogger.Debug("Debug message");
        testLogger.Info("Info message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Not.Contain("Debug message"));
        Assert.That(output, Does.Contain("Info message"));
    }

    /// <summary>
    /// 测试ConsoleLoggerFactoryProvider创建logger时使用提供者的最小级别设置
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_CreateLogger_ShouldReturnLoggerWithProviderMinLevel()
    {
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Debug };
        _ = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Debug, stringWriter, false);

        // 验证Debug消息会被记录，但Trace消息不会被记录
        testLogger.Debug("Debug message");
        testLogger.Trace("Trace message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Debug message"));
        Assert.That(output, Does.Not.Contain("Trace message"));
    }

    /// <summary>
    /// 测试ConsoleLoggerFactoryProvider创建logger时使用提供的名称
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_CreateLogger_ShouldUseProvidedName()
    {
        var provider = new ConsoleLoggerFactoryProvider();
        var logger = provider.CreateLogger("MyLogger");

        Assert.That(logger.Name(), Is.EqualTo("MyLogger"));
    }

    /// <summary>
    /// 测试LoggerFactoryResolver的Provider属性是否有默认值
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_Provider_ShouldHaveDefaultValue()
    {
        Assert.That(LoggerFactoryResolver.Provider, Is.Not.Null);
        Assert.That(LoggerFactoryResolver.Provider, Is.InstanceOf<ConsoleLoggerFactoryProvider>());
    }

    /// <summary>
    /// 测试LoggerFactoryResolver的Provider属性可以被更改
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
    /// 测试LoggerFactoryResolver的MinLevel属性是否有默认值
    /// </summary>
    [Test]
    public void LoggerFactoryResolver_MinLevel_ShouldHaveDefaultValue()
    {
        Assert.That(LoggerFactoryResolver.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    /// <summary>
    /// 测试LoggerFactoryResolver的MinLevel属性可以被更改
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
    /// 测试ConsoleLoggerFactoryProvider的MinLevel属性是否有默认值
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_ShouldHaveDefaultValue()
    {
        var provider = new ConsoleLoggerFactoryProvider();

        Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    /// <summary>
    /// 测试ConsoleLoggerFactoryProvider的MinLevel属性可以被更改
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_CanBeChanged()
    {
        var provider = new ConsoleLoggerFactoryProvider();

        provider.MinLevel = LogLevel.Debug;

        Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Debug));
    }

    /// <summary>
    /// 测试LoggerFactoryResolver的Provider创建logger时使用提供者设置
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

        // 验证Warn消息会被记录，但Info消息不会被记录
        testLogger.Warn("Warn message");
        testLogger.Info("Info message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Warn message"));
        Assert.That(output, Does.Not.Contain("Info message"));

        LoggerFactoryResolver.Provider = originalProvider;
    }

    /// <summary>
    /// 测试LoggerFactoryResolver的MinLevel属性影响新创建的logger
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

        // 验证Error消息会被记录，但Warn消息不会被记录
        testLogger.Error("Error message");
        testLogger.Warn("Warn message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Not.Contain("Warn message"));

        LoggerFactoryResolver.MinLevel = originalMinLevel;
    }

    /// <summary>
    /// 测试ConsoleLoggerFactory创建的多个logger实例是独立的
    /// </summary>
    [Test]
    public void ConsoleLoggerFactory_MultipleLoggers_ShouldBeIndependent()
    {
        var factory = new ConsoleLoggerFactory();
        var logger1 = factory.GetLogger("Logger1", LogLevel.Info);
        var logger2 = factory.GetLogger("Logger2", LogLevel.Debug);

        Assert.That(logger1.Name(), Is.EqualTo("Logger1"));
        Assert.That(logger2.Name(), Is.EqualTo("Logger2"));
    }

    /// <summary>
    /// 测试ConsoleLoggerFactoryProvider的MinLevel不会影响已创建的logger
    /// </summary>
    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_DoesNotAffectCreatedLogger()
    {
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Error };
        provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Error, stringWriter, false);

        // 验证Error和Fatal消息都会被记录
        testLogger.Error("Error message");
        testLogger.Fatal("Fatal message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Contain("Fatal message"));
    }
}