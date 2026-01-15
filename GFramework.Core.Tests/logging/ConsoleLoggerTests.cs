using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

[TestFixture]
public class ConsoleLoggerTests
{
    [SetUp]
    public void SetUp()
    {
        _stringWriter = new StringWriter();
        _logger = new ConsoleLogger("TestLogger", LogLevel.Info, _stringWriter, false);
    }

    [TearDown]
    public void TearDown()
    {
        _stringWriter?.Dispose();
    }

    private StringWriter _stringWriter = null!;
    private ConsoleLogger _logger = null!;

    [Test]
    public void Constructor_WithDefaultName_ShouldUseRootLoggerName()
    {
        var defaultLogger = new ConsoleLogger();

        Assert.That(defaultLogger.Name(), Is.EqualTo("ROOT"));
    }

    [Test]
    public void Constructor_WithCustomName_ShouldUseCustomName()
    {
        var customLogger = new ConsoleLogger("CustomLogger");

        Assert.That(customLogger.Name(), Is.EqualTo("CustomLogger"));
    }

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

    [Test]
    public void Constructor_WithCustomWriter_ShouldWriteToCustomWriter()
    {
        _logger.Info("Test message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Test message"));
    }

    [Test]
    public void Write_ShouldIncludeTimestamp()
    {
        _logger.Info("Test message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Match(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]"));
    }

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

    [Test]
    public void Write_ShouldIncludeLoggerName()
    {
        _logger.Info("Test message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("[TestLogger]"));
    }

    [Test]
    public void Write_WithException_ShouldIncludeException()
    {
        var exception = new Exception("Test exception");
        _logger.Error("Error message", exception);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Error message"));
        Assert.That(output, Does.Contain("Test exception"));
    }

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

    [Test]
    public void Write_WithFormattedMessage_ShouldFormatCorrectly()
    {
        _logger.Info("Value: {0}", 42);

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Value: 42"));
    }

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

    [Test]
    public void Write_WithColorsEnabled_ShouldNotAffectOutputContent()
    {
        var coloredLogger = new ConsoleLogger("ColorLogger", LogLevel.Info, _stringWriter, false);

        coloredLogger.Info("Colored message");

        var output = _stringWriter.ToString();
        Assert.That(output, Does.Contain("Colored message"));
    }

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

    [Test]
    public void Write_WithNullWriter_ShouldNotThrow()
    {
        var logger = new ConsoleLogger("TestLogger", LogLevel.Info, null, false);

        Assert.DoesNotThrow(() => logger.Info("Test message"));
    }

    [Test]
    public void Write_WithEmptyMessage_ShouldStillWrite()
    {
        _logger.Info("");

        var output = _stringWriter.ToString();
        Assert.That(output.Length, Is.GreaterThan(0));
    }
}