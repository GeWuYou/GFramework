using System.IO;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.appenders;
using GFramework.Core.logging.formatters;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 FileAppender 的功能和行为
/// </summary>
[TestFixture]
public class FileAppenderTests
{
    [SetUp]
    public void SetUp()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.log");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
        {
            try
            {
                File.Delete(_testFilePath);
            }
            catch
            {
            }
        }
    }

    private string _testFilePath = null!;

    [Test]
    public void Constructor_WithNullFilePath_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FileAppender(null!));
    }

    [Test]
    public void Constructor_WithEmptyFilePath_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new FileAppender(""));
    }

    [Test]
    public void Constructor_WhenDirectoryDoesNotExist_ShouldCreateIt()
    {
        var dirPath = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        var filePath = Path.Combine(dirPath, "test.log");

        try
        {
            using (new FileAppender(filePath))
            {
                Assert.That(Directory.Exists(dirPath), Is.True);
            }
        }
        finally
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, true);
            }
        }
    }

    [Test]
    public void Append_ShouldWriteToFile()
    {
        using (var appender = new FileAppender(_testFilePath))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);
            appender.Append(entry);
            appender.Flush();
        }

        var content = File.ReadAllText(_testFilePath);
        Assert.That(content, Does.Contain("Test message"));
        Assert.That(content, Does.Contain("INFO"));
    }

    [Test]
    public void Append_MultipleEntries_ShouldWriteAll()
    {
        using (var appender = new FileAppender(_testFilePath))
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                appender.Append(entry);
            }

            appender.Flush();
        }

        var lines = File.ReadAllLines(_testFilePath);
        Assert.That(lines.Length, Is.EqualTo(10));
        for (int i = 0; i < 10; i++)
        {
            Assert.That(lines[i], Does.Contain($"Message {i}"));
        }
    }

    [Test]
    public void Append_WithJsonFormatter_ShouldWriteJson()
    {
        using (var appender = new FileAppender(_testFilePath, new JsonLogFormatter()))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);
            appender.Append(entry);
            appender.Flush();
        }

        var content = File.ReadAllText(_testFilePath);
        Assert.That(content, Does.Contain("\"message\""));
        Assert.That(content, Does.Contain("\"level\""));
    }

    [Test]
    public void Append_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var appender = new FileAppender(_testFilePath);
        appender.Dispose();

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

        Assert.Throws<ObjectDisposedException>(() => appender.Append(entry));
    }

    [Test]
    public void Append_WithAppendMode_ShouldAppendToExistingFile()
    {
        // 第一次写入
        using (var appender1 = new FileAppender(_testFilePath))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "First message", null, null);
            appender1.Append(entry);
            appender1.Flush();
        }

        // 第二次写入
        using (var appender2 = new FileAppender(_testFilePath))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Second message", null, null);
            appender2.Append(entry);
            appender2.Flush();
        }

        var lines = File.ReadAllLines(_testFilePath);
        Assert.That(lines.Length, Is.EqualTo(2));
        Assert.That(lines[0], Does.Contain("First message"));
        Assert.That(lines[1], Does.Contain("Second message"));
    }

    [Test]
    public void Flush_ShouldEnsureDataWritten()
    {
        using (var appender = new FileAppender(_testFilePath))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test message", null, null);

            appender.Append(entry);
            appender.Flush();
        }

        // 立即读取文件应该能看到内容
        var content = File.ReadAllText(_testFilePath);
        Assert.That(content, Does.Contain("Test message"));
    }
}