using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
/// 测试Logger功能的单元测试类
/// </summary>
[TestFixture]
public class LoggerTests
{
    /// <summary>
    /// 在每个测试方法执行前设置测试环境
    /// 创建一个新的TestLogger实例，名称为"TestLogger"，最小日志级别为Info
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _logger = new TestLogger("TestLogger", LogLevel.Info);
    }

    private TestLogger _logger = null!;

    /// <summary>
    /// 验证Name方法是否正确返回Logger的名称
    /// </summary>
    [Test]
    public void Name_Should_ReturnLoggerName()
    {
        var name = _logger.Name();

        Assert.That(name, Is.EqualTo("TestLogger"));
    }

    /// <summary>
    /// 验证当使用默认名称时，Name方法是否返回根Logger名称"ROOT"
    /// </summary>
    [Test]
    public void Name_WithDefaultName_Should_ReturnRootLoggerName()
    {
        var defaultLogger = new TestLogger();

        Assert.That(defaultLogger.Name(), Is.EqualTo("ROOT"));
    }

    /// <summary>
    /// 验证当最小日志级别为Info时，IsTraceEnabled方法是否返回false
    /// </summary>
    [Test]
    public void IsTraceEnabled_WithInfoMinLevel_Should_ReturnFalse()
    {
        Assert.That(_logger.IsTraceEnabled(), Is.False);
    }

    /// <summary>
    /// 验证当最小日志级别为Info时，IsDebugEnabled方法是否返回false
    /// </summary>
    [Test]
    public void IsDebugEnabled_WithInfoMinLevel_Should_ReturnFalse()
    {
        Assert.That(_logger.IsDebugEnabled(), Is.False);
    }

    /// <summary>
    /// 验证当最小日志级别为Info时，IsInfoEnabled方法是否返回true
    /// </summary>
    [Test]
    public void IsInfoEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsInfoEnabled(), Is.True);
    }

    /// <summary>
    /// 验证当最小日志级别为Info时，IsWarnEnabled方法是否返回true
    /// </summary>
    [Test]
    public void IsWarnEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsWarnEnabled(), Is.True);
    }

    /// <summary>
    /// 验证当最小日志级别为Info时，IsErrorEnabled方法是否返回true
    /// </summary>
    [Test]
    public void IsErrorEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsErrorEnabled(), Is.True);
    }

    /// <summary>
    /// 验证当最小日志级别为Info时，IsFatalEnabled方法是否返回true
    /// </summary>
    [Test]
    public void IsFatalEnabled_WithInfoMinLevel_Should_ReturnTrue()
    {
        Assert.That(_logger.IsFatalEnabled(), Is.True);
    }

    /// <summary>
    /// 验证IsEnabledForLevel方法对于不同日志级别的返回值是否正确
    /// </summary>
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

    /// <summary>
    /// 验证当传入无效的日志级别时，IsEnabledForLevel方法是否会抛出ArgumentException异常
    /// </summary>
    [Test]
    public void IsEnabledForLevel_WithInvalidLevel_Should_ThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => { _logger.IsEnabledForLevel((LogLevel)999); });
    }

    /// <summary>
    /// 验证当Trace级别被禁用时，Trace方法不会写入日志
    /// </summary>
    [Test]
    public void Trace_ShouldNotWrite_WhenTraceDisabled()
    {
        _logger.Trace("Trace message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证当Trace级别被禁用时，带格式化参数的Trace方法不会写入日志
    /// </summary>
    [Test]
    public void Trace_WithFormat_ShouldNotWrite_WhenTraceDisabled()
    {
        _logger.Trace("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证当Trace级别被禁用时，带两个参数的Trace方法不会写入日志
    /// </summary>
    [Test]
    public void Trace_WithTwoArgs_ShouldNotWrite_WhenTraceDisabled()
    {
        _logger.Trace("Formatted {0} and {1}", "arg1", "arg2");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证当Trace级别被禁用时，带异常参数的Trace方法不会写入日志
    /// </summary>
    [Test]
    public void Trace_WithException_ShouldNotWrite_WhenTraceDisabled()
    {
        var exception = new Exception("Test exception");
        _logger.Trace("Trace message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证当Debug级别被禁用时，Debug方法不会写入日志
    /// </summary>
    [Test]
    public void Debug_ShouldNotWrite_WhenDebugDisabled()
    {
        _logger.Debug("Debug message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证当Debug级别被禁用时，带格式化参数的Debug方法不会写入日志
    /// </summary>
    [Test]
    public void Debug_WithFormat_ShouldNotWrite_WhenDebugDisabled()
    {
        _logger.Debug("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证当Info级别启用时，Info方法会正确写入日志
    /// </summary>
    [Test]
    public void Info_ShouldWrite_WhenInfoEnabled()
    {
        _logger.Info("Info message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Info));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Info message"));
        Assert.That(_logger.Logs[0].Exception, Is.Null);
    }

    /// <summary>
    /// 验证带格式化参数的Info方法会正确写入格式化后的消息
    /// </summary>
    [Test]
    public void Info_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Info("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    /// <summary>
    /// 验证带两个参数的Info方法会正确写入格式化后的消息
    /// </summary>
    [Test]
    public void Info_WithTwoArgs_ShouldWriteFormattedMessage()
    {
        _logger.Info("Formatted {0} and {1}", "arg1", "arg2");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted arg1 and arg2"));
    }

    /// <summary>
    /// 验证带多个参数的Info方法会正确写入格式化后的消息
    /// </summary>
    [Test]
    public void Info_WithMultipleArgs_ShouldWriteFormattedMessage()
    {
        _logger.Info("Formatted {0}, {1}, {2}", "arg1", "arg2", "arg3");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted arg1, arg2, arg3"));
    }

    /// <summary>
    /// 验证带异常参数的Info方法会正确写入消息和异常
    /// </summary>
    [Test]
    public void Info_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Info("Info message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Info message"));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    /// <summary>
    /// 验证当Warn级别启用时，Warn方法会正确写入日志
    /// </summary>
    [Test]
    public void Warn_ShouldWrite_WhenWarnEnabled()
    {
        _logger.Warn("Warn message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Warn message"));
    }

    /// <summary>
    /// 验证带格式化参数的Warn方法会正确写入格式化后的消息
    /// </summary>
    [Test]
    public void Warn_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Warn("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    /// <summary>
    /// 验证带异常参数的Warn方法会正确写入消息和异常
    /// </summary>
    [Test]
    public void Warn_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Warn("Warn message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    /// <summary>
    /// 验证当Error级别启用时，Error方法会正确写入日志
    /// </summary>
    [Test]
    public void Error_ShouldWrite_WhenErrorEnabled()
    {
        _logger.Error("Error message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Error message"));
    }

    /// <summary>
    /// 验证带格式化参数的Error方法会正确写入格式化后的消息
    /// </summary>
    [Test]
    public void Error_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Error("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    /// <summary>
    /// 验证带异常参数的Error方法会正确写入消息和异常
    /// </summary>
    [Test]
    public void Error_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Error("Error message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    /// <summary>
    /// 验证当Fatal级别启用时，Fatal方法会正确写入日志
    /// </summary>
    [Test]
    public void Fatal_ShouldWrite_WhenFatalEnabled()
    {
        _logger.Fatal("Fatal message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Level, Is.EqualTo(LogLevel.Fatal));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Fatal message"));
    }

    /// <summary>
    /// 验证带格式化参数的Fatal方法会正确写入格式化后的消息
    /// </summary>
    [Test]
    public void Fatal_WithFormat_ShouldWriteFormattedMessage()
    {
        _logger.Fatal("Formatted {0}", "message");

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Message, Is.EqualTo("Formatted message"));
    }

    /// <summary>
    /// 验证带异常参数的Fatal方法会正确写入消息和异常
    /// </summary>
    [Test]
    public void Fatal_WithException_ShouldWriteMessageAndException()
    {
        var exception = new Exception("Test exception");
        _logger.Fatal("Fatal message", exception);

        Assert.That(_logger.Logs.Count, Is.EqualTo(1));
        Assert.That(_logger.Logs[0].Exception, Is.SameAs(exception));
    }

    /// <summary>
    /// 验证多次调用日志方法会累积日志条目
    /// </summary>
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

    /// <summary>
    /// 验证当最小日志级别设置为Trace时，所有级别的日志都会被启用
    /// </summary>
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

    /// <summary>
    /// 验证当最小日志级别设置为Fatal时，只有Fatal级别的日志会被启用
    /// </summary>
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

/// <summary>
/// 测试用的日志记录器实现类，继承自AbstractLogger
/// </summary>
public sealed class TestLogger : AbstractLogger
{
    /// <summary>
    /// 初始化TestLogger的新实例
    /// </summary>
    /// <param name="name">日志记录器的名称，默认为null</param>
    /// <param name="minLevel">最小日志级别，默认为LogLevel.Info</param>
    public TestLogger(string? name = null, LogLevel minLevel = LogLevel.Info) : base(name, minLevel)
    {
    }

    /// <summary>
    /// 获取记录的日志条目列表
    /// </summary>
    public List<LogEntry> Logs { get; } = new();

    /// <summary>
    /// 将日志信息写入内部存储
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    /// <param name="exception">相关异常（可选）</param>
    protected override void Write(LogLevel level, string message, Exception? exception)
    {
        Logs.Add(new LogEntry(level, message, exception));
    }

    /// <summary>
    /// 表示单个日志条目的记录类型
    /// </summary>
    /// <param name="Level">日志级别</param>
    /// <param name="Message">日志消息</param>
    /// <param name="Exception">相关异常（可选）</param>
    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}