// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Appenders;
using GFramework.Core.Tests.Time;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

[TestFixture]
public class StatisticsAppenderTests
{
    [SetUp]
    public void SetUp()
    {
        _timeProvider = new FakeTimeProvider();
        _appender = new StatisticsAppender(_timeProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _appender.Dispose();
    }

    private FakeTimeProvider _timeProvider = null!;
    private StatisticsAppender _appender = null!;

    [Test]
    public void StatisticsAppender_Should_Count_Total_Logs()
    {
        var entry = CreateLogEntry(LogLevel.Info, "TestLogger", "Message");

        _appender.Append(entry);
        _appender.Append(entry);
        _appender.Append(entry);

        Assert.That(_appender.TotalCount, Is.EqualTo(3));
    }

    [Test]
    public void StatisticsAppender_Should_Count_Errors()
    {
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Info"));
        _appender.Append(CreateLogEntry(LogLevel.Error, "Logger", "Error"));
        _appender.Append(CreateLogEntry(LogLevel.Fatal, "Logger", "Fatal"));
        _appender.Append(CreateLogEntry(LogLevel.Warning, "Logger", "Warning"));

        Assert.That(_appender.ErrorCount, Is.EqualTo(2)); // Error + Fatal
        Assert.That(_appender.TotalCount, Is.EqualTo(4));
    }

    [Test]
    public void StatisticsAppender_Should_Calculate_Error_Rate()
    {
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Info"));
        _appender.Append(CreateLogEntry(LogLevel.Error, "Logger", "Error"));
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Info"));
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Info"));

        // 1 error out of 4 total = 0.25
        Assert.That(_appender.ErrorRate, Is.EqualTo(0.25).Within(0.001));
    }

    [Test]
    public void StatisticsAppender_Should_Count_By_Level()
    {
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Info1"));
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Info2"));
        _appender.Append(CreateLogEntry(LogLevel.Error, "Logger", "Error"));
        _appender.Append(CreateLogEntry(LogLevel.Warning, "Logger", "Warning"));

        Assert.That(_appender.GetCountByLevel(LogLevel.Info), Is.EqualTo(2));
        Assert.That(_appender.GetCountByLevel(LogLevel.Error), Is.EqualTo(1));
        Assert.That(_appender.GetCountByLevel(LogLevel.Warning), Is.EqualTo(1));
        Assert.That(_appender.GetCountByLevel(LogLevel.Debug), Is.EqualTo(0));
    }

    [Test]
    public void StatisticsAppender_Should_Count_By_Logger()
    {
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger1", "Message"));
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger1", "Message"));
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger2", "Message"));

        Assert.That(_appender.GetCountByLogger("Logger1"), Is.EqualTo(2));
        Assert.That(_appender.GetCountByLogger("Logger2"), Is.EqualTo(1));
        Assert.That(_appender.GetCountByLogger("Logger3"), Is.EqualTo(0));
    }

    [Test]
    public void StatisticsAppender_Should_Track_Uptime()
    {
        var startTime = _appender.StartTime;
        _timeProvider.Advance(TimeSpan.FromSeconds(100));

        Assert.That(_appender.Uptime, Is.EqualTo(TimeSpan.FromSeconds(100)));
        Assert.That(_appender.StartTime, Is.EqualTo(startTime));
    }

    [Test]
    public void StatisticsAppender_Should_Reset_Statistics()
    {
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger", "Message"));
        _appender.Append(CreateLogEntry(LogLevel.Error, "Logger", "Error"));

        Assert.That(_appender.TotalCount, Is.EqualTo(2));
        Assert.That(_appender.ErrorCount, Is.EqualTo(1));

        var oldStartTime = _appender.StartTime;
        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        _appender.Reset();

        Assert.That(_appender.TotalCount, Is.EqualTo(0));
        Assert.That(_appender.ErrorCount, Is.EqualTo(0));
        Assert.That(_appender.GetLevelCounts().Count, Is.EqualTo(0));
        Assert.That(_appender.GetLoggerCounts().Count, Is.EqualTo(0));
        Assert.That(_appender.StartTime, Is.GreaterThan(oldStartTime));
    }

    [Test]
    public void StatisticsAppender_Should_Generate_Report()
    {
        _appender.Append(CreateLogEntry(LogLevel.Info, "Logger1", "Info"));
        _appender.Append(CreateLogEntry(LogLevel.Error, "Logger2", "Error"));
        _appender.Append(CreateLogEntry(LogLevel.Warning, "Logger1", "Warning"));

        var report = _appender.GenerateReport();

        Assert.That(report, Does.Contain("总日志数: 3"));
        Assert.That(report, Does.Contain("错误日志数: 1"));
        Assert.That(report, Does.Contain("Info"));
        Assert.That(report, Does.Contain("Error"));
        Assert.That(report, Does.Contain("Warning"));
        Assert.That(report, Does.Contain("Logger1"));
        Assert.That(report, Does.Contain("Logger2"));
    }

    [Test]
    public void StatisticsAppender_Should_Return_Empty_Collections_When_No_Data()
    {
        Assert.That(_appender.TotalCount, Is.EqualTo(0));
        Assert.That(_appender.ErrorCount, Is.EqualTo(0));
        Assert.That(_appender.ErrorRate, Is.EqualTo(0));
        Assert.That(_appender.GetLevelCounts().Count, Is.EqualTo(0));
        Assert.That(_appender.GetLoggerCounts().Count, Is.EqualTo(0));
    }

    [Test]
    public void StatisticsAppender_Should_Be_Thread_Safe()
    {
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            var loggerId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var level = j % 2 == 0 ? LogLevel.Info : LogLevel.Error;
                    _appender.Append(CreateLogEntry(level, $"Logger{loggerId}", "Message"));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.That(_appender.TotalCount, Is.EqualTo(1000));
        Assert.That(_appender.ErrorCount, Is.EqualTo(500));
        Assert.That(_appender.GetCountByLevel(LogLevel.Info), Is.EqualTo(500));
        Assert.That(_appender.GetCountByLevel(LogLevel.Error), Is.EqualTo(500));
    }

    [Test]
    public void Flush_Should_Not_Throw()
    {
        Assert.DoesNotThrow(() => _appender.Flush());
    }

    private LogEntry CreateLogEntry(LogLevel level, string loggerName, string message)
    {
        return new LogEntry(
            _timeProvider.UtcNow,
            level,
            loggerName,
            message,
            null,
            null
        );
    }
}