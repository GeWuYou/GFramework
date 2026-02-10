using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.coroutine.instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     WaitForSecondsScaled的单元测试类
/// </summary>
[TestFixture]
public class WaitForSecondsScaledTests
{
    /// <summary>
    ///     验证WaitForSecondsScaled初始状态根据时间设置
    /// </summary>
    [Test]
    public void WaitForSecondsScaled_Should_Handle_Initial_State_Correctly()
    {
        var waitZero = new WaitForSecondsScaled(0);
        var waitPositive = new WaitForSecondsScaled(1.0);

        Assert.That(waitZero.IsDone, Is.True);
        Assert.That(waitPositive.IsDone, Is.False);
    }

    /// <summary>
    ///     验证WaitForSecondsScaled应该在指定时间后完成
    /// </summary>
    [Test]
    public void WaitForSecondsScaled_Should_Be_Done_After_Specified_Time()
    {
        var wait = new WaitForSecondsScaled(1.0);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForSecondsScaled可以处理负数时间
    /// </summary>
    [Test]
    public void WaitForSecondsScaled_Should_Handle_Negative_Time()
    {
        var wait = new WaitForSecondsScaled(-1.0);

        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForSecondsScaled多次更新累积时间
    /// </summary>
    [Test]
    public void WaitForSecondsScaled_Should_Accumulate_Time_Over_Multiple_Updates()
    {
        var wait = new WaitForSecondsScaled(2.0);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(1.0);
        Assert.That(wait.IsDone, Is.False);

        wait.Update(0.5);
        Assert.That(wait.IsDone, Is.True);
    }

    /// <summary>
    ///     验证WaitForSecondsScaled实现IYieldInstruction接口
    /// </summary>
    [Test]
    public void WaitForSecondsScaled_Should_Implement_IYieldInstruction()
    {
        var wait = new WaitForSecondsScaled(1.0);

        Assert.That(wait, Is.InstanceOf<IYieldInstruction>());
    }
}