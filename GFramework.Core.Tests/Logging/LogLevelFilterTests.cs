// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Filters;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 LogLevelFilter 的功能和行为
/// </summary>
[TestFixture]
public class LogLevelFilterTests
{
    [Test]
    public void ShouldLog_WithLevelAboveMinimum_ShouldReturnTrue()
    {
        var filter = new LogLevelFilter(LogLevel.Information);
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Warning, "TestLogger", "Test", Exception: null, Properties: null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithLevelEqualToMinimum_ShouldReturnTrue()
    {
        var filter = new LogLevelFilter(LogLevel.Information);
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Information, "TestLogger", "Test", Exception: null, Properties: null);

        Assert.That(filter.ShouldLog(entry), Is.True);
    }

    [Test]
    public void ShouldLog_WithLevelBelowMinimum_ShouldReturnFalse()
    {
        var filter = new LogLevelFilter(LogLevel.Information);
        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Debug, "TestLogger", "Test", Exception: null, Properties: null);

        Assert.That(filter.ShouldLog(entry), Is.False);
    }

    [Test]
    public void ShouldLog_WithAllLevels_ShouldWorkCorrectly()
    {
        var filter = new LogLevelFilter(LogLevel.Warning);

        var traceEntry = new LogEntry(DateTime.UtcNow, LogLevel.Trace, "TestLogger", "Test", Exception: null, Properties: null);
        var debugEntry = new LogEntry(DateTime.UtcNow, LogLevel.Debug, "TestLogger", "Test", Exception: null, Properties: null);
        var infoEntry = new LogEntry(DateTime.UtcNow, LogLevel.Information, "TestLogger", "Test", Exception: null, Properties: null);
        var warningEntry = new LogEntry(DateTime.UtcNow, LogLevel.Warning, "TestLogger", "Test", Exception: null, Properties: null);
        var errorEntry = new LogEntry(DateTime.UtcNow, LogLevel.Error, "TestLogger", "Test", Exception: null, Properties: null);
        var fatalEntry = new LogEntry(DateTime.UtcNow, LogLevel.Critical, "TestLogger", "Test", Exception: null, Properties: null);

        Assert.That(filter.ShouldLog(traceEntry), Is.False);
        Assert.That(filter.ShouldLog(debugEntry), Is.False);
        Assert.That(filter.ShouldLog(infoEntry), Is.False);
        Assert.That(filter.ShouldLog(warningEntry), Is.True);
        Assert.That(filter.ShouldLog(errorEntry), Is.True);
        Assert.That(filter.ShouldLog(fatalEntry), Is.True);
    }
}
