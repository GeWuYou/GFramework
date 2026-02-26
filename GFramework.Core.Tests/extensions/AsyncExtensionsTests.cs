using System.Diagnostics;
using GFramework.Core.extensions;
using GFramework.Core.Functional.Async;
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
        // Act
        var result = await AsyncExtensions.WithTimeout(
            _ => Task.FromResult(42),
            TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void WithTimeout_Should_Throw_TimeoutException_When_Task_Exceeds_Timeout()
    {
        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await AsyncExtensions.WithTimeout(
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    return 42;
                },
                TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public void WithTimeout_Should_Throw_OperationCanceledException_When_Cancellation_Requested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await AsyncExtensions.WithTimeout(
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    return 42;
                },
                TimeSpan.FromSeconds(1),
                cts.Token));
    }

    [Test]
    public void WithTimeout_Should_Cancel_Inner_Task_When_Timeout_Elapses()
    {
        // Arrange
        var innerTaskCanceled = false;

        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await AsyncExtensions.WithTimeout(
                async ct =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                        return 0;
                    }
                    catch (OperationCanceledException)
                    {
                        innerTaskCanceled = true;
                        throw;
                    }
                },
                TimeSpan.FromMilliseconds(100)));

        Assert.That(innerTaskCanceled, Is.True, "内部任务应在超时时收到取消信号");
    }

    [Test]
    public async Task WithTimeout_NoResult_Should_Complete_When_Task_Completes_Before_Timeout()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await AsyncExtensions.WithTimeout(
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(1));
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), "Task should complete before timeout");
        Assert.Pass("Task completed successfully within timeout period");
    }

    [Test]
    public void WithTimeout_NoResult_Should_Throw_TimeoutException_When_Task_Exceeds_Timeout()
    {
        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await AsyncExtensions.WithTimeout(
                ct => Task.Delay(TimeSpan.FromSeconds(2), ct),
                TimeSpan.FromMilliseconds(100)));
    }

    [Test]
    public void WithTimeout_NoResult_Should_Cancel_Inner_Task_When_Timeout_Elapses()
    {
        // Arrange
        var innerTaskCanceled = false;

        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(async () =>
            await AsyncExtensions.WithTimeout(
                async ct =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    }
                    catch (OperationCanceledException)
                    {
                        innerTaskCanceled = true;
                        throw;
                    }
                },
                TimeSpan.FromMilliseconds(100)));

        Assert.That(innerTaskCanceled, Is.True, "内部任务应在超时时收到取消信号");
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
        var result = await task.WithFallback(_ => -1);

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