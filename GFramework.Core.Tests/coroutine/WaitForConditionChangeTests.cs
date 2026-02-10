using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForConditionChange的单元测试类
/// </summary>
[TestFixture]
public class WaitForConditionChangeTests
{
    /// <summary>
    ///     验证WaitForConditionChange初始状态为未完成
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Not_Be_Done_Initially()
    {
        var condition = false;
        var wait = new WaitForConditionChange(() => condition, true);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForConditionChange从false变为true时完成
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Be_Done_When_Changing_From_False_To_True()
    {
        var condition = false;
        var wait = new WaitForConditionChange(() => condition, true);

        // 初始状态记录
        wait.Update(0.1);

        condition = true;
        wait.Update(0.1);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForConditionChange从true变为false时完成
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Be_Done_When_Changing_From_True_To_False()
    {
        var condition = true;
        var wait = new WaitForConditionChange(() => condition, false);

        // 初始状态记录
        wait.Update(0.1);

        condition = false;
        wait.Update(0.1);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForConditionChange不响应相同状态的变化
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Not_Be_Done_When_No_State_Change()
    {
        var condition = false;
        var wait = new WaitForConditionChange(() => condition, true);

        // 初始状态记录
        wait.Update(0.1);

        // 仍然是false，没有状态改变
        wait.Update(0.1);

        Assert.That(wait.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForConditionChange多次状态切换只响应第一次
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Only_Respond_To_First_Transition()
    {
        var condition = false;
        var wait = new WaitForConditionChange(() => condition, true);

        // 记录初始状态
        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.False);

        // 触发状态转换到目标状态
        condition = true;
        wait.Update(0.1);
        Assert.That(wait.IsDone, Is.True);

        // 再次切换回原始状态
        condition = false;
        wait.Update(0.1);

        // 应该仍然保持完成状态
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForConditionChange抛出ArgumentNullException当conditionGetter为null
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Throw_ArgumentNullException_When_ConditionGetter_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new WaitForConditionChange(null!, true));
    }

    /// <summary>
    ///     验证WaitForConditionChange实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForConditionChange_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForConditionChange(() => false, true);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}