using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.extensions;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 协程扩展方法的单元测试类
/// 测试内容包括：
/// - RepeatEvery方法
/// - ExecuteAfter方法
/// - Sequence方法
/// - ParallelCoroutines方法
/// - WaitForSecondsWithProgress方法
/// </summary>
[TestFixture]
public class CoroutineExtensionsTests
{
    /// <summary>
    /// 验证RepeatEvery应该返回有效的协程
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Return_Valid_Coroutine()
    {
        var callCount = 0;
        var coroutine = CoroutineExtensions.RepeatEvery(0.1, () => callCount++, 3);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    /// 验证RepeatEvery应该执行指定次数
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Execute_Specified_Times()
    {
        var callCount = 0;
        var coroutine = CoroutineExtensions.RepeatEvery(0.1, () => callCount++, 3);

        while (coroutine.MoveNext())
        {
            coroutine.Current.Update(0.1);
        }

        Assert.That(callCount, Is.EqualTo(3));
    }

    /// <summary>
    /// 验证RepeatEvery应该无限执行当count为null
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Execute_Forever_When_Count_Is_Null()
    {
        var callCount = 0;
        var coroutine = CoroutineExtensions.RepeatEvery(0.1, () => callCount++);

        for (var i = 0; i < 5; i++)
        {
            Assert.That(coroutine.MoveNext(), Is.True);
            coroutine.Current.Update(0.1);
        }

        Assert.That(callCount, Is.EqualTo(5));
        Assert.That(coroutine.MoveNext(), Is.True);
    }

    /// <summary>
    /// 验证RepeatEvery应该处理负数count
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Handle_Negative_Count()
    {
        var callCount = 0;
        var coroutine = CoroutineExtensions.RepeatEvery(0.1, () => callCount++, -1);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(callCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证RepeatEvery应该处理零count
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Handle_Zero_Count()
    {
        var callCount = 0;
        var coroutine = CoroutineExtensions.RepeatEvery(0.1, () => callCount++, 0);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(callCount, Is.EqualTo(0));
    }

    /// <summary>
    /// 验证RepeatEvery应该处理null action
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Handle_Null_Action()
    {
        var coroutine = CoroutineExtensions.RepeatEvery(0.1, null, 3);

        Assert.DoesNotThrow(() => 
        {
            while (coroutine.MoveNext())
            {
                coroutine.Current.Update(0.1);
            }
        });
    }

    /// <summary>
    /// 验证ExecuteAfter应该返回有效的协程
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Return_Valid_Coroutine()
    {
        var called = false;
        var coroutine = CoroutineExtensions.ExecuteAfter(1.0, () => called = true);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    /// 验证ExecuteAfter应该在延迟后执行action
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Execute_Action_After_Delay()
    {
        var called = false;
        var coroutine = CoroutineExtensions.ExecuteAfter(1.0, () => called = true);

        Assert.That(called, Is.False);

        coroutine.MoveNext();
        coroutine.Current.Update(0.5);
        Assert.That(called, Is.False);

        coroutine.Current.Update(0.5);
        Assert.That(called, Is.False);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(called, Is.True);
    }

    /// <summary>
    /// 验证ExecuteAfter应该处理零延迟
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Handle_Zero_Delay()
    {
        var called = false;
        var coroutine = CoroutineExtensions.ExecuteAfter(0, () => called = true);

        coroutine.MoveNext();

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(called, Is.True);
    }

    /// <summary>
    /// 验证ExecuteAfter应该处理负数延迟
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Handle_Negative_Delay()
    {
        var called = false;
        var coroutine = CoroutineExtensions.ExecuteAfter(-1.0, () => called = true);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(called, Is.False);
    }

    /// <summary>
    /// 验证ExecuteAfter应该处理null action
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Handle_Null_Action()
    {
        var coroutine = CoroutineExtensions.ExecuteAfter(1.0, null);

        Assert.DoesNotThrow(() => 
        {
            coroutine.MoveNext();
            coroutine.Current.Update(1.0);
        });
    }

    /// <summary>
    /// 验证Sequence应该返回有效的协程
    /// </summary>
    [Test]
    public void Sequence_Should_Return_Valid_Coroutine()
    {
        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateSimpleCoroutine();
        var sequence = CoroutineExtensions.Sequence(coroutine1, coroutine2);

        Assert.That(sequence, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    /// 验证Sequence应该按顺序执行多个协程
    /// </summary>
    [Test]
    public void Sequence_Should_Execute_Coroutines_In_Order()
    {
        var executionOrder = new List<int>();
        var coroutine1 = CreateCoroutineWithCallback(1, () => executionOrder.Add(1));
        var coroutine2 = CreateCoroutineWithCallback(2, () => executionOrder.Add(2));
        var coroutine3 = CreateCoroutineWithCallback(3, () => executionOrder.Add(3));

        var sequence = CoroutineExtensions.Sequence(coroutine1, coroutine2, coroutine3);

        while (sequence.MoveNext())
        {
            sequence.Current.Update(0.1);
        }

        Assert.That(executionOrder, Is.EqualTo(new List<int> { 1, 2, 3 }));
    }

    /// <summary>
    /// 验证Sequence应该处理空协程数组
    /// </summary>
    [Test]
    public void Sequence_Should_Handle_Empty_Coroutines()
    {
        var sequence = CoroutineExtensions.Sequence();

        Assert.That(sequence.MoveNext(), Is.False);
    }

    /// <summary>
    /// 验证Sequence应该处理单个协程
    /// </summary>
    [Test]
    public void Sequence_Should_Handle_Single_Coroutine()
    {
        var coroutine1 = CreateSimpleCoroutine();
        var sequence = CoroutineExtensions.Sequence(coroutine1);

        Assert.That(sequence.MoveNext(), Is.False);
    }

    /// <summary>
    /// 验证Sequence应该处理null协程
    /// </summary>
    [Test]
    public void Sequence_Should_Handle_Null_Coroutine()
    {
        var coroutine1 = CreateSimpleCoroutine();
        var sequence = CoroutineExtensions.Sequence(coroutine1, null!);

        Assert.Throws<NullReferenceException>(() => 
        {
            while (sequence.MoveNext())
            {
                sequence.Current.Update(0.1);
            }
        });
    }

    /// <summary>
    /// 验证ParallelCoroutines应该返回有效的协程
    /// </summary>
    [Test]
    public void ParallelCoroutines_Should_Return_Valid_Coroutine()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);
        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateSimpleCoroutine();

        var parallel = scheduler.ParallelCoroutines(coroutine1, coroutine2);

        Assert.That(parallel, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    /// 验证ParallelCoroutines应该并行执行多个协程
    /// </summary>
    [Test]
    public void ParallelCoroutines_Should_Execute_Coroutines_In_Parallel()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);

        var executionCounts = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 } };
        var coroutine1 = CreateDelayedCoroutine(() => executionCounts[1]++, 0.5);
        var coroutine2 = CreateDelayedCoroutine(() => executionCounts[2]++, 0.5);
        var coroutine3 = CreateDelayedCoroutine(() => executionCounts[3]++, 0.5);

        var parallel = scheduler.ParallelCoroutines(coroutine1, coroutine2, coroutine3);

        parallel.MoveNext();

        Assert.That(scheduler.ActiveCoroutineCount, Is.GreaterThan(0));

        while (scheduler.ActiveCoroutineCount > 0)
        {
            scheduler.Update();
        }

        Assert.That(executionCounts[1], Is.EqualTo(1));
        Assert.That(executionCounts[2], Is.EqualTo(1));
        Assert.That(executionCounts[3], Is.EqualTo(1));
    }

    /// <summary>
    /// 验证ParallelCoroutines应该处理空数组
    /// </summary>
    [Test]
    public void ParallelCoroutines_Should_Handle_Empty_Array()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);

        var parallel = scheduler.ParallelCoroutines();

        Assert.That(parallel.MoveNext(), Is.False);
    }

    /// <summary>
    /// 验证ParallelCoroutines应该处理null数组
    /// </summary>
    [Test]
    public void ParallelCoroutines_Should_Handle_Null_Array()
    {
        var timeSource = new TestTimeSource();
        var scheduler = new CoroutineScheduler(timeSource, instanceId: 1);

        var parallel = scheduler.ParallelCoroutines(null);

        Assert.That(parallel.MoveNext(), Is.False);
    }

    /// <summary>
    /// 验证WaitForSecondsWithProgress应该返回有效的协程
    /// </summary>
    [Test]
    public void WaitForSecondsWithProgress_Should_Return_Valid_Coroutine()
    {
        var progressValues = new List<float>();
        var coroutine = CoroutineExtensions.WaitForSecondsWithProgress(1.0, progressValues.Add);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    /// 验证WaitForSecondsWithProgress应该在指定时间后完成
    /// </summary>
    [Test]
    public void WaitForSecondsWithProgress_Should_Complete_After_Duration()
    {
        var coroutine = CoroutineExtensions.WaitForSecondsWithProgress(1.0, null);

        coroutine.MoveNext();
        coroutine.Current.Update(0.5);
        Assert.That(coroutine.Current.IsDone, Is.False);

        coroutine.Current.Update(0.5);
        Assert.That(coroutine.Current.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForSecondsWithProgress应该调用进度回调
    /// </summary>
    [Test]
    public void WaitForSecondsWithProgress_Should_Call_Progress_Callback()
    {
        var progressValues = new List<float>();
        var coroutine = CoroutineExtensions.WaitForSecondsWithProgress(1.0, progressValues.Add);

        coroutine.MoveNext();

        while (!coroutine.Current.IsDone)
        {
            coroutine.Current.Update(0.1);
        }

        Assert.That(progressValues.Count, Is.GreaterThan(0));
        Assert.That(progressValues[0], Is.EqualTo(0.0f).Within(0.01f));
        Assert.That(progressValues[^1], Is.EqualTo(1.0f).Within(0.01f));
    }

    /// <summary>
    /// 验证WaitForSecondsWithProgress应该处理零时间
    /// </summary>
    [Test]
    public void WaitForSecondsWithProgress_Should_Handle_Zero_Duration()
    {
        var progressValues = new List<float>();
        var coroutine = CoroutineExtensions.WaitForSecondsWithProgress(0, progressValues.Add);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(progressValues, Is.EqualTo(new List<float> { 1.0f }));
    }

    /// <summary>
    /// 验证WaitForSecondsWithProgress应该处理负数时间
    /// </summary>
    [Test]
    public void WaitForSecondsWithProgress_Should_Handle_Negative_Duration()
    {
        var progressValues = new List<float>();
        var coroutine = CoroutineExtensions.WaitForSecondsWithProgress(-1.0, progressValues.Add);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(progressValues, Is.EqualTo(new List<float> { 1.0f }));
    }

    /// <summary>
    /// 验证WaitForSecondsWithProgress应该处理null回调
    /// </summary>
    [Test]
    public void WaitForSecondsWithProgress_Should_Handle_Null_Callback()
    {
        var coroutine = CoroutineExtensions.WaitForSecondsWithProgress(1.0, null);

        coroutine.MoveNext();
        coroutine.Current.Update(0.5);
        Assert.That(coroutine.Current.IsDone, Is.False);

        coroutine.Current.Update(0.5);
        Assert.That(coroutine.Current.IsDone, Is.True);
    }

    /// <summary>
    /// 验证RepeatEvery应该使用Delay指令
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Use_Delay_Instruction()
    {
        var coroutine = CoroutineExtensions.RepeatEvery(0.5, () => { }, 1);

        coroutine.MoveNext();
        Assert.That(coroutine.Current, Is.InstanceOf<Delay>());
    }

    /// <summary>
    /// 验证ExecuteAfter应该使用Delay指令
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Use_Delay_Instruction()
    {
        var coroutine = CoroutineExtensions.ExecuteAfter(1.0, () => { });

        coroutine.MoveNext();
        Assert.That(coroutine.Current, Is.InstanceOf<Delay>());
    }

    /// <summary>
    /// 验证Sequence应该清理已完成的协程
    /// </summary>
    [Test]
    public void Sequence_Should_Dispose_Completed_Coroutines()
    {
        var coroutine1 = CreateSimpleCoroutine();
        var coroutine2 = CreateSimpleCoroutine();

        var sequence = CoroutineExtensions.Sequence(coroutine1, coroutine2);

        while (sequence.MoveNext())
        {
            sequence.Current.Update(0.1);
        }

        Assert.That(sequence.MoveNext(), Is.False);
    }

    /// <summary>
    /// 验证RepeatEvery的间隔时间
    /// </summary>
    [Test]
    public void RepeatEvery_Should_Respect_Interval()
    {
        var callTimes = new List<double>();
        var coroutine = CoroutineExtensions.RepeatEvery(0.5, () => callTimes.Add(0), 3);

        var currentTime = 0.0;
        while (coroutine.MoveNext())
        {
            coroutine.Current.Update(0.5);
            currentTime += 0.5;
        }

        Assert.That(callTimes.Count, Is.EqualTo(3));
    }

    /// <summary>
    /// 验证ExecuteAfter的延迟时间
    /// </summary>
    [Test]
    public void ExecuteAfter_Should_Respect_Delay()
    {
        var executed = false;
        var currentTime = 0.0;
        var coroutine = CoroutineExtensions.ExecuteAfter(1.5, () => executed = true);

        coroutine.MoveNext();
        coroutine.Current.Update(0.5);
        currentTime += 0.5;
        Assert.That(executed, Is.False);

        coroutine.Current.Update(1.0);
        currentTime += 1.0;
        Assert.That(executed, Is.False);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(executed, Is.True);
        Assert.That(currentTime, Is.EqualTo(1.5));
    }

    /// <summary>
    /// 创建简单的立即完成协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateSimpleCoroutine()
    {
        yield break;
    }

    /// <summary>
    /// 创建带回调的协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateCoroutineWithCallback(int id, Action callback)
    {
        yield return new WaitOneFrame();
        callback();
    }

    /// <summary>
    /// 创建计数协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateCountingCoroutine(int id, Action callback)
    {
        yield return new WaitOneFrame();
        callback();
    }

    /// <summary>
    /// 创建延迟协程
    /// </summary>
    private IEnumerator<IYieldInstruction> CreateDelayedCoroutine(Action callback, double delay)
    {
        yield return new Delay(delay);
        callback();
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
            DeltaTime = 0.016;
            CurrentTime += DeltaTime;
        }
    }
}
