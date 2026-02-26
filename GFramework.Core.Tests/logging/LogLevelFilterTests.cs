using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.filters;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 LogLevelFilter 的功能和行为
/// </summary>
[TestFixture]
public class LogLevelFilterTests
{
    [Test]
    public void ShouldLog_WithLevelAboveMinimum_ShouldReturnTrue()
    {
        var filter = new LogLevelFilter(LogLevel.Info);
        var entry = new LogEntry(DateTime.Now, LogLevel.Warning, "TestLogger", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithLevelEqualToMinimum_ShouldReturnTrue()
    {
        var filter = new LogLevelFilter(LogLevel.Info);
        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithLevelBelowMinimum_ShouldReturnFalse()
    {
        var filter = new LogLevelFilter(LogLevel.Info);
        var entry = new LogEntry(DateTime.Now, LogLevel.Debug, "TestLogger", "Test", null, null);

        Assert.That(filter.ShouldLog(entry), Is.False);
    }

    [Test]
    public void ShouldLog_WithAllLevels_ShouldWorkCorrectly()
    {
        var filter = new LogLevelFilter(LogLevel.Warning);

        var traceEntry = new LogEntry(DateTime.Now, LogLevel.Trace, "TestLogger", "Test", null, null);
        var debugEntry = new LogEntry(DateTime.Now, LogLevel.Debug, "TestLogger", "Test", null, null);
        var infoEntry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test", null, null);
        var warningEntry = new LogEntry(DateTime.Now, LogLevel.Warning, "TestLogger", "Test", null, null);
        var errorEntry = new LogEntry(DateTime.Now, LogLevel.Error, "TestLogger", "Test", null, null);
        var fatalEntry = new LogEntry(DateTime.Now, LogLevel.Fatal, "TestLogger", "Test", null, null);

        Assert.That(filter.ShouldLog(traceEntry), Is.False);
        Assert.That(filter.ShouldLog(debugEntry), Is.False);
        Assert.That(filter.ShouldLog(infoEntry), Is.False);
        Assert.That(filter.ShouldLog(warningEntry), Is.True);
        Assert.That(filter.ShouldLog(errorEntry), Is.True);
        Assert.That(filter.ShouldLog(fatalEntry), Is.True);
    }
}