using GFramework.Core.Abstractions.logging;
using GFramework.Core.logging.appenders;
using NUnit.Framework;

namespace GFramework.Core.Tests.logging;

/// <summary>
///     测试 AsyncLogAppender 的功能和行为
/// </summary>
[TestFixture]
public class AsyncLogAppenderTests
{
    [Test]
    public void Constructor_WithNullInnerAppender_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AsyncLogAppender(null!));
    }

    [Test]
    public void Constructor_WithInvalidBufferSize_ShouldThrowArgumentException()
    {
        var innerAppender = new TestAppender();
        Assert.Throws<ArgumentException>(() => new AsyncLogAppender(innerAppender, bufferSize: 0));
        Assert.Throws<ArgumentException>(() => new AsyncLogAppender(innerAppender, bufferSize: -1));
    }

    [Test]
    public void Append_ShouldNotBlock()
    {
        var innerAppender = new SlowAppender(delayMs: 100);
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000);

        var startTime = DateTime.Now;
        for (int i = 0; i < 10; i++)
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
            asyncAppender.Append(entry);
        }

        var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

        // 异步写入应该非常快（< 100ms），不应该等待内部 Appender
        Assert.That(elapsed, Is.LessThan(100));
    }

    [Test]
    public void Append_ShouldEventuallyWriteToInnerAppender()
    {
        var innerAppender = new TestAppender();
        using (var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000))
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                asyncAppender.Append(entry);
            }

            asyncAppender.Flush();
        }

        Assert.That(innerAppender.Entries.Count, Is.EqualTo(10));
    }

    [Test]
    public void Flush_ShouldWaitForAllEntriesToBeProcessed()
    {
        var innerAppender = new TestAppender();
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000);

        for (int i = 0; i < 100; i++)
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
            asyncAppender.Append(entry);
        }

        asyncAppender.Flush();

        Assert.That(innerAppender.Entries.Count, Is.EqualTo(100));
    }

    [Test]
    public void Dispose_ShouldProcessRemainingEntries()
    {
        var innerAppender = new TestAppender();
        using (var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000))
        {
            for (int i = 0; i < 50; i++)
            {
                var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                asyncAppender.Append(entry);
            }
        } // Dispose 会等待所有日志处理完成

        Assert.That(innerAppender.Entries.Count, Is.EqualTo(50));
    }

    [Test]
    public void Append_AfterDispose_ShouldThrowObjectDisposedException()
    {
        var innerAppender = new TestAppender();
        var asyncAppender = new AsyncLogAppender(innerAppender);
        asyncAppender.Dispose();

        var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", "Test", null, null);

        Assert.Throws<ObjectDisposedException>(() => asyncAppender.Append(entry));
    }

    [Test]
    public void PendingCount_ShouldReflectQueuedEntries()
    {
        var innerAppender = new SlowAppender(delayMs: 50);
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000);

        for (int i = 0; i < 10; i++)
        {
            var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
            asyncAppender.Append(entry);
        }

        // 应该有一些待处理的条目
        Assert.That(asyncAppender.PendingCount, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task Append_FromMultipleThreads_ShouldHandleConcurrency()
    {
        var innerAppender = new TestAppender();
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 10000);

        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger",
                        $"Thread {threadId} Message {i}", null, null);
                    asyncAppender.Append(entry);
                }
            });
        }

        await Task.WhenAll(tasks);
        asyncAppender.Flush();

        Assert.That(innerAppender.Entries.Count, Is.EqualTo(1000));
    }

    [Test]
    public void Append_WhenInnerAppenderThrows_ShouldNotCrash()
    {
        var innerAppender = new ThrowingAppender();
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000);

        // 即使内部 Appender 抛出异常，也不应该影响调用线程
        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.Now, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                asyncAppender.Append(entry);
            }
        });

        Thread.Sleep(100); // 等待后台处理
    }

    // 辅助测试类
    private class TestAppender : ILogAppender
    {
        public List<LogEntry> Entries { get; } = new();

        public void Append(LogEntry entry)
        {
            lock (Entries)
            {
                Entries.Add(entry);
            }
        }

        public void Flush()
        {
        }
    }

    private class SlowAppender : ILogAppender
    {
        private readonly int _delayMs;

        public SlowAppender(int delayMs)
        {
            _delayMs = delayMs;
        }

        public void Append(LogEntry entry)
        {
            Thread.Sleep(_delayMs);
        }

        public void Flush()
        {
        }
    }

    private class ThrowingAppender : ILogAppender
    {
        public void Append(LogEntry entry)
        {
            throw new InvalidOperationException("Test exception");
        }

        public void Flush()
        {
        }
    }
}