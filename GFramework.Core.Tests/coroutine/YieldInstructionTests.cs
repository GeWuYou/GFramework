using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
/// Yield指令测试类，验证各种Yield指令的等待和状态管理功能
/// </summary>
[TestFixture]
public class YieldInstructionTests
{
    // ==================== WaitForSeconds 测试 ====================

    /// <summary>
    /// 测试基础等待功能 - 指定秒数后 IsDone = true
    /// </summary>
    [Test]
    public void WaitForSeconds_Should_CompleteAfterSpecifiedTime()
    {
        var wait = new WaitForSeconds(0.2f);

        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试IsDone 属性 - 等待前为 false，等待后为 true
    /// </summary>
    [Test]
    public void WaitForSeconds_IsDone_Should_BeCorrect()
    {
        var wait = new WaitForSeconds(0.1f);

        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.05f);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.05f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试Update(deltaTime) 方法 - 时间累加正确
    /// </summary>
    [Test]
    public void WaitForSeconds_Update_Should_AccumulateTimeCorrectly()
    {
        var wait = new WaitForSeconds(1.0f);

        for (int i = 0; i < 10; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试精确时间计算 - 多次 Update 累加到阈值
    /// </summary>
    [Test]
    public void WaitForSeconds_Should_CalculateTimeAccurately()
    {
        var wait = new WaitForSeconds(0.333f);

        wait.Update(0.111f);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.111f);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.111f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试Reset() 方法 - 重置状态可复用
    /// </summary>
    [Test]
    public void WaitForSeconds_Reset_Should_ReuseInstance()
    {
        var wait = new WaitForSeconds(0.1f);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);

        wait.Reset();
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.05f);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.05f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试累积误差测试 - Reset 后重新计数
    /// </summary>
    [Test]
    public void WaitForSeconds_Reset_Should_ClearAccumulatedError()
    {
        var wait = new WaitForSeconds(0.2f);

        wait.Update(0.15f);
        Assert.That(wait.IsDone, Is.False);

        wait.Reset();
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试零秒等待 - seconds = 0 立即完成
    /// </summary>
    [Test]
    public void WaitForSeconds_WithZeroSeconds_Should_CompleteImmediately()
    {
        var wait = new WaitForSeconds(0f);

        Assert.That(wait.IsDone, Is.False);

        wait.Update(0f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试负秒数处理 - seconds < 0 行为
    /// </summary>
    [Test]
    public void WaitForSeconds_WithNegativeSeconds_Should_CompleteImmediately()
    {
        var wait = new WaitForSeconds(-0.1f);

        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试大数值等待 - 长时间等待场景
    /// </summary>
    [Test]
    public void WaitForSeconds_WithLargeValue_Should_WaitLongTime()
    {
        var wait = new WaitForSeconds(1000f);

        for (int i = 0; i < 999; i++)
        {
            wait.Update(1f);
        }

        Assert.That(wait.IsDone, Is.False);

        wait.Update(1f);
        Assert.That(wait.IsDone, Is.True);
    }

    // ==================== WaitUntil 测试 ====================

    /// <summary>
    /// 测试条件为真时完成 - predicate 返回 true 时 IsDone = true
    /// </summary>
    [Test]
    public void WaitUntil_Should_CompleteWhenPredicateIsTrue()
    {
        var conditionMet = false;
        var wait = new WaitUntil(() => conditionMet);

        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        conditionMet = true;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试条件为假时等待 - predicate 返回 false 时继续等待
    /// </summary>
    [Test]
    public void WaitUntil_Should_ContinueWaitingWhenPredicateIsFalse()
    {
        var conditionMet = false;
        var wait = new WaitUntil(() => conditionMet);

        for (int i = 0; i < 100; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(wait.IsDone, Is.False);

        conditionMet = true;
        wait.Update(0.1f);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试Update(deltaTime) 方法 - 每次更新检查条件
    /// </summary>
    [Test]
    public void WaitUntil_Update_Should_CheckPredicateEachTime()
    {
        var checkCount = 0;
        var wait = new WaitUntil(() =>
        {
            checkCount++;
            return checkCount >= 5;
        });

        for (int i = 0; i < 4; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(checkCount, Is.EqualTo(4));
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);

        Assert.That(checkCount, Is.EqualTo(5));
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试Reset() 方法 - 重置状态可复用
    /// </summary>
    [Test]
    public void WaitUntil_Reset_Should_AllowReuse()
    {
        var conditionMet = false;
        var wait = new WaitUntil(() => conditionMet);

        conditionMet = true;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);

        wait.Reset();
        Assert.That(wait.IsDone, Is.False);

        conditionMet = false;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        conditionMet = true;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试谓词参数传递 - predicate 正确调用
    /// </summary>
    [Test]
    public void WaitUntil_Should_PassParametersToPredicate()
    {
        var counter = 0;
        var wait = new WaitUntil(() => counter++ >= 3);

        Assert.That(wait.IsDone, Is.False);

        for (int i = 0; i < 3; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(counter, Is.EqualTo(3));
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(counter, Is.EqualTo(4));
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试谓词闭包支持 - 捕获外部变量
    /// </summary>
    [Test]
    public void WaitUntil_Should_SupportClosureCapture()
    {
        var externalValue = 0;
        var wait = new WaitUntil(() => externalValue > 10);

        externalValue = 5;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        externalValue = 15;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试谓词异常处理 - predicate 抛出异常时的行为
    /// </summary>
    [Test]
    public void WaitUntil_Should_HandleExceptionInPredicate()
    {
        var shouldThrow = false;
        var wait = new WaitUntil(() =>
        {
            if (shouldThrow)
                throw new InvalidOperationException("Test exception");
            return false;
        });

        Assert.DoesNotThrow(() => wait.Update(0.1f));

        shouldThrow = true;
        Assert.Throws<InvalidOperationException>(() => wait.Update(0.1f));
    }

    // ==================== WaitWhile 测试 ====================

    /// <summary>
    /// 测试条件为假时完成 - predicate 返回 false 时 IsDone = true
    /// </summary>
    [Test]
    public void WaitWhile_Should_CompleteWhenPredicateIsFalse()
    {
        var shouldWait = true;
        var wait = new WaitWhile(() => shouldWait);

        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        shouldWait = false;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试条件为真时等待 - predicate 返回 true 时继续等待
    /// </summary>
    [Test]
    public void WaitWhile_Should_ContinueWaitingWhenPredicateIsTrue()
    {
        var shouldWait = true;
        var wait = new WaitWhile(() => shouldWait);

        for (int i = 0; i < 100; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(wait.IsDone, Is.False);

        shouldWait = false;
        wait.Update(0.1f);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试Update(deltaTime) 方法 - 每次更新检查条件
    /// </summary>
    [Test]
    public void WaitWhile_Update_Should_CheckPredicateEachTime()
    {
        var checkCount = 0;
        var wait = new WaitWhile(() => checkCount++ < 5);

        for (int i = 0; i < 4; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(checkCount, Is.EqualTo(4));
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);

        Assert.That(checkCount, Is.EqualTo(5));
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);

        Assert.That(checkCount, Is.EqualTo(6));
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试Reset() 方法 - 重置状态可复用
    /// </summary>
    [Test]
    public void WaitWhile_Reset_Should_AllowReuse()
    {
        var shouldWait = false;
        var wait = new WaitWhile(() => shouldWait);

        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);

        wait.Reset();
        Assert.That(wait.IsDone, Is.False);

        shouldWait = true;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.False);

        shouldWait = false;
        wait.Update(0.1f);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试谓词参数传递 - predicate 正确调用
    /// </summary>
    [Test]
    public void WaitWhile_Should_PassParametersToPredicate()
    {
        var counter = 0;
        var wait = new WaitWhile(() => counter++ < 3);

        Assert.That(wait.IsDone, Is.False);

        for (int i = 0; i < 3; i++)
        {
            wait.Update(0.1f);
        }

        Assert.That(counter, Is.EqualTo(3));
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.1f);
        Assert.That(counter, Is.EqualTo(4));
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    /// 测试与 WaitUntil 对比 - 逻辑相反性验证
    /// </summary>
    [Test]
    public void WaitWhile_Should_BeOppositeOfWaitUntil()
    {
        var condition = true;

        var waitUntil = new WaitUntil(() => !condition);
        var waitWhile = new WaitWhile(() => condition);

        waitUntil.Update(0.1f);
        waitWhile.Update(0.1f);

        Assert.That(waitUntil.IsDone, Is.False);
        Assert.That(waitWhile.IsDone, Is.False);

        condition = false;

        waitUntil.Update(0.1f);
        waitWhile.Update(0.1f);

        Assert.That(waitUntil.IsDone, Is.True);
        Assert.That(waitWhile.IsDone, Is.True);
    }
}
