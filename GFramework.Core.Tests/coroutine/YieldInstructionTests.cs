using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// 等待指令的单元测试类
/// 测试内容包括：
/// - Delay指令
/// - WaitOneFrame指令
/// - WaitForFrames指令
/// - WaitUntil指令
/// - WaitWhile指令
/// - WaitForCoroutine指令
/// </summary>
[TestFixture]
public class YieldInstructionTests
{
    /// <summary>
    /// 验证Delay指令初始状态为未完成
    /// </summary>
    [Test]
    public void Delay_Should_Not_Be_Done_Initially()
    {
        var delay = new Delay(1.0);

        Assert.That(delay.IsDone, Is.False);
    }

    /// <summary>
    /// 验证Delay指令应该在指定时间后完成
    /// </summary>
    [Test]
    public void Delay_Should_Be_Done_After_Specified_Time()
    {
        var delay = new Delay(1.0);

        delay.Update(0.5);
        Assert.That(delay.IsDone, Is.False);

        delay.Update(0.5);
        Assert.That(delay.IsDone, Is.True);
    }

    /// <summary>
    /// 验证Delay指令可以处理零秒延迟
    /// </summary>
    [Test]
    public void Delay_Should_Handle_Zero_Seconds()
    {
        var delay = new Delay(0);

        Assert.That(delay.IsDone, Is.True);
    }

    /// <summary>
    /// 验证Delay指令可以处理负数秒数
    /// </summary>
    [Test]
    public void Delay_Should_Handle_Negative_Seconds()
    {
        var delay = new Delay(-1.0);

        Assert.That(delay.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitOneFrame指令初始状态为未完成
    /// </summary>
    [Test]
    public void WaitOneFrame_Should_Not_Be_Done_Initially()
    {
        var wait = new WaitOneFrame();

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitOneFrame指令应该在第一次Update后完成
    /// </summary>
    [Test]
    public void WaitOneFrame_Should_Be_Done_After_First_Update()
    {
        var wait = new WaitOneFrame();

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForFrames指令初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Not_Be_Done_Initially()
    {
        var wait = new WaitForFrames(3);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitForFrames指令应该在指定帧数后完成
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Be_Done_After_Specified_Frames()
    {
        var wait = new WaitForFrames(3);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForFrames指令可以处理最小帧数1
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Handle_Minimum_Frames_Of_1()
    {
        var wait = new WaitForFrames(1);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForFrames指令可以处理0帧数（会被修正为1）
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Handle_Zero_Frames_As_1()
    {
        var wait = new WaitForFrames(0);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForFrames指令可以处理负数帧数（会被修正为1）
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Handle_Negative_Frames_As_1()
    {
        var wait = new WaitForFrames(-5);

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitForFrames指令每次Update减少剩余帧数
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Decrement_On_Each_Update()
    {
        var wait = new WaitForFrames(5);

        for (var i = 5; i > 0; i--)
        {
            Assert.That(wait.IsDone, Is.False, $"Should not be done at frame {5 - i + 1}");
            wait.Update(0.1);
        }

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitUntil指令使用谓词函数
    /// </summary>
    [Test]
    public void WaitUntil_Should_Use_Predicate_Function()
    {
        var conditionMet = false;
        var wait = new WaitUntil(() => conditionMet);

        Assert.That(wait.IsDone, Is.False);

        conditionMet = true;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitUntil指令应该在条件满足时完成
    /// </summary>
    [Test]
    public void WaitUntil_Should_Be_Done_When_Condition_Is_True()
    {
        var counter = 0;
        var wait = new WaitUntil(() => counter >= 3);

        Assert.That(wait.IsDone, Is.False);

        counter = 1;
        Assert.That(wait.IsDone, Is.False);

        counter = 2;
        Assert.That(wait.IsDone, Is.False);

        counter = 3;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitUntil指令抛出ArgumentNullException当predicate为null
    /// </summary>
    [Test]
    public void WaitUntil_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitUntil(null!));
    }

    /// <summary>
    /// 验证WaitWhile指令使用谓词函数
    /// </summary>
    [Test]
    public void WaitWhile_Should_Use_Predicate_Function()
    {
        var callCount = 0;
        var shouldContinue = true;

        var wait = new WaitWhile(() =>
        {
            callCount++;
            return shouldContinue;
        });

        // 访问 IsDone，会触发 predicate
        Assert.That(wait.IsDone, Is.False);
        Assert.That(callCount, Is.GreaterThan(0),
            "Predicate should be evaluated when checking IsDone");

        var previousCount = callCount;

        shouldContinue = false;

        // 再次访问 IsDone，应再次调用 predicate，并改变结果
        Assert.That(wait.IsDone, Is.True);
        Assert.That(callCount, Is.GreaterThan(previousCount),
            "Predicate should be re-evaluated when condition changes");
    }

    /// <summary>
    /// 验证WaitWhile指令应该在条件为假时完成
    /// </summary>
    [Test]
    public void WaitWhile_Should_Be_Done_When_Condition_Is_False()
    {
        var continueWaiting = true;
        var wait = new WaitWhile(() => continueWaiting);

        Assert.That(wait.IsDone, Is.False);

        continueWaiting = false;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitWhile指令应该在条件为真时持续等待
    /// </summary>
    [Test]
    public void WaitWhile_Should_Continue_Waiting_While_Condition_Is_True()
    {
        var continueWaiting = true;
        var wait = new WaitWhile(() => continueWaiting);

        Assert.That(wait.IsDone, Is.False);

        for (var i = 0; i < 10; i++)
        {
            Assert.That(wait.IsDone, Is.False);
        }

        continueWaiting = false;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitWhile指令抛出ArgumentNullException当predicate为null
    /// </summary>
    [Test]
    public void WaitWhile_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitWhile(null!));
    }

    /// <summary>
    /// 验证WaitForCoroutine指令初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForCoroutine_Should_Not_Be_Done_Initially()
    {
        var wait = new WaitForCoroutine();

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证WaitForCoroutine指令的Update方法不影响状态
    /// </summary>
    [Test]
    public void WaitForCoroutine_Update_Should_Not_Affect_State()
    {
        var wait = new WaitForCoroutine();

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    /// 验证Delay指令实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void Delay_Should_Implement_IYieldInstruction_Interface()
    {
        var delay = new Delay(1.0);

        Assert.That(delay, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitOneFrame指令实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitOneFrame_Should_Implement_IYieldInstruction_Interface()
    {
        var wait = new WaitOneFrame();

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitForFrames指令实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForFrames_Should_Implement_IYieldInstruction_Interface()
    {
        var wait = new WaitForFrames(3);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitUntil指令实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitUntil_Should_Implement_IYieldInstruction_Interface()
    {
        var wait = new WaitUntil(() => true);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitWhile指令实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitWhile_Should_Implement_IYieldInstruction_Interface()
    {
        var wait = new WaitWhile(() => false);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitForCoroutine指令实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForCoroutine_Should_Implement_IYieldInstruction_Interface()
    {
        var wait = new WaitForCoroutine();

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }

    /// <summary>
    /// 验证WaitUntil指令在Update后立即检查条件
    /// </summary>
    [Test]
    public void WaitUntil_Should_Evaluate_Condition_Immediately()
    {
        var counter = 0;
        var wait = new WaitUntil(() => counter >= 1);

        Assert.That(wait.IsDone, Is.False);

        counter = 1;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 验证WaitWhile指令在Update后立即检查条件
    /// </summary>
    [Test]
    public void WaitWhile_Should_Evaluate_Condition_Immediately()
    {
        var callCount = 0;
        var continueWaiting = true;
        var wait = new WaitWhile(() =>
        {
            callCount++;
            return continueWaiting;
        });

        // 初始检查，确保谓词被调用
        Assert.That(wait.IsDone, Is.False);
        Assert.That(callCount, Is.EqualTo(1), "Predicate should be called once initially");

        // 改变条件并验证谓词再次被调用
        continueWaiting = false;
        Assert.That(wait.IsDone, Is.True);
        Assert.That(callCount, Is.EqualTo(2), "Predicate should be called again after condition change");
    }
}