using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForPredicate的单元测试类
/// </summary>
[TestFixture]
public class WaitForPredicateTests
{
    /// <summary>
    ///     验证WaitForPredicate默认等待条件为真时完成
    /// </summary>
    [Test]
    public void WaitForPredicate_Should_Wait_For_True_By_Default()
    {
        var condition = false;
        var wait = new WaitForPredicate(() => condition);

        Assert.That(wait.IsDone, Is.False);

        condition = true;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForPredicate可以等待条件为假时完成
    /// </summary>
    [Test]
    public void WaitForPredicate_Should_Wait_For_False_When_Specified()
    {
        var condition = true;
        var wait = new WaitForPredicate(() => condition, false);

        Assert.That(wait.IsDone, Is.False);

        condition = false;
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForPredicate多次检查条件
    /// </summary>
    [Test]
    public void WaitForPredicate_Should_Check_Condition_Multiple_Times()
    {
        var callCount = 0;
        var wait = new WaitForPredicate(() =>
        {
            callCount++;
            return callCount >= 3;
        });

        Assert.That(wait.IsDone, Is.False);
        Assert.That(callCount, Is.EqualTo(1));

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);
        Assert.That(callCount, Is.EqualTo(2));

        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);
        Assert.That(callCount, Is.EqualTo(3));
    }

    /// <summary>
    ///     验证WaitForPredicate抛出ArgumentNullException当predicate为null
    /// </summary>
    [Test]
    public void WaitForPredicate_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForPredicate(null!));
    }

    /// <summary>
    ///     验证WaitForPredicate实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForPredicate_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForPredicate(() => true);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}