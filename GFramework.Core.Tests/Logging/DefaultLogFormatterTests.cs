// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Formatters;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 DefaultLogFormatter 的功能和行为
/// </summary>
[TestFixture]
public class DefaultLogFormatterTests
{
    [SetUp]
    public void SetUp()
    {
        _formatter = new DefaultLogFormatter();
    }

    private DefaultLogFormatter _formatter = null!;

    [Test]
    public void Format_WithBasicEntry_ShouldFormatCorrectly()
    {
        var timestamp = new DateTime(2026, 2, 26, 10, 30, 45, 123);
        var entry = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message", null, null);

        var result = _formatter.Format(entry);

        Assert.That(result, Does.Contain("[2026-02-26 10:30:45.123]"));
        Assert.That(result, Does.Contain("INFO"));
        Assert.That(result, Does.Contain("[TestLogger]"));
        Assert.That(result, Does.Contain("Test message"));
    }

    [Test]
    public void Format_WithException_ShouldIncludeException()
    {
        var exception = new InvalidOperationException("Test exception");
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Error, "TestLogger", "Error occurred", exception, null);

        var result = _formatter.Format(entry);

        Assert.That(result, Does.Contain("Error occurred"));
        Assert.That(result, Does.Contain("InvalidOperationException"));
        Assert.That(result, Does.Contain("Test exception"));
    }

    [Test]
    public void Format_WithProperties_ShouldIncludeProperties()
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["UserId"] = 12345,
            ["UserName"] = "TestUser"
        };
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "User action", null, properties);

        var result = _formatter.Format(entry);

        Assert.That(result, Does.Contain("User action"));
        Assert.That(result, Does.Contain("|"));
        Assert.That(result, Does.Contain("UserId=12345"));
        Assert.That(result, Does.Contain("UserName=TestUser"));
    }

    [Test]
    public void Format_WithNullProperty_ShouldHandleNull()
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Key1"] = null
        };
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, properties);

        var result = _formatter.Format(entry);

        Assert.That(result, Does.Contain("Key1="));
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

            Assert.That(result, Does.Contain(expectedStrings[i]));
        }
    }

    [Test]
    public void Format_WithLongMessage_ShouldNotTruncate()
    {
        var longMessage = new string('A', 1000);
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", longMessage, null, null);

        var result = _formatter.Format(entry);

        Assert.That(result, Does.Contain(longMessage));
    }

    [Test]
    public void Format_WithSpecialCharacters_ShouldPreserveCharacters()
    {
        var message = "Test\nNew\tLine\r\nSpecial: <>&\"'";
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", message, null, null);

        var result = _formatter.Format(entry);

        Assert.That(result, Does.Contain(message));
    }
}
