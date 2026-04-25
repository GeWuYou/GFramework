using GFramework.Core.Abstractions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 LogEntry 的功能和行为
/// </summary>
[TestFixture]
public class LogEntryTests
{
    [Test]
    public void Constructor_WithAllParameters_ShouldCreateEntry()
    {
        var timestamp = DateTime.UtcNow;
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Key1"] = "Value1" };
        var exception = new InvalidOperationException("Test");

        var entry = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message", exception, properties);

        Assert.That(entry.Timestamp, Is.EqualTo(timestamp));
        Assert.That(entry.Level, Is.EqualTo(LogLevel.Info));
        Assert.That(entry.LoggerName, Is.EqualTo("TestLogger"));
        Assert.That(entry.Message, Is.EqualTo("Test message"));
        Assert.That(entry.Exception, Is.SameAs(exception));
        Assert.That(entry.Properties, Is.SameAs(properties));
    }

    [Test]
    public void Constructor_WithNullException_ShouldWork()
    {
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

        Assert.That(entry.Exception, Is.Null);
    }

    [Test]
    public void Constructor_WithNullProperties_ShouldWork()
    {
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

        Assert.That(entry.Properties, Is.Null);
    }

    [Test]
    public void GetAllProperties_WithNoProperties_ShouldReturnContextProperties()
    {
        LogContext.Clear();
        using (LogContext.Push("ContextKey", "ContextValue"))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

            var allProps = entry.GetAllProperties();

            Assert.That(allProps.Count, Is.EqualTo(1));
            Assert.That(allProps["ContextKey"], Is.EqualTo("ContextValue"));
        }

        LogContext.Clear();
    }

    [Test]
    public void GetAllProperties_WithProperties_ShouldReturnOnlyProperties()
    {
        LogContext.Clear();
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["PropKey"] = "PropValue" };
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, properties);

        var allProps = entry.GetAllProperties();

        Assert.That(allProps.Count, Is.EqualTo(1));
        Assert.That(allProps["PropKey"], Is.EqualTo("PropValue"));
    }

    [Test]
    public void GetAllProperties_WithBothPropertiesAndContext_ShouldMerge()
    {
        LogContext.Clear();
        using (LogContext.Push("ContextKey", "ContextValue"))
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["PropKey"] = "PropValue" };
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, properties);

            var allProps = entry.GetAllProperties();

            Assert.That(allProps.Count, Is.EqualTo(2));
            Assert.That(allProps["ContextKey"], Is.EqualTo("ContextValue"));
            Assert.That(allProps["PropKey"], Is.EqualTo("PropValue"));
        }

        LogContext.Clear();
    }

    [Test]
    public void GetAllProperties_WithConflictingKeys_ShouldPreferEntryProperties()
    {
        LogContext.Clear();
        using (LogContext.Push("Key1", "ContextValue"))
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Key1"] = "PropValue" };
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, properties);

            var allProps = entry.GetAllProperties();

            Assert.That(allProps.Count, Is.EqualTo(1));
            Assert.That(allProps["Key1"], Is.EqualTo("PropValue")); // 日志属性优先
        }

        LogContext.Clear();
    }

    [Test]
    public void GetAllProperties_WithEmptyPropertiesAndEmptyContext_ShouldReturnEmpty()
    {
        LogContext.Clear();
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

        var allProps = entry.GetAllProperties();

        Assert.That(allProps.Count, Is.EqualTo(0));
    }

    [Test]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        var timestamp = DateTime.UtcNow;
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Key1"] = "Value1" };

        var entry1 = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message", null, properties);
        var entry2 = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message", null, properties);

        Assert.That(entry1, Is.EqualTo(entry2));
    }

    [Test]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        var timestamp = DateTime.UtcNow;

        var entry1 = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message 1", null, null);
        var entry2 = new LogEntry(timestamp, LogLevel.Info, "TestLogger", "Test message 2", null, null);

        Assert.That(entry1, Is.Not.EqualTo(entry2));
    }
}
