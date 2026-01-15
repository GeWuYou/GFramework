using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

[TestFixture]
public class LoggerFactoryTests
{
    [Test]
    public void ConsoleLoggerFactory_GetLogger_ShouldReturnConsoleLogger()
    {
        var factory = new ConsoleLoggerFactory();
        var logger = factory.GetLogger("TestLogger", LogLevel.Info);

        Assert.That(logger, Is.Not.Null);
        Assert.That(logger, Is.InstanceOf<ConsoleLogger>());
        Assert.That(logger.Name(), Is.EqualTo("TestLogger"));
    }

    [Test]
    public void ConsoleLoggerFactory_GetLogger_WithDifferentNames_ShouldReturnDifferentLoggers()
    {
        var factory = new ConsoleLoggerFactory();
        var logger1 = factory.GetLogger("Logger1");
        var logger2 = factory.GetLogger("Logger2");

        Assert.That(logger1.Name(), Is.EqualTo("Logger1"));
        Assert.That(logger2.Name(), Is.EqualTo("Logger2"));
    }

    [Test]
    public void ConsoleLoggerFactory_GetLogger_WithDefaultMinLevel_ShouldUseInfo()
    {
        var factory = new ConsoleLoggerFactory();
        var logger = (ConsoleLogger)factory.GetLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Info, stringWriter, false);

        testLogger.Debug("Debug message");
        testLogger.Info("Info message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Not.Contain("Debug message"));
        Assert.That(output, Does.Contain("Info message"));
    }

    [Test]
    public void ConsoleLoggerFactoryProvider_CreateLogger_ShouldReturnLoggerWithProviderMinLevel()
    {
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Debug };
        var logger = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Debug, stringWriter, false);

        testLogger.Debug("Debug message");
        testLogger.Trace("Trace message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Debug message"));
        Assert.That(output, Does.Not.Contain("Trace message"));
    }

    [Test]
    public void ConsoleLoggerFactoryProvider_CreateLogger_ShouldUseProvidedName()
    {
        var provider = new ConsoleLoggerFactoryProvider();
        var logger = provider.CreateLogger("MyLogger");

        Assert.That(logger.Name(), Is.EqualTo("MyLogger"));
    }

    [Test]
    public void LoggerFactoryResolver_Provider_ShouldHaveDefaultValue()
    {
        Assert.That(LoggerFactoryResolver.Provider, Is.Not.Null);
        Assert.That(LoggerFactoryResolver.Provider, Is.InstanceOf<ConsoleLoggerFactoryProvider>());
    }

    [Test]
    public void LoggerFactoryResolver_Provider_CanBeChanged()
    {
        var customProvider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Debug };
        var originalProvider = LoggerFactoryResolver.Provider;

        LoggerFactoryResolver.Provider = customProvider;

        Assert.That(LoggerFactoryResolver.Provider, Is.SameAs(customProvider));

        LoggerFactoryResolver.Provider = originalProvider;
    }

    [Test]
    public void LoggerFactoryResolver_MinLevel_ShouldHaveDefaultValue()
    {
        Assert.That(LoggerFactoryResolver.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    [Test]
    public void LoggerFactoryResolver_MinLevel_CanBeChanged()
    {
        var originalLevel = LoggerFactoryResolver.MinLevel;

        LoggerFactoryResolver.MinLevel = LogLevel.Debug;

        Assert.That(LoggerFactoryResolver.MinLevel, Is.EqualTo(LogLevel.Debug));

        LoggerFactoryResolver.MinLevel = originalLevel;
    }

    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_ShouldHaveDefaultValue()
    {
        var provider = new ConsoleLoggerFactoryProvider();

        Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_CanBeChanged()
    {
        var provider = new ConsoleLoggerFactoryProvider();

        provider.MinLevel = LogLevel.Debug;

        Assert.That(provider.MinLevel, Is.EqualTo(LogLevel.Debug));
    }

    [Test]
    public void LoggerFactoryResolver_Provider_CreateLogger_ShouldUseProviderSettings()
    {
        var originalProvider = LoggerFactoryResolver.Provider;
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Warning };

        LoggerFactoryResolver.Provider = provider;

        var logger = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Warning, stringWriter, false);

        testLogger.Warn("Warn message");
        testLogger.Info("Info message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Warn message"));
        Assert.That(output, Does.Not.Contain("Info message"));

        LoggerFactoryResolver.Provider = originalProvider;
    }

    [Test]
    public void LoggerFactoryResolver_MinLevel_AffectsNewLoggers()
    {
        var originalMinLevel = LoggerFactoryResolver.MinLevel;

        LoggerFactoryResolver.MinLevel = LogLevel.Error;

        var provider = LoggerFactoryResolver.Provider;
        var logger = (ConsoleLogger)provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Error, stringWriter, false);

        testLogger.Error("Error message");
        testLogger.Warn("Warn message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Not.Contain("Warn message"));

        LoggerFactoryResolver.MinLevel = originalMinLevel;
    }

    [Test]
    public void ConsoleLoggerFactory_MultipleLoggers_ShouldBeIndependent()
    {
        var factory = new ConsoleLoggerFactory();
        var logger1 = factory.GetLogger("Logger1", LogLevel.Info);
        var logger2 = factory.GetLogger("Logger2", LogLevel.Debug);

        Assert.That(logger1.Name(), Is.EqualTo("Logger1"));
        Assert.That(logger2.Name(), Is.EqualTo("Logger2"));
    }

    [Test]
    public void ConsoleLoggerFactoryProvider_MinLevel_DoesNotAffectCreatedLogger()
    {
        var provider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Error };
        var logger = provider.CreateLogger("TestLogger");

        var stringWriter = new StringWriter();
        var testLogger = new ConsoleLogger("TestLogger", LogLevel.Error, stringWriter, false);

        testLogger.Error("Error message");
        testLogger.Fatal("Fatal message");

        var output = stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Contain("Fatal message"));
    }
}