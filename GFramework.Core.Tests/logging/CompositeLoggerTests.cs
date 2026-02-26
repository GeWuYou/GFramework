using System.IO;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using GFramework.Core.logging.appenders;
using GFramework.Core.logging.formatters;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 CompositeLogger 的功能和行为
/// </summary>
[TestFixture]
public class CompositeLoggerTests
{
    [Test]
    public void Constructor_WithNullAppenders_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CompositeLogger("Test", LogLevel.Info, null!));
    }

    [Test]
    public void Constructor_WithEmptyAppenders_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CompositeLogger("Test", LogLevel.Info, Array.Empty<ILogAppender>()));
    }

    [Test]
    public void Write_ShouldWriteToAllAppenders()
    {
        var writer1 = new StringWriter();
        var writer2 = new StringWriter();
        var appender1 = new ConsoleAppender(new DefaultLogFormatter(), writer1, useColors: false);
        var appender2 = new ConsoleAppender(new DefaultLogFormatter(), writer2, useColors: false);

        using var logger = new CompositeLogger("TestLogger", LogLevel.Info, appender1, appender2);

        logger.Info("Test message");

        var output1 = writer1.ToString();
        var output2 = writer2.ToString();

        Assert.That(output1, Does.Contain("Test message"));
        Assert.That(output2, Does.Contain("Test message"));

        writer1.Dispose();
        writer2.Dispose();
    }

    [Test]
    public void Log_WithStructuredProperties_ShouldWriteToAllAppenders()
    {
        var writer1 = new StringWriter();
        var writer2 = new StringWriter();
        var appender1 = new ConsoleAppender(new DefaultLogFormatter(), writer1, useColors: false);
        var appender2 = new ConsoleAppender(new DefaultLogFormatter(), writer2, useColors: false);

        using var logger = new CompositeLogger("TestLogger", LogLevel.Info, appender1, appender2);

        logger.Log(LogLevel.Info, "User action", ("UserId", 12345), ("Action", "Login"));

        var output1 = writer1.ToString();
        var output2 = writer2.ToString();

        Assert.That(output1, Does.Contain("User action"));
        Assert.That(output1, Does.Contain("UserId=12345"));
        Assert.That(output2, Does.Contain("User action"));
        Assert.That(output2, Does.Contain("UserId=12345"));

        writer1.Dispose();
        writer2.Dispose();
    }

    [Test]
    public void Log_WithException_ShouldWriteToAllAppenders()
    {
        var writer1 = new StringWriter();
        var writer2 = new StringWriter();
        var appender1 = new ConsoleAppender(new DefaultLogFormatter(), writer1, useColors: false);
        var appender2 = new ConsoleAppender(new DefaultLogFormatter(), writer2, useColors: false);

        using var logger = new CompositeLogger("TestLogger", LogLevel.Info, appender1, appender2);

        var exception = new InvalidOperationException("Test exception");
        logger.Log(LogLevel.Error, "Error occurred", exception, ("ErrorCode", 500));

        var output1 = writer1.ToString();
        var output2 = writer2.ToString();

        Assert.That(output1, Does.Contain("Error occurred"));
        Assert.That(output1, Does.Contain("InvalidOperationException"));
        Assert.That(output2, Does.Contain("Error occurred"));
        Assert.That(output2, Does.Contain("InvalidOperationException"));

        writer1.Dispose();
        writer2.Dispose();
    }

    [Test]
    public void Flush_ShouldFlushAllAppenders()
    {
        var testAppender1 = new TestFlushAppender();
        var testAppender2 = new TestFlushAppender();

        using var logger = new CompositeLogger("TestLogger", LogLevel.Info, testAppender1, testAppender2);

        logger.Info("Test message");
        logger.Flush();

        Assert.That(testAppender1.FlushCalled, Is.True);
        Assert.That(testAppender2.FlushCalled, Is.True);
    }

    [Test]
    public void Dispose_ShouldDisposeAllAppenders()
    {
        var testAppender1 = new TestDisposableAppender();
        var testAppender2 = new TestDisposableAppender();

        var logger = new CompositeLogger("TestLogger", LogLevel.Info, testAppender1, testAppender2);
        logger.Dispose();

        Assert.That(testAppender1.DisposeCalled, Is.True);
        Assert.That(testAppender2.DisposeCalled, Is.True);
    }

    [Test]
    public void Write_WithLevelFiltering_ShouldRespectMinLevel()
    {
        var writer = new StringWriter();
        var appender = new ConsoleAppender(new DefaultLogFormatter(), writer, useColors: false);

        using var logger = new CompositeLogger("TestLogger", LogLevel.Warning, appender);

        logger.Debug("Debug message");
        logger.Info("Info message");
        logger.Warn("Warning message");
        logger.Error("Error message");

        var output = writer.ToString();

        Assert.That(output, Does.Not.Contain("Debug message"));
        Assert.That(output, Does.Not.Contain("Info message"));
        Assert.That(output, Does.Contain("Warning message"));
        Assert.That(output, Does.Contain("Error message"));

        writer.Dispose();
    }

    // 辅助测试类
    private class TestFlushAppender : ILogAppender
    {
        public bool FlushCalled { get; private set; }

        public void Append(LogEntry entry)
        {
        }

        public void Flush()
        {
            FlushCalled = true;
        }
    }

    private class TestDisposableAppender : ILogAppender, IDisposable
    {
        public bool DisposeCalled { get; private set; }

        public void Dispose()
        {
            DisposeCalled = true;
        }

        public void Append(LogEntry entry)
        {
        }

        public void Flush()
        {
        }
    }
}