using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     协程辅助方法的单元测试类
///     测试内容包括：
///     - WaitForSeconds方法
///     - WaitForOneFrame方法
///     - WaitForFrames方法
///     - WaitUntil方法
///     - WaitWhile方法
///     - DelayedCall方法
///     - RepeatCall方法
///     - RepeatCallForever方法
/// </summary>
[TestFixture]
public class CoroutineHelperTests
{
    /// <summary>
    ///     验证WaitForSeconds应该返回Delay实例
    /// </summary>
    [Test]
    public void WaitForSeconds_Should_Return_Delay_Instance()
    {
        var delay = CoroutineHelper.WaitForSeconds(1.5);

        Assert.That(delay, Is.InstanceOf<Delay>());
    }

    /// <summary>
    ///     验证WaitForSeconds可以处理正数秒数
    /// </summary>
    [Test]
    public void WaitForSeconds_Should_Handle_Positive_Seconds()
    {
        var delay = CoroutineHelper.WaitForSeconds(2.0);

        Assert.That(delay, Is.Not.Null);
        Assert.That(delay.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForSeconds可以处理零秒数
    /// </summary>
    [Test]
    public void WaitForSeconds_Should_Handle_Zero_Seconds()
    {
        var delay = CoroutineHelper.WaitForSeconds(0);

        Assert.That(delay, Is.Not.Null);
    }

    /// <summary>
    ///     验证WaitForOneFrame应该返回WaitOneFrame实例
    /// </summary>
    [Test]
    public void WaitForOneFrame_Should_Return_WaitOneFrame_Instance()
    {
        var wait = CoroutineHelper.WaitForOneFrame();

        Assert.That(wait, Is.InstanceOf<WaitOneFrame>());
    }

    /// <summary>
    ///     验证WaitForFrames应该返回WaitForFrames实例
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Return_WaitForFrames_Instance()
    {
        var wait = CoroutineHelper.WaitForFrames(5);

        Assert.That(wait, Is.InstanceOf<WaitForFrames>());
    }

    /// <summary>
    ///     验证WaitForFrames可以处理正数帧数
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Handle_Positive_Frames()
    {
        var wait = CoroutineHelper.WaitForFrames(10);

        Assert.That(wait, Is.Not.Null);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForFrames可以处理最小帧数1
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Handle_Minimum_Frame_Count_Of_1()
    {
        var wait = CoroutineHelper.WaitForFrames(0);

        Assert.That(wait, Is.Not.Null);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitUntil应该返回WaitUntil实例
    /// </summary>
    [Test]
    public void WaitUntil_Should_Return_WaitUntil_Instance()
    {
        var conditionMet = false;
        var wait = CoroutineHelper.WaitUntil(() => conditionMet);

        Assert.That(wait, Is.InstanceOf<WaitUntil>());
    }

    /// <summary>
    ///     验证WaitUntil应该使用提供的谓词函数
    /// </summary>
    [Test]
    public void WaitUntil_Should_Use_Provided_Predicate()
    {
        var conditionMet = false;
        var wait = CoroutineHelper.WaitUntil(() => conditionMet);

        Assert.That(wait.IsDone, Is.False);

        conditionMet = true;

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitWhile应该返回WaitWhile实例
    /// </summary>
    [Test]
    public void WaitWhile_Should_Return_WaitWhile_Instance()
    {
        var continueWaiting = true;
        var wait = CoroutineHelper.WaitWhile(() => continueWaiting);

        Assert.That(wait, Is.InstanceOf<WaitWhile>());
    }

    /// <summary>
    ///     验证WaitWhile应该在条件为假时完成
    /// </summary>
    [Test]
    public void WaitWhile_Should_Complete_When_Condition_Is_False()
    {
        var continueWaiting = true;
        var wait = CoroutineHelper.WaitWhile(() => continueWaiting);

        Assert.That(wait.IsDone, Is.False);

        continueWaiting = false;

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证DelayedCall应该返回IEnumerator实例
    /// </summary>
    [Test]
    public void DelayedCall_Should_Return_IEnumerator()
    {
        var called = false;
        var coroutine = CoroutineHelper.DelayedCall(1.0, () => called = true);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证DelayedCall的action可以在延迟后执行
    /// </summary>
    [Test]
    public void DelayedCall_Should_Execute_Action_After_Delay()
    {
        var called = false;
        var coroutine = CoroutineHelper.DelayedCall(1.0, () => called = true);

        Assert.That(called, Is.False);

        coroutine.MoveNext();
        var yieldInstruction = coroutine.Current;
        yieldInstruction.Update(1.0);

        Assert.That(yieldInstruction.IsDone, Is.True);
    }

    /// <summary>
    ///     验证DelayedCall可以处理null action
    /// </summary>
    [Test]
    public void DelayedCall_Should_Handle_Null_Action()
    {
        var coroutine = CoroutineHelper.DelayedCall(1.0, null);

        Assert.That(coroutine, Is.Not.Null);
        Assert.DoesNotThrow(() => coroutine.MoveNext());
    }

    /// <summary>
    ///     验证RepeatCall应该返回IEnumerator实例
    /// </summary>
    [Test]
    public void RepeatCall_Should_Return_IEnumerator()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCall(0.1, 3, () => callCount++);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证RepeatCall应该执行指定次数
    /// </summary>
    [Test]
    public void RepeatCall_Should_Execute_Specified_Times()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCall(0.1, 3, () => callCount++);

        while (coroutine.MoveNext()) coroutine.Current.Update(0.1);

        Assert.That(callCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     验证RepeatCall可以处理0次调用
    /// </summary>
    [Test]
    public void RepeatCall_Should_Handle_Zero_Count()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCall(0.1, 0, () => callCount++);

        Assert.That(coroutine.MoveNext(), Is.False);
        Assert.That(callCount, Is.EqualTo(0));
    }

    /// <summary>
    ///     验证RepeatCallForever应该返回IEnumerator实例
    /// </summary>
    [Test]
    public void RepeatCallForever_Should_Return_IEnumerator()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCallForever(0.1, () => callCount++);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证RepeatCallForever应该无限执行
    /// </summary>
    [Test]
    public void RepeatCallForever_Should_Execute_Forever()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCallForever(0.1, () => callCount++);

        var iterations = 10;
        for (var i = 0; i < iterations; i++)
        {
            Assert.That(coroutine.MoveNext(), Is.True);
            coroutine.Current.Update(0.1);
        }

        Assert.That(callCount, Is.EqualTo(iterations));
        Assert.That(coroutine.MoveNext(), Is.True);
    }

    /// <summary>
    ///     验证RepeatCallForever可以处理null action
    /// </summary>
    [Test]
    public void RepeatCallForever_Should_Handle_Null_Action()
    {
        var coroutine = CoroutineHelper.RepeatCallForever(0.1, null);

        Assert.That(coroutine, Is.Not.Null);
        Assert.DoesNotThrow(() => coroutine.MoveNext());
    }

    /// <summary>
    ///     验证RepeatCallForever可以处理负数间隔
    /// </summary>
    [Test]
    public void RepeatCallForever_Should_Handle_Negative_Interval()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCallForever(-0.1, () => callCount++);

        Assert.That(coroutine, Is.Not.Null);
        Assert.DoesNotThrow(() => coroutine.MoveNext());
    }

    /// <summary>
    ///     验证DelayedCall可以处理负数延迟
    /// </summary>
    [Test]
    public void DelayedCall_Should_Handle_Negative_Delay()
    {
        var called = false;
        var coroutine = CoroutineHelper.DelayedCall(-1.0, () => called = true);

        Assert.That(coroutine, Is.Not.Null);
        Assert.DoesNotThrow(() => coroutine.MoveNext());
    }

    /// <summary>
    ///     验证RepeatCall可以处理负数间隔
    /// </summary>
    [Test]
    public void RepeatCall_Should_Handle_Negative_Interval()
    {
        var callCount = 0;
        var coroutine = CoroutineHelper.RepeatCall(-0.1, 3, () => callCount++);

        Assert.That(coroutine, Is.Not.Null);
        Assert.DoesNotThrow(() => coroutine.MoveNext());
    }

    /// <summary>
    ///     验证WaitUntil应该抛出ArgumentNullException当predicate为null
    /// </summary>
    [Test]
    public void WaitUntil_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => CoroutineHelper.WaitUntil(null!));
    }

    /// <summary>
    ///     验证WaitWhile应该抛出ArgumentNullException当predicate为null
    /// </summary>
    [Test]
    public void WaitWhile_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => CoroutineHelper.WaitWhile(null!));
    }
}