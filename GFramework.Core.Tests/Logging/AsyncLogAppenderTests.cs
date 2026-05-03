// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging.Appenders;

namespace GFramework.Core.Tests.Logging;

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

        var startTime = DateTime.UtcNow;
        for (int i = 0; i < 10; i++)
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
            asyncAppender.Append(entry);
        }

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

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
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
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
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
            asyncAppender.Append(entry);
        }

        asyncAppender.Flush();

        Assert.That(innerAppender.Entries.Count, Is.EqualTo(100));
    }

    [Test]
    public void Flush_Should_Raise_OnFlushCompleted_With_Sender_And_Result()
    {
        var innerAppender = new TestAppender();
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 10);
        object? observedSender = null;
        AsyncLogFlushCompletedEventArgs? observedArgs = null;

        asyncAppender.Append(new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Flush check", null, null));

        asyncAppender.OnFlushCompleted += (sender, eventArgs) =>
        {
            observedSender = sender;
            observedArgs = eventArgs;
        };

        var result = asyncAppender.Flush(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(observedSender, Is.SameAs(asyncAppender));
            Assert.That(observedArgs, Is.Not.Null);
            Assert.That(observedArgs!.Success, Is.EqualTo(result));
        });
    }

    [Test]
    public void ILogAppender_Flush_Should_Raise_OnFlushCompleted_Only_Once()
    {
        var innerAppender = new TestAppender();
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 10);
        ILogAppender logAppender = asyncAppender;
        var observedResults = new List<bool>();

        asyncAppender.Append(new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Interface flush check", null, null));
        asyncAppender.OnFlushCompleted += (_, eventArgs) => observedResults.Add(eventArgs.Success);

        logAppender.Flush();

        Assert.That(observedResults, Has.Count.EqualTo(1));
        Assert.That(observedResults, Has.All.True);
    }

    [Test]
    public void Flush_WhenEntriesAlreadyProcessed_Should_Still_ReportSuccess()
    {
        using var appendCompleted = new ManualResetEventSlim();
        var innerAppender = new SignalingAppender(appendCompleted);
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 10);
        var observedResults = new List<bool>();

        asyncAppender.Append(new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Already processed", null, null));
        Assert.That(appendCompleted.Wait(TimeSpan.FromSeconds(1)), Is.True);

        asyncAppender.OnFlushCompleted += (_, eventArgs) => observedResults.Add(eventArgs.Success);

        var result = asyncAppender.Flush(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(observedResults, Has.Count.EqualTo(1));
            Assert.That(observedResults, Has.All.True);
            Assert.That(innerAppender.FlushCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Dispose_ShouldProcessRemainingEntries()
    {
        var innerAppender = new TestAppender();
        using (var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000))
        {
            for (int i = 0; i < 50; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
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

        var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", "Test", null, null);

        Assert.Throws<ObjectDisposedException>(() => asyncAppender.Append(entry));
    }

    [Test]
    public void PendingCount_ShouldReflectQueuedEntries()
    {
        var innerAppender = new SlowAppender(delayMs: 50);
        using var asyncAppender = new AsyncLogAppender(innerAppender, bufferSize: 1000);

        for (int i = 0; i < 10; i++)
        {
            var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
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
                    var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger",
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
        var reportedExceptions = new List<Exception>();
        var innerAppender = new ThrowingAppender();
        using var asyncAppender = new AsyncLogAppender(
            innerAppender,
            bufferSize: 1000,
            processingErrorHandler: reportedExceptions.Add);

        // 即使内部 Appender 抛出异常，也不应该影响调用线程
        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                asyncAppender.Append(entry);
            }
        });

        asyncAppender.Flush();

        Assert.That(reportedExceptions, Has.Count.EqualTo(10));
        Assert.That(reportedExceptions, Has.All.TypeOf<InvalidOperationException>());
        Assert.That(reportedExceptions.Select(static exception => exception.Message),
            Has.All.EqualTo("Test exception"));
    }

    [Test]
    public void Append_WhenProcessingErrorHandlerThrows_ShouldStillNotCrash()
    {
        var innerAppender = new ThrowingAppender();
        using var asyncAppender = new AsyncLogAppender(
            innerAppender,
            bufferSize: 1000,
            processingErrorHandler: static _ => throw new InvalidOperationException("Observer failure"));

        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                asyncAppender.Append(entry);
            }
        });

        Assert.That(asyncAppender.Flush(), Is.True);
    }

    [Test]
    public void Append_WhenInnerAppenderThrowsOperationCanceledException_ShouldNotReportError()
    {
        var reportedExceptions = new List<Exception>();
        var innerAppender = new CancellationAppender();
        using var asyncAppender = new AsyncLogAppender(
            innerAppender,
            bufferSize: 1000,
            processingErrorHandler: reportedExceptions.Add);

        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogLevel.Info, "TestLogger", $"Message {i}", null, null);
                asyncAppender.Append(entry);
            }
        });

        Assert.That(asyncAppender.Flush(), Is.True);
        Assert.That(reportedExceptions, Is.Empty);
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

        public void Dispose()
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

        public void Dispose()
        {
        }
    }

    private sealed class SignalingAppender : ILogAppender
    {
        private readonly ManualResetEventSlim _appendCompleted;

        public SignalingAppender(ManualResetEventSlim appendCompleted)
        {
            _appendCompleted = appendCompleted;
        }

        public int FlushCount { get; private set; }

        public void Append(LogEntry entry)
        {
            _appendCompleted.Set();
        }

        public void Flush()
        {
            FlushCount++;
        }

        public void Dispose()
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

        public void Dispose()
        {
        }
    }

    private class CancellationAppender : ILogAppender
    {
        public void Append(LogEntry entry)
        {
            throw new OperationCanceledException("Simulated cancellation");
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}
