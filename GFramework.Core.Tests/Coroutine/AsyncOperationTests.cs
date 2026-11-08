// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine;
using GFramework.Core.Coroutine.Instructions;

namespace GFramework.Core.Tests.Coroutine;

/// <summary>
///     验证外部完成通知复用 Task 的竞争、延续和结果传播契约。
/// </summary>
[TestFixture]
public sealed class AsyncOperationTests
{
    [ThreadStatic]
    private static bool _settingResult;

    /// <summary>
    ///     验证完成前的 await 与完成后的 await 都能观察同一结果。
    /// </summary>
    [Test]
    public async Task Completion_Should_Resume_All_Awaiters_And_Support_Repeated_Await()
    {
        var operation = new AsyncOperation();
        var first = Observe(operation);
        var second = Observe(operation);
        Assert.That(first.IsCompleted, Is.False);

        operation.SetCompleted();
        await Task.WhenAll(first, second).ConfigureAwait(false);
        await operation;
        Assert.That(operation.Task.IsCompletedSuccessfully, Is.True);
    }

    /// <summary>
    ///     验证异常通过 await 和协程等待保留相同异常对象。
    /// </summary>
    [Test]
    public void Failure_Should_Reach_Await_And_Coroutine_Update()
    {
        var operation = new AsyncOperation();
        var failure = new InvalidOperationException("operation failed");
        operation.SetException(failure);

        Assert.That(operation.Task.IsFaulted, Is.True);
        Assert.That(Assert.Throws<InvalidOperationException>(() => operation.Update(0)), Is.SameAs(failure));
        Assert.That(Assert.Throws<InvalidOperationException>(() => operation.GetAwaiter().GetResult()),
            Is.SameAs(failure));
    }

    /// <summary>
    ///     验证取消保持 Task 取消状态，并在协程等待中传播。
    /// </summary>
    [Test]
    public void Cancellation_Should_Be_Observable_By_Both_Waiting_Models()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var operation = new AsyncOperation();
        operation.SetCanceled(source.Token);

        Assert.That(operation.Task.IsCanceled, Is.True);
        Assert.Catch<OperationCanceledException>(() => operation.Update(0));
    }

    /// <summary>
    ///     验证竞争完成只有一个赢家，重复请求不抛错或覆盖结果。
    /// </summary>
    [Test]
    public async Task Concurrent_Termination_Should_Preserve_One_Result()
    {
        var operation = new AsyncOperation();
        var tasks = Enumerable.Range(0, 64).Select(index => Task.Run(() =>
        {
            if (index % 3 == 0)
            {
                operation.SetCompleted();
            }
            else if (index % 3 == 1)
            {
                operation.SetException(new InvalidOperationException("competing result"));
            }
            else
            {
                operation.SetCanceled();
            }
        }));
        await Task.WhenAll(tasks).ConfigureAwait(false);

        Assert.That(operation.IsCompleted, Is.True);
        if (operation.Task.IsFaulted)
        {
            _ = operation.Task.Exception;
        }

        var status = operation.Task.Status;
        operation.SetCompleted();
        operation.SetException(new InvalidOperationException("late result"));
        operation.SetCanceled();
        Assert.That(operation.Task.Status, Is.EqualTo(status));
    }

    /// <summary>
    ///     验证请求同步延续也不能在设置结果的调用栈中执行。
    /// </summary>
    [Test]
    public async Task Completion_Should_Not_Inline_Continuations()
    {
        var operation = new AsyncOperation();
        var continuation = operation.Task.ContinueWith(
            _ => _settingResult,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        _settingResult = true;
        try
        {
            operation.SetCompleted();
        }
        finally
        {
            _settingResult = false;
        }

        Assert.That(await continuation.ConfigureAwait(false), Is.False);
    }

    /// <summary>
    ///     验证调度器直接等待失败操作时记录 Faulted，不执行成功路径。
    /// </summary>
    [Test]
    public void Scheduler_Should_Observe_Operation_Failure()
    {
        var operation = new AsyncOperation();
        var scheduler = new CoroutineScheduler(new FakeTimeSource());
        var continued = false;

        IEnumerator<IYieldInstruction> Routine()
        {
            yield return operation;
            continued = true;
        }

        var handle = scheduler.Run(Routine());
        operation.SetException(new InvalidOperationException("failure"));
        scheduler.Update();

        Assert.That(continued, Is.False);
        Assert.That(scheduler.TryGetCompletionStatus(handle, out var status), Is.True);
        Assert.That(status, Is.EqualTo(CoroutineCompletionStatus.Faulted));
    }

    /// <summary>
    ///     验证传入空异常会直接失败，不影响后续正常完成。
    /// </summary>
    [Test]
    public void Null_Failure_Should_Not_Terminate_Operation()
    {
        var operation = new AsyncOperation();
        Assert.Throws<ArgumentNullException>(() => operation.SetException(null!));
        Assert.That(operation.IsDone, Is.False);
        operation.SetCompleted();
        Assert.That(operation.IsDone, Is.True);
    }

    private static async Task Observe(AsyncOperation operation)
    {
        await operation;
    }
}
