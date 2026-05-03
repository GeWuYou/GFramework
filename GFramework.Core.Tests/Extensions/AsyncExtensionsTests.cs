// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using GFramework.Core.Extensions;
using GFramework.Core.Functional.Async;
using NUnit.Framework;

namespace GFramework.Core.Tests.Extensions;

/// <summary>
///     测试 AsyncExtensions 扩展方法的功能
/// </summary>
[TestFixture]
public class AsyncExtensionsTests
{
    /// <summary>
    ///     测试WithTimeout方法在任务超时前完成时返回结果
    /// </summary>
    [Test]
    public async Task WithTimeout_Should_Return_Result_When_Task_Completes_Before_Timeout()
    {
        // Act
        var result = await AsyncExtensions.WithTimeoutAsync(
            _ => Task.FromResult(42),
            TimeSpan.FromSeconds(1));

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试WithTimeout方法在任务超过超时时抛出TimeoutException
    /// </summary>
    [Test]
    public void WithTimeout_Should_Throw_TimeoutException_When_Task_Exceeds_Timeout()
    {
        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(() =>
            AsyncExtensions.WithTimeoutAsync(
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                    return 42;
                },
                TimeSpan.FromMilliseconds(100)));
    }

    /// <summary>
    ///     测试WithTimeout方法在取消请求时抛出OperationCanceledException
    /// </summary>
    [Test]
    public void WithTimeout_Should_Throw_OperationCanceledException_When_Cancellation_Requested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(() =>
            AsyncExtensions.WithTimeoutAsync(
                async ct =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                    return 42;
                },
                TimeSpan.FromSeconds(1),
                cts.Token));
    }

    /// <summary>
    ///     测试WithTimeout方法在超时时取消内部任务
    /// </summary>
    [Test]
    public void WithTimeout_Should_Cancel_Inner_Task_When_Timeout_Elapses()
    {
        // Arrange
        var innerTaskCanceled = false;

        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(() =>
            AsyncExtensions.WithTimeoutAsync(
                async ct =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
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

    /// <summary>
    ///     测试无返回值的WithTimeout在任务超时前完成
    /// </summary>
    [Test]
    public async Task WithTimeout_NoResult_Should_Complete_When_Task_Completes_Before_Timeout()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await AsyncExtensions.WithTimeoutAsync(
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(1));
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), "Task should complete before timeout");
        Assert.Pass("Task completed successfully within timeout period");
    }

    /// <summary>
    ///     测试无返回值的WithTimeout在任务超时时抛出TimeoutException
    /// </summary>
    [Test]
    public void WithTimeout_NoResult_Should_Throw_TimeoutException_When_Task_Exceeds_Timeout()
    {
        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(() =>
            AsyncExtensions.WithTimeoutAsync(
                ct => Task.Delay(TimeSpan.FromSeconds(2), ct),
                TimeSpan.FromMilliseconds(100)));
    }

    /// <summary>
    ///     测试无返回值的WithTimeout在超时时取消内部任务
    /// </summary>
    [Test]
    public void WithTimeout_NoResult_Should_Cancel_Inner_Task_When_Timeout_Elapses()
    {
        // Arrange
        var innerTaskCanceled = false;

        // Act & Assert
        Assert.ThrowsAsync<TimeoutException>(() =>
            AsyncExtensions.WithTimeoutAsync(
                async ct =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
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

    /// <summary>
    ///     测试WithRetry方法在任务成功时返回结果
    /// </summary>
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
        var result = await taskFactory.WithRetryAsync(3, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(attemptCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     测试WithRetry方法在失败时重试
    /// </summary>
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
        var result = await taskFactory.WithRetryAsync(3, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.That(result, Is.EqualTo(42));
        Assert.That(attemptCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     测试WithRetry方法在所有重试都失败时抛出AggregateException
    /// </summary>
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
        Assert.ThrowsAsync<AggregateException>(() =>
            taskFactory.WithRetryAsync(2, TimeSpan.FromMilliseconds(10)));
    }

    /// <summary>
    ///     测试WithRetry方法遵守ShouldRetry谓词
    /// </summary>
    [Test]
    public void WithRetry_Should_Respect_ShouldRetry_Predicate()
    {
        static Task<int> ThrowShouldNotRetry(string parameterName)
        {
            throw new ArgumentException("Should not retry", parameterName);
        }

        // Arrange
        var attemptCount = 0;
        Func<Task<int>> taskFactory = () =>
        {
            attemptCount++;
            return ThrowShouldNotRetry(nameof(taskFactory));
        };

        // Act & Assert
        var exception = Assert.ThrowsAsync<AggregateException>(() =>
            taskFactory.WithRetryAsync(3, TimeSpan.FromMilliseconds(10),
                ex => ex is not ArgumentException));

        Assert.That(attemptCount, Is.EqualTo(1)); // 不应该重试
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.InnerExceptions, Has.Count.EqualTo(1));
        Assert.That(exception.InnerExceptions[0], Is.TypeOf<ArgumentException>());
        Assert.That(((ArgumentException)exception.InnerExceptions[0]).ParamName, Is.EqualTo(nameof(taskFactory)));
    }

    /// <summary>
    ///     测试TryAsync方法在任务成功时返回成功结果
    /// </summary>
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

    /// <summary>
    ///     测试TryAsync方法在任务抛出异常时返回失败结果
    /// </summary>
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

    /// <summary>
    ///     测试WithFallback方法在任务成功时返回结果
    /// </summary>
    [Test]
    public async Task WithFallback_Should_Return_Result_When_Task_Succeeds()
    {
        // Arrange
        var task = Task.FromResult(42);

        // Act
        var result = await task.WithFallbackAsync(_ => -1);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    /// <summary>
    ///     测试WithFallback方法在任务失败时返回备用值
    /// </summary>
    [Test]
    public async Task WithFallback_Should_Return_Fallback_Value_When_Task_Fails()
    {
        // Arrange
        var task = Task.FromException<int>(new InvalidOperationException("Test error"));

        // Act
        var result = await task.WithFallbackAsync(_ => -1);

        // Assert
        Assert.That(result, Is.EqualTo(-1));
    }

    /// <summary>
    ///     测试WithFallback方法将异常传递给备用函数
    /// </summary>
    [Test]
    public async Task WithFallback_Should_Pass_Exception_To_Fallback()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var task = Task.FromException<int>(expectedException);
        Exception? capturedEx = null;

        // Act
        await task.WithFallbackAsync(ex =>
        {
            capturedEx = ex;
            return -1;
        });

        // Assert
        Assert.That(capturedEx, Is.SameAs(expectedException));
    }
}
