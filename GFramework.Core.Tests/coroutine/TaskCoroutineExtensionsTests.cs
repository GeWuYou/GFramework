using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.extensions;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// TaskCoroutineExtensions的单元测试类
/// 测试内容包括：
/// - AsCoroutineInstruction方法
/// - StartTaskAsCoroutine方法
/// </summary>
[TestFixture]
public class TaskCoroutineExtensionsTests
{
    /// <summary>
    /// 验证AsCoroutineInstruction应该返回WaitForTask
    /// </summary>
    [Test]
    public void AsCoroutineInstruction_Should_Return_WaitForTask()
    {
        var task = Task.CompletedTask;
        var instruction = task.AsCoroutineInstruction();

        Assert.That(instruction, Is.InstanceOf<WaitForTask>());
    }

    /// <summary>
    /// 验证AsCoroutineInstruction<T>应该返回WaitForTask<T>
    /// </summary>
    [Test]
    public void AsCoroutineInstructionOfT_Should_Return_WaitForTaskOfT()
    {
        var task = Task.FromResult(42);
        var instruction = task.AsCoroutineInstruction<int>();

        Assert.That(instruction, Is.InstanceOf<WaitForTask<int>>());
    }

    /// <summary>
    /// 验证AsCoroutineInstruction可以处理已完成的Task并验证其状态
    /// </summary>
    [Test]
    public void AsCoroutineInstruction_Should_Handle_Completed_Task()
    {
        var task = Task.CompletedTask;
        var instruction = task.AsCoroutineInstruction();

        // 验证指令类型
        Assert.That(instruction, Is.InstanceOf<WaitForTask>());
    
        // 验证已完成的任务是否立即可用
        Assert.That(task.IsCompleted, Is.True);
        Assert.That(task.Status, Is.EqualTo(TaskStatus.RanToCompletion));
    }
    

    /// <summary>
    /// 验证AsCoroutineInstruction<T>应该能够访问Task结果
    /// </summary>
    [Test]
    public void AsCoroutineInstructionOfT_Should_Access_Task_Result()
    {
        var task = Task.FromResult(42);
        var instruction = task.AsCoroutineInstruction<int>();

        task.Wait();

        Assert.That(instruction.Result, Is.EqualTo(42));
    }

    /// <summary>
    /// 验证AsCoroutineInstruction应该处理null Task（抛出异常）
    /// </summary>
    [Test]
    public void AsCoroutineInstruction_Should_Handle_Null_Task()
    {
        Task task = null!;

        Assert.Throws<ArgumentNullException>(() => task.AsCoroutineInstruction());
    }

    /// <summary>
    /// 验证AsCoroutineInstruction<T>应该处理null Task（抛出异常）
    /// </summary>
    [Test]
    public void AsCoroutineInstructionOfT_Should_Handle_Null_Task()
    {
        Task<int> task = null!;

        Assert.Throws<ArgumentNullException>(() => task.AsCoroutineInstruction<int>());
    }

    /// <summary>
    /// 验证AsCoroutineInstruction应该处理失败的Task
    /// </summary>
    [Test]
    public void AsCoroutineInstruction_Should_Handle_Faulted_Task()
    {
        var task = Task.FromException(new InvalidOperationException("Test exception"));
        var instruction = task.AsCoroutineInstruction();

        Assert.That(instruction, Is.InstanceOf<WaitForTask>());
    }

    /// <summary>
    /// 验证AsCoroutineInstruction<T>应该处理失败的Task
    /// </summary>
    [Test]
    public void AsCoroutineInstructionOfT_Should_Handle_Faulted_Task()
    {
        var task = Task.FromException<int>(new InvalidOperationException("Test exception"));
        var instruction = task.AsCoroutineInstruction<int>();

        Assert.That(instruction, Is.InstanceOf<WaitForTask<int>>());
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine应该返回有效的协程句柄
    /// </summary>
    [Test]
    public void StartTaskAsCoroutine_Should_Return_Valid_Handle()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);
        var task = Task.CompletedTask;

        var handle = scheduler.StartTaskAsCoroutine(task);

        Assert.That(handle.IsValid, Is.True);
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine<T>应该返回有效的协程句柄
    /// </summary>
    [Test]
    public void StartTaskAsCoroutineOfT_Should_Return_Valid_Handle()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);
        var task = Task.FromResult(42);

        var handle = scheduler.StartTaskAsCoroutine(task);

        Assert.That(handle.IsValid, Is.True);
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine应该等待Task完成
    /// </summary>
    [Test]
    public void StartTaskAsCoroutine_Should_Wait_For_Task_Completion()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);

        var completed = false;
        var tcs = new TaskCompletionSource<object?>();

        scheduler.StartTaskAsCoroutine(tcs.Task);

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(1));
        Assert.That(completed, Is.False);

        tcs.SetResult(null);
        Task.Delay(50).Wait();

        scheduler.Update();
        scheduler.Update();

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine<T>应该等待Task完成
    /// </summary>
    [Test]
    public void StartTaskAsCoroutineOfT_Should_Wait_For_Task_Completion()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);

        var tcs = new TaskCompletionSource<int>();

        scheduler.StartTaskAsCoroutine(tcs.Task);

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(1));

        tcs.SetResult(42);
        Task.Delay(50).Wait();

        scheduler.Update();
        scheduler.Update();

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine应该处理已完成的Task
    /// </summary>
    [Test]
    public void StartTaskAsCoroutine_Should_Handle_Completed_Task()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);
        var task = Task.CompletedTask;

        scheduler.StartTaskAsCoroutine(task);

        scheduler.Update();

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine应该处理失败的Task
    /// </summary>
    [Test]
    public void StartTaskAsCoroutine_Should_Handle_Faulted_Task()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);
        var task = Task.FromException(new InvalidOperationException("Test"));

        scheduler.StartTaskAsCoroutine(task);

        Assert.DoesNotThrow(() => scheduler.Update());
    }

    /// <summary>
    /// 验证StartTaskAsCoroutine<T>应该处理失败的Task
    /// </summary>
    [Test]
    public void StartTaskAsCoroutineOfT_Should_Handle_Faulted_Task()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);
        var task = Task.FromException<int>(new InvalidOperationException("Test"));

        scheduler.StartTaskAsCoroutine(task);

        Assert.DoesNotThrow(() => scheduler.Update());
    }
    
    
    /// <summary>
    /// 验证StartTaskAsCoroutine应该与调度器正常协作
    /// </summary>
    [Test]
    public void StartTaskAsCoroutine_Should_Work_With_Scheduler()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);

        var tcs = new TaskCompletionSource<object?>();
        scheduler.StartTaskAsCoroutine(tcs.Task);

        var tcs2 = new TaskCompletionSource<int>();
        scheduler.StartTaskAsCoroutine(tcs2.Task);

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(2));

        tcs.SetResult(null);
        tcs2.SetResult(42);

        Task.Delay(50).Wait();
        scheduler.Update();
        scheduler.Update();

        Assert.That(scheduler.ActiveCoroutineCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 测试用时间源类
    /// </summary>
    private class TestTimeSource : ITimeSource
    {
        public double CurrentTime { get; private set; }
        public double DeltaTime { get; private set; }

        public void Update()
        {
            DeltaTime = 0.1;
            CurrentTime += DeltaTime;
        }
    }
}
