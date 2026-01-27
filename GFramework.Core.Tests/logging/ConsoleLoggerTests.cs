using System.IO;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试ConsoleLogger类的功能和行为的单元测试类
/// </summary>
[TestFixture]
public class ConsoleLoggerTests
{
    /// <summary>
    ///     在每个测试方法执行前设置测试环境
    ///     创建StringWriter和ConsoleLogger实例用于测试
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _stringWriter = new StringWriter();
        _logger = new ConsoleLogger("TestLogger", LogLevel.Info, _stringWriter, false);
    }

    /// <summary>
    ///     在每个测试方法执行后清理测试资源
    ///     释放StringWriter资源
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _stringWriter?.Dispose();
    }

    private StringWriter _stringWriter = null!;
    private ConsoleLogger _logger = null!;

    /// <summary>
    ///     测试使用默认名称构造函数时是否正确使用根日志器名称
    ///     验证当未指定名称时，日志器使用"ROOT"作为默认名称
    /// </summary>
    [Test]
    public void Constructor_WithDefaultName_ShouldUseRootLoggerName()
    {
        var defaultLogger = new ConsoleLogger();

        Assert.That(defaultLogger.Name(), Is.EqualTo("ROOT"));
    }

    /// <summary>
    ///     测试使用自定义名称构造函数时是否正确使用自定义名称
    ///     验证构造函数能够正确设置并返回指定的日志器名称
    /// </summary>
    [Test]
    public void Constructor_WithCustomName_ShouldUseCustomName()
    {
        var customLogger = new ConsoleLogger("CustomLogger");

        Assert.That(customLogger.Name(), Is.EqualTo("CustomLogger"));
    }

    /// <summary>
    ///     测试使用自定义最小级别构造函数时是否正确遵循最小日志级别
    ///     验证只有达到或超过最小级别的日志消息才会被记录
    /// </summary>
    [Test]
    public void Constructor_WithCustomMinLevel_ShouldRespectMinLevel()
    {
        var debugLogger = new ConsoleLogger(null, LogLevel.Debug, _stringWriter, false);

        debugLogger.Debug("Debug message");
        debugLogger.Trace("Trace message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("DEBUG"));
        Assert.That(output, Does.Not.Contain("TRACE"));
    }

    /// <summary>
    ///     测试使用自定义写入器构造函数时是否将日志写入到自定义写入器
    ///     验证日志消息能够正确写入到指定的StringWriter中
    /// </summary>
    [Test]
    public void Constructor_WithCustomWriter_ShouldWriteToCustomWriter()
    {
        _logger.Info("Test message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Test message"));
    }

    /// <summary>
    ///     测试写入操作是否包含时间戳信息
    ///     验证每条日志消息都包含格式化的日期时间信息
    /// </summary>
    [Test]
    public void Write_ShouldIncludeTimestamp()
    {
        _logger.Info("Test message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Match(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]"));
    }

    /// <summary>
    ///     测试写入操作是否包含日志级别信息
    ///     验证不同级别的日志消息都能正确显示对应的级别标识
    /// </summary>
    [Test]
    public void Write_ShouldIncludeLevel()
    {
        _logger.Info("Test message");
        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("INFO"));

        _stringWriter.GetStringBuilder().Clear();

        _logger.Error("Error message");
        output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("ERROR"));
    }

    /// <summary>
    ///     测试写入操作是否包含日志器名称
    ///     验证日志输出中包含创建时指定的日志器名称
    /// </summary>
    [Test]
    public void Write_ShouldIncludeLoggerName()
    {
        _logger.Info("Test message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("[TestLogger]"));
    }

    /// <summary>
    ///     测试写入操作在包含异常时是否正确包含异常信息
    ///     验证异常的详细信息能够正确记录在日志中
    /// </summary>
    [Test]
    public void Write_WithException_ShouldIncludeException()
    {
        var exception = new Exception("Test exception");
        _logger.Error("Error message", exception);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Contain("Test exception"));
    }

    /// <summary>
    ///     测试写入多行日志时是否正确格式化
    ///     验证多条日志消息能够正确分行显示且包含正确的级别信息
    /// </summary>
    [Test]
    public void Write_WithMultipleLines_ShouldFormatCorrectly()
    {
        _logger.Info("Line 1");
        _logger.Warn("Line 2");
        _logger.Error("Line 3");

        var output = _stringWriter.ToString();
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        Assert.That(lines.Length, Is.EqualTo(3));
        Assert.That(lines[0], Does.Contain("INFO"));
        Assert.That(lines[1], Does.Contain("WARN"));
        Assert.That(lines[2], Does.Contain("ERROR"));
    }

    /// <summary>
    ///     测试写入格式化消息时是否正确格式化
    ///     验证带参数的消息格式化功能正常工作
    /// </summary>
    [Test]
    public void Write_WithFormattedMessage_ShouldFormatCorrectly()
    {
        _logger.Info("Value: {0}", 42);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Value: 42"));
    }

    /// <summary>
    ///     测试写入操作是否遵循最小日志级别限制
    ///     验证低于最小级别的日志消息不会被记录
    /// </summary>
    [Test]
    public void Write_ShouldRespectMinLevel()
    {
        _logger.Info("Info message");
        _logger.Debug("Debug message");
        _logger.Trace("Trace message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Info message"));
        Assert.That(output, Does.Not.Contain("Debug message"));
        Assert.That(output, Does.Not.Contain("Trace message"));
    }

    /// <summary>
    ///     测试启用颜色功能时是否不影响输出内容
    ///     验证即使颜色功能被禁用，日志内容仍然正确记录
    /// </summary>
    [Test]
    public void Write_WithColorsEnabled_ShouldNotAffectOutputContent()
    {
        var coloredLogger = new ConsoleLogger("ColorLogger", LogLevel.Info, _stringWriter, false);

        coloredLogger.Info("Colored message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Colored message"));
    }

    /// <summary>
    ///     测试所有日志级别是否都能正确格式化
    ///     验证从Trace到Fatal的所有日志级别都能正确显示
    /// </summary>
    [Test]
    public void Write_AllLogLevels_ShouldFormatCorrectly()
    {
        _logger.Trace("Trace");
        _logger.Debug("Debug");
        _logger.Info("Info");
        _logger.Warn("Warn");
        _logger.Error("Error");
        _logger.Fatal("Fatal");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("INFO"));
        Assert.That(output, Does.Contain("WARN"));
        Assert.That(output, Does.Contain("ERROR"));
        Assert.That(output, Does.Contain("FATAL"));
    }

    /// <summary>
    ///     测试写入嵌套异常时是否包含完整的异常信息
    ///     验证嵌套异常的所有层级信息都能被正确记录
    /// </summary>
    [Test]
    public void Write_WithNestedException_ShouldIncludeFullException()
    {
        var innerException = new Exception("Inner exception");
        var outerException = new Exception("Outer exception", innerException);

        _logger.Error("Error", outerException);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Error"));
        Assert.That(output, Does.Contain("Outer exception"));
        Assert.That(output, Does.Contain("Inner exception"));
    }

    /// <summary>
    ///     测试使用空写入器时是否不会抛出异常
    ///     验证当传入null写入器时，日志器能够安全处理而不崩溃
    /// </summary>
    [Test]
    public void Write_WithNullWriter_ShouldNotThrow()
    {
        var logger = new ConsoleLogger("TestLogger", LogLevel.Info, null, false);

        Assert.DoesNotThrow(() => logger.Info("Test message"));
    }

    /// <summary>
    ///     测试写入空消息时是否仍能正常写入
    ///     验证即使消息为空字符串，日志框架仍能生成包含其他信息的完整日志条目
    /// </summary>
    [Test]
    public void Write_WithEmptyMessage_ShouldStillWrite()
    {
        _logger.Info("");

        var output = _stringWriter.ToString();
        Assert.That(output.Length, Is.GreaterThan(0));
    }
}