using System.IO;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Appenders;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 RollingFileAppender 的功能和行为
/// </summary>
[TestFixture]
public class RollingFileAppenderTests
{
    [SetUp]
    public void SetUp()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"rolling_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _testFilePath = Path.Combine(_testDir, "app.log");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch
            {
            }
        }
    }

    private string _testDir = null!;
    private string _testFilePath = null!;

    [Test]
    public void Constructor_WithInvalidMaxFileSize_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RollingFileAppender(_testFilePath, maxFileSize: 0));
        Assert.Throws<ArgumentException>(() => new RollingFileAppender(_testFilePath, maxFileSize: -1));
    }

    [Test]
    public void Constructor_WithInvalidMaxFileCount_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RollingFileAppender(_testFilePath, maxFileCount: 0));
        Assert.Throws<ArgumentException>(() => new RollingFileAppender(_testFilePath, maxFileCount: -1));
    }

    [Test]
    public void Append_WhenFileSizeExceedsLimit_ShouldRollFiles()
    {
        using (var appender = new RollingFileAppender(_testFilePath, maxFileSize: 500, maxFileCount: 3))
        {
            // 写入足够多的日志触发轮转
            for (int i = 0; i < 20; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger",
                    $"This is a test message number {i} with some padding to increase size", null, null);
                appender.Append(entry);
            }

            appender.Flush();
        }

        // 检查是否生成了多个文件
        var files = Directory.GetFiles(_testDir, "*.log");
        Assert.That(files.Length, Is.GreaterThan(1));
    }

    [Test]
    public void Append_ShouldNotExceedMaxFileCount()
    {
        const int maxFileCount = 3;
        using (var appender = new RollingFileAppender(_testFilePath, maxFileSize: 300, maxFileCount: maxFileCount))
        {
            // 写入大量日志触发多次轮转
            for (int i = 0; i < 50; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger",
                    $"This is a test message number {i} with some padding to increase size significantly", null, null);
                appender.Append(entry);
            }

            appender.Flush();
        }

        var files = Directory.GetFiles(_testDir, "*.log");
        Assert.That(files.Length, Is.LessThanOrEqualTo(maxFileCount));
    }

    [Test]
    public void Append_RolledFiles_ShouldHaveCorrectNaming()
    {
        using (var appender = new RollingFileAppender(_testFilePath, maxFileSize: 400, maxFileCount: 3))
        {
            for (int i = 0; i < 30; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger",
                    $"Test message {i} with padding to trigger rolling", null, null);
                appender.Append(entry);
            }

            appender.Flush();
        }

        var files = Directory.GetFiles(_testDir, "*.log")
            .Select(static path => Path.GetFileName(path) ?? string.Empty)
            .OrderBy(f => f, System.StringComparer.Ordinal)
            .ToArray();

        // 应该有 app.log, app.1.log, app.2.log 等
        Assert.That(files, Does.Contain("app.log"));
        if (files.Length > 1)
        {
            Assert.That(
                files.Any(f =>
                    f.StartsWith("app.", System.StringComparison.Ordinal) &&
                    f.EndsWith(".log", System.StringComparison.Ordinal) &&
                    !string.Equals(f, "app.log", System.StringComparison.Ordinal)),
                Is.True);
        }
    }

    [Test]
    public void Append_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var appender = new RollingFileAppender(_testFilePath);
        appender.Dispose();

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

        Assert.Throws<ObjectDisposedException>(() => appender.Append(entry));
    }

    [Test]
    public void Append_WithSmallMaxFileSize_ShouldRollFrequently()
    {
        using (var appender = new RollingFileAppender(_testFilePath, maxFileSize: 200, maxFileCount: 5))
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger",
                    "This is a longer message to trigger rolling more frequently", null, null);
                appender.Append(entry);
            }

            appender.Flush();
        }

        var files = Directory.GetFiles(_testDir, "*.log");
        Assert.That(files.Length, Is.GreaterThan(1));
    }

    [Test]
    public void Flush_ShouldEnsureDataWritten()
    {
        using (var appender = new RollingFileAppender(_testFilePath))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

            appender.Append(entry);
            appender.Flush();
        }

        Assert.That(File.Exists(_testFilePath), Is.True);
        var content = File.ReadAllText(_testFilePath);
        Assert.That(content, Does.Contain("Test message"));
    }
}
