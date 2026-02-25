using System.Diagnostics;
using GFramework.Core.extensions;
using NUnit.Framework;

namespace GFramework.Core.Tests.extensions;

/// <summary>
///     测试 AsyncExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class AsyncExtensionsTests
{
    [Test]
    public async Task WithTimeout_Should_Return_Result_When_Task_Completes_Before_Timeout()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act
        var result = await task.WithTimeout(TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void WithTimeout_Should_Throw_TimeoutException_When_Task_Exceeds_Timeout()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => 42);

        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await task.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public void WithTimeout_Should_Throw_OperationCanceledException_When_Cancellation_Requested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var task = Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(_ => 42);
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await task.WithTimeout(TimeSpan.FromSeconds(1), cts.Token));
    }

    [Test]
    public async Task WithTimeout_NoResult_Should_Complete_When_Task_Completes_Before_Timeout()
    {
        // Arrange
        var task = Task.CompletedTask;
        var stopwatch = Stopwatch.StartNew();

        // Act
        await task.WithTimeout(TimeSpan.FromSeconds(1));
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), "Task should complete before timeout");
        Assert.Pass("Task completed successfully within timeout period");
    }

    [Test]
    public void WithTimeout_NoResult_Should_Throw_TimeoutException_When_Task_Exceeds_Timeout()
    {
        // Arrange
        var task = Task.Delay(TimeSpan.FromSeconds(2));

        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await task.WithTimeout(TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public async Task WithRetry_Should_Return_Result_When_Task_Succeeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<int>> taskFactory = () =>
        {
            attemptCount++;
            return Task.FromResult(42);
        };

        // Act
        var result = await taskFactory.WithRetry(3, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(attemptCount, Is.EqualTo(1));
    }

    [Test]
    public async Task WithRetry_Should_Retry_On_Failure()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<int>> taskFactory = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new InvalidOperationException("Temporary failure");
            return Task.FromResult(42);
        };

        // Act
        var result = await taskFactory.WithRetry(3, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(attemptCount, Is.EqualTo(3));
    }

    [Test]
    public void WithRetry_Should_Throw_AggregateException_When_All_Retries_Fail()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<int>> taskFactory = () =>
        {
            attemptCount++;
            throw new InvalidOperationException("Permanent failure");
        };

        // Act & Assert
        Assert.ThrowsAsync<AggregateException>(async () =>
            await taskFactory.WithRetry(2, TimeSpan.FromMilliseconds(10)));
    }

    [Test]
    public async Task WithRetry_Should_Respect_ShouldRetry_Predicate()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<int>> taskFactory = () =>
        {
            attemptCount++;
            throw new ArgumentException("Should not retry");
        };

        // Act & Assert
        Assert.ThrowsAsync<AggregateException>(async () =>
            await taskFactory.WithRetry(3, TimeSpan.FromMilliseconds(10),
                ex => ex is not ArgumentException));

        await Task.Delay(50); // 等待任务完成
        Assert.That(attemptCount, Is.EqualTo(1)); // 不应该重试
    }

    [Test]
    public async Task TryAsync_Should_Return_Success_When_Task_Succeeds()
    {
        // Arrange
        Func<Task<int>> func = () => Task.FromResult(42);

        // Act
        var result = await func.TryAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.IfFail(0), Is.EqualTo(42));
    }

    [Test]
    public async Task TryAsync_Should_Return_Failure_When_Task_Throws()
    {
        // Arrange
        Func<Task<int>> func = () => throw new InvalidOperationException("Test error");

        // Act
        var result = await func.TryAsync();

        // Assert
        Assert.That(result.IsFaulted, Is.True);
    }

    [Test]
    public async Task WhenAll_Should_Wait_For_All_Tasks()
    {
        // Arrange
        var task1 = Task.Delay(10);
        var task2 = Task.Delay(20);
        var task3 = Task.Delay(30);
        var tasks = new[] { task1, task2, task3 };

        // Act
        await tasks.WhenAll();

        // Assert
        Assert.That(task1.IsCompleted, Is.True);
        Assert.That(task2.IsCompleted, Is.True);
        Assert.That(task3.IsCompleted, Is.True);
    }

    [Test]
    public async Task WhenAll_WithResults_Should_Return_All_Results()
    {
        // Arrange
        var task1 = Task.FromResult(1);
        var task2 = Task.FromResult(2);
        var task3 = Task.FromResult(3);
        var tasks = new[] { task1, task2, task3 };

        // Act
        var results = await tasks.WhenAll();

        // Assert
        Assert.That(results, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task WithFallback_Should_Return_Result_When_Task_Succeeds()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act
        var result = await task.WithFallback(_ => -1);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task WithFallback_Should_Return_Fallback_Value_When_Task_Fails()
    {
        // Arrange
        var task = Task.FromException<int>(new InvalidOperationException("Test error"));

        // Act
        var result = await task.WithFallback(ex => -1);

        // Assert
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public async Task WithFallback_Should_Pass_Exception_To_Fallback()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var task = Task.FromException<int>(expectedException);
        Exception? capturedEx = null;

        // Act
        await task.WithFallback(ex =>
        {
            capturedEx = ex;
            return -1;
        });

        // Assert
        Assert.That(capturedEx, Is.SameAs(expectedException));
    }
}
