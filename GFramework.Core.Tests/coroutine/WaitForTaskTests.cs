using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// WaitForTask的单元测试类
/// 测试内容包括：
/// - WaitForTask初始化和等待
/// - WaitForTask<T>初始化、等待和结果获取
/// - 异常处理
/// - 边界条件
/// </summary>
[TestFixture]
public class WaitForTaskTests
{
    /// <summary>
    /// 验证WaitForTask初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForTask_Should_Not_Be_Done_Initially()
    {
        var task = Task.Run(() => { });
        var wait = new WaitForTask(task);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitForTask应该在Task完成后完成
    /// </summary>
    [Test]
    public void WaitForTask_Should_Be_Done_After_Task_Completes()
    {
        var tcs = new TaskCompletionSource<object?>();
        var wait = new WaitForTask(tcs.Task);

        Assert.That(wait.IsDone, Is.False);

        tcs.SetResult(null);

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForTask应该处理已完成的Task
    /// </summary>
    [Test]
    public void WaitForTask_Should_Handle_Already_Completed_Task()
    {
        var task = Task.CompletedTask;

        Task.Delay(100).Wait();

        var wait = new WaitForTask(task);

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForTask应该处理失败的Task
    /// </summary>
    [Test]
    public void WaitForTask_Should_Handle_Faulted_Task()
    {
        var tcs = new TaskCompletionSource<object?>();
        var wait = new WaitForTask(tcs.Task);

        tcs.SetException(new InvalidOperationException("Test exception"));

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForTask应该处理取消的Task
    /// </summary>
    [Test]
    public void WaitForTask_Should_Handle_Cancelled_Task()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = Task.FromCanceled(cts.Token);

        Task.Delay(100).Wait();

        var wait = new WaitForTask(task);

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForTask应该抛出ArgumentNullException当task为null
    /// </summary>
    [Test]
    public void WaitForTask_Should_Throw_ArgumentNullException_When_Task_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForTask(null!));
    }

    /// <summary>
    /// 验证WaitForTask<T>初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Not_Be_Done_Initially()
    {
        var tcs = new TaskCompletionSource<int>();
        var wait = new WaitForTask<int>(tcs.Task);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitForTask<T>应该在Task完成后完成
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Be_Done_After_Task_Completes()
    {
        var tcs = new TaskCompletionSource<int>();
        var wait = new WaitForTask<int>(tcs.Task);

        Assert.That(wait.IsDone, Is.False);

        tcs.SetResult(42);

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForTask<T>应该返回Task的结果
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Return_Task_Result()
    {
        var tcs = new TaskCompletionSource<int>();
        var wait = new WaitForTask<int>(tcs.Task);

        tcs.SetResult(42);

        Task.Delay(100).Wait();

        Assert.That(wait.Result, Is.EqualTo(42));
    }

    /// <summary>
    /// 验证WaitForTask<T>应该处理已完成的Task
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Handle_Already_Completed_Task()
    {
        var task = Task.FromResult(42);

        Task.Delay(100).Wait();

        var wait = new WaitForTask<int>(task);

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.Result, Is.EqualTo(42));
    }

    /// <summary>
    /// 验证WaitForTask<T>应该处理失败的Task
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Handle_Faulted_Task()
    {
        var tcs = new TaskCompletionSource<int>();
        var wait = new WaitForTask<int>(tcs.Task);

        tcs.SetException(new InvalidOperationException("Test exception"));

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);

        Assert.Throws<InvalidOperationException>(() => { var _ = wait.Result; });
    }

    /// <summary>
    /// 验证WaitForTask<T>应该抛出ArgumentNullException当task为null
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Throw_ArgumentNullException_When_Task_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForTask<int>(null!));
    }

    /// <summary>
    /// 验证WaitForTask实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForTask_Should_Implement_IYieldInstruction()
    {
        var task = Task.CompletedTask;
        var wait = new WaitForTask(task);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitForTask<T>实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Should_Implement_IYieldInstruction()
    {
        var task = Task.FromResult(42);
        var wait = new WaitForTask<int>(task);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitForTask的Update方法不影响状态
    /// </summary>
    [Test]
    public void WaitForTask_Update_Should_Not_Affect_State()
    {
        var tcs = new TaskCompletionSource<object?>();
        var wait = new WaitForTask(tcs.Task);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitForTask<T>的Update方法不影响状态
    /// </summary>
    [Test]
    public void WaitForTaskOfT_Update_Should_Not_Affect_State()
    {
        var tcs = new TaskCompletionSource<int>();
        var wait = new WaitForTask<int>(tcs.Task);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitForTask在延迟完成后能够正确等待
    /// </summary>
    [Test]
    public void WaitForTask_Should_Wait_For_Delayed_Task()
    {
        var delayMs = 100;
        var task = Task.Delay(delayMs);
        var wait = new WaitForTask(task);

        Assert.That(wait.IsDone, Is.False);

        Task.Delay(delayMs + 50).Wait();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForTask<T>在异步操作完成后能够正确获取结果
    /// </summary>
    [Test]
    public async Task WaitForTaskOfT_Should_Get_Result_From_Async_Operation()
    {
        var expectedValue = 123;
        var task = Task.Run(async () =>
        {
            await Task.Delay(50);
            return expectedValue;
        });

        var wait = new WaitForTask<int>(task);

        await task;

        Task.Delay(100).Wait();

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.Result, Is.EqualTo(expectedValue));
    }

    /// <summary>
    /// 验证WaitForTask应该处理长时间运行的Task
    /// </summary>
    [Test]
    public void WaitForTask_Should_Handle_Long_Running_Task()
    {
        var task = Task.Delay(200);
        var wait = new WaitForTask(task);

        Assert.That(wait.IsDone, Is.False);

        Task.Delay(250).Wait();

        Assert.That(wait.IsDone, Is.True);
    }
}
