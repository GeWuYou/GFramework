using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitUntilOrTimeout的单元测试类
/// </summary>
[TestFixture]
public class WaitUntilOrTimeoutTests
{
    /// <summary>
    ///     验证WaitUntilOrTimeout初始状态为未完成
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Not_Be_Done_Initially()
    {
        var condition = false;
        var wait = new WaitUntilOrTimeout(() => condition, 5.0);

        Assert.That(wait.IsDone, Is.False);
        Assert.That(wait.ConditionMet, Is.False);
        Assert.That(wait.IsTimedOut, Is.False);
    }

    /// <summary>
    ///     验证WaitUntilOrTimeout应该在条件满足时完成
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Be_Done_When_Condition_Met()
    {
        var condition = false;
        var wait = new WaitUntilOrTimeout(() => condition, 5.0);

        condition = true;
        wait.Update(0.1);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.ConditionMet, Is.True);
        Assert.That(wait.IsTimedOut, Is.False);
    }

    /// <summary>
    ///     验证WaitUntilOrTimeout应该在超时时完成
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Be_Done_When_Timed_Out()
    {
        var condition = false;
        var wait = new WaitUntilOrTimeout(() => condition, 1.0);

        wait.Update(1.5);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.ConditionMet, Is.False);
        Assert.That(wait.IsTimedOut, Is.True);
    }

    /// <summary>
    ///     验证WaitUntilOrTimeout可以处理零超时时间
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Handle_Zero_Timeout()
    {
        var condition = false;
        var wait = new WaitUntilOrTimeout(() => condition, 0);

        wait.Update(0.1);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimedOut, Is.True);
    }

    /// <summary>
    ///     验证WaitUntilOrTimeout可以处理负数超时时间
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Handle_Negative_Timeout()
    {
        var condition = false;
        var wait = new WaitUntilOrTimeout(() => condition, -1.0);

        wait.Update(0.1);

        Assert.That(wait.IsDone, Is.True);
        Assert.That(wait.IsTimedOut, Is.True);
    }

    /// <summary>
    ///     验证WaitUntilOrTimeout抛出ArgumentNullException当predicate为null
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitUntilOrTimeout(null!, 1.0));
    }

    /// <summary>
    ///     验证WaitUntilOrTimeout实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitUntilOrTimeout_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitUntilOrTimeout(() => false, 1.0);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}