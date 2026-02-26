using System.Text.Json;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.formatters;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 JsonLogFormatter 的功能和行为
/// </summary>
[TestFixture]
public class JsonLogFormatterTests
{
    [SetUp]
    public void SetUp()
    {
        _formatter = new JsonLogFormatter();
    }

    private JsonLogFormatter _formatter = null!;

    [Test]
    public void Format_WithBasicEntry_ShouldProduceValidJson()
    {
        var timestamp = new DateTime(2026, 2, 26, 10, 30, 45, 123, DateTimeKind.Utc);
        var entry = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message", null, null);

        var result = _formatter.Format(entry);

        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);

        var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("level").GetString(), Is.EqualTo("INFO"));
        Assert.That(doc.RootElement.GetProperty("logger").GetString(), Is.EqualTo("TestLogger"));
        Assert.That(doc.RootElement.GetProperty("message").GetString(), Is.EqualTo("Test message"));
    }

    [Test]
    public void Format_WithException_ShouldIncludeExceptionDetails()
    {
        var exception = new InvalidOperationException("Test exception");
        var entry = new LogEntry(DateTime.Now, LogLevel.Error, "TestLogger", "Error occurred", exception, null);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        var exceptionObj = doc.RootElement.GetProperty("exception");

        Assert.That(exceptionObj.GetProperty("type").GetString(), Does.Contain("InvalidOperationException"));
        Assert.That(exceptionObj.GetProperty("message").GetString(), Is.EqualTo("Test exception"));
        Assert.That(exceptionObj.TryGetProperty("stackTrace", out _), Is.True);
    }

    [Test]
    public void Format_WithProperties_ShouldIncludePropertiesObject()
    {
        var properties = new Dictionary<string, object?>
        {
            ["UserId"] = 12345,
            ["UserName"] = "TestUser",
            ["IsActive"] = true
        };
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "User action", null, properties);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        var propsObj = doc.RootElement.GetProperty("properties");

        Assert.That(propsObj.GetProperty("userId").GetInt32(), Is.EqualTo(12345));
        Assert.That(propsObj.GetProperty("userName").GetString(), Is.EqualTo("TestUser"));
        Assert.That(propsObj.GetProperty("isActive").GetBoolean(), Is.True);
    }

    [Test]
    public void Format_WithNullProperty_ShouldHandleNull()
    {
        var properties = new Dictionary<string, object?>
        {
            ["Key1"] = null
        };
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test message", null, properties);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        var propsObj = doc.RootElement.GetProperty("properties");

        Assert.That(propsObj.GetProperty("key1").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void Format_WithAllLogLevels_ShouldFormatCorrectly()
    {
        var levels = new[]
            { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error, LogLevel.Fatal };
        var expectedStrings = new[] { "TRACE", "DEBUG", "INFO", "WARNING", "ERROR", "FATAL" };

        for (int i = 0; i < levels.Length; i++)
        {
            var entry = new LogEntry(DateTime.Now, levels[i], "TestLogger", "Test", null, null);
            var result = _formatter.Format(entry);

            var doc = JsonDocument.Parse(result);
            Assert.That(doc.RootElement.GetProperty("level").GetString(), Is.EqualTo(expectedStrings[i]));
        }
    }

    [Test]
    public void Format_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        var message = "Test \"quoted\" and \n newline";
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", message, null, null);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        Assert.That(doc.RootElement.GetProperty("message").GetString(), Is.EqualTo(message));
    }

    [Test]
    public void Format_ShouldUseIso8601Timestamp()
    {
        var timestamp = new DateTime(2026, 2, 26, 10, 30, 45, 123, DateTimeKind.Utc);
        var entry = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test", null, null);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        var timestampStr = doc.RootElement.GetProperty("timestamp").GetString();

        Assert.That(timestampStr, Does.Contain("2026-02-26"));
        Assert.That(timestampStr, Does.Contain("T"));
    }

    [Test]
    public void Format_WithComplexProperties_ShouldSerializeCorrectly()
    {
        var properties = new Dictionary<string, object?>
        {
            ["Number"] = 123,
            ["String"] = "test",
            ["Boolean"] = true,
            ["Null"] = null,
            ["Array"] = new[] { 1, 2, 3 }
        };
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test", null, properties);

        var result = _formatter.Format(entry);

        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
    }
}