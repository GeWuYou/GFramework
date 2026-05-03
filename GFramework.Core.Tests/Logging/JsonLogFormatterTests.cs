// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Formatters;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

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
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Error, "TestLogger", "Error occurred", exception, null);

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
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["UserId"] = 12345,
            ["UserName"] = "TestUser",
            ["IsActive"] = true
        };
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "User action", null, properties);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        if (doc.RootElement.TryGetProperty("properties", out var propsObj))
        {
            // 使用 TryGetProperty 来安全访问属性
            Assert.That(
                propsObj.TryGetProperty("userId", out var userIdProp) ||
                propsObj.TryGetProperty("UserId", out userIdProp), Is.True,
                $"userId/UserId not found. Available properties: {string.Join(", ", propsObj.EnumerateObject().Select(p => p.Name))}");
            Assert.That(userIdProp.GetInt32(), Is.EqualTo(12345));

            Assert.That(
                propsObj.TryGetProperty("userName", out var userNameProp) ||
                propsObj.TryGetProperty("UserName", out userNameProp), Is.True);
            Assert.That(userNameProp.GetString(), Is.EqualTo("TestUser"));

            Assert.That(
                propsObj.TryGetProperty("isActive", out var isActiveProp) ||
                propsObj.TryGetProperty("IsActive", out isActiveProp), Is.True);
            Assert.That(isActiveProp.GetBoolean(), Is.True);
        }
        else
        {
            Assert.Fail($"Properties object should be present when properties are provided. JSON: {result}");
        }
    }

    [Test]
    public void Format_WithNullProperty_ShouldHandleNull()
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Key1"] = null,
            ["Key2"] = "value"
        };
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, properties);

        var result = _formatter.Format(entry);

        var doc = JsonDocument.Parse(result);
        if (doc.RootElement.TryGetProperty("properties", out var propsObj))
        {
            // 使用 TryGetProperty 来安全访问属性
            Assert.That(
                propsObj.TryGetProperty("key1", out var key1Prop) || propsObj.TryGetProperty("Key1", out key1Prop),
                Is.True,
                $"key1/Key1 not found. Available properties: {string.Join(", ", propsObj.EnumerateObject().Select(p => p.Name))}");
            Assert.That(key1Prop.ValueKind, Is.EqualTo(JsonValueKind.Null));

            Assert.That(
                propsObj.TryGetProperty("key2", out var key2Prop) || propsObj.TryGetProperty("Key2", out key2Prop),
                Is.True);
            Assert.That(key2Prop.GetString(), Is.EqualTo("value"));
        }
        else
        {
            Assert.Fail($"Properties object should be present when properties are provided. JSON: {result}");
        }
    }

    [Test]
    public void Format_WithAllLogLevels_ShouldFormatCorrectly()
    {
        var levels = new[]
            { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error, LogLevel.Fatal };
        var expectedStrings = new[] { "TRACE", "DEBUG", "INFO", "WARNING", "ERROR", "FATAL" };

        for (int i = 0; i < levels.Length; i++)
        {
            var entry = new LogEntry(DateTime.UtcNow, levels[i], "TestLogger", "Test", null, null);
            var result = _formatter.Format(entry);

            var doc = JsonDocument.Parse(result);
            Assert.That(doc.RootElement.GetProperty("level").GetString(), Is.EqualTo(expectedStrings[i]));
        }
    }

    [Test]
    public void Format_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        var message = "Test \"quoted\" and \n newline";
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", message, null, null);

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
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Number"] = 123,
            ["String"] = "test",
            ["Boolean"] = true,
            ["Null"] = null,
            ["Array"] = new[] { 1, 2, 3 }
        };
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test", null, properties);

        var result = _formatter.Format(entry);

        Assert.That(() => JsonDocument.Parse(result), Throws.Nothing);
    }
}
