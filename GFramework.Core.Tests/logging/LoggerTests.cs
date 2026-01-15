using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

[TestFixture]
public class LoggerTests
{
    [SetUp]
    public void SetUp()
    {
        _logger = new TestLogger("TestLogger", LogLevel.Info);
    }

    private TestLogger _logger = null!;

    [Test]
    public void Name_Should_ReturnLoggerName()
    {
        var name = _logger.Name();

        Assert.That(name, Is.EqualTo("TestLogger"));
    }

    [Test]
    public void Name_WithDefaultName_Should_ReturnRootLoggerName()
    {
        var defaultLogger = new TestLogger();

        Assert.That(defaultLogger.Name(), Is.EqualTo("ROOT"));
    }

    [Test]
    public void IsTraceEnabled_WithInfoMinLevel_Should_ReturnFalse()
    {
        Assert.That(_logger.IsTraceEnabled(), Is.False);
    }

    [Test]
    public void IsDebugEnabled_WithInfoMinLevel_Should_ReturnFalse()
    {
        Assert.That(_logger.IsDebugEnabled(), Is.False);
    }

    [Test]
    public void IsInfoEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsInfoEnabled(), Is.True);
    }

    [Test]
    public void IsWarnEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsWarnEnabled(), Is.True);
    }

    [Test]
    public void IsErrorEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsErrorEnabled(), Is.True);
    }

    [Test]
    public void IsFatalEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsFatalEnabled(), Is.True);
    }

    [Test]
    public void IsEnabledForLevel_WithValidLevel_Should_ReturnCorrectResult()
    {
        Assert.That(_logger.IsEnabledForLevel(LogLevel.Trace), Is.False);
        Assert.That(_logger.IsEnabledForLevel(LogLevel.Debug), Is.False);
        Assert.That(_logger.IsEnabledForLevel(LogLevel.Info), Is.True);
        Assert.That(_logger.IsEnabledForLevel(LogLevel.Warning), Is.True);
        Assert.That(_logger.IsEnabledForLevel(LogLevel.Error), Is.True);
        Assert.That(_logger.IsEnabledForLevel(LogLevel.Fatal), Is.True);
    }

    [Test]
    public void IsEnabledForLevel_WithInvalidLevel_Should_ThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _logger.IsEnabledForLevel((LogLevel)999); });
    }

    [Test]
    public void Trace_ShouldNotWrite_WhenTraceDisabled()
    {
        _logger.Trace("Trace message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Trace_WithFormat_ShouldNotWrite_WhenTraceDisabled()
    {
        _logger.Trace("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Trace_WithTwoArgs_ShouldNotWrite_WhenTraceDisabled()
    {
        _logger.Trace("Formatted {0} and {1}", "arg1", "arg2");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Trace_WithException_ShouldNotWrite_WhenTraceDisabled()
    {
        var exception = new Exception("Test exception");
        _logger.Trace("Trace message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Debug_ShouldNotWrite_WhenDebugDisabled()
    {
        _logger.Debug("Debug message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Debug_WithFormat_ShouldNotWrite_WhenDebugDisabled()
    {
        _logger.Debug("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    [Test]
    public void Info_ShouldWrite_WhenInfoEnabled()
    {
        _logger.Info("Info message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Info));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Info message"));
        Assert.That(_logger.Logs[0].Exception, Is.Null);
    }

    [Test]
    public void Info_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Info("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    [Test]
    public void Info_WithTwoArgs_ShouldWriteFormattedMessage()
    {
        _logger.Info("Formatted {0} and {1}", "arg1", "arg2");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted arg1 and arg2"));
    }

    [Test]
    public void Info_WithMultipleArgs_ShouldWriteFormattedMessage()
    {
        _logger.Info("Formatted {0}, {1}, {2}", "arg1", "arg2", "arg3");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted arg1, arg2, arg3"));
    }

    [Test]
    public void Info_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Info("Info message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Info message"));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    [Test]
    public void Warn_ShouldWrite_WhenWarnEnabled()
    {
        _logger.Warn("Warn message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Warn message"));
    }

    [Test]
    public void Warn_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Warn("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    [Test]
    public void Warn_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Warn("Warn message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    [Test]
    public void Error_ShouldWrite_WhenErrorEnabled()
    {
        _logger.Error("Error message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Error message"));
    }

    [Test]
    public void Error_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Error("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    [Test]
    public void Error_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Error("Error message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    [Test]
    public void Fatal_ShouldWrite_WhenFatalEnabled()
    {
        _logger.Fatal("Fatal message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Fatal));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Fatal message"));
    }

    [Test]
    public void Fatal_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Fatal("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    [Test]
    public void Fatal_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Fatal("Fatal message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    [Test]
    public void MultipleLogCalls_ShouldAccumulateLogs()
    {
        _logger.Info("Message 1");
        _logger.Warn("Message 2");
        _logger.Error("Message 3");

        Assert.That(_logger.Logs.Count, Is.EqualTo(3));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Message 1"));
        Assert.That(_logger.Logs[1].Message, Is.EqualTo("Message 2"));
        Assert.That(_logger.Logs[2].Message, Is.EqualTo("Message 3"));
    }

    [Test]
    public void Logger_WithTraceMinLevel_ShouldEnableAllLevels()
    {
        var traceLogger = new TestLogger("TraceLogger", LogLevel.Trace);

        traceLogger.Trace("Trace");
        traceLogger.Debug("Debug");
        traceLogger.Info("Info");
        traceLogger.Warn("Warn");
        traceLogger.Error("Error");
        traceLogger.Fatal("Fatal");

        Assert.That(traceLogger.Logs.Count, Is.EqualTo(6));
    }

    [Test]
    public void Logger_WithFatalMinLevel_ShouldDisableAllButFatal()
    {
        var fatalLogger = new TestLogger("FatalLogger", LogLevel.Fatal);

        fatalLogger.Trace("Trace");
        fatalLogger.Debug("Debug");
        fatalLogger.Info("Info");
        fatalLogger.Warn("Warn");
        fatalLogger.Error("Error");
        fatalLogger.Fatal("Fatal");

        Assert.That(fatalLogger.Logs.Count, Is.EqualTo(1));
        Assert.That(fatalLogger.Logs[0].Level, Is.EqualTo(LogLevel.Fatal));
    }
}

public sealed class TestLogger : AbstractLogger
{
    public TestLogger(string? name = null, LogLevel minLevel = LogLevel.Info) : base(name, minLevel)
    {
    }

    public List<LogEntry> Logs { get; } = new();

    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        Logs.Add(new LogEntry(level, message, exception));
    }

    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}