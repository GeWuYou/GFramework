// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 AppenderLoggerProvider 的功能和行为：
///     标准日志调用到 <see cref="LogEntry" /> 的转换、结构化状态提取、appender 扇出与故障隔离。
/// </summary>
[TestFixture]
public class AppenderLoggerProviderTests
{
    [Test]
    public void Constructor_WithNullAppenders_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AppenderLoggerProvider(null!));
    }

    [Test]
    public void Constructor_WithEmptyAppenders_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new AppenderLoggerProvider([]));
    }

    [Test]
    public void CreateLogger_WithNullCategory_ShouldThrowArgumentNullException()
    {
        using var provider = new AppenderLoggerProvider([new TestLogAppender()]);

        Assert.Throws<ArgumentNullException>(() => provider.CreateLogger(null!));
    }

    [Test]
    public void CreateLogger_WithEmptyCategory_ShouldThrowArgumentException()
    {
        using var provider = new AppenderLoggerProvider([new TestLogAppender()]);

        Assert.Throws<ArgumentException>(() => provider.CreateLogger(string.Empty));
    }

    [Test]
    public void CreateLogger_WithSameCategory_ShouldReturnSameInstance()
    {
        using var provider = new AppenderLoggerProvider([new TestLogAppender()]);

        var logger1 = provider.CreateLogger("TestLogger");
        var logger2 = provider.CreateLogger("TestLogger");

        Assert.That(logger2, Is.SameAs(logger1));
    }

    [Test]
    public void CreateLogger_WithDifferentCategories_ShouldReturnDifferentInstances()
    {
        using var provider = new AppenderLoggerProvider([new TestLogAppender()]);

        var logger1 = provider.CreateLogger("Logger1");
        var logger2 = provider.CreateLogger("Logger2");

        Assert.That(logger2, Is.Not.SameAs(logger1));
    }

    [Test]
    public void IsEnabled_WithRealLevels_ShouldReturnTrue()
    {
        using var provider = new AppenderLoggerProvider([new TestLogAppender()]);
        var logger = provider.CreateLogger("TestLogger");

        Assert.Multiple(() =>
        {
            Assert.That(logger.IsEnabled(LogLevel.Trace), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Debug), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Information), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Warning), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Error), Is.True);
            Assert.That(logger.IsEnabled(LogLevel.Critical), Is.True);
        });
    }

    [Test]
    public void IsEnabled_WithNone_ShouldReturnFalse()
    {
        using var provider = new AppenderLoggerProvider([new TestLogAppender()]);
        var logger = provider.CreateLogger("TestLogger");

        Assert.That(logger.IsEnabled(LogLevel.None), Is.False);
    }

    [Test]
    public void Log_ShouldConvertCallToLogEntry()
    {
        var appender = new TestLogAppender();
        using var provider = new AppenderLoggerProvider([appender]);
        var logger = provider.CreateLogger("TestLogger");
        var exception = new InvalidOperationException("Test exception");

        logger.LogError(exception, "Error message");

        var entries = appender.Entries;
        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(entries[0].LoggerName, Is.EqualTo("TestLogger"));
            Assert.That(entries[0].Message, Is.EqualTo("Error message"));
            Assert.That(entries[0].Exception, Is.SameAs(exception));
            Assert.That(entries[0].Timestamp, Is.Not.EqualTo(default(DateTime)));
        });
    }

    [Test]
    public void Log_WithStructuredState_ShouldExtractPropertiesWithoutOriginalFormat()
    {
        var appender = new TestLogAppender();
        using var provider = new AppenderLoggerProvider([appender]);
        var logger = provider.CreateLogger("TestLogger");

        logger.LogInformation("User {UserId} logged in from {Country}", 42, "CN");

        var entries = appender.Entries;
        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Message, Is.EqualTo("User 42 logged in from CN"));
            Assert.That(entries[0].Properties, Is.Not.Null);
            Assert.That(entries[0].Properties!["UserId"], Is.EqualTo(42));
            Assert.That(entries[0].Properties!["Country"], Is.EqualTo("CN"));
            Assert.That(entries[0].Properties!.ContainsKey("{OriginalFormat}"), Is.False);
        });
    }

    [Test]
    public void Log_WithNonStandardState_ShouldHaveNullProperties()
    {
        var appender = new TestLogAppender();
        using var provider = new AppenderLoggerProvider([appender]);
        var logger = provider.CreateLogger("TestLogger");

        logger.Log<object>(
            LogLevel.Information,
            default,
            new object(),
            null,
            static (_, _) => "Plain message");

        var entries = appender.Entries;
        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].Message, Is.EqualTo("Plain message"));
            Assert.That(entries[0].Properties, Is.Null);
        });
    }

    [Test]
    public void Log_WithNoneLevel_ShouldNotAppend()
    {
        var appender = new TestLogAppender();
        using var provider = new AppenderLoggerProvider([appender]);
        var logger = provider.CreateLogger("TestLogger");

        logger.Log<object>(
            LogLevel.None,
            default,
            new object(),
            null,
            static (_, _) => "Should not appear");

        Assert.That(appender.Entries, Is.Empty);
    }

    [Test]
    public void Log_ShouldFanOutToAllAppenders()
    {
        var appender1 = new TestLogAppender();
        var appender2 = new TestLogAppender();
        using var provider = new AppenderLoggerProvider([appender1, appender2]);
        var logger = provider.CreateLogger("TestLogger");

        logger.LogInformation("Fan-out message");

        Assert.Multiple(() =>
        {
            Assert.That(appender1.Entries, Has.Count.EqualTo(1));
            Assert.That(appender1.Entries[0].Message, Is.EqualTo("Fan-out message"));
            Assert.That(appender2.Entries, Has.Count.EqualTo(1));
            Assert.That(appender2.Entries[0].Message, Is.EqualTo("Fan-out message"));
        });
    }

    [Test]
    public void Log_WhenOneAppenderThrows_ShouldContinueWithOtherAppenders()
    {
        var throwingAppender = new ThrowingAppender();
        var appender = new TestLogAppender();
        using var provider = new AppenderLoggerProvider([throwingAppender, appender]);
        var logger = provider.CreateLogger("TestLogger");

        Assert.DoesNotThrow(() => logger.LogInformation("Isolation message"));

        Assert.Multiple(() =>
        {
            Assert.That(appender.Entries, Has.Count.EqualTo(1));
            Assert.That(appender.Entries[0].Message, Is.EqualTo("Isolation message"));
        });
    }

    [Test]
    public void Dispose_ShouldDisposeDisposableAppenders()
    {
        var appender1 = new TestLogAppender();
        var appender2 = new TestLogAppender();
        var provider = new AppenderLoggerProvider([appender1, appender2]);

        provider.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(appender1.DisposeCalled, Is.True);
            Assert.That(appender2.DisposeCalled, Is.True);
        });
    }

    [Test]
    public void Dispose_WhenOneAppenderThrows_ShouldStillDisposeOtherAppenders()
    {
        var throwingAppender = new ThrowingDisposableAppender();
        var appender = new TestLogAppender();
        var provider = new AppenderLoggerProvider([throwingAppender, appender]);

        Assert.DoesNotThrow(provider.Dispose);

        Assert.That(appender.DisposeCalled, Is.True);
    }

    /// <summary>追加时始终抛出异常的测试输出器。</summary>
    private sealed class ThrowingAppender : ILogAppender
    {
        public void Append(LogEntry entry)
        {
            throw new InvalidOperationException("Test exception");
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>释放时抛出异常的测试输出器。</summary>
    private sealed class ThrowingDisposableAppender : ILogAppender
    {
        public void Append(LogEntry entry)
        {
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
            throw new InvalidOperationException("Test dispose exception");
        }
    }
}
