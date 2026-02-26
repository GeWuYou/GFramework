using System.IO;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.appenders;
using GFramework.Core.logging.filters;
using GFramework.Core.logging.formatters;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 ConsoleAppender 的功能和行为
/// </summary>
[TestFixture]
public class ConsoleAppenderTests
{
    [SetUp]
    public void SetUp()
    {
        _stringWriter = new StringWriter();
        _appender = new ConsoleAppender(new DefaultLogFormatter(), _stringWriter, useColors: false);
    }

    [TearDown]
    public void TearDown()
    {
        _appender?.Dispose();
        _stringWriter?.Dispose();
    }

    private StringWriter _stringWriter = null!;
    private ConsoleAppender _appender = null!;

    [Test]
    public void Constructor_WithNullFormatter_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsoleAppender(null!));
    }

    [Test]
    public void Append_ShouldWriteToWriter()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test message", null, null);

        _appender.Append(entry);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Test message"));
        Assert.That(output, Does.Contain("INFO"));
    }

    [Test]
    public void Append_WithFilter_ShouldRespectFilter()
    {
        var filter = new LogLevelFilter(LogLevel.Warning);
        var appender = new ConsoleAppender(new DefaultLogFormatter(), _stringWriter, useColors: false, filter: filter);

        var infoEntry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Info message", null, null);
        var warningEntry = new LogEntry(DateTime.Now, LogLevel.Warning, "TestLogger", "Warning message", null, null);

        appender.Append(infoEntry);
        appender.Append(warningEntry);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Not.Contain("Info message"));
        Assert.That(output, Does.Contain("Warning message"));

        appender.Dispose();
    }

    [Test]
    public void Flush_ShouldFlushWriter()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test message", null, null);

        _appender.Append(entry);
        _appender.Flush();

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Test message"));
    }

    [Test]
    public void Dispose_ShouldFlushWriter()
    {
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test message", null, null);

        _appender.Append(entry);
        _appender.Dispose();

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Test message"));
    }

    [Test]
    public void Append_MultipleEntries_ShouldWriteAll()
    {
        for (int i = 0; i < 10; i++)
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
            _appender.Append(entry);
        }

        var output = _stringWriter.ToString();
        for (int i = 0; i < 10; i++)
        {
            Assert.That(output, Does.Contain($"Message {i}"));
        }
    }
}