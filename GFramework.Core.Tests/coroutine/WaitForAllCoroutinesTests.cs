using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.extensions;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForAllCoroutines的单元测试类
///     测试内容包括：
///     - 初始化和基本功能
///     - IsDone属性行为
///     - 空句柄集合处理
///     - 单个协程处理
///     - 多个协程处理
///     - 与CoroutineScheduler集成
/// </summary>
[TestFixture]
public class WaitForAllCoroutinesTests
{
    /// <summary>
    ///     验证WaitForAllCoroutines初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Not_Be_Done_Initially_With_Running_Coroutines()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var coroutine1 = CreateDelayedCoroutine(() => { }, 1.0);
        var coroutine2 = CreateDelayedCoroutine(() => { }, 1.0);

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该在所有协程完成后完成
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Be_Done_When_All_Coroutines_Complete()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateSimpleCoroutine();

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        scheduler.Update();
        scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该在所有协程完成后完成（使用Delay）
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Wait_For_All_Delayed_Coroutines()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var executionCount = 0;
        var coroutine1 = CreateDelayedCoroutine(() => executionCount++, 1.0);
        var coroutine2 = CreateDelayedCoroutine(() => executionCount++, 1.0);
        var coroutine3 = CreateDelayedCoroutine(() => executionCount++, 1.0);

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2),
            scheduler.Run(coroutine3)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.False);
        Assert.That(executionCount, Is.EqualTo(0));

        for (var i = 0; i < 12; i++) scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
        Assert.That(executionCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理空句柄列表
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Empty_Handles_List()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var handles = Array.Empty<CoroutineHandle>();

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该抛出ArgumentNullException当handles为null
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Throw_ArgumentNullException_When_Handles_Is_Null()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        Assert.Throws<ArgumentNullException>(() => new WaitForAllCoroutines(scheduler, null!));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该抛出ArgumentNullException当scheduler为null
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Throw_ArgumentNullException_When_Scheduler_Is_Null()
    {
        var handles = Array.Empty<CoroutineHandle>();

        Assert.Throws<ArgumentNullException>(() => new WaitForAllCoroutines(null!, handles));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理单个协程
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Single_Coroutine()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var coroutine = CreateSimpleCoroutine();

        var handles = new List<CoroutineHandle> { scheduler.Run(coroutine) };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该在部分协程完成时未完成
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Not_Be_Done_When_Some_Coroutines_Complete()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var executionCount = 0;
        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateDelayedCoroutine(() => executionCount++, 1.0);
        var coroutine3 = CreateDelayedCoroutine(() => executionCount++, 1.0);

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2),
            scheduler.Run(coroutine3)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        scheduler.Update();

        Assert.That(wait.IsDone, Is.False);
        Assert.That(executionCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理被终止的协程
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Killed_Coroutines()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var coroutine1 = CreateDelayedCoroutine(() => { }, 1.0);
        var coroutine2 = CreateDelayedCoroutine(() => { }, 1.0);

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.False);

        scheduler.Kill(handles[0]);

        Assert.That(wait.IsDone, Is.False);

        scheduler.Kill(handles[1]);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理被暂停和恢复的协程
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Paused_And_Resumed_Coroutines()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var executionCount = 0;
        var coroutine1 = CreateDelayedCoroutine(() => executionCount++, 1.0);
        var coroutine2 = CreateDelayedCoroutine(() => executionCount++, 1.0);

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        scheduler.Pause(handles[0]);

        for (var i = 0; i < 12; i++) scheduler.Update();

        Assert.That(wait.IsDone, Is.False);
        Assert.That(executionCount, Is.EqualTo(1));

        scheduler.Resume(handles[0]);

        for (var i = 0; i < 12; i++) scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
        Assert.That(executionCount, Is.EqualTo(2));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines的Update方法不影响状态
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Update_Should_Not_Affect_State()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var coroutine = CreateDelayedCoroutine(() => { }, 1.0);

        var handles = new List<CoroutineHandle> { scheduler.Run(coroutine) };
        var wait = new WaitForAllCoroutines(scheduler, handles);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理无效句柄
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Invalid_Handles()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var handles = new List<CoroutineHandle> { default };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理混合的有效和无效句柄
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Mixed_Valid_And_Invalid_Handles()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var coroutine = CreateSimpleCoroutine();

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine),
            default
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理大量协程
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Many_Coroutines()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var executionCount = 0;

        var handles = new List<CoroutineHandle>();
        for (var i = 0; i < 20; i++) handles.Add(scheduler.Run(CreateDelayedCoroutine(() => executionCount++, 1.0)));

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.False);

        for (var i = 0; i < 120; i++) scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
        Assert.That(executionCount, Is.EqualTo(20));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理抛出异常的协程
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Coroutines_With_Exceptions()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateExceptionCoroutine();
        var coroutine3 = CreateSimpleCoroutine();

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2),
            scheduler.Run(coroutine3)
        };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该与ParallelCoroutines扩展方法一起工作
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Work_With_ParallelCoroutines()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var executionOrder = new List<int>();
        var coroutine1 = CreateDelayedCoroutine(() => executionOrder.Add(1), 0.5);
        var coroutine2 = CreateDelayedCoroutine(() => executionOrder.Add(2), 0.5);
        var coroutine3 = CreateDelayedCoroutine(() => executionOrder.Add(3), 0.5);

        var parallel = scheduler.ParallelCoroutines(coroutine1, coroutine2, coroutine3);

        parallel.MoveNext();

        while (scheduler.ActiveCoroutineCount > 0)
        {
            parallel.MoveNext();
            scheduler.Update();
        }

        Assert.That(executionOrder.Count, Is.EqualTo(3));
        Assert.That(executionOrder, Does.Contain(1));
        Assert.That(executionOrder, Does.Contain(2));
        Assert.That(executionOrder, Does.Contain(3));
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Implement_IYieldInstruction()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var handles = Array.Empty<CoroutineHandle>();

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该在所有协程立即完成时立即完成
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Be_Done_Immediately_When_All_Coroutines_Complete_Immediately()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);

        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateSimpleCoroutine();

        var handles = new List<CoroutineHandle>
        {
            scheduler.Run(coroutine1),
            scheduler.Run(coroutine2)
        };

        scheduler.Update();
        scheduler.Update();

        var wait = new WaitForAllCoroutines(scheduler, handles);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForAllCoroutines应该处理重复的句柄
    /// </summary>
    [Test]
    public void WaitForAllCoroutines_Should_Handle_Duplicate_Handles()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource);
        var coroutine = CreateDelayedCoroutine(() => { }, 1.0);

        var handle = scheduler.Run(coroutine);
        var handles = new List<CoroutineHandle> { handle, handle };

        var wait = new WaitForAllCoroutines(scheduler, handles);

        for (var i = 0; i < 12; i++) scheduler.Update();

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     创建简单的立即完成协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateSimpleCoroutine()
    {
        yield break;
    }

    /// <summary>
    ///     创建延迟协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateDelayedCoroutine(Action callback, double delay)
    {
        yield return new Delay(delay);
        callback();
    }

    /// <summary>
    ///     创建抛出异常的协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateExceptionCoroutine()
    {
        yield return new WaitOneFrame();
        throw new InvalidOperationException("Test exception");
    }

    /// <summary>
    ///     测试用时间源类
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